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

    // One object at a time for now (multi-bind comes later), so swapping it is explicit.
    public void Bind(object instance)
    {
        CheckBindable(instance);

        if (Instance != null)
            throw new InvalidOperationException($"'{Name}' is already bound; call Unbind() or Rebind() first.");

        Instance = instance;
    }

    public void Unbind()
    {
        Instance = null;
    }

    public void Rebind(object instance)
    {
        // Checked before unbinding, so a refused instance leaves the current one bound.
        CheckBindable(instance);

        Unbind();
        Bind(instance);
    }

    public override object? GetValue() => Instance;

    // The bound object only changes through Bind, Unbind and Rebind.
    public override void SetValue(object? value)
    {
        throw new InvalidOperationException($"'{Name}' is the root; use Rebind() to change the bound object.");
    }

    // A struct at the root is edited in its own box, so the write-back hands that same box back.
    internal override void Write(object? value) => Instance = value;

    // The tree was built for Target, so only an instance of it (or of a type derived from it) fits.
    private void CheckBindable(object instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (!Target.IsInstanceOfType(instance))
            throw new ArgumentException($"'{Name}' cannot bind an instance of '{instance.GetType().Name}'.", nameof(instance));
    }
}