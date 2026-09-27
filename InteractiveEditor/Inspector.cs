using InteractiveEditor.Model;
using InteractiveEditor.Options.Policies;

namespace InteractiveEditor;

public class Inspector : InspectorNode
{
    private readonly Type Target;
    private object? Instance;

    private Inspector(Type target) : base(null, null)
    {
        Target = target;
    }

    public override string Name => Target.Name;

    public static Inspector Create<T>()
    {
        var root = new Inspector(typeof(T));

        ReflectionDiscovery.AddMembers(root, typeof(T));

        // The layers, in order: reflection, then attributes. Whatever the caller sets afterwards comes last.
        foreach (var node in root.Descendants())
        {
            ReflectionPolicy.Apply(node);
            AttributePolicy.Apply(node);
        }

        return root;
    }

    public void bind(object instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        Instance = instance;
    }

    public override object? GetValue() => Instance;

    public override void SetValue(object? value) => Instance = value;
}