using System.Collections;
using InteractiveEditor.Model;
using InteractiveEditor.Options.Policies;

namespace InteractiveEditor;

// Holds the tree and the bound object. It is not a node itself: the options live on the nodes,
// and the root stays inside.
public sealed class Inspector : IEnumerable<InspectorNode>
{
    private static int LastId;

    private readonly RootNode Root;

    private Inspector(Type target)
    {
        Id = Interlocked.Increment(ref LastId);
        Root = new RootNode(target);
    }

    // Tells inspectors apart, so an event can be traced back to the one that raised it.
    public int Id { get; }

    public string Name => Root.Name;

    // The bound object, or null. A struct at the root is the inspector's own copy, read back here.
    public object? Instance => Root.Instance;

    public static Inspector Create<T>()
    {
        var inspector = new Inspector(typeof(T));

        ReflectionDiscovery.AddMembers(inspector.Root, typeof(T));

        // The layers, in order: reflection, then attributes. Whatever the caller sets afterwards comes last.
        foreach (var node in inspector.Root.Descendants().OfType<MemberNode>())
        {
            ReflectionPolicy.Apply(node);
            AttributePolicy.Apply(node);
        }

        return inspector;
    }

    // A path from the root ("Moo.MooY"); chaining works too: inspector["Moo"]["MooY"].
    public InspectorNode this[string path] => Root[path];

    // What a view shows: ignored nodes left out, siblings by Order, and only groups opened.
    public IEnumerable<InspectorNode> Rows => Root.Rows;

    // One object at a time for now (multi-bind comes later), so swapping it is explicit.
    public void Bind(object instance)
    {
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

    // The tree was built for Target, so only an instance of it (or of a type derived from it) fits.
    private void CheckBindable(object instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (!Root.Target.IsInstanceOfType(instance))
            throw new ArgumentException($"'{Name}' cannot bind an instance of '{instance.GetType().Name}'.", nameof(instance));
    }
}