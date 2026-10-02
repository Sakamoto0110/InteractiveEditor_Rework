using System.Windows;
using System.Windows.Controls;

namespace InteractiveEditor.Views.Wpf;

// A combo box for choosing (P7.9). The list comes from GetChoices() whenever the box opens or takes the
// focus (P6.3), so it can change with the objects, and the choice is written at once (P2.12). Closed and
// out of focus, the box holds what it showed last.
internal sealed class ChoiceEditor : WpfEditor
{
    private readonly ComboBox Box = new();

    // Set while the view fills or selects, so only the user's choice writes.
    private bool Showing;

    public ChoiceEditor(WpfRow row) : base(row)
    {
        Box.DropDownOpened += (_, _) => Row.Try(List);
        // The focus that reaches the items of an open list bubbles up here too; only the box's own counts.
        Box.GotKeyboardFocus += (_, e) =>
        {
            if (e.NewFocus == Box)
                Row.Try(List);
        };
        Box.SelectionChanged += OnSelected;
    }

    public override FrameworkElement Control => Box;

    // Objects that hold different values leave nothing chosen (P7.19); a choice goes to all of them.
    public override void ShowValue()
    {
        Showing = true;

        try
        {
            if (Row.Mixed)
            {
                Box.SelectedIndex = -1;
                return;
            }

            var value = Node.ViewValue;
            var index = IndexOf(value);

            if (index < 0)
            {
                Box.Items.Clear();

                if (value != null)
                    index = Box.Items.Add(new Choice(value, Format(value)));
            }

            Box.SelectedIndex = index;
        }
        finally
        {
            Showing = false;
        }
    }

    private void List()
    {
        Showing = true;

        try
        {
            Box.Items.Clear();

            foreach (var choice in Node.GetChoices())
                Box.Items.Add(new Choice(choice, Format(choice)));
        }
        finally
        {
            Showing = false;
        }

        ShowValue();
    }

    // The value itself, or one that shows the same (the text "2" for the number 2).
    private int IndexOf(object? value)
    {
        var items = Box.Items.Cast<Choice>().ToList();
        var index = items.FindIndex(choice => Equals(choice.Value, value));
        return index >= 0 || value == null ? index : items.FindIndex(choice => choice.Text == Format(value));
    }

    private void OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (Showing || Box.SelectedItem is not Choice choice)
            return;

        Row.Write(() => Node.SetValue(choice.Value));
        Row.Show();
    }

    private sealed record Choice(object? Value, string Text)
    {
        public override string ToString() => Text;
    }
}
