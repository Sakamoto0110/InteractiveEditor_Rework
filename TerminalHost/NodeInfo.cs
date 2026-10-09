using System.Globalization;
using System.Reflection;
using InteractiveEditor;
using InteractiveEditor.Binding;

namespace TerminalHost;

internal static class NodeInfo
{
    public static Type? MemberTypeOf(InspectorNode node)
    {
        if (node.Descriptor is { } descriptor)
            return descriptor.Type;

        return TryGetValue(node, out var value) ? value?.GetType() : null;
    }

    public static bool TryGetValue(InspectorNode node, out object? value)
    {
        try
        {
            value = node.GetValue();
            return true;
        }
        catch (Exception ex)
        {
            value = Unwrap(ex);
            return false;
        }
    }

    public static Exception Unwrap(Exception ex) =>
        ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;

    public static string Format(object? value) => ValueText.Format(value);

    // The edit box spells null out, so that reference members (strings included) can be set to null too.
    public static string EditText(object? value) => value == null ? "null" : ValueText.ToText(value);

    public static object? Parse(string text, Type type)
    {
        if (text == "null" && (!type.IsValueType || Nullable.GetUnderlyingType(type) != null))
            return null;

        return ValueText.Parse(text, type);
    }

    public static string Label(InspectorNode node)
    {
        if (!TryGetValue(node, out var value))
            return $"{node.Name} = <error: {((Exception)value!).Message}>";

        return node switch
        {
            Fieldset => $"{node.Name} = {Format(value)}",
            _ when value == null => $"{node.Name} = null",
            _ => node.Name
        };
    }

    public static string Describe(InspectorNode node)
    {
        var lines = new List<(string Key, string Value)>();
        var ok = TryGetValue(node, out var value);

        if (node.Descriptor is not { } descriptor)
        {
            lines.Add(("Kind", "Inspector (root)"));
            lines.Add(("Name", node.Name));
        }
        else
        {
            var accessors = descriptor.Accessors;

            lines.Add(("Kind", node is Inspector ? "Inspector" : "Fieldset"));
            lines.Add(("Name", descriptor.Name));
            lines.Add(("Path", descriptor.Path));
            lines.Add(("Full path", descriptor.FullPath));
            lines.Add(("Member", descriptor.MemberType.ToString()));
            lines.Add(("Type", ValueText.TypeName(descriptor.Type)));
            lines.Add(("Owner type", ValueText.TypeName(descriptor.OwnerType)));
            lines.Add(("Getter", accessors.Getter != null ? "yes" : "no"));
            lines.Add(("Setter", accessors.Setter != null ? "yes" : "no"));
            lines.Add(("Parent", node.Parent?.Name ?? "-"));
        }

        if (node is Inspector inspector)
            lines.Add(("Children", inspector.Nodes.Count.ToString(CultureInfo.InvariantCulture)));

        if (ok)
        {
            lines.Add(("Value", Format(value)));
            lines.Add(("Value type", value == null ? "-" : ValueText.TypeName(value.GetType())));
        }
        else
        {
            lines.Add(("Value", $"<error: {((Exception)value!).Message}>"));
        }

        return string.Join(Environment.NewLine, lines.Select(l => $"{l.Key,-12} {l.Value}"));
    }
}
