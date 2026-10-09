using System.Drawing;
using System.Windows.Forms;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Layout;
using InteractiveEditor.Options;
using InteractiveEditor.Views;

namespace InteractiveEditor.WinForms;

// One row of the view: the label, the help mark and the editor of a node, on the rectangles the layout
// gives them. A group's label carries the arrow that collapses it (P7.10), and a number's label can be
// dragged to scrub it (P7.18); the help mark, (?), opens the long help (P7.15). The label goes italic
// when the objects hold different values (P7.19). The label and the mark let the row's background
// through, so the light one of the row under the mouse shows behind them (P7.14). What fails while the
// row shows its objects stays in the row (3.11).
internal sealed class WinFormsRow : IDisposable
{
    private const int ArrowWidth = 14;

    private readonly WinFormsInspectorView View;
    private readonly Label Label;
    private readonly HelpMark Mark;
    private readonly Font? Bold;
    private readonly WinFormsEditor? Editor;
    private readonly LabelScrub? Scrub;
    private Font? Italic;

    // What the core refused as a mistake at the last write, until a write goes through.
    private string? Error;

    // What the row could not show (a ToString or an Equals of the objects that throws), until it shows
    // whole again.
    private string? Fault;

    public WinFormsRow(WinFormsInspectorView view, InspectorNode node)
    {
        View = view;
        Node = node;
        Kind = ViewRules.KindFor(node);
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
        else if (Kind is not (null or EditorKind.Header or EditorKind.Separator))
        {
            Scrub = new LabelScrub(this, Label);
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

    // The controls the row made, for the escape valves (P7.4).
    public Control LabelControl => Label;

    public Control HelpControl => Mark;

    public Control? EditorControl => Editor?.Control;

    // Where the row is: the control it sits in, and its rectangle there, the one the background of the
    // row under the mouse fills.
    public Control? Container { get; private set; }

    public Rectangle Bounds { get; private set; }

    // The editor the row was made for, null for none; a node that changes it gets a new row.
    public EditorKind? Kind { get; }

    public bool IsGroup { get; }

    // Whether the label scrubs the number now (P7.17).
    public bool Scrubs => Scrub != null && ViewRules.CanScrub(Node);

    // Whether the row shows its objects as holding different values (P7.19).
    public bool Mixed => ViewRules.ShowsMixed(Kind, Node);

    public bool Fits() => ViewRules.KindFor(Node) == Kind && Node.IsGroup == IsGroup;

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

    // The value and the state; a row that shows whole again leaves its fault behind. The label and the
    // value are shown apart, so an object that cannot be shown does not take the label with it.
    public void Show()
    {
        var whole = Try(ShowLabel);
        whole &= Try(() => Editor?.ShowValue());

        if (whole)
            Fault = null;

        ShowEditorState();
    }

    // The label and the state of the editor, keeping the value shown.
    public void ShowState()
    {
        Try(ShowLabel);
        ShowEditorState();
    }

    // Runs the row's own work with the objects (their ToString in the editor, their Equals for the mixed
    // values): what throws there turns the row light red with the message, as a failure of the node
    // does, and the view goes on (3.11); the row tries again at the next change.
    public bool Try(Action show)
    {
        try
        {
            show();
            return true;
        }
        catch (Exception e)
        {
            if (Fault == null)
                View.OnRowFailed(Node, FailureSeverity.WorkedAround, e, $"The row of '{Node.Path}' could not show its objects; it keeps what it showed.");

            Fault = $"This row could not show its objects.\n{e.Message}";
            ShowEditorState();
            return false;
        }
    }

    // The row under the mouse, or not: the container paints the background, and an editor that cannot
    // let it through (a track bar) takes it.
    public void ShowHover(bool hovered)
    {
        Editor?.ShowHover(hovered);
    }

    // Writes through the node: a value, a press, an operation of the list. What the core refuses as a
    // mistake (a read-only node, a disabled branch) shows on the row instead of bringing the view down.
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
        Scrub?.Dispose();
        Italic?.Dispose();
        Label.Dispose();
        Mark.Dispose();
        Bold?.Dispose();
        Editor?.Dispose();
    }

    // The label: its text, the cursor of scrubbing, the italic of mixed values (P7.19).
    private void ShowLabel()
    {
        Label.Text = Node.Label;

        if (IsGroup)
            Label.Invalidate();
        else
            Label.Cursor = !Scrubs ? Cursors.Default : Node.ScrubAxis == ScrubAxis.Vertical ? Cursors.SizeNS : Cursors.SizeWE;

        ShowMixed();
    }

    // The tooltips and the state of the editor: read-only, disabled in a branch whose group was replaced
    // (P3.4), light red with the message in its tooltip after a failure (P7.11), the row's own included.
    // A row with no editor shows its fault on the label.
    private void ShowEditorState()
    {
        View.Tips.SetToolTip(Label, Editor == null ? Fault ?? Node.Tooltip : Node.Tooltip);

        if (Editor == null)
            return;

        var failure = Error ?? Fault ?? Describe(Node.Failure);
        Editor.ShowState(Node.ReadOnly, !Node.IsCompromised, failure != null);
        View.Tips.SetToolTip(Editor.Control, failure ?? Node.Tooltip);
    }

    private void ShowMixed()
    {
        if (Kind == EditorKind.Header)
            return;

        if (Mixed)
        {
            Italic ??= new Font(Label.Font, FontStyle.Italic);
            Label.Font = Italic;
        }
        else if (Italic != null && Label.Font == Italic)
        {
            Label.ResetFont();
        }
    }

    private static string? Describe(InspectorFailureEventArgs? failure)
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
