using System.ComponentModel;
using System.Globalization;

namespace InteractiveEditor.Binding;

/// <summary>
/// Text forms of member values, shared by the views. Everything goes through the invariant culture, so the
/// text a view shows can be typed back as is.
/// </summary>
public static class ValueText
{
    // Int32?, List<String>, ... instead of Nullable`1 and List`1.
    public static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{TypeName(underlying)}?";

        var tick = type.Name.IndexOf('`');

        if (!type.IsGenericType || tick < 0)
            return type.Name;

        return $"{type.Name[..tick]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }

    // For display: null reads "null" and strings are quoted, so an empty string and null look different.
    public static string Format(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => ToText(value)
    };

    // For editing: the raw text that Parse accepts back; null is empty.
    public static string ToText(object? value) => value switch
    {
        null => string.Empty,
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => TypeDescriptor.GetConverter(value.GetType()).ConvertToInvariantString(value) ?? string.Empty
    };

    public static bool CanParse(Type type) =>
        type == typeof(string) || TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));

    // Strings are taken verbatim, empty text is null for a Nullable<T>, anything else goes through the type's converter.
    public static object? Parse(string text, Type type)
    {
        if (type == typeof(string))
            return text;

        if (text.Length == 0 && Nullable.GetUnderlyingType(type) != null)
            return null;

        var converter = TypeDescriptor.GetConverter(type);

        if (!converter.CanConvertFrom(typeof(string)))
            throw new NotSupportedException($"{TypeName(type)} cannot be converted from text.");

        return converter.ConvertFromInvariantString(text);
    }
}
