namespace InteractiveEditor;

// The top of the tree, kept inside the Inspector: the bound object lives here, and every member
// resolves its owner from it.
internal sealed class RootNode : InspectorNode
{
    internal RootNode(Type target) : base(null, target.Name)
    {
        Target = target;
    }

    public Type Target { get; }

    public object? Instance { get; set; }

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
