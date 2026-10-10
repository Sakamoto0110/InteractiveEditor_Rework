using System.Numerics;
using Gui = Hexa.NET.ImGui.ImGui;

namespace InteractiveEditor.ImGui;

// A button with the Text of a ButtonNode, across the column, which calls Press() (P7.9). An action that
// throws is a failure on the row, as the core reports it (P1.6).
internal sealed class ButtonEditor(ImGuiRow row) : ImGuiEditor(row)
{
    // Only a ButtonNode has an action to run.
    public override bool Enabled => base.Enabled && Node is ButtonNode;

    public override void Draw()
    {
        var text = Node is ButtonNode button ? button.Text : Node.Label;

        // The ID stays the same when the text changes.
        if (Gui.Button($"{text}###button", new Vector2(-float.Epsilon, 0f)) && Node is ButtonNode pressed)
            Row.Write(pressed.Press);
    }
}
