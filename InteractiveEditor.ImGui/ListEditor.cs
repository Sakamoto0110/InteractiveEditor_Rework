using System.Numerics;
using Hexa.NET.ImGui;
using Gui = Hexa.NET.ImGui.ImGui;

namespace InteractiveEditor.ImGui;

// One line per item, with adding, removing and moving them (P7.9, P5.10): a list box over the item lines,
// as many as the items up to ListRows before it scrolls, and the four buttons on the line below. Choosing
// a line chooses the item shown in the row below; the operations write at once, whatever the binder
// control (P5.13).
internal sealed class ListEditor(ImGuiRow row, CollectionNode collection) : ImGuiEditor(row)
{
    // The lines still choose in a read-only collection; the buttons do not edit it.
    public override bool Enabled => !Node.IsCompromised;

    public override void Draw()
    {
        var items = collection.Items;
        var index = collection.SelectedIndex;
        var lines = Math.Clamp(items.Count, 1, Node.Inspector.Options.ListRows);
        var height = lines * Gui.GetTextLineHeightWithSpacing() + Gui.GetStyle().FramePadding.Y * 2f;
        var picked = -1;

        if (Gui.BeginListBox("##items", new Vector2(-float.Epsilon, height)))
        {
            for (var i = 0; i < items.Count; i++)
            {
                // The index keeps the ID apart for two items that read the same.
                if (Gui.Selectable($"{ItemText(i, items[i])}##{i}", i == index))
                    picked = i;
            }

            Gui.EndListBox();
        }

        var operation = DrawButtons(index, items.Count);

        if (picked >= 0 && picked != index)
            Row.Write(() => collection.SelectedIndex = picked);
        else if (operation != null)
            Row.Write(operation);
    }

    // The operation of the button pressed, or null. The two arrows keep their square, and + and - share
    // what is left of the line.
    private Action? DrawButtons(int index, int count)
    {
        var editable = !Node.ReadOnly;
        var arrow = Gui.GetFrameHeight();
        var width = Math.Max(arrow, (Gui.GetContentRegionAvail().X - 2f * arrow - 3f * Gui.GetStyle().ItemSpacing.X) / 2f);
        Action? operation = null;

        if (Button("+", width, editable))
            operation = collection.AddItem;

        Gui.SameLine();

        if (Button("-", width, editable && index >= 0))
            operation = () => collection.RemoveItem(index);

        Gui.SameLine();

        if (Arrow("##up", ImGuiDir.Up, editable && index > 0))
            operation = () => collection.MoveItem(index, index - 1);

        Gui.SameLine();

        if (Arrow("##down", ImGuiDir.Down, editable && index >= 0 && index < count - 1))
            operation = () => collection.MoveItem(index, index + 1);

        return operation;
    }

    private static bool Button(string text, float width, bool enabled)
    {
        Gui.BeginDisabled(!enabled);
        var pressed = Gui.Button(text, new Vector2(width, 0f));
        Gui.EndDisabled();
        return pressed;
    }

    private static bool Arrow(string id, ImGuiDir direction, bool enabled)
    {
        Gui.BeginDisabled(!enabled);
        var pressed = Gui.ArrowButton(id, direction);
        Gui.EndDisabled();
        return pressed;
    }
}
