using System.Collections;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Model;
using InteractiveEditor.Options;
using InteractiveEditor.Options.Policies;

namespace InteractiveEditor;

// Holds the tree and the bound object. It is not a node itself: the options live on the nodes,
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

    // What went wrong during the Create, for whoever checks after it.
    public InspectorReport Report { get; } = new();

    public string Name => Root.Name;

    // The bound object, or null. A struct at the root is the inspector's own copy, read back here.
    public object? Instance => Root.Instance;

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

    // What a view shows: ignored nodes left out, siblings by Order, and only groups opened.
    public IEnumerable<InspectorNode> Rows => Root.Rows;

    // One object at a time for now (multi-bind comes later), so swapping it is explicit.
    public void Bind(object instance)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        CheckBindable(instance);

        if (Root.Instance != null)
            throw new InvalidOperationException($"'{Name}' is already bound; call Unbind() or Rebind() first.");

        Root.Instance = instance;
    }

    public void Unbind()
    {
        Root.Instance = null;
    }

    public void Rebind(object instance)
    {
        // Checked before unbinding, so a refused instance leaves the current one bound.
        ObjectDisposedException.ThrowIf(Disposed, this);
        CheckBindable(instance);

        Unbind();
        Bind(instance);
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

    // The tree was built for Target, so only an instance of it (or of a type derived from it) fits.
    private void CheckBindable(object instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (!Root.Target.IsInstanceOfType(instance))
            throw new ArgumentException($"'{Name}' cannot bind an instance of '{instance.GetType().Name}'.", nameof(instance));
    }
}