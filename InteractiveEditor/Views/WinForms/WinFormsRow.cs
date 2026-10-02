using System.Drawing;
using System.Windows.Forms;
using InteractiveEditor.Layout;
using InteractiveEditor.Options;

namespace InteractiveEditor.Views.WinForms;

// One row of the view: the label, the help mark and the editor of a node, on the rectangles the layout
// gives them. A group's label carries the arrow that collapses it (P7.10); the help mark, (?), opens
// the long help (P7.15). The label and the mark let the row's background through, so the light one of
// the row under the mouse shows behind them (P7.14).
internal sealed class WinFormsRow : IDisposable
{
    private const int ArrowWidth = 14;

    private readonly WinFormsInspectorView View;
    private readonly Label Label;
    private readonly HelpMark Mark;
    private readonly Font? Bold;
    private readonly WinFormsEditor? Editor;

    // What the core refused as a mistake at the last write, until a write goes through.
    private string? Error;

    public WinFormsRow(WinFormsInspectorView view, InspectorNode node)
    {
        View = view;
        Node = node;
        Kind = KindFor(node);
        IsGroup = node.IsGroup;

        Label = new Label
        {
            Name = node.Path + "#label",
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
            Visible = Kind != EditorKind.Separator,
            BackColor = Color.Transparent,
        };

        Mark = new HelpMark { Name = node.Path + "#help", Visible = false };
        Mark.Click += ShowHelp;

        if (Kind == EditorKind.Header)
        {
            Bold = new Font(Control.DefaultFont, FontStyle.Bold);
            Label.Font = Bold;
        }

        if (IsGroup)
        {
            Label.Padding = new Padding(ArrowWidth, 0, 0, 0);
            Label.Cursor = Cursors.Hand;
            Label.Click += ToggleCollapsed;
            Label.Paint += PaintArrow;
        }

        Editor = WinFormsEditor.For(this, Kind);

        if (Editor != null)
            Editor.Control.Name = node.Path;

        view.Follow(Label);
        view.Follow(Mark);

        if (Editor != null)
            view.Follow(Editor.Control);
    }

    public InspectorNode Node { get; }

    // Where the row is: the control it sits in, and its rectangle there, the one the background of the
    // row under the mouse fills.
    public Control? Container { get; private set; }

    public Rectangle Bounds { get; private set; }

    // The editor the row was made for, null for none; a node that changes it gets a new row.
    public EditorKind? Kind { get; }

    public bool IsGroup { get; }

    // The editor a row makes for a node (P7.9): none for a group, unless it is a collection (its
    // selector or its list); a slider with no range is a number, and an editor still to be chosen
    // (an inspector with no type, before the bind) is text.
    public static EditorKind? KindFor(InspectorNode node)
    {
        if (node.Editor == EditorKind.Header)
            return EditorKind.Header;

        if (node.IsGroup && node.Editor is not (EditorKind.Selector or EditorKind.List))
            return null;

        return node.Editor switch
        {
            EditorKind.Slider when node.Range == null => EditorKind.Number,
            EditorKind.Auto => EditorKind.Text,
            var kind => kind,
        };
    }

    public bool Fits() => KindFor(Node) == Kind && Node.IsGroup == IsGroup;

    public void Place(Control container, LayoutRow layout, ref int tab)
    {
        Container = container;
        Bounds = (Rectangle)layout.Row;

        Move(Label, container);
        Label.Bounds = (Rectangle)layout.Label;

        Move(Mark, container);
        Mark.Bounds = (Rectangle)layout.Help;
        Mark.Visible = layout.Help.Width > 0;

        if (Editor == null)
            return;

        Move(Editor.Control, container);
        Editor.Place((Rectangle)(Kind == EditorKind.Separator ? layout.Row : layout.Editor));
        Editor.Control.TabIndex = tab++;
    }

    public void ShowValue()
    {
        Editor?.ShowValue();
    }

    // The row under the mouse, or not: the container paints the background, and an editor that cannot
    // let it through (a track bar) takes it.
    public void ShowHover(bool hovered)
    {
        Editor?.ShowHover(hovered);
    }

    // The label, the tooltips, and the state of the editor: read-only, disabled in a branch whose group
    // was replaced (P3.4), light red with the message in its tooltip after a failure (P7.11).
    public void ShowState()
    {
        Label.Text = Node.Label;
        View.Tips.SetToolTip(Label, Node.Tooltip);

        if (IsGroup)
            Label.Invalidate();

        if (Editor == null)
            return;

        var failure = Error ?? Describe(Node.Failure);
        Editor.ShowState(Node.ReadOnly, !Node.IsCompromised, failure != null);
        View.Tips.SetToolTip(Editor.Control, failure ?? Node.Tooltip);
    }

    // Writes through the node: a value, a press, an operation of the list. What the core refuses as a
    // mistake (a read-only node, a disabled branch) shows on the row instead of bringing the view down;
    // the premise of errors in the views comes whole in the fourth cut.
    public bool Write(Action write)
    {
        try
        {
            write();
            Error = null;
            return true;
        }
        catch (Exception e)
        {
            Error = e.Message;
            return false;
        }
        finally
        {
            ShowState();
        }
    }

    public void Dispose()
    {
        Label.Dispose();
        Mark.Dispose();
        Bold?.Dispose();
        Editor?.Dispose();
    }

    private static string? Describe(Diagnostics.InspectorFailureEventArgs? failure)
    {
        if (failure == null)
            return null;

        return failure.Message.Contains(failure.Reason) ? failure.Message : $"{failure.Message}\n{failure.Reason}";
    }

    private static void Move(Control control, Control container)
    {
        if (control.Parent != container)
            container.Controls.Add(control);
    }

    private void ShowHelp(object? sender, EventArgs e)
    {
        using var dialog = new HelpDialog(Node.Label, Node.Help ?? string.Empty);

        if (View.FindForm() is { } owner)
            dialog.ShowDialog(owner);
        else
            dialog.ShowDialog();
    }

    private void ToggleCollapsed(object? sender, EventArgs e)
    {
        Node.Collapsed = !Node.Collapsed;
    }

    // Pointing right when the group is collapsed, down when it is open.
    private void PaintArrow(object? sender, PaintEventArgs e)
    {
        Arrow.Paint(e.Graphics, new Rectangle(0, 0, ArrowWidth, Label.Height),
            Node.Collapsed ? ArrowDirection.Right : ArrowDirection.Down,
            Label.Enabled ? Label.ForeColor : SystemColors.GrayText);
    }
}
