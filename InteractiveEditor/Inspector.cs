using InteractiveEditor.Binding;
using InteractiveEditor.Model;
using System.Collections;

namespace InteractiveEditor;

public class Inspector : InspectorNode, IEnumerable<InspectorNode>
{
    protected object? Host;
    protected Type? Target;

    private bool IsTypeBound = false;
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

    public static Inspector Create<T>(System.Windows.Forms.Control host)
    {
        var inspector = new Presentation.WF.InspectorView(host);

         

        return inspector;
    }

    public static Inspector Create<T>(System.Windows.Controls.Control host)
    {
        var inspector = new Presentation.WPF.InspectorView(host);

         

        return inspector;
    }



    public override void bind<T>(T instance) => bind((object?)instance);

    private void bind(object? instance)
    {
        if (Parent == null)
        {
            if (IsTypeBound)
                throw new InvalidOperationException("Inspector is already bound to a type.");

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
        }

        Instance = instance;

        var target = Descriptor == null
            ? instance
            : Descriptor.Accessors.Getter?.Invoke(instance);

        foreach (var child in Children)
            child.bind(target);

        if (Parent == null)
            IsTypeBound = true;
    }

    public override void SetValue(object? value)
    {
        throw new InvalidOperationException(
            $"Inspector '{Name}' does not allow replacing its instance.");
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