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
            var member = node.Member!;
            node.Label = member.Name;
            node.Editor = EditorFor(node.ValueType);
            node.ReadOnly = !IsPubliclyWritable(member);
            node.Ignored = HasHiddenSetter(member);
            // A type with more than one editor stays closed until one is chosen (P5.5).
            node.Expandable = node.HasMembers && !GlobalOptions.RequireExpandableAttribute
                && !ReflectionDiscovery.HasManyEditors(node.ValueType);
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

    internal static EditorKind EditorFor(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return EditorKind.Choice;

        if (type == typeof(bool))
            return EditorKind.Toggle;

        if (ValueConverter.IsNumber(type))
            return EditorKind.Number;

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
