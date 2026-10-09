using System.Drawing;
using System.Globalization;
using PixieLib;

namespace TerminalHost;

// The text of a color, for the Color editor (P7.9), which a terminal has no dialog for. A member can hold
// a System.Drawing.Color, a PxColorRgba or a PxColorHsl; all of them show as PxColorRgba does,
// "(r, g, b, a)", which is typed back as is, and "#RRGGBB" or "#RRGGBBAA" is read too.
internal static class ColorText
{
    // Null for a value that is not a color.
    public static string? Format(object value)
    {
        return ToRgba(value)?.ToString();
    }

    // The color in the text, as the type of the member; false when the text is not a color.
    public static bool TryParse(string text, Type? memberType, out object? value)
    {
        value = null;

        if (!TryRead(text.Trim(), out var color))
            return false;

        var type = memberType == null ? typeof(PxColorRgba) : Nullable.GetUnderlyingType(memberType) ?? memberType;

        value = type == typeof(PxColorHsl) ? PxColorHsl.FromRgba(color)
            : type == typeof(Color) ? (Color)color
            : color;

        return true;
    }

    private static PxColorRgba? ToRgba(object value)
    {
        return value switch
        {
            PxColorRgba rgba => rgba,
            PxColorHsl hsl => PxColorHsl.ToRgba(hsl),
            Color color => (PxColorRgba)color,
            _ => null,
        };
    }

    private static bool TryRead(string text, out PxColorRgba color)
    {
        color = default;

        if (text.StartsWith('#'))
        {
            var hex = text[1..];

            if (hex.Length is not (6 or 8) || !uint.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var number))
                return false;

            color = PxColorRgba.FromHex(hex.Length == 6 ? number << 8 | 0xFF : number);
            return true;
        }

        var parts = text.Trim('(', ')').Split(',', StringSplitOptions.TrimEntries);
        var bytes = new byte[4] { 0, 0, 0, 255 };

        if (parts.Length is not (3 or 4))
            return false;

        for (var i = 0; i < parts.Length; i++)
        {
            if (!byte.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out bytes[i]))
                return false;
        }

        color = new PxColorRgba(bytes[0], bytes[1], bytes[2], bytes[3]);
        return true;
    }
}
