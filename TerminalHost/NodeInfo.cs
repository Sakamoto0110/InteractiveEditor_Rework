using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using InteractiveEditor;
using InteractiveEditor.Binding;

namespace TerminalHost;

internal static class NodeInfo
{
    public static MemberInfo? FindMember(Type owner, string name) =>
        owner.GetMember(name, BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(mi => mi is FieldInfo or PropertyInfo);

    // FieldDescriptor.Type is the type that owns the member, so the member's own type is looked up from it.
    public static Type? MemberTypeOf(InspectorNode node)
    {
        if (node.Descriptor is not { } descriptor)
            return TryGetValue(node, out var value) ? value?.GetType() : null;

        return FindMember(descriptor.Type, descriptor.Name) switch
        {
            FieldInfo fi => fi.FieldType,
            PropertyInfo pi => pi.PropertyType,
            _ => null
        };
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

    public static string Format(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? value.GetType().Name
    };

    // The raw text shown in the edit box, which is also what Parse accepts back.
    public static string EditText(object? value) => value switch
    {
        null => "null",
        string s => s,
        _ => Format(value)
    };

    public static object? Parse(string text, Type type)
    {
        if (text == "null" && (!type.IsValueType || Nullable.GetUnderlyingType(type) != null))
            return null;

        if (type == typeof(string))
            return text;

        var converter = TypeDescriptor.GetConverter(type);

        if (!converter.CanConvertFrom(typeof(string)))
            throw new NotSupportedException($"'{type.Name}' cannot be converted from text.");

        return converter.ConvertFromInvariantString(text);
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
            lines.Add(("Owner type", $"{descriptor.Type.Name}  (FieldDescriptor.Type)"));
            lines.Add(("Member type", MemberTypeOf(node)?.Name ?? "?"));
            lines.Add(("Getter", accessors.Getter != null ? "yes" : "no"));
            lines.Add(("Setter", accessors.Setter != null ? "yes" : "no"));
            lines.Add(("Parent", node.Parent?.Name ?? "-"));
        }

        if (node is Inspector inspector)
            lines.Add(("Children", inspector.Nodes.Count.ToString(CultureInfo.InvariantCulture)));

        if (ok)
        {
            lines.Add(("Value", Format(value)));
            lines.Add(("Value type", value?.GetType().Name ?? "-"));
        }
        else
        {
            lines.Add(("Value", $"<error: {((Exception)value!).Message}>"));
        }

        return string.Join(Environment.NewLine, lines.Select(l => $"{l.Key,-12} {l.Value}"));
    }
}
