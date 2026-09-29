using System.Reflection;

namespace InteractiveEditor;

// A field or a property of the object above it. Group or leaf is decided at run time (IsGroup),
// because Expandable can change after the Create.
public sealed class MemberNode : InspectorNode
{
    internal MemberNode(InspectorNode parent, MemberInfo member) : base(parent, member.Name)
    {
        Member = member;
    }

    public MemberInfo Member { get; }

    public override Type ValueType => Member is FieldInfo fi ? fi.FieldType : ((PropertyInfo)Member).PropertyType;

    public override void SetValue(object? value)
    {
        // A group is edited through its fields; the object behind it is never replaced from here.
        if (IsGroup)
            throw new InvalidOperationException($"'{Name}' is a group; set its fields instead.");

        WriteTo(Root.Instance, value);
    }

    internal override object? Resolve(object? instance)
    {
        var owner = Parent!.Resolve(instance);

        if (owner == null)
            return null;

        return Member switch
        {
            FieldInfo fi => fi.GetValue(owner),
            PropertyInfo { CanRead: true } pi => pi.GetValue(owner),
            _ => null
        };
    }

    internal override void WriteTo(object? instance, object? value)
    {
        if (ReadOnly)
            throw new InvalidOperationException($"'{Name}' is read-only.");

        var owner = Parent!.Resolve(instance)
            ?? throw new InvalidOperationException($"Cannot set '{Name}': '{Parent.Name}' is null.");

        switch (Member)
        {
            case FieldInfo fi:
                fi.SetValue(owner, value);
                break;
            case PropertyInfo { CanWrite: true } pi:
                pi.SetValue(owner, value);
                break;
            default:
                throw new InvalidOperationException($"'{Name}' is read-only.");
        }

        // A struct owner is a boxed copy, so it has to be written back into its own owner.
        if (owner.GetType().IsValueType)
            Parent.WriteTo(instance, owner);
    }
}
