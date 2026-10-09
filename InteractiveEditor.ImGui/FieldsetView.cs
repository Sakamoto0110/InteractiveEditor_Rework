using System.Numerics;
using System.Reflection;
using ImGuiNET;
using InteractiveEditor.Binding;
// Inside InteractiveEditor.ImGui the bare name ImGui resolves to this namespace, hence the alias.
using Gui = ImGuiNET.ImGui;

namespace InteractiveEditor.ImGui;

/// <summary>
/// Draws the editor of one <see cref="Fieldset"/>, picked from the member type. ImGui keeps no widget state
/// between frames, so the value is read on every frame; what this class keeps is the text being typed and the
/// error of the last edit.
/// </summary>
internal sealed class FieldsetView(Fieldset fieldset, InspectorView owner)
{
    public static readonly Vector4 ErrorColor = new(1f, 0.4f, 0.4f, 1f);

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

    private string? PendingText;
    private string? Error;

    public void Draw(bool focus)
    {
        var descriptor = fieldset.Descriptor!;
        var type = descriptor.Type;
        object? value;

        try
        {
            value = fieldset.GetValue();
        }
        catch (Exception ex)
        {
            InspectorView.Text(Unwrap(ex).Message, ErrorColor);
            return;
        }

        // A value type only reads null when its owner is null, and then there is nothing to edit.
        if (value == null && type.IsValueType && Nullable.GetUnderlyingType(type) == null)
        {
            InspectorView.Text("null", InspectorView.DisabledColor);
            return;
        }

        var readOnly = descriptor.Accessors.Setter == null;

        if (readOnly)
            Gui.BeginDisabled();

        if (focus)
            Gui.SetKeyboardFocusHere();

        Gui.SetNextItemWidth(-float.Epsilon);
        var edit = DrawEditor(type, value);

        if (readOnly)
            Gui.EndDisabled();

        if (edit != null)
            Commit(edit);

        if (Error != null)
            InspectorView.Text(Error, ErrorColor);
    }

    private Func<object?>? DrawEditor(Type type, object? value)
    {
        if (type == typeof(bool))
        {
            var current = (bool)value!;
            return Gui.Checkbox("##value", ref current) ? () => current : null;
        }

        if (type.IsEnum)
            return DrawEnum(type, value!);

        if (Numbers.TryGetValue(type, out var dataType))
            return DrawNumber(dataType, value!);

        if (ValueText.CanParse(type))
            return DrawText(type, value);

        InspectorView.Text(ValueText.Format(value), InspectorView.DisabledColor);
        return null;
    }

    private static Func<object?>? DrawEnum(Type type, object value)
    {
        object? picked = null;

        if (Gui.BeginCombo("##value", value.ToString()))
        {
            foreach (var item in Enum.GetValues(type))
            {
                var selected = item.Equals(value);

                if (Gui.Selectable(item.ToString(), selected))
                    picked = item;

                if (selected)
                    Gui.SetItemDefaultFocus();
            }

            Gui.EndCombo();
        }

        return picked == null ? null : () => picked;
    }

    private static Func<object?>? DrawNumber(ImGuiDataType dataType, object value) => value switch
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

    // ImGui edits the number in place and returns true on every change, so edits are applied as they are typed.
    private static unsafe Func<object?>? DrawScalar<T>(ImGuiDataType dataType, T value, T? step, string? format = null)
        where T : unmanaged
    {
        var stepValue = step.GetValueOrDefault();
        var stepPointer = step.HasValue ? (IntPtr)(&stepValue) : IntPtr.Zero;

        if (!Gui.InputScalar("##value", dataType, (IntPtr)(&value), stepPointer, IntPtr.Zero, format, ImGuiInputTextFlags.None))
            return null;

        var result = value;
        return () => result;
    }

    // Free text through ValueText (string, decimal, char, DateTime, nullables...), applied on Enter or when the
    // field loses focus. While the field is active the typed text is kept here, since the value is read every frame.
    private Func<object?>? DrawText(Type type, object? value)
    {
        var shown = ValueText.ToText(value);
        var text = PendingText ?? shown;
        var entered = Gui.InputTextWithHint("##value", value == null ? "null" : string.Empty, ref text, 4096, ImGuiInputTextFlags.EnterReturnsTrue);

        if (Gui.IsItemActive())
            PendingText = text;

        if (entered || Gui.IsItemDeactivatedAfterEdit())
        {
            PendingText = null;

            if (text != shown)
            {
                var typed = text;
                return () => ValueText.Parse(typed, type);
            }
        }
        else if (!Gui.IsItemActive())
        {
            PendingText = null;
        }

        return null;
    }

    private void Commit(Func<object?> produce)
    {
        try
        {
            fieldset.SetValue(produce());
            Error = null;
            owner.OnFieldEdited(fieldset, null);
        }
        catch (Exception ex)
        {
            var error = Unwrap(ex);
            Error = error.Message;
            owner.OnFieldEdited(fieldset, error);
        }
    }

    private static Exception Unwrap(Exception ex) =>
        ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
}
