using System.Reflection;
using System.Runtime.CompilerServices;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Model;

namespace InteractiveEditor.Options.Policies;

internal static class ReflectionPolicy
{
    public static void Apply(MemberNode node, Inspector inspector)
    {
        try
        {
            node.Label = node.Name;
            node.Editor = EditorFor(node);

            // The item of a collection has no member to say these (P5.10). A collection held by
            // reference keeps its content editable without a public setter, which only replaces the
            // collection itself (P4.7); one that is a struct is written whole, as any struct.
            if (node.Member is { } member)
            {
                node.ReadOnly = !IsPubliclyWritable(member) && node is not CollectionNode { ValueType.IsValueType: false };
                node.Ignored = HasHiddenSetter(member);
            }

            // A collection always shows the row of its item, which opens as a member would. A type with
            // more than one editor stays closed until one is chosen (P5.5).
            node.Expandable = node is CollectionNode
                || node.HasMembers && !GlobalOptions.RequireExpandableAttribute && !ReflectionDiscovery.HasManyEditors(node.ValueType);
        }
        catch (Exception e)
        {
            // The node stays, with its name as the label and the defaults for the rest.
            node.Label = node.Name;
            inspector.OnDiscoveryFailed(node.Path, FailureSeverity.WorkedAround, e,
                $"The defaults of '{node.Path}' could not be read from the member.",
                "Set the options of the node in the inspector after the Create.");
        }
    }

    // Only a collection node lists its items: a collection anywhere else (an item that is one, or a
    // member of an inspector with no type, made before its type was known) is a Display row.
    internal static EditorKind EditorFor(MemberNode node)
    {
        var editor = EditorFor(node.ValueType);
        return editor == EditorKind.Selector && node is not CollectionNode ? EditorKind.Display : editor;
    }

    private static EditorKind EditorFor(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return EditorKind.Choice;

        if (type == typeof(bool))
            return EditorKind.Toggle;

        if (ValueConverter.IsNumber(type))
            return EditorKind.Number;

        // A collection lists its items, and the one chosen shows in the row below (P5.10).
        if (ReflectionDiscovery.IsCollection(type))
            return EditorKind.Selector;

        if (type == typeof(object) || !ReflectionDiscovery.IsTerminal(type))
            return EditorKind.Display;

        return EditorKind.Text;
    }

    internal static bool IsPubliclyWritable(MemberInfo member)
    {
        return member switch
        {
            FieldInfo fi => !fi.IsInitOnly && !fi.IsLiteral,
            PropertyInfo pi => pi.SetMethod is { IsPublic: true } setter && !IsInitOnly(setter),
            _ => false
        };
    }

    // A setter the type keeps to itself (private, protected, internal) hides the member; init, get-only
    // and readonly members stay, read-only.
    private static bool HasHiddenSetter(MemberInfo member)
    {
        return member is PropertyInfo { SetMethod: { IsPublic: false } };
    }

    // init accessors are public setters marked with the IsExternalInit modifier
    private static bool IsInitOnly(MethodInfo setter)
    {
        return setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(IsExternalInit));
    }
}
