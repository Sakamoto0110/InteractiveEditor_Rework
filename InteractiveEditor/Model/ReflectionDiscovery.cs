using System.Reflection;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Primitives;

namespace InteractiveEditor.Model;

internal static class ReflectionDiscovery
{
    // Types that fit more than one editor (P5.5): a color can be its fields, a hex text or a picker.
    private static readonly HashSet<Type> ManyEditors = [typeof(System.Drawing.Color), typeof(PxColorArgb), typeof(PxColorHsl)];

    // A member whose type cannot be read is left out and reported; the root type failing to read
    // is fatal and propagates.
    public static void AddMembers(InspectorNode parent, Type type, Inspector inspector)
    {
        AddMembers(parent, type, [], inspector);
    }

    private static void AddMembers(InspectorNode parent, Type type, HashSet<Type> ancestry, Inspector inspector)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (IsTerminal(type) || IsCollection(type) || !ancestry.Add(type))
            return;

        try
        {
            var members = MembersOf(type, parent, inspector);

            // Names that more than one member has: a member hidden with new, and the one that hides
            // it. Each goes into the group of the type that declares it (P5.9).
            var hidden = members.GroupBy(m => m.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
            var groups = new Dictionary<Type, TypeGroupNode>();

            foreach (var member in members)
            {
                var owner = parent;

                if (hidden.Contains(member.Name))
                {
                    if (!groups.TryGetValue(member.DeclaringType!, out var group))
                        groups[member.DeclaringType!] = group = new TypeGroupNode(parent, member.DeclaringType!);

                    owner = group;
                }

                var node = new MemberNode(owner, member);

                try
                {
                    // Reading the member's signature is what fails when an assembly it uses cannot be
                    // loaded; the indexer check reads it too.
                    if (member is PropertyInfo property && property.GetIndexParameters().Length != 0)
                        continue;

                    if (member is FieldInfo || ((PropertyInfo)member).CanRead)
                        AddMembers(node, node.ValueType, ancestry, inspector);
                    else
                        _ = node.ValueType;

                    // A group goes in with its first member, so a member left out leaves no empty group.
                    if (owner is TypeGroupNode { HasMembers: false })
                        parent.AddChild(owner);

                    owner.AddChild(node);
                }
                catch (Exception e)
                {
                    inspector.OnDiscoveryFailed(node.Path, FailureSeverity.Critical, e,
                        $"'{node.Path}' was left out: its type could not be read.",
                        "Make sure the assemblies the member's type comes from are available.");
                }
            }
        }
        finally
        {
            ancestry.Remove(type);
        }
    }

    // The public fields and properties, in the order they were declared. When that order cannot be
    // read, the reflection's own order stays, and it is reported.
    private static List<MemberInfo> MembersOf(Type type, InspectorNode parent, Inspector inspector)
    {
        var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(member => member is FieldInfo or PropertyInfo)
            .ToList();

        try
        {
            return DeclarationOrder.Sort(members);
        }
        catch (Exception e)
        {
            inspector.OnDiscoveryFailed(parent.Path, FailureSeverity.WorkedAround, e,
                $"The members of '{type.Name}' keep the reflection's order: the declaration order could not be read.",
                "Use [InspectorOrder] to put them in order.");
            return members;
        }
    }

    // The public field or property with that name, the one of the most derived type when one hides
    // another (P5.4); indexers do not count.
    internal static MemberInfo? FindMember(Type type, string name)
    {
        return type.GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance)
            .Where(member => member is not PropertyInfo property || property.GetIndexParameters().Length == 0)
            .OrderByDescending(member => DeclarationOrder.Depth(member.DeclaringType!))
            .FirstOrDefault();
    }

    // The editor of these has to be chosen (P5.5); until then, they stay a closed Display row (P5.8).
    internal static bool HasManyEditors(Type type)
    {
        return ManyEditors.Contains(Nullable.GetUnderlyingType(type) ?? type);
    }

    // A collection shows its content, not the members of its type (Capacity, Count, Length...), so the
    // discovery does not open it (P5.2). How the content shows is open (P5.10); until then, it is a
    // Display row. A string is text, not a collection of chars.
    internal static bool IsCollection(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
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