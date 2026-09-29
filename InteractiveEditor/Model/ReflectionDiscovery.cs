using System.Reflection;
using InteractiveEditor.Diagnostics;

namespace InteractiveEditor.Model;

internal static class ReflectionDiscovery
{
    // A member whose type cannot be read is left out and reported; the root type failing to read
    // is fatal and propagates.
    public static void AddMembers(InspectorNode parent, Type type, Inspector inspector)
    {
        AddMembers(parent, type, [], inspector);
    }

    private static void AddMembers(InspectorNode parent, Type type, HashSet<Type> ancestry, Inspector inspector)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (IsTerminal(type) || !ancestry.Add(type))
            return;

        try
        {
            foreach (var member in MembersOf(type, parent, inspector))
            {
                var node = new MemberNode(parent, member);

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

                    parent.Add(node);
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