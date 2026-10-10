using Hexa.NET.ImGui;
using Gui = Hexa.NET.ImGui.ImGui;

namespace InteractiveEditor.ImGui;

// A text field, for Text and for what NumberEditor leaves to text (P7.9). The text goes to the node as
// typed, and the core converts it with its own rules; it is written on Enter and when the field loses the
// focus, and Esc goes back to what the view holds (P2.12). A text the core refuses stays as it was typed,
// with the failure under it, until the value changes; the object keeps its value.
internal class TextEditor(ImGuiRow row) : ImGuiEditor(row)
{
    private const uint MaxLength = 4096;

    // The text being typed: the value is read every frame, so it is kept here while the field is active.
    private string? Typing;

    // The text the last write refused, and the text the field showed when it was refused.
    private string? Refused;
    private string? RefusedOver;

    // Objects that hold different values show none, with a grey dash, and what is typed goes to all of
    // them; a row that scrubs shows the first one's instead, since the drag moves each from its own (P2.4,
    // P7.19). A null shows as a grey hint, so it is told from an empty text.
    public override void Draw()
    {
        var value = Node.ViewValue;
        var mixed = Row.Mixed && !Row.Scrubs;
        var shown = mixed ? string.Empty : Format(value);

        if (Refused != null && RefusedOver != shown)
            Refused = null;

        var text = Typing ?? Refused ?? shown;
        var hint = mixed ? MixedText : value == null ? "null" : string.Empty;
        var entered = Gui.InputTextWithHint("##value", hint, ref text, MaxLength, ImGuiInputTextFlags.EnterReturnsTrue);

        // With EnterReturnsTrue, ImGui 1.92 hands the text back only when the field lets go, and on Esc it
        // hands back what was typed instead of reverting it; a field let go on Esc is a cancel, not a write.
        var cancelled = Gui.IsItemDeactivated() && Gui.IsKeyPressed(ImGuiKey.Escape, false);

        if (Gui.IsItemActive())
        {
            Typing = text;
        }
        else
        {
            Typing = null;

            if (!cancelled && (entered || Gui.IsItemDeactivatedAfterEdit()))
                Commit(text, shown);
        }
    }

    // A write that goes through shows the value as the node now holds it (7,6 in an int shows 8); one that
    // fails leaves the text, and is not tried again until the text changes.
    private void Commit(string typed, string shown)
    {
        if (typed == shown || typed == Refused)
            return;

        if (Row.Write(() => Node.SetValue(typed)) && Node.Failure == null)
        {
            Refused = null;
        }
        else
        {
            Refused = typed;
            RefusedOver = shown;
        }
    }
}
