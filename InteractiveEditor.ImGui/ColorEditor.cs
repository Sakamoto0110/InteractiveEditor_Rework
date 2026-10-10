using System.Numerics;
using Hexa.NET.ImGui;
using PixieLib;
using Gui = Hexa.NET.ImGui.ImGui;

namespace InteractiveEditor.ImGui;

// ImGui's color field, a swatch that opens a picker and the RGBA inputs, written while it is dragged
// (P7.9). The member can hold a PxColorRgba, a PxColorHsl or a System.Drawing.Color; ImGui works in RGBA
// from 0 to 1, rounded to bytes on the way back. Objects that hold different colors show a dash (P7.19),
// which opens the picker on the first object's color; a color picked goes to all of them.
internal sealed class ColorEditor(ImGuiRow row) : ImGuiEditor(row)
{
    private const ImGuiColorEditFlags Flags = ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf;
    private const string Picker = "picker";

    private static readonly Vector4 Black = new(0f, 0f, 0f, 1f);

    public override void Draw()
    {
        var color = ToVector(Node.ViewValue) ?? Black;

        if (Row.Mixed)
        {
            if (Gui.Button($"{MixedText}###mixed", new Vector2(-float.Epsilon, 0f)))
                Gui.OpenPopup(Picker);
        }
        else if (Gui.ColorEdit4("##value", ref color, Flags))
        {
            Write(color);
        }

        // Drawn whatever the row shows, so the picker stays open once its first color made the objects
        // hold the same one.
        if (!Gui.BeginPopup(Picker))
            return;

        var picked = Gui.ColorPicker4("##picker", ref color, Flags);
        Gui.EndPopup();

        if (picked)
            Write(color);
    }

    private static Vector4? ToVector(object? value) => value switch
    {
        PxColorRgba rgba => rgba.ToVector4(),
        PxColorHsl hsl => hsl.ToVector4(),
        System.Drawing.Color color => ((PxColorRgba)color).ToVector4(),
        _ => null,
    };

    // The color, as the type of the member.
    private void Write(Vector4 color)
    {
        var rgba = color.ToPrimitive();
        var type = Node.ValueType is { } held ? Nullable.GetUnderlyingType(held) ?? held : null;

        object value = type == typeof(PxColorHsl) ? PxColorHsl.FromRgba(rgba)
            : type == typeof(System.Drawing.Color) ? (System.Drawing.Color)rgba
            : rgba;

        Row.Write(() => Node.SetValue(value));
    }
}
