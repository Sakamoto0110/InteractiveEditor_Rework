using System.Collections;
using System.Reflection;
using InteractiveEditor;

namespace TerminalHost;

// Compares what each member node reads through the inspector (GetValues) with what plain reflection reads
// from every bound object, walking the member names from the root. It does not use the nodes' members,
// so it catches reads that point at the wrong member and writes that never reach the real object
// (struct copies). Only the names, the type of a group of hidden members (P5.9) and the item chosen in a
// collection (P5.10) come from the tree.
internal static class BindingCheck
{
    // Every member of the tree, ignored ones and the insides of closed groups too. Buttons, displays and
    // groups of a type have no member of their own to compare with.
    public static IEnumerable<BindingResult> Run(Inspector inspector)
    {
        return inspector.OfType<MemberNode>().Select(Check);
    }

    public static BindingResult Check(InspectorNode node)
    {
        try
        {
            var bound = node.GetValues();
            var direct = node.Inspector.Instances.Select(instance => ReadDirect(instance, node)).ToList();

            return new BindingResult(node, bound, direct, null);
        }
        catch (Exception ex)
        {
            return new BindingResult(node, [], [], NodeText.Unwrap(ex).Message);
        }
    }

    public static bool Same(object? a, object? b)
    {
        if (a == null || b == null)
            return a == null && b == null;

        return a.GetType().IsValueType ? a.Equals(b) : ReferenceEquals(a, b);
    }

    // The node's value in one bound object; null when something on the way is null. The root does not
    // count: the chain starts at the top-level members.
    private static object? ReadDirect(object instance, InspectorNode node)
    {
        var chain = new Stack<InspectorNode>();

        for (var current = node; current.Parent != null; current = current.Parent)
            chain.Push(current);

        object? value = instance;

        foreach (var step in chain)
        {
            if (value == null)
                return null;

            value = step switch
            {
                // A group of hidden members sits in the object it groups.
                TypeGroupNode => value,
                ItemNode item => ItemAt(value, item.Collection.SelectedIndex),
                _ => ReadMember(value, step.Name, (step.Parent as TypeGroupNode)?.DeclaringType),
            };
        }

        return value;
    }

    private static object? ItemAt(object collection, int index)
    {
        if (index < 0)
            return null;

        if (collection is IList list)
            return index < list.Count ? list[index] : null;

        return ((IEnumerable)collection).Cast<object?>().ElementAtOrDefault(index);
    }

    // The public field or property with that name: the one of the most derived type, as C# finds it, or
    // the one the group's type declares.
    private static object? ReadMember(object owner, string name, Type? declaringType)
    {
        var member = owner.GetType().GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m is not PropertyInfo property || property.GetIndexParameters().Length == 0)
            .Where(m => declaringType == null || m.DeclaringType == declaringType)
            .OrderByDescending(m => Depth(m.DeclaringType))
            .FirstOrDefault();

        return member switch
        {
            FieldInfo fi => fi.GetValue(owner),
            PropertyInfo pi => pi.GetValue(owner),
            _ => throw new MissingMemberException(owner.GetType().Name, name),
        };
    }

    private static int Depth(Type? type)
    {
        var depth = 0;

        for (; type != null; type = type.BaseType)
            depth++;

        return depth;
    }
}
