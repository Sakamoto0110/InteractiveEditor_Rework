using System.Reflection;
using InteractiveEditor;

namespace TerminalHost;

internal record BindingResult(InspectorNode Node, object? Bound, object? Direct, string? Error)
{
    public bool Ok => Error == null && BindingCheck.Same(Bound, Direct);
}

/// <summary>
/// Compares what each node reads through the inspector with what plain reflection reads from the bound
/// instance, walking the member names from the root. It does not use the descriptors, so it catches
/// accessors that point at the wrong member or writes that never reach the real object (struct copies).
/// </summary>
internal static class BindingCheck
{
    public static IEnumerable<BindingResult> Run(Inspector root)
    {
        var instance = root.GetValue();

        return root.Select(node => Check(instance, node));
    }

    public static BindingResult Check(object? instance, InspectorNode node)
    {
        try
        {
            return new BindingResult(node, node.GetValue(), ReadDirect(instance, node), null);
        }
        catch (Exception ex)
        {
            return new BindingResult(node, null, null, NodeInfo.Unwrap(ex).Message);
        }
    }

    public static object? ReadDirect(object? instance, InspectorNode node)
    {
        var names = new Stack<string>();

        for (var current = node; current.Parent is { } parent; current = parent)
            names.Push(current.Name);

        var value = instance;

        foreach (var name in names)
        {
            if (value == null)
                return null;

            value = NodeInfo.FindMember(value.GetType(), name) switch
            {
                FieldInfo fi => fi.GetValue(value),
                PropertyInfo pi => pi.GetValue(value),
                _ => throw new MissingMemberException(value.GetType().Name, name)
            };
        }

        return value;
    }

    public static bool Same(object? a, object? b)
    {
        if (a == null || b == null)
            return a == null && b == null;

        return a.GetType().IsValueType ? a.Equals(b) : ReferenceEquals(a, b);
    }
}
