using System.Collections;
using InteractiveEditor.Binding;
using InteractiveEditor.Model;
using InteractiveEditor.Options.Policies;

namespace InteractiveEditor;

public partial class Inspector : InspectorNode, IEnumerable<InspectorNode>
{
    private static readonly IFieldPolicy[] Policies = [new ReflectionPolicy(), new AttributePolicy()];

    protected object? Host;
    protected Type? Target;

    private object? Instance;
    private List<InspectorNode> Children = [];

    protected Inspector() { }

    public override string Name => Descriptor?.Name ?? Target?.Name ?? " -- ";

    public override bool IsGroup => Expandable;

    internal InspectorNode? Child(string name) => Children.FirstOrDefault(c => c.Name == name);

    public static Inspector Create<T>()
    {
        var root = new Inspector { Target = typeof(T) };
        var groups = new Dictionary<string, Inspector>(StringComparer.Ordinal);
        var descriptors = ReflectionDiscovery.ResolveFor(typeof(T));

        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];

            // Descriptors come in pre-order, so a member's own members come right after it.
            var hasMembers = i + 1 < descriptors.Count
                && descriptors[i + 1].FullPath.StartsWith(descriptor.FullPath + ".", StringComparison.Ordinal);

            InspectorNode node = hasMembers
                ? new Inspector { Descriptor = descriptor }
                : new Fieldset { Descriptor = descriptor };

            var separator = descriptor.FullPath.LastIndexOf('.');
            var parent = separator == -1 ? root : groups[descriptor.FullPath[..separator]];

            node.Parent = parent;
            parent.Children.Add(node);

            if (node is Inspector group)
                groups.Add(descriptor.FullPath, group);

            foreach (var policy in Policies)
                policy.Apply(node);
        }

        return root;
    }

    public void bind<T>(T instance)
    {
        if (Parent != null)
            throw new InvalidOperationException($"Only the root inspector can be bound; '{Name}' is nested.");

        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        Instance = instance;
    }

    public override object? GetValue() => Parent == null ? Instance : base.GetValue();

    public override void SetValue(object? value)
    {
        if (Parent == null)
            Instance = value;
        else
            base.SetValue(value);
    }

    // What a view shows: ignored nodes left out, siblings by Order, and only groups opened.
    public IEnumerator<InspectorNode> GetEnumerator()
    {
        foreach (var child in Children.Where(c => !c.Ignored).OrderBy(c => c.Order))
        {
            yield return child;

            if (child is Inspector { IsGroup: true } group)
            {
                foreach (var node in group)
                    yield return node;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}