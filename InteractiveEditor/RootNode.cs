namespace InteractiveEditor;

// The top of the tree, kept inside the Inspector: the bound object lives here, and every member
// resolves its owner from it.
internal sealed class RootNode : InspectorNode
{
    internal RootNode(Type target, Inspector inspector, bool typed) : base(null, target.Name)
    {
        Target = target;
        Owner = inspector;
        Typed = typed;
    }

    // The type the tree is for. Without a type, it is the type the first bind into nothing fixed.
    public Type Target { get; set; }

    // False for an inspector with no type (P1.14), whose members are found by name at the bind.
    public bool Typed { get; }

    // The inspector that keeps this root; every node reaches it from here.
    public Inspector Owner { get; }

    // The bound objects, in the order they were bound. GetValue shows the first one.
    public List<object> Instances { get; } = [];

    public override Type ValueType => Target;

    // The bound object only changes through the inspector's Bind, Unbind and Rebind.
    public override void SetValue(object? value)
    {
        throw new InvalidOperationException($"'{Name}' is the root; use Rebind() to change the bound object.");
    }

    internal override object? Resolve(object? instance) => instance;

    // A struct at the root is edited in its own box, so there is nothing to write back.
    internal override void WriteTo(object? instance, object? value)
    {
    }
}
