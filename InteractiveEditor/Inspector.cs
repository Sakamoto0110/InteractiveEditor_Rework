using InteractiveEditor.Binding;
using InteractiveEditor.Model;
using System.Collections;

namespace InteractiveEditor;

public class Inspector : InspectorNode, IEnumerable<InspectorNode>
{
    protected Type? Target;

    private object? Instance;
    private List<InspectorNode> Children = [];

    protected Inspector() { }

    public override string Name => Descriptor?.Name ?? Target?.Name ?? " -- ";

    public static Inspector Create<T>()
    {
        var inspector = new Inspector
        {
            Target = typeof(T)
        };

        var descriptors = ReflectionDiscovery.ResolveFor(typeof(T));
        var inspectors = new Dictionary<string, Inspector>();

        foreach (var descriptor in descriptors)
        {
            var hasChildren = descriptors.Any(f =>
                f.FullPath.StartsWith(descriptor.FullPath + ".", StringComparison.Ordinal));

            InspectorNode node;

            if (hasChildren)
            {
                node = new Inspector
                {
                    Descriptor = descriptor
                };
            }
            else
            {
                node = new Fieldset
                {
                    Descriptor = descriptor
                };
            }

            var separator = descriptor.FullPath.LastIndexOf('.');

            Inspector parent;

            if (separator == -1)
            {
                parent = inspector;
            }
            else
            {
                var parentPath = descriptor.FullPath[..separator];
                parent = inspectors[parentPath];
            }

            node.Parent = parent;
            parent.Children.Add(node);

            if (node is Inspector nestedInspector)
                inspectors.Add(descriptor.FullPath, nestedInspector);
        }

        return inspector;
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

    public InspectorNode this[string name]
    {
        get
        {
            if (Parent == null && Name == name)
                return this;

            var child = Children.FirstOrDefault(c => c.Name == name);

            if (child == null)
                throw new KeyNotFoundException(
                    $"Node '{name}' not found in inspector '{Name}'.");

            return child;
        }
    }

    public IEnumerator<InspectorNode> GetEnumerator()
    {
        foreach (var child in Children)
        {
            yield return child;

            if (child is Inspector inspector)
            {
                 foreach (var node in inspector)
                    yield return node;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}