using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace InteractiveEditor.Wpf;

// One line per item, with adding, removing and moving them (P7.9, P5.10): a list box over the item
// lines, and the four buttons on the line the layout keeps for them. Choosing a line chooses the item
// shown in the row below; the operations write at once, whatever the binder control (P5.13).
internal sealed class ListEditor : WpfEditor
{
    private readonly Canvas Frame = new() { Background = Brushes.Transparent };
    private readonly ListBox Lines = new();
    private readonly Button AddButton = new() { Content = "+" };
    private readonly Button RemoveButton = new() { Content = "−" };
    private readonly Button UpButton = new();
    private readonly Button DownButton = new();
    private readonly CollectionNode Collection;
    private bool Showing;
    private bool Editable;

    public ListEditor(WpfRow row, CollectionNode collection) : base(row)
    {
        Collection = collection;
        UpButton.Content = Pointer(Arrow.Pointing.Up, UpButton);
        DownButton.Content = Pointer(Arrow.Pointing.Down, DownButton);

        foreach (var (button, id, name) in new[] { (AddButton, "add", "Add"), (RemoveButton, "remove", "Remove"), (UpButton, "up", "Move up"), (DownButton, "down", "Move down") })
        {
            WpfInspectorView.AutomationId(button, id);
            System.Windows.Automation.AutomationProperties.SetName(button, name);
        }

        Frame.Children.Add(Lines);
        Frame.Children.Add(AddButton);
        Frame.Children.Add(RemoveButton);
        Frame.Children.Add(UpButton);
        Frame.Children.Add(DownButton);

        Lines.SelectionChanged += OnSelected;
        AddButton.Click += (_, _) => Row.Write(Collection.AddItem);
        RemoveButton.Click += (_, _) => Row.Write(() => Collection.RemoveItem(Collection.SelectedIndex));
        UpButton.Click += (_, _) => Row.Write(() => Collection.MoveItem(Collection.SelectedIndex, Collection.SelectedIndex - 1));
        DownButton.Click += (_, _) => Row.Write(() => Collection.MoveItem(Collection.SelectedIndex, Collection.SelectedIndex + 1));
    }

    public override FrameworkElement Control => Frame;

    // The item lines on top, the buttons on the last line, a quarter of the width each.
    public override void Place(Rect bounds)
    {
        WpfRow.PlaceAt(Frame, bounds);

        var line = Math.Round(Node.Inspector.Options.RowHeight);
        var top = Math.Max(0, bounds.Height - line);
        WpfRow.PlaceAt(Lines, new Rect(0, 0, bounds.Width, top));

        Button[] buttons = [AddButton, RemoveButton, UpButton, DownButton];

        for (var i = 0; i < buttons.Length; i++)
        {
            var left = Math.Floor(bounds.Width * i / buttons.Length);
            var right = Math.Floor(bounds.Width * (i + 1) / buttons.Length);
            WpfRow.PlaceAt(buttons[i], new Rect(left, top, right - left, bounds.Height - top));
        }
    }

    public override void ShowValue()
    {
        var texts = Collection.Items.Select((item, index) => ItemText(index, item)).ToArray();
        Showing = true;

        try
        {
            if (!Lines.Items.Cast<string>().SequenceEqual(texts))
            {
                Lines.Items.Clear();

                foreach (var text in texts)
                    Lines.Items.Add(text);
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
    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Frame.IsEnabled = enabled;
        Editable = !readOnly;
        ShowButtons();

        if (failed)
            Lines.Background = FailedBrush;
        else
            Lines.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
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

    // An arrow drawn in the button's text color, grey while the button is disabled.
    private static Polygon Pointer(Arrow.Pointing pointing, Button button)
    {
        var polygon = new Polygon { Points = Arrow.Points(pointing, 10) };
        polygon.SetBinding(Shape.FillProperty, new System.Windows.Data.Binding(nameof(Button.Foreground)) { Source = button });
        return polygon;
    }

    private void OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (Showing)
            return;

        var index = Lines.SelectedIndex;
        Row.Write(() => Collection.SelectedIndex = index);
        ShowButtons();
    }
}
