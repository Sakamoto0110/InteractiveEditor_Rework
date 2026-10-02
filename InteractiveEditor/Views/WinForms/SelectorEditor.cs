using System.Windows.Forms;

namespace InteractiveEditor.Views.WinForms;

// A combo box with the items of a collection (P7.9): choosing one sets SelectedIndex, and the item shows
// in the row below (P5.10).
internal sealed class SelectorEditor : WinFormsEditor
{
    private readonly ComboBox Box = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CollectionNode Collection;

    public SelectorEditor(WinFormsRow row, CollectionNode collection) : base(row)
    {
        Collection = collection;
        Box.SelectionChangeCommitted += OnCommitted;
    }

    public override Control Control => Box;

    public override void ShowValue()
    {
        var texts = Collection.Items.Select((item, index) => ItemText(index, item)).ToArray();

        if (!Box.Items.Cast<string>().SequenceEqual(texts))
        {
            Box.Items.Clear();
            Box.Items.AddRange(texts);
        }

        Box.SelectedIndex = Collection.SelectedIndex;
    }

    // Choosing an item is not an edit, so a read-only collection still chooses which one shows.
    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        base.ShowState(readOnly: false, enabled, failed);
    }

    private void OnCommitted(object? sender, EventArgs e)
    {
        var index = Box.SelectedIndex;
        Row.Write(() => Collection.SelectedIndex = index);
        ShowValue();
    }
}
