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

                MemberNode node;

                try
                {
                    // Reading the member's signature is what fails when an assembly it uses cannot be
                    // loaded; the indexer check and the choice of the node read it too.
                    if (member is PropertyInfo property && property.GetIndexParameters().Length != 0)
                        continue;

                    node = MemberNode.For(owner, member);

                    // Below a member go the members of its type; a collection has the row of its item
                    // below it, and the members of the item's type go below that (P5.10).
                    var holder = node is CollectionNode collection ? collection.Item : node;

                    if (member is FieldInfo || ((PropertyInfo)member).CanRead)
                        AddMembers(holder, holder.ValueType, ancestry, inspector);

                    // A group goes in with its first member, so a member left out leaves no empty group.
                    if (owner is TypeGroupNode { HasMembers: false })
                        parent.AddChild(owner);

                    owner.AddChild(node);
                }
                catch (Exception e)
                {
                    var path = InspectorNode.PathOf(owner, member.Name);

                    inspector.OnDiscoveryFailed(path, FailureSeverity.Critical, e,
                        $"'{path}' was left out: its type could not be read.",
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
    // discovery does not open it (P5.2): it is a CollectionNode, whose selector chooses the item that
    // shows in the row below it (P5.10). A string is text, not a collection of chars.
    internal static bool IsCollection(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    // The member a node added by name stands for (P1.12, P1.14). A type with no members to add throws,
    // as the discovery never opens one, and so does a name the type does not have.
    internal static MemberInfo MemberNamed(Type type, string name, string holder)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (IsTerminal(type))
            throw new InvalidOperationException($"'{holder}' holds a {type.Name}, which has no members to add.");

        if (IsCollection(type))
            throw new InvalidOperationException($"'{holder}' holds a collection, which shows its content, not its members.");

        return FindMember(type, name)
            ?? throw new ArgumentException($"'{type.Name}' has no public field or property named '{name}'.", nameof(name));
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