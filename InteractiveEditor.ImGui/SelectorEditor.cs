using Gui = ImGuiNET.ImGui;

namespace InteractiveEditor.ImGui;

// A combo box with the items of a collection (P7.9): choosing one sets SelectedIndex, and the item shows
// in the row below (P5.10).
internal sealed class SelectorEditor(ImGuiRow row, CollectionNode collection) : ImGuiEditor(row)
{
    // Choosing an item is not an edit, so a read-only collection still chooses which one shows.
    public override bool Enabled => !Node.IsCompromised;

    public override void Draw()
    {
        var items = collection.Items;
        var index = collection.SelectedIndex;
        var shown = index >= 0 && index < items.Count ? ItemText(index, items[index]) : string.Empty;

        if (!Gui.BeginCombo("##value", shown))
            return;

        var picked = -1;

        for (var i = 0; i < items.Count; i++)
        {
            var selected = i == index;

            // The index keeps the ID apart for two items that read the same.
            if (Gui.Selectable($"{ItemText(i, items[i])}##{i}", selected))
                picked = i;

            if (selected)
                Gui.SetItemDefaultFocus();
        }

        Gui.EndCombo();

        if (picked >= 0 && picked != index)
            Row.Write(() => collection.SelectedIndex = picked);
    }
}
