using System.Reflection;
using System.Runtime.CompilerServices;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Model;

namespace InteractiveEditor.Options.Policies;

internal static class ReflectionPolicy
{
    private static readonly HashSet<Type> NumberTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
    ];

    public static void Apply(MemberNode node, Inspector inspector)
    {
        try
        {
            node.Label = node.Member.Name;
            node.Editor = EditorFor(node.ValueType);
            node.ReadOnly = !IsPubliclyWritable(node.Member);
            node.Ignored = HasHiddenSetter(node.Member);
            node.Expandable = node.HasMembers && !GlobalOptions.RequireExpandableAttribute;
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

    private static EditorKind EditorFor(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return EditorKind.Choice;

        if (type == typeof(bool))
            return EditorKind.Toggle;

        if (NumberTypes.Contains(type))
            return EditorKind.Number;

        if (type == typeof(object) || !ReflectionDiscovery.IsTerminal(type))
            return EditorKind.Display;

        return EditorKind.Text;
    }

    private static bool IsPubliclyWritable(MemberInfo member)
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
