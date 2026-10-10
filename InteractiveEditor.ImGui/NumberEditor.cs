using Hexa.NET.ImGui;
using Gui = Hexa.NET.ImGui.ImGui;

namespace InteractiveEditor.ImGui;

// ImGui's own number field for the number types it knows, with step buttons for the integers (P7.9). ImGui
// edits the number in place and reports every change, so it is written as it is typed, as a number of the
// member's type, through the node's rules (range, value rules). What the field cannot show goes through
// the text of TextEditor: a nullable number (an empty text is null), a decimal, a value that is not there,
// and objects that hold different values, unless the row scrubs (P7.19).
internal sealed class NumberEditor(ImGuiRow row) : TextEditor(row)
{
    private static readonly Dictionary<Type, ImGuiDataType> Numbers = new()
    {
        [typeof(sbyte)] = ImGuiDataType.S8,
        [typeof(byte)] = ImGuiDataType.U8,
        [typeof(short)] = ImGuiDataType.S16,
        [typeof(ushort)] = ImGuiDataType.U16,
        [typeof(int)] = ImGuiDataType.S32,
        [typeof(uint)] = ImGuiDataType.U32,
        [typeof(long)] = ImGuiDataType.S64,
        [typeof(ulong)] = ImGuiDataType.U64,
        [typeof(float)] = ImGuiDataType.Float,
        [typeof(double)] = ImGuiDataType.Double,
    };

    public override void Draw()
    {
        var value = Node.ViewValue;

        if (Row.Mixed && !Row.Scrubs || value == null || value.GetType() != Node.ValueType
            || !Numbers.TryGetValue(value.GetType(), out var dataType))
        {
            base.Draw();
            return;
        }

        if (DrawNumber(dataType, value) is { } edited)
            Row.Write(() => Node.SetValue(edited));
    }

    private static object? DrawNumber(ImGuiDataType dataType, object value) => value switch
    {
        sbyte v => DrawScalar(dataType, v, (sbyte)1),
        byte v => DrawScalar(dataType, v, (byte)1),
        short v => DrawScalar(dataType, v, (short)1),
        ushort v => DrawScalar(dataType, v, (ushort)1),
        int v => DrawScalar(dataType, v, 1),
        uint v => DrawScalar(dataType, v, 1u),
        long v => DrawScalar(dataType, v, 1L),
        ulong v => DrawScalar(dataType, v, 1ul),
        float v => DrawScalar<float>(dataType, v, null, "%g"),
        double v => DrawScalar<double>(dataType, v, null, "%g"),
        _ => null
    };

    // The edited value, or null when the field did not change it in this frame.
    private static unsafe object? DrawScalar<T>(ImGuiDataType dataType, T value, T? step, string? format = null)
        where T : unmanaged
    {
        var stepValue = step.GetValueOrDefault();
        var stepPointer = step.HasValue ? &stepValue : null;

        if (!Gui.InputScalar("##value", dataType, &value, stepPointer, null, format, ImGuiInputTextFlags.None))
            return null;

        return value;
    }
}
