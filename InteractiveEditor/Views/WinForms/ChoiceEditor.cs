using System.Windows.Forms;

namespace InteractiveEditor.Views.WinForms;

// A combo box for choosing (P7.9). The list comes from GetChoices() whenever the box opens or takes the
// focus (P6.3), so it can change with the objects, and the choice is written at once (P2.12). Closed and
// out of focus, the box holds what it showed last.
internal sealed class ChoiceEditor : WinFormsEditor
{
    private readonly ComboBox Box = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public ChoiceEditor(WinFormsRow row) : base(row)
    {
        Box.DropDown += (_, _) => List();
        Box.Enter += (_, _) => List();
        Box.SelectionChangeCommitted += OnCommitted;
    }

    public override Control Control => Box;

    // Objects that hold different values leave nothing chosen (P7.19); a choice goes to all of them.
    public override void ShowValue()
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

    private void List()
    {
        Box.Items.Clear();

        foreach (var choice in Node.GetChoices())
            Box.Items.Add(new Choice(choice, Format(choice)));

        ShowValue();
    }

    // The value itself, or one that shows the same (the text "2" for the number 2).
    private int IndexOf(object? value)
    {
        var items = Box.Items.Cast<Choice>().ToList();
        var index = items.FindIndex(choice => Equals(choice.Value, value));
        return index >= 0 || value == null ? index : items.FindIndex(choice => choice.Text == Format(value));
    }

    private void OnCommitted(object? sender, EventArgs e)
    {
        if (Box.SelectedItem is not Choice choice)
            return;

        Row.Write(() => Node.SetValue(choice.Value));
        ShowValue();
    }

    private sealed record Choice(object? Value, string Text)
    {
        public override string ToString() => Text;
    }
}
