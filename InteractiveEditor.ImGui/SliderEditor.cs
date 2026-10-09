using ImGuiNET;
using InteractiveEditor.Options;
using InteractiveEditor.Views;
using Gui = ImGuiNET.ImGui;

namespace InteractiveEditor.ImGui;

// A slider over the node's range (P7.9), in double whatever the member's number type, which the core
// converts to. It goes by the range's step, and writes while it is dragged (P2.12).
internal sealed class SliderEditor(ImGuiRow row) : ImGuiEditor(row)
{
    // The most decimals a slider shows, for a step that small or no step.
    private const int MaxDecimals = 3;

    public override unsafe void Draw()
    {
        var range = Node.Range ?? default;
        var min = range.Min;
        var max = range.Max;
        var current = Math.Clamp(ViewRules.ToDouble(Node.ViewValue, min), min, max);
        var value = current;

        if (!Gui.SliderScalar("##value", ImGuiDataType.Double, (IntPtr)(&value), (IntPtr)(&min), (IntPtr)(&max), FormatFor(range),
                ImGuiSliderFlags.AlwaysClamp))
        {
            return;
        }

        if (range.Step > 0)
            value = Math.Min(max, min + Math.Round((value - min) / range.Step) * range.Step);

        // The slider edits the value through its address, so the write takes a copy.
        var written = value;

        if (written != current)
            Row.Write(() => Node.SetValue(written));
    }

    // Whole numbers for a member that holds them, and otherwise as many decimals as the step has.
    private string FormatFor(NumericRange range)
    {
        var type = Node.ValueType is { } held ? Nullable.GetUnderlyingType(held) ?? held : null;

        if (Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32
            or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64)
        {
            return "%.0f";
        }

        var decimals = range.Step > 0 ? Math.Clamp((int)Math.Ceiling(-Math.Log10(range.Step)), 0, MaxDecimals) : MaxDecimals;
        return $"%.{decimals}f";
    }
}
