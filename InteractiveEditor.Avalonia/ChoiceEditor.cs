using Avalonia.Controls;
using Avalonia.Layout;

namespace InteractiveEditor.Avalonia;

// A combo box for choosing (P7.9): the values of an enum, or what the node's Choices give. The list comes
// from GetChoices() again whenever the box opens (P6.3), so it can change with the objects, and the choice
// is written at once (P2.12).
internal sealed class ChoiceEditor : AvaloniaEditor
{
    private readonly ComboBox Box = new() { HorizontalAlignment = HorizontalAlignment.Stretch };

    // Set while the view fills or selects, so only the user's choice writes.
    private bool Showing;

    public ChoiceEditor(AvaloniaRow row) : base(row)
    {
        Box.DropDownOpened += (_, _) => Row.Try(List);
        Box.SelectionChanged += OnSelected;
    }

    public override Control Control => Box;

    // Objects that hold different values leave nothing chosen, with a grey dash (P7.19); a choice goes to
    // all of them. A value the list does not have is listed again, and added when it still is not there.
    public override void ShowValue()
    {
        Showing = true;

        try
        {
            var mixed = Row.Mixed;
            Box.PlaceholderText = mixed ? "—" : null;

            if (mixed)
            {
                Box.SelectedIndex = -1;
                return;
            }

            var value = Node.ViewValue;
            var index = IndexOf(value);

            if (index < 0)
            {
                Fill();
                index = IndexOf(value);
            }

            if (index < 0 && value != null)
            {
                Box.Items.Add(new Choice(value, Format(value)));
                index = Box.Items.Count - 1;
            }

            Box.SelectedIndex = index;
        }
        finally
        {
            Showing = false;
        }
    }

    // The list as the node gives it now, with the value shown chosen again.
    private void List()
    {
        Showing = true;

        try
        {
            Fill();
        }
        finally
        {
            Showing = false;
        }

        ShowValue();
    }

    // Only a list that changed is filled again, so an open list keeps its items.
    private void Fill()
    {
        var choices = Node.GetChoices().Select(choice => new Choice(choice, Format(choice))).ToList();

        if (Box.Items.Cast<Choice>().SequenceEqual(choices))
            return;

        Box.Items.Clear();

        foreach (var choice in choices)
            Box.Items.Add(choice);
    }

    // The value itself, or one that shows the same (the text "2" for the number 2).
    private int IndexOf(object? value)
    {
        var items = Box.Items.Cast<Choice>().ToList();
        var index = items.FindIndex(choice => Equals(choice.Value, value));
        return index >= 0 || value == null ? index : items.FindIndex(choice => choice.Text == Format(value));
    }

    private void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (Showing || Box.SelectedItem is not Choice choice)
            return;

        Row.Write(() => Node.SetValue(choice.Value));
        Row.Show();
    }

    // An item of the list: the value, and the text the box shows for it.
    private sealed record Choice(object? Value, string Text)
    {
        public override string ToString() => Text;
    }
}
