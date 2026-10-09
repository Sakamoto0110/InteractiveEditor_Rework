using System.Drawing;
using System.Windows.Forms;

namespace InteractiveEditor.WinForms;

// One line per item, with adding, removing and moving them (P7.9, P5.10): a list box over the item
// lines, and the four buttons on the line the layout keeps for them. Choosing a line chooses the item
// shown in the row below; the operations write at once, whatever the binder control (P5.13).
internal sealed class ListEditor : WinFormsEditor
{
    private readonly Panel Frame = new() { BackColor = Color.Transparent };
    private readonly ListBox Lines = new() { IntegralHeight = false };
    private readonly Button AddButton = new() { Text = "+", Name = "add" };
    private readonly Button RemoveButton = new() { Text = "\u2212", Name = "remove" };
    private readonly Button UpButton = new() { Name = "up", AccessibleName = "Move up" };
    private readonly Button DownButton = new() { Name = "down", AccessibleName = "Move down" };
    private readonly CollectionNode Collection;
    private bool Showing;
    private bool Editable;

    public ListEditor(WinFormsRow row, CollectionNode collection) : base(row)
    {
        Collection = collection;
        Frame.Controls.AddRange([Lines, AddButton, RemoveButton, UpButton, DownButton]);

        Lines.SelectedIndexChanged += OnSelected;
        AddButton.Click += (_, _) => Row.Write(Collection.AddItem);
        RemoveButton.Click += (_, _) => Row.Write(() => Collection.RemoveItem(Collection.SelectedIndex));
        UpButton.Click += (_, _) => Row.Write(() => Collection.MoveItem(Collection.SelectedIndex, Collection.SelectedIndex - 1));
        DownButton.Click += (_, _) => Row.Write(() => Collection.MoveItem(Collection.SelectedIndex, Collection.SelectedIndex + 1));
        UpButton.Paint += (_, e) => PaintArrow(UpButton, e, ArrowDirection.Up);
        DownButton.Paint += (_, e) => PaintArrow(DownButton, e, ArrowDirection.Down);
    }

    public override Control Control => Frame;

    // The item lines on top, the buttons on the last line, a quarter of the width each.
    public override void Place(Rectangle bounds)
    {
        Frame.Bounds = bounds;

        var line = (int)Math.Round(Node.Inspector.Options.RowHeight);
        var top = Math.Max(0, bounds.Height - line);
        Lines.Bounds = new Rectangle(0, 0, bounds.Width, top);

        Button[] buttons = [AddButton, RemoveButton, UpButton, DownButton];

        for (var i = 0; i < buttons.Length; i++)
        {
            var left = bounds.Width * i / buttons.Length;
            var right = bounds.Width * (i + 1) / buttons.Length;
            buttons[i].Bounds = new Rectangle(left, top, right - left, bounds.Height - top);
        }
    }

    public override void ShowValue()
    {
        var texts = Collection.Items.Select((item, index) => ItemText(index, item)).ToArray();

        Showing = true;

        if (!Lines.Items.Cast<string>().SequenceEqual(texts))
        {
            Lines.Items.Clear();
            Lines.Items.AddRange(texts);
        }

        Lines.SelectedIndex = Collection.SelectedIndex;
        Showing = false;

        ShowButtons();
    }

    // The lines still choose in a read-only collection; the buttons do not edit it.
    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Frame.Enabled = enabled;
        Editable = !readOnly;
        ShowButtons();

        if (failed)
            Lines.BackColor = FailedColor;
        else
            Lines.ResetBackColor();
    }

    private void ShowButtons()
    {
        var index = Collection.SelectedIndex;
        var count = Collection.Items.Count;

        AddButton.Enabled = Editable;
        RemoveButton.Enabled = Editable && index >= 0;
        UpButton.Enabled = Editable && index > 0;
        DownButton.Enabled = Editable && index >= 0 && index < count - 1;
    }

    private static void PaintArrow(Button button, PaintEventArgs e, ArrowDirection direction)
    {
        Arrow.Paint(e.Graphics, button.ClientRectangle, direction, button.Enabled ? button.ForeColor : SystemColors.GrayText);
    }

    private void OnSelected(object? sender, EventArgs e)
    {
        if (Showing)
            return;

        var index = Lines.SelectedIndex;
        Row.Write(() => Collection.SelectedIndex = index);
        ShowButtons();
    }
}
