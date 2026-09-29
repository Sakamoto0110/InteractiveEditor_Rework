using System.Collections;
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
    private bool Disposed;

    private Inspector(Type target)
    {
        Id = Interlocked.Increment(ref LastId);
        Root = new RootNode(target);
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

    // What went wrong during the Create, for whoever checks after it.
    public InspectorReport Report { get; } = new();

    public string Name => Root.Name;

    // The first bound object, or null. A struct at the root is the inspector's own copy, read back
    // here.
    public object? Instance => Root.Instances.FirstOrDefault();

    // Every bound object, in the order they were bound.
    public IReadOnlyList<object> Instances => Root.Instances.AsReadOnly();

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

    // What a view shows: ignored and hidden nodes left out, siblings by Order, and only groups opened.
    public IEnumerable<InspectorNode> Rows => Root.Rows;

    // Binding over a bound object throws: the swap is explicit (Rebind), and adding one is AddBind.
    public void Bind(object instance)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);

        if (Root.Instances.Count > 0)
            throw new InvalidOperationException($"'{Name}' is already bound; call Unbind(), Rebind() or AddBind().");

        CheckBindable([instance]);
        Register([instance]);
    }

    // Puts more objects in the bind (multi-bind). With nothing bound, it binds them.
    public void AddBind(params object[] instances)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        CheckBindable(instances);
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
        OnBindRemoved(removed);
        OnUnbound();
    }

    // Unbind and bind again, with one object or several.
    public void Rebind(params object[] instances)
    {
        // Checked before unbinding, so a refused instance leaves the current ones bound.
        ObjectDisposedException.ThrowIf(Disposed, this);
        CheckBindable(instances);

        Unbind();
        Register(instances);
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
        OnBindRegistered(instances);
    }

    // The tree was built for Target, so only an instance of it (or of a type derived from it) fits,
    // and an object is bound once.
    private void CheckBindable(object[] instances)
    {
        if (instances == null)
            throw new ArgumentNullException(nameof(instances));

        foreach (var instance in instances)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!Root.Target.IsInstanceOfType(instance))
                throw new ArgumentException($"'{Name}' cannot bind an instance of '{instance.GetType().Name}'.", nameof(instance));

            if (Root.Instances.Concat(instances).Count(bound => ReferenceEquals(bound, instance)) > 1)
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
}