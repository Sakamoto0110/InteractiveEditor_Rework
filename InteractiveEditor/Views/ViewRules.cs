using System.Globalization;
using InteractiveEditor.Options;

namespace InteractiveEditor.Views;

// What every view decides the same way about a row, so the WinForms and the WPF ones do not drift apart:
// the editor it makes (P7.9), whether its label scrubs (P7.17), and whether it shows mixed values
// (P7.19).
internal static class ViewRules
{
    // The editor a row makes for a node: none for a group, unless it is a collection (its selector or
    // its list); a slider with no range is a number, and an editor still to be chosen (an inspector
    // with no type, before the bind) is text.
    public static EditorKind? KindFor(InspectorNode node)
    {
        if (node.Editor == EditorKind.Header)
            return EditorKind.Header;

        if (node.IsGroup && node.Editor is not (EditorKind.Selector or EditorKind.List))
            return null;

        return node.Editor switch
        {
            EditorKind.Slider when node.Range == null => EditorKind.Number,
            EditorKind.Auto => EditorKind.Text,
            var kind => kind,
        };
    }

    // Whether a node scrubs now: a number with a ScrubMultiplier, which can be written.
    public static bool CanScrub(InspectorNode node)
    {
        return !node.IsGroup && node.ScrubMultiplier is { } multiplier && multiplier != 0 && IsNumber(node.ValueType)
            && !node.ReadOnly && !node.IsCompromised;
    }

    // Whether a row of this editor shows its objects as holding different values: only a row with a
    // value of its own; the objects behind a group or a collection are always different ones.
    public static bool ShowsMixed(EditorKind? kind, InspectorNode node)
    {
        return kind is EditorKind.Text or EditorKind.Number or EditorKind.Toggle or EditorKind.Choice
            or EditorKind.Slider or EditorKind.Color or EditorKind.Display && node.IsMixed;
    }

    // A number as a double; a null (a nullable number with no value) counts as the fallback.
    public static double ToDouble(object? value, double fallback = 0)
    {
        return value is IConvertible convertible ? convertible.ToDouble(CultureInfo.InvariantCulture) : fallback;
    }

    // The numeric types and their nullable forms; an enum is a choice, not a number.
    private static bool IsNumber(Type? type)
    {
        if (type == null)
            return false;

        type = Nullable.GetUnderlyingType(type) ?? type;

        return !type.IsEnum && Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
            or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double
            or TypeCode.Decimal;
    }
}
