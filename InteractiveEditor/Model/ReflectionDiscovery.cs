using System.Reflection;

namespace InteractiveEditor.Model;

internal static class ReflectionDiscovery
{
    public static void AddMembers(InspectorNode parent, Type type)
    {
        AddMembers(parent, type, []);
    }

    private static void AddMembers(InspectorNode parent, Type type, HashSet<Type> ancestry)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (IsTerminal(type) || !ancestry.Add(type))
            return;

        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
        {
            if (member is PropertyInfo property && property.GetIndexParameters().Length != 0)
                continue;

            if (member is not (FieldInfo or PropertyInfo))
                continue;

            var node = parent.Add(member);

            if (member is FieldInfo || ((PropertyInfo)member).CanRead)
                AddMembers(node, node.ValueType!, ancestry);
        }

        ancestry.Remove(type);
    }

    internal static bool IsTerminal(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(object)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }
}