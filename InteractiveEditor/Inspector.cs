using System.Collections;
using System.Linq.Expressions;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.Model;
using InteractiveEditor.Options;
using InteractiveEditor.Options.Policies;

namespace InteractiveEditor;

// Holds the tree and the bound objects. It is not a node itself: the options live on the nodes,
// and the root stays inside.
public sealed class Inspector : IEnumerable<InspectorNode>, IDisposable
{
    private static int LastId;

    private readonly RootNode Root;
    private readonly InstanceWatcher Watcher;
    private bool Disposed;

    private Inspector(Type target)
    {
        Id = Interlocked.Increment(ref LastId);
        Root = new RootNode(target, this);
        Watcher = new InstanceWatcher(Root);
    }

    // Raised inside the Create, before anyone can subscribe to the new inspector, so they are
    // static; the inspector is the sender and comes in the args.
    public static event EventHandler<InspectorEventArgs>? DiscoveryFinished;
    public static event EventHandler<InspectorFailureEventArgs>? DiscoveryFailed;
    public static event EventHandler<InspectorCreatedEventArgs>? Created;

    // Tells inspectors apart, so an event can be traced back to the one that raised it.
    public int Id { get; }

    // Objects coming into the bind and leaving it; Unbound when the last one leaves.
    public event EventHandler<BindEventArgs>? BindRegistered;
    public event EventHandler<BindEventArgs>? BindRemoved;
    public event EventHandler<InspectorEventArgs>? Unbound;

    // Raised after each forced operation, an override or a fallback for when the normal flow (Apply(),
    // Reload() and Refresh()) failed or does not fit.
    public event EventHandler<InspectorEventArgs>? ForcedApply;
    public event EventHandler<InspectorEventArgs>? ForcedReload;
    public event EventHandler<InspectorEventArgs>? ForcedClear;

    // What went wrong during the Create, for whoever checks after it.
    public InspectorReport Report { get; } = new();

    // The options of this inspector, between the global ones and those of each node.
    public InspectorOptions Options { get; } = new();

    public string Name => Root.Name;

    // The first bound object, or null. A struct at the root is the inspector's own copy, read back
    // here.
    public object? Instance => Root.Instances.FirstOrDefault();

    // Every bound object, in the order they were bound.
    public IReadOnlyList<object> Instances => Root.Instances.AsReadOnly();

    // True when a node holds a value waiting for Apply().
    public bool HasPendingValues => Root.Any(node => node.HasPendingValue);

    // A failure inside the Create does not stop it: the failed piece falls back or is left out, and
    // it is reported. Only a fatal one (the type itself cannot be read) reaches the caller.
    public static Inspector Create<T>()
    {
        var inspector = new Inspector(typeof(T));

        // The global options are read below, so they are frozen from here until the Dispose.
        GlobalOptions.Lock(inspector.Id);

        try
        {
            ReflectionDiscovery.AddMembers(inspector.Root, typeof(T), inspector);
            OnDiscoveryFinished(inspector);

            // The layers, in order: reflection, then attributes. Whatever the caller sets afterwards comes last.
            foreach (var node in inspector.Root.Descendants().OfType<MemberNode>())
            {
                ReflectionPolicy.Apply(node, inspector);
                AttributePolicy.Apply(node, inspector);
            }
        }
        catch (Exception e)
        {
            GlobalOptions.Unlock(inspector.Id);
            inspector.OnDiscoveryFailed(string.Empty, FailureSeverity.Fatal, e,
                $"'{inspector.Name}' could not be created.");
            throw;
        }

        OnCreated(inspector);
        return inspector;
    }

    // A path from the root ("Moo.MooY"); chaining works too: inspector["Moo"]["MooY"].
    public InspectorNode this[string path] => Root[path];

    // The same with a member chain from the root, checked by the compiler:
    // inspector.Node<Foo>(f => f.Moo.MooY).
    public InspectorNode Node<T>(Expression<Func<T, object?>> selector) => Root.Node(selector);

    // What a view shows: ignored and hidden nodes left out, siblings by Order, and only groups opened.
    public IEnumerable<InspectorNode> Rows => Root.Rows;

    // Binding over a bound object throws: the swap is explicit (Rebind), and adding one is AddBind.
    public void Bind(object instance)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);

        if (Root.Instances.Count > 0)
            throw new InvalidOperationException($"'{Name}' is already bound; call Unbind(), Rebind() or AddBind().");

        CheckBindable([instance], Root.Instances);
        Register([instance]);
    }

    // Puts more objects in the bind (multi-bind). With nothing bound, it binds them.
    public void AddBind(params object[] instances)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        CheckBindable(instances, Root.Instances);
        Register(instances);
    }

    // Takes one object out of the bind; taking the last one out is the same as Unbind().
    public void RemoveBind(object instance)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);

        var index = Root.Instances.FindIndex(bound => ReferenceEquals(bound, instance));

        if (index < 0)
            throw new ArgumentException($"'{Name}' does not have this object bound.", nameof(instance));

        Root.Instances.RemoveAt(index);
        ResetNodes();
        OnBindRemoved([instance]);

        if (Root.Instances.Count == 0)
            OnUnbound();
    }

    public void Unbind()
    {
        if (Root.Instances.Count == 0)
            return;

        var removed = Root.Instances.ToList();
        Root.Instances.Clear();
        ResetNodes();
        OnBindRemoved(removed);
        OnUnbound();
    }

    // Unbind and bind again, with one object or several.
    public void Rebind(params object[] instances)
    {
        // Checked before unbinding, so a refused instance leaves the current ones bound; the current
        // ones are about to leave, so binding one of them again is fine.
        ObjectDisposedException.ThrowIf(Disposed, this);
        CheckBindable(instances, []);

        Unbind();
        Register(instances);
    }

    // Reads every node again and raises ValueChanged (Refresh) where something changed since the last
    // read: the natural way to catch changes in objects that do not report them. Groups whose object
    // was replaced outside are found first, and their branches are left out. It is the way from the
    // objects to the view on its own, so without InstanceToView it does nothing; pending values stay.
    public void Refresh()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);

        if (Options.InstanceToView)
            ReadAll(node => node.Update(ValueSource.Refresh));
    }

    // Writes the values the nodes hold while ViewToInstance is off: the normal flow for writing by
    // hand. A node that cannot take its value now keeps it, and the row shows why.
    public void Apply()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);

        foreach (var node in Root)
            node.ApplyPending();
    }

    // Drops the pending values and reads every node again, raising ValueChanged (Reload) where the view
    // changed: the normal flow for reading by hand, whatever the binder control. Replaced groups are
    // found first, as in Refresh().
    public void Reload()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        ReadAll(node => node.Reload());
    }

    // Writes what the view holds (the pending values, and the values read last) into every bound
    // object, whatever the binder control: for when the objects went their own way and the view is
    // right. ValueChanged (Force) comes where the view changed with it: a pending value that went, or
    // a value a setter changed on its way in.
    public void ForceApply()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);

        // Taken before anything is written, because a write reads its branch again.
        var held = Root.Select(node => (Node: node, Values: node.HeldValues())).ToList();
        var dropped = new HashSet<InspectorNode>();

        foreach (var (node, values) in held)
        {
            if (node.ForceWrite(values))
                dropped.Add(node);
        }

        foreach (var node in Root)
            node.Update(ValueSource.Force, dropped.Contains(node));

        Watcher.Rewire();
        OnForcedApply();
    }

    // Starts over from what the objects hold now, whatever the binder control: pending values and
    // failures go, and a branch disabled by a replaced group takes the new object, as a Rebind with the
    // same objects would, without the bind events. ValueChanged (Force) says what the view has to show
    // again.
    public void ForceReload()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);

        foreach (var node in Root)
            node.Reset(ValueSource.Force);

        Watcher.Rewire();
        OnForcedReload();
    }

    // Empties what the view shows (zero, false, an empty text or null), whatever the binder control.
    // The objects keep their values, and the next read brings them back.
    public void ForceClear()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);

        foreach (var node in Root)
            node.Clear();

        OnForcedClear();
    }

    // The whole tree, as discovered: ignored nodes and the insides of closed groups too.
    public IEnumerator<InspectorNode> GetEnumerator()
    {
        return Root.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // Unbinds, disposes the nodes and releases this inspector's hold on the global options.
    public void Dispose()
    {
        if (Disposed)
            return;

        Disposed = true;

        try
        {
            Unbind();
            Root.Dispose();
        }
        finally
        {
            GlobalOptions.Unlock(Id);
        }
    }

    // Keeps a failure of the Create in the report and raises DiscoveryFailed with it.
    internal InspectorFailureEventArgs OnDiscoveryFailed(string path, FailureSeverity severity, Exception exception,
        string message, string? suggestion = null)
    {
        var failure = new InspectorFailureEventArgs(this, path, severity, exception, message, suggestion);
        Report.Add(failure);
        DiscoveryFailed?.Invoke(this, failure);
        return failure;
    }

    private static void OnDiscoveryFinished(Inspector inspector)
    {
        DiscoveryFinished?.Invoke(inspector, new InspectorEventArgs(inspector));
    }

    private static void OnCreated(Inspector inspector)
    {
        Created?.Invoke(inspector, new InspectorCreatedEventArgs(inspector));
    }

    private void Register(object[] instances)
    {
        Root.Instances.AddRange(instances);
        ResetNodes();
        OnBindRegistered(instances);
    }

    // The objects the watcher listens to may have changed.
    internal void Rewire()
    {
        Watcher.Rewire();
    }

    // Replaced groups first, from the top down, so a parent is checked before its children (the tree
    // comes in pre-order); then every node is read.
    private void ReadAll(Action<InspectorNode> read)
    {
        foreach (var node in Root)
        {
            if (!node.IsCompromised)
                node.CheckReplaced();
        }

        foreach (var node in Root)
            read(node);

        Watcher.Rewire();
    }

    // The bound objects changed, so every node starts over from what they hold now.
    private void ResetNodes()
    {
        foreach (var node in Root)
            node.Reset();

        Watcher.Rewire();
    }

    // The tree was built for Target, so only an instance of it (or of a type derived from it) fits,
    // and an object is bound once.
    private void CheckBindable(object[] instances, IEnumerable<object> alreadyBound)
    {
        if (instances == null)
            throw new ArgumentNullException(nameof(instances));

        foreach (var instance in instances)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!Root.Target.IsInstanceOfType(instance))
                throw new ArgumentException($"'{Name}' cannot bind an instance of '{instance.GetType().Name}'.", nameof(instance));

            if (alreadyBound.Concat(instances).Count(bound => ReferenceEquals(bound, instance)) > 1)
                throw new ArgumentException($"'{Name}' already has this object bound.", nameof(instance));
        }
    }

    private void OnBindRegistered(IReadOnlyList<object> instances)
    {
        BindRegistered?.Invoke(this, new BindEventArgs(this, instances));
    }

    private void OnBindRemoved(IReadOnlyList<object> instances)
    {
        BindRemoved?.Invoke(this, new BindEventArgs(this, instances));
    }

    private void OnUnbound()
    {
        Unbound?.Invoke(this, new InspectorEventArgs(this));
    }

    private void OnForcedApply()
    {
        ForcedApply?.Invoke(this, new InspectorEventArgs(this));
    }

    private void OnForcedReload()
    {
        ForcedReload?.Invoke(this, new InspectorEventArgs(this));
    }

    private void OnForcedClear()
    {
        ForcedClear?.Invoke(this, new InspectorEventArgs(this));
    }
}