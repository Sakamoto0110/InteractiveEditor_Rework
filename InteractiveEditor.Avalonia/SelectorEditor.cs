using Avalonia.Controls;
using Avalonia.Layout;

namespace InteractiveEditor.Avalonia;

// A combo box with the items of a collection (P7.9), on top of the collection's group: choosing one sets
// SelectedIndex, and the item shows in the row below it (P5.10).
internal sealed class SelectorEditor : AvaloniaEditor
{
    private readonly ComboBox Box = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly CollectionNode Collection;

    // Set while the view fills or selects, so only the user's choice writes.
    private bool Showing;

    public SelectorEditor(AvaloniaRow row, CollectionNode collection) : base(row)
    {
        Collection = collection;
        Box.SelectionChanged += OnSelected;
    }

    public override Control Control => Box;

    public override void ShowValue()
    {
        var texts = Collection.Items.Select((item, index) => ItemText(index, item)).ToArray();
        Showing = true;

        try
        {
            if (!Box.Items.Cast<string>().SequenceEqual(texts))
            {
                Box.Items.Clear();

                foreach (var text in texts)
                    Box.Items.Add(text);
            }

            Box.SelectedIndex = Collection.SelectedIndex;
        }
        finally
        {
            Showing = false;
        }
    }

    // Choosing an item is not an edit, so a read-only collection still chooses which one shows.
    public override void ShowState(bool readOnly, bool enabled)
    {
        base.ShowState(readOnly: false, enabled);
    }

    private void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (Showing)
            return;

        var index = Box.SelectedIndex;
        Row.Write(() => Collection.SelectedIndex = index);
        Row.Show();
    }
}
