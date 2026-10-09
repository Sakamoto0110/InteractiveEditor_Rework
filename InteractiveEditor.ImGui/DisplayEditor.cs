namespace InteractiveEditor.ImGui;

// A value shown and not edited (P7.9): a display added by hand, or a member with no editor of its own,
// as grey text that wraps. A null reads "null", and objects that hold different values a dash (P7.19).
internal sealed class DisplayEditor(ImGuiRow row) : ImGuiEditor(row)
{
    // The text is grey already; a read-only node is what a display is, not a reason to dim it again.
    public override bool Enabled => true;

    public override void Draw()
    {
        var value = Node.ViewValue;
        var text = Row.Mixed ? MixedText : value == null ? "null" : Format(value);
        ImGuiInspectorView.Text(text, ImGuiInspectorView.DisabledColor);
    }
}
