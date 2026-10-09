using System.Globalization;
using System.Reflection;
using InteractiveEditor;
using InteractiveEditor.Options;

namespace TerminalHost;

// The text of the values the host shows and takes. It uses the culture the core reads typed text with
// (the inspector's Culture, or the current one), so what the host shows can be typed back as is.
internal static class NodeText
{
    public static CultureInfo CultureOf(Inspector inspector)
    {
        return inspector.Options.Culture ?? CultureInfo.CurrentCulture;
    }

    // For reading: null reads "null" and strings are quoted, so an empty text and null look different.
    public static string Format(InspectorNode node, object? value)
    {
        return value switch
        {
            null => "null",
            string text => $"\"{text}\"",
            _ => ToText(node, value),
        };
    }

    // Several values, one per bound object, as the binding check compares them.
    public static string Format(InspectorNode node, IReadOnlyList<object?> values)
    {
        return values.Count == 1 ? Format(node, values[0]) : $"[{string.Join(", ", values.Select(value => Format(node, value)))}]";
    }

    // For editing: the raw text the core reads back. Null is spelled out, so that a reference or a
    // nullable member can be set to null from the edit box.
    public static string EditText(InspectorNode node, object? value)
    {
        return value == null ? "null" : ToText(node, value);
    }

    // What the edit box hands to SetValue: the text itself, which the core converts with its own rules;
    // null for "null" where the member takes it; and a color read here, since the core reads no text
    // into a color (the views of the other frameworks pick one in a dialog).
    public static bool TryValueFor(InspectorNode node, string text, out object? value)
    {
        value = text;

        if (text == "null" && TakesNull(node.ValueType))
        {
            value = null;
            return true;
        }

        if (node.Editor != EditorKind.Color)
            return true;

        return ColorText.TryParse(text, node.ValueType, out value);
    }

    // Int32?, List<Moo>, ... instead of Nullable`1 and List`1.
    public static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{TypeName(underlying)}?";

        var tick = type.Name.IndexOf('`');

        if (!type.IsGenericType || tick < 0)
            return type.Name;

        return $"{type.Name[..tick]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }

    // The exception a getter or a setter threw, out of the reflection's wrapper.
    public static Exception Unwrap(Exception exception)
    {
        return exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
    }

    private static bool TakesNull(Type? type)
    {
        return type != null && (!type.IsValueType || Nullable.GetUnderlyingType(type) != null);
    }

    private static string ToText(InspectorNode node, object value)
    {
        if (node.Editor == EditorKind.Color && ColorText.Format(value) is { } color)
            return color;

        return value is IFormattable formattable
            ? formattable.ToString(null, CultureOf(node.Inspector))
            : value.ToString() ?? string.Empty;
    }
}
