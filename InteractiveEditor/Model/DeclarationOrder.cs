using System.Reflection;

namespace InteractiveEditor.Model;

// The order members were declared in, as far as the metadata keeps it (P5.1). The base types come
// first. Within a type, the fields keep their order, and an auto-property takes the place of its
// backing field, which the compiler emits where the property is declared. A property with no backing
// field (a computed one) goes right before the next auto-property, after the fields declared before
// that one, or to the end when none follows; where it really stood among the fields is not kept. An
// override stands where the property was first declared.
internal static class DeclarationOrder
{
    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.DeclaredOnly
        | BindingFlags.Public | BindingFlags.NonPublic;

    public static List<MemberInfo> Sort(IEnumerable<MemberInfo> members)
    {
        var places = new Dictionary<Type, Dictionary<int, (int, int)>>();

        (int Depth, int Major, int Minor) Key(MemberInfo member)
        {
            member = FirstDeclaration(member);
            var declaring = member.DeclaringType!;

            if (!places.TryGetValue(declaring, out var inType))
                places[declaring] = inType = PlacesIn(declaring);

            var (major, minor) = inType.TryGetValue(member.MetadataToken, out var place) ? place : (int.MaxValue, int.MaxValue);
            return (Depth(declaring), major, minor);
        }

        // OrderBy keeps the reflection's order for whatever ties.
        return members.OrderBy(Key).ToList();
    }

    // Where each field and property of the type stands, by metadata token.
    private static Dictionary<int, (int, int)> PlacesIn(Type type)
    {
        var fields = type.GetFields(Declared).OrderBy(f => f.MetadataToken).ToList();
        var byName = fields.Select((field, index) => (field.Name, index)).ToDictionary(f => f.Name, f => f.index);
        var places = new Dictionary<int, (int, int)>();
        var computed = new List<PropertyInfo>();

        for (var i = 0; i < fields.Count; i++)
            places[fields[i].MetadataToken] = (i, 0);

        foreach (var property in type.GetProperties(Declared).OrderBy(p => p.MetadataToken))
        {
            if (!byName.TryGetValue($"<{property.Name}>k__BackingField", out var backing))
            {
                computed.Add(property);
                continue;
            }

            // The computed properties declared since the last auto-property go right before this one.
            for (var k = 0; k < computed.Count; k++)
                places[computed[k].MetadataToken] = (backing, k - computed.Count);

            computed.Clear();
            places[property.MetadataToken] = (backing, 0);
        }

        for (var k = 0; k < computed.Count; k++)
            places[computed[k].MetadataToken] = (int.MaxValue, k);

        return places;
    }

    // The property an override overrides, in the type that first declared it; any other member is its
    // own first declaration.
    private static MemberInfo FirstDeclaration(MemberInfo member)
    {
        if (member is not PropertyInfo property || (property.GetMethod ?? property.SetMethod) is not { } accessor)
            return member;

        var first = accessor.GetBaseDefinition();

        if (first.DeclaringType is not { } declaring || declaring == accessor.DeclaringType)
            return member;

        return declaring.GetProperties(Declared).FirstOrDefault(p =>
            p.GetMethod?.MetadataToken == first.MetadataToken || p.SetMethod?.MetadataToken == first.MetadataToken) ?? member;
    }

    private static int Depth(Type type)
    {
        var depth = 0;

        for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            depth++;

        return depth;
    }
}
