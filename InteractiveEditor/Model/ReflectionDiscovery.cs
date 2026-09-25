using System.Reflection;

namespace InteractiveEditor.Model;

public static class ReflectionDiscovery
{
    public static IReadOnlyList<FieldDescriptor> ResolveFor(Type type)
    {
        var fields = new List<FieldDescriptor>();
        var ancestry = new HashSet<Type>();

        ResolveType(
            type,
            type,
            fields,
            null,
            ancestry);

        return fields;
    }

    private static void ResolveType(
        Type rootType,
        Type type,
        List<FieldDescriptor> fields,
        string? parentFullPath,
        HashSet<Type> ancestry)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (IsTerminal(type))
            return;

        if (!ancestry.Add(type))
            return;

        var flags = BindingFlags.Public | BindingFlags.Instance;

        foreach (var mi in type.GetMembers(flags).Where(mi => mi is FieldInfo or PropertyInfo))
        {
            if (mi is PropertyInfo pi && pi.GetIndexParameters().Length != 0)
                continue;

            var accessors = CreateAccessors(mi);
            var memberType = GetMemberType(mi);

            var fullPath = parentFullPath == null
                ? $"{rootType.Name}_{mi.Name}"
                : $"{parentFullPath}.{mi.Name}";

            var descriptor = new FieldDescriptor
            {
                Type = type,
                Name = mi.Name,
                Path = $"{mi.DeclaringType?.Name ?? string.Empty}.{mi.Name}",
                FullPath = fullPath,
                MemberType = mi.MemberType,
                Accessors = accessors,
                OwnerGetter = obj => obj
            };

            fields.Add(descriptor);

            if (!IsTerminal(memberType) && accessors.Getter != null)
            {
                ResolveType(
                    rootType,
                    memberType,
                    fields,
                    fullPath,
                    ancestry);
            }
        }

        ancestry.Remove(type);
    }

    private static FieldDescriptor.FieldAccessors CreateAccessors(MemberInfo mi)
    {
        return mi switch
        {
            FieldInfo fi => new FieldDescriptor.FieldAccessors
            {
                Getter = obj => fi.GetValue(obj),
                Setter = (obj, value) => fi.SetValue(obj, value)
            },

            PropertyInfo pi => new FieldDescriptor.FieldAccessors
            {
                Getter = pi.CanRead ? obj => pi.GetValue(obj) : null,
                Setter = pi.CanWrite ? (obj, value) => pi.SetValue(obj, value) : null
            },

            _ => throw new NotSupportedException()
        };
    }

    private static Type GetMemberType(MemberInfo mi)
    {
        return mi switch
        {
            FieldInfo fi => fi.FieldType,
            PropertyInfo pi => pi.PropertyType,
            _ => throw new NotSupportedException()
        };
    }

    private static bool IsTerminal(Type type)
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