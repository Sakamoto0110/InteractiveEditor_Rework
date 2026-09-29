using System.Reflection;
using System.Runtime.CompilerServices;
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

    public static void Apply(MemberNode node)
    {
        node.Label = node.Member.Name;
        node.Editor = EditorFor(node.ValueType);
        node.ReadOnly = !IsPubliclyWritable(node.Member);
        node.Expandable = node.HasMembers && !GlobalOptions.RequireExpandableAttribute;
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

    // init accessors are public setters marked with the IsExternalInit modifier
    private static bool IsInitOnly(MethodInfo setter)
    {
        return setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(IsExternalInit));
    }
}
