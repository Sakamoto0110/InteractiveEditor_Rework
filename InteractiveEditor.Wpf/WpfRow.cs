using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Layout;
using InteractiveEditor.Options;
using InteractiveEditor.Views;

namespace InteractiveEditor.Wpf;

// One row of the view: the label, the help mark and the editor of a node, on the rectangles the layout
// gives them. A group's label carries the arrow that collapses it (P7.10), and a number's label can be
// dragged to scrub it (P7.18); the help mark, (?), opens the long help (P7.15). The label goes italic
// when the objects hold different values (P7.19). The label and the mark let the row's background
// through, so the light one of the row under the mouse shows behind them (P7.14). What fails while the
// row shows its objects stays in the row (3.11).
internal sealed class WpfRow : IDisposable
{
    private const double ArrowWidth = 14;

    private readonly WpfInspectorView View;

    // The label's area takes the mouse in all of it, for the click of a group and the drag of scrubbing;
    // its text is cut with an ellipsis.
    private readonly Border Label;
    private readonly TextBlock Text;
    private readonly Polygon? Pointer;
    private readonly HelpMark Mark;
    private readonly WpfEditor? Editor;
    private readonly LabelScrub? Scrub;

    // What the core refused as a mistake at the last write, until a write goes through.
    private string? Error;

    // What the row could not show (a ToString or an Equals of the objects that throws), until it shows
    // whole again.
    private string? Fault;

    public WpfRow(WpfInspectorView view, InspectorNode node)
    {
        View = view;
        Node = node;
        Kind = ViewRules.KindFor(node);
        IsGroup = node.IsGroup;

        Text = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        var inside = new Grid();
        inside.Children.Add(Text);

        Label = new Border
        {
            Background = Brushes.Transparent,
            Child = inside,
            Visibility = Kind == EditorKind.Separator ? Visibility.Collapsed : Visibility.Visible,
        };
        WpfInspectorView.AutomationId(Label, node.Path + "#label");

        Mark = new HelpMark();
        WpfInspectorView.AutomationId(Mark, node.Path + "#help");
        Mark.Clicked += ShowHelp;

        if (Kind == EditorKind.Header)
            Text.FontWeight = FontWeights.Bold;

        if (IsGroup)
        {
            Pointer = new Polygon
            {
                Width = ArrowWidth,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            inside.Children.Add(Pointer);
            Text.Margin = new Thickness(ArrowWidth, 0, 0, 0);
            Label.Cursor = Cursors.Hand;
            Label.MouseLeftButtonUp += ToggleCollapsed;
        }
        else if (Kind is not (null or EditorKind.Header or EditorKind.Separator))
        {
            Scrub = new LabelScrub(this, Label, view);
        }

        Editor = WpfEditor.For(this, Kind);

        if (Editor != null)
        {
            WpfInspectorView.AutomationId(Editor.Control, node.Path);
            ToolTipService.SetShowOnDisabled(Editor.Control, true);
        }
    }

    public InspectorNode Node { get; }

    // The controls the row made, for the escape valves (P7.4).
    public FrameworkElement LabelControl => Label;

    public FrameworkElement HelpControl => Mark;

    public FrameworkElement? EditorControl => Editor?.Control;

    // Where the row is: the canvas it sits in, and its rectangle there, the one the background of the
    // row under the mouse fills.
    public Canvas? Container { get; private set; }

    public Rect Bounds { get; private set; }

    // The editor the row was made for, null for none; a node that changes it gets a new row.
    public EditorKind? Kind { get; }

    public bool IsGroup { get; }

    // Whether the label scrubs the number now (P7.17).
    public bool Scrubs => Scrub != null && ViewRules.CanScrub(Node);

    // Whether the row shows its objects as holding different values (P7.19).
    public bool Mixed => ViewRules.ShowsMixed(Kind, Node);

    public bool Fits() => ViewRules.KindFor(Node) == Kind && Node.IsGroup == IsGroup;

    public void Place(Canvas container, LayoutRow layout, ref int tab)
    {
        Container = container;
        Bounds = layout.Row.ToWpf();

        WpfInspectorView.Move(Label, container);
        PlaceAt(Label, layout.Label.ToWpf());

        WpfInspectorView.Move(Mark, container);
        PlaceAt(Mark, layout.Help.ToWpf());
        Mark.Visibility = layout.Help.Width > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (Editor == null)
            return;

        foreach (var element in Editor.Elements)
            WpfInspectorView.Move(element, container);

        Editor.Place((Kind == EditorKind.Separator ? layout.Row : layout.Editor).ToWpf());
        KeyboardNavigation.SetTabIndex(Editor.Control, tab++);
    }

    public static void PlaceAt(FrameworkElement element, Rect bounds)
    {
        Canvas.SetLeft(element, bounds.X);
        Canvas.SetTop(element, bounds.Y);
        element.Width = bounds.Width;
        element.Height = bounds.Height;
    }

    // The value and the state; a row that shows whole again leaves its fault behind.
    public void Show()
    {
        Try(() =>
        {
            Editor?.ShowValue();
            ShowLabel();
            Fault = null;
        });

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
    public void Try(Action show)
    {
        try
        {
            show();
        }
        catch (Exception e)
        {
            if (Fault == null)
                View.OnRowFailed(Node, FailureSeverity.WorkedAround, e, $"The row of '{Node.Path}' could not show its objects; it keeps what it showed.");

            Fault = $"This row could not show its objects.\n{e.Message}";
            ShowEditorState();
        }
    }

    // The row under the mouse, or not: the container paints the background behind the controls.
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
        WpfInspectorView.Detach(Label);
        WpfInspectorView.Detach(Mark);

        if (Editor != null)
        {
            foreach (var element in Editor.Elements)
                WpfInspectorView.Detach(element);
        }
    }

    // The label: its text, the arrow of a group, the cursor of scrubbing, the italic of mixed values.
    private void ShowLabel()
    {
        Text.Text = Node.Label;

        if (Pointer != null)
        {
            Pointer.Points = Arrow.Points(Node.Collapsed ? Arrow.Pointing.Right : Arrow.Pointing.Down, ArrowWidth);
            Pointer.Fill = Label.IsEnabled ? Text.Foreground : SystemColors.GrayTextBrush;
        }
        else if (Kind is not (null or EditorKind.Header or EditorKind.Separator))
        {
            Label.Cursor = !Scrubs ? null : Node.ScrubAxis == ScrubAxis.Vertical ? Cursors.SizeNS : Cursors.SizeWE;
        }

        if (Kind != EditorKind.Header)
            Text.FontStyle = Mixed ? FontStyles.Italic : FontStyles.Normal;
    }

    // The tooltips and the state of the editor: read-only, disabled in a branch whose group was replaced
    // (P3.4), light red with the message in its tooltip after a failure (P7.11), the row's own included.
    // A row with no editor shows its fault on the label.
    private void ShowEditorState()
    {
        Label.ToolTip = Editor == null ? Fault ?? Node.Tooltip : Node.Tooltip;

        if (Editor == null)
            return;

        var failure = Error ?? Fault ?? Describe(Node.Failure);
        Editor.ShowState(Node.ReadOnly, !Node.IsCompromised, failure != null);
        Editor.Control.ToolTip = failure ?? Node.Tooltip;
    }

    private static string? Describe(InspectorFailureEventArgs? failure)
    {
        if (failure == null)
            return null;

        return failure.Message.Contains(failure.Reason) ? failure.Message : $"{failure.Message}\n{failure.Reason}";
    }

    private void ShowHelp(object? sender, EventArgs e)
    {
        var dialog = new HelpDialog(Node.Label, Node.Help ?? string.Empty) { Owner = Window.GetWindow(Label) };
        dialog.ShowDialog();
    }

    private void ToggleCollapsed(object sender, MouseButtonEventArgs e)
    {
        Node.Collapsed = !Node.Collapsed;
    }
}
