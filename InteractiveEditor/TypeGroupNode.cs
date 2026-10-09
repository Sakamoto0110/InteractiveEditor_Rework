using InteractiveEditor.Options;

namespace InteractiveEditor;

// The level a member hidden with new gets (P5.9). When two members of the same object share a name
// (Value in Base, and a new Value in Derived), each goes under the name of the type that declares it:
// Base.Value and Derived.Value. The group holds no value of its own; its members read and write the
// object the group sits in.
public sealed class TypeGroupNode : InspectorNode
{
    internal TypeGroupNode(InspectorNode parent, Type declaringType) : base(parent, NameOf(declaringType))
    {
        DeclaringType = declaringType;
        Label = Name;
        Editor = EditorKind.Header;
        Expandable = true;
    }

    // The type whose members the group holds.
    public Type DeclaringType { get; }

    public override void SetValue(object? value)
    {
        throw new InvalidOperationException($"'{Name}' groups members hidden with new; set its fields instead.");
    }

    // The object the group sits in, which its members belong to.
    internal override object? Resolve(object? instance) => Parent!.Resolve(instance);

    // Nothing is written to the group itself; whatever comes up goes on to the object it sits in.
    internal override void WriteTo(object? instance, object? value) => Parent!.WriteTo(instance, value);

    // "Box`1" reads as "Box".
    private static string NameOf(Type type)
    {
        return type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;
    }
}
