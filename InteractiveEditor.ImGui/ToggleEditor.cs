using Gui = Hexa.NET.ImGui.ImGui;

namespace InteractiveEditor.ImGui;

// A check box for a bool (P7.9), written at once (P2.12). Objects that hold different values leave it
// indeterminate (P7.19), which ImGui draws for a CheckboxFlags with only some of its flags set; a click
// sets all of them.
internal sealed class ToggleEditor(ImGuiRow row) : ImGuiEditor(row)
{
    private const int All = 0b11;
    private const int Some = 0b01;

    public override void Draw()
    {
        var flags = Row.Mixed ? Some : Node.ViewValue is true ? All : 0;

        if (!Gui.CheckboxFlags("##value", ref flags, All))
            return;

        var value = flags == All;
        Row.Write(() => Node.SetValue(value));
    }
}
