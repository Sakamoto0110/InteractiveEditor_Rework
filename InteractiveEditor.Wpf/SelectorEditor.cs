using System.Windows;
using System.Windows.Controls;

namespace InteractiveEditor.Wpf;

// A combo box with the items of a collection (P7.9): choosing one sets SelectedIndex, and the item shows
// in the row below (P5.10).
internal sealed class SelectorEditor : WpfEditor
{
    private readonly ComboBox Box = new();
    private readonly CollectionNode Collection;

    // Set while the view fills or selects, so only the user's choice writes.
    private bool Showing;

    public SelectorEditor(WpfRow row, CollectionNode collection) : base(row)
    {
        Collection = collection;
        Box.SelectionChanged += OnSelected;
    }

    public override FrameworkElement Control => Box;

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
    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        base.ShowState(readOnly: false, enabled, failed);
    }

    private void OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (Showing)
            return;

        var index = Box.SelectedIndex;
        Row.Write(() => Collection.SelectedIndex = index);
        Row.Show();
    }
}
