using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace InteractiveEditor.Avalonia;

// One line per item, with adding, removing and moving them (P7.9, P5.10), on top of the collection's
// group: a list box over the item lines, as many as the inspector's ListRows before it scrolls, and the
// four buttons under it. Choosing a line chooses the item shown in the row below; the operations write at
// once, whatever the binder control (P5.13).
internal sealed class ListEditor : AvaloniaEditor
{
    // The height of an item line, compact as the rows of the grid.
    private const double LineHeight = 26;

    private readonly DockPanel Frame = new();

    // The list box does not bring the line chosen into sight by itself: when its own lines do not scroll,
    // the view's would, and the grid would jump to the list each time it is built.
    private readonly ListBox Lines = new() { AutoScrollToSelectedItem = false };

    private readonly Button AddButton = Command("+", "Add");
    private readonly Button RemoveButton = Command("−", "Remove");
    private readonly Button UpButton = Command(Arrow("M0,6 L5,0 L10,6 Z"), "Move up");
    private readonly Button DownButton = Command(Arrow("M0,0 L10,0 L5,6 Z"), "Move down");
    private readonly CollectionNode Collection;
    private bool Showing;
    private bool Editable;

    public ListEditor(AvaloniaRow row, CollectionNode collection) : base(row)
    {
        Collection = collection;
        Lines.MaxHeight = Node.Inspector.Options.ListRows * LineHeight + 2;

        var buttons = new UniformGrid { Columns = 4, Margin = new Thickness(0, 4, 0, 0) };
        buttons.Children.AddRange([AddButton, RemoveButton, UpButton, DownButton]);
        DockPanel.SetDock(buttons, Dock.Bottom);
        Frame.Children.Add(buttons);
        Frame.Children.Add(Lines);

        Lines.SelectionChanged += OnSelected;
        AddButton.Click += (_, _) => Operate(Collection.AddItem);
        RemoveButton.Click += (_, _) => Operate(() => Collection.RemoveItem(Collection.SelectedIndex));
        UpButton.Click += (_, _) => Operate(() => Collection.MoveItem(Collection.SelectedIndex, Collection.SelectedIndex - 1));
        DownButton.Click += (_, _) => Operate(() => Collection.MoveItem(Collection.SelectedIndex, Collection.SelectedIndex + 1));
    }

    public override Control Control => Frame;

    // The theme shows no error under a list box.
    public override bool ShowsErrors => false;

    public override void ShowValue()
    {
        var texts = Collection.Items.Select((item, index) => ItemText(index, item)).ToArray();
        Showing = true;

        try
        {
            if (!Lines.Items.Cast<ListBoxItem>().Select(line => line.Content as string).SequenceEqual(texts))
            {
                Lines.Items.Clear();

                foreach (var text in texts)
                    Lines.Items.Add(new ListBoxItem { Content = text, Height = LineHeight, Padding = new Thickness(8, 0), VerticalContentAlignment = VerticalAlignment.Center });
            }

            Lines.SelectedIndex = Collection.SelectedIndex;
        }
        finally
        {
            Showing = false;
        }

        ShowButtons();
    }

    // The lines still choose in a read-only collection; the buttons do not edit it.
    public override void ShowState(bool readOnly, bool enabled)
    {
        Frame.IsEnabled = enabled;
        Editable = !readOnly;
        ShowButtons();
    }

    private static Button Command(object content, string name)
    {
        // One height for all four: an arrow is lower than a line of text.
        var button = new Button
        {
            Content = content,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        ToolTip.SetTip(button, name);
        AutomationProperties.SetName(button, name);
        return button;
    }

    // A small triangle in the button's text color, drawn so it does not depend on the font.
    private static PathIcon Arrow(string data)
    {
        return new PathIcon { Data = StreamGeometry.Parse(data), Width = 10, Height = 6 };
    }

    // An operation of the list, and then the line chosen in sight, which the user is working on.
    private void Operate(Action operation)
    {
        if (Row.Write(operation) && Collection.SelectedIndex >= 0)
            Lines.ScrollIntoView(Collection.SelectedIndex);
    }

    private void ShowButtons()
    {
        var index = Collection.SelectedIndex;
        var count = Collection.Items.Count;

        AddButton.IsEnabled = Editable;
        RemoveButton.IsEnabled = Editable && index >= 0;
        UpButton.IsEnabled = Editable && index > 0;
        DownButton.IsEnabled = Editable && index >= 0 && index < count - 1;
    }

    private void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (Showing)
            return;

        var index = Lines.SelectedIndex;
        Row.Write(() => Collection.SelectedIndex = index);
        ShowButtons();
    }
}
