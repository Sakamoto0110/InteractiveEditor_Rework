using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Options;
using InteractiveEditor.Views;

namespace InteractiveEditor.Avalonia;

// One row of the property grid: the label and the editor of a node side by side, the label in a column of
// its own. A group is an expander instead, with the label and the type in its header and the rows of the
// group in a panel inside, which opens and closes with the node's Collapsed (P7.10); a collection's
// selector or list goes on top of them. A number's label can be dragged to scrub it (P7.18). The label
// goes italic when the objects hold different values (P7.19), and its tooltip has the node's tooltip and its type and path. A node with Help has the help
// mark, (?), right before its editor, or right after a group's label, which opens the long help (P7.15).
// What fails while the row shows its objects, and what the core refuses as a mistake, stays in the row
// (P7.11, 3.11).
internal sealed class AvaloniaRow : IDisposable
{
    // The column of the labels, the same at every level, as the property grid had it.
    private const double LabelWidth = 160;

    // The cursors of a label that scrubs, across or up and down.
    private static readonly Cursor Across = new(StandardCursorType.SizeWestEast);
    private static readonly Cursor UpAndDown = new(StandardCursorType.SizeNorthSouth);

    private readonly TextBlock Label = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    // The message of a failure under an editor that does not show one itself (a check box, a button, a
    // display, a list), in the theme's color for errors.
    private readonly TextBlock Message = new() { TextWrapping = TextWrapping.Wrap, IsVisible = false };

    private readonly AvaloniaInspectorView View;
    private readonly HelpMark Mark = new();

    // The column of the help marks, between the label and the editor of a leaf.
    private readonly ColumnDefinition? Marks;

    private readonly Expander? Group;
    private readonly AvaloniaEditor? Editor;
    private readonly LabelScrub? Scrub;

    // The type the row was made for: a node of an inspector with no type takes another at the bind.
    private readonly Type? MadeFor;

    // What the core refused as a mistake at the last write, until a write goes through.
    private string? Error;

    // What the row could not show (a ToString or an Equals of the objects that throws), until it shows
    // whole again.
    private string? Fault;

    // Set while the row opens or closes the expander as the node says, so only the user's click changes
    // the node.
    private bool Showing;

    public AvaloniaRow(AvaloniaInspectorView view, InspectorNode node)
    {
        View = view;
        Node = node;
        Kind = ViewRules.KindFor(node);
        IsGroup = node.IsGroup;
        MadeFor = node.ValueType;
        Editor = AvaloniaEditor.For(this, Kind);

        // The node's path names the controls for UI automation and tests, as in the WPF view (P7.16); the
        // Name and the Tag stay free for whoever uses the library.
        AutomationProperties.SetAutomationId(Label, node.Path + "#label");
        AutomationProperties.SetAutomationId(Mark, node.Path + "#help");
        Mark.Clicked += ShowHelp;

        if (Editor != null)
        {
            AutomationProperties.SetAutomationId(Editor.Control, node.Path);
            ToolTip.SetShowOnDisabled(Editor.Control, true);
            Message.Bind(TextBlock.ForegroundProperty, Message.GetResourceObservable("SystemControlErrorTextForegroundBrush"));
        }

        if (IsGroup)
        {
            Label.FontWeight = Kind == EditorKind.Header ? FontWeight.Bold : FontWeight.SemiBold;
            Panel = new StackPanel { Spacing = 4 };
            AutomationProperties.SetAutomationId(Panel, node.Path + "#panel");

            Group = new Expander
            {
                Header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { Label, Mark } },
                Content = Editor == null ? Panel : new StackPanel { Spacing = 4, Children = { Editor.Control, Message, Panel } },
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            Group.PropertyChanged += OnGroupChanged;
            Control = Group;
            return;
        }

        Marks = new ColumnDefinition(0, GridUnitType.Pixel);

        var line = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(LabelWidth, GridUnitType.Pixel),
                Marks,
                new ColumnDefinition(1, GridUnitType.Star),
            },
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };

        // A header (the group of a type with no members) is its bold label, and a separator its line,
        // each across the whole row.
        if (Kind == EditorKind.Header)
        {
            Label.FontWeight = FontWeight.Bold;
            Grid.SetColumnSpan(Label, 3);
        }

        if (Kind != EditorKind.Separator)
            line.Children.Add(Label);

        // The label's column takes the mouse in all of it, for the drag of scrubbing.
        if (Kind is not (null or EditorKind.Header or EditorKind.Separator))
        {
            Label.Background = Brushes.Transparent;
            Scrub = new LabelScrub(this, Label, view);
        }

        if (Editor != null)
        {
            if (Kind == EditorKind.Separator)
                Grid.SetColumnSpan(Editor.Control, 3);
            else
                Grid.SetColumn(Editor.Control, 2);

            line.Children.Add(Editor.Control);

            if (!Editor.ShowsErrors)
            {
                Grid.SetRow(Message, 1);
                Grid.SetColumn(Message, 2);
                line.Children.Add(Message);
            }
        }

        if (Kind is not (EditorKind.Header or EditorKind.Separator))
        {
            Mark.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(Mark, 1);
            line.Children.Add(Mark);
        }

        Control = line;
    }

    public InspectorNode Node { get; }

    // The controls the row made, for the escape valves (P7.4).
    public Control LabelControl => Label;

    public Control HelpControl => Mark;

    public Control? EditorControl => Editor?.Control;

    // What the row puts in the panel it sits in: the line of a leaf, or the expander of a group.
    public Control Control { get; }

    // Where the rows of a group go; null for a leaf.
    public StackPanel? Panel { get; }

    // The editor the row was made for, null for none; a node that changes it gets a new row.
    public EditorKind? Kind { get; }

    public bool IsGroup { get; }

    // Whether the label scrubs the number now (P7.17).
    public bool Scrubs => Scrub != null && ViewRules.CanScrub(Node);

    // Whether the row shows its objects as holding different values (P7.19).
    public bool Mixed => ViewRules.ShowsMixed(Kind, Node);

    public bool Fits() => ViewRules.KindFor(Node) == Kind && Node.IsGroup == IsGroup && Node.ValueType == MadeFor;

    // The column of the help marks (P7.15): on every row when any node of the tree has Help, so the
    // editors line up, and as wide as the layout step makes it; with no Help anywhere, it is not there.
    public void ShowMarks(bool any)
    {
        if (Marks == null)
            return;

        var options = Node.Inspector.Options;
        Marks.Width = new GridLength(any ? options.HelpWidth + options.LabelSpacing : 0, GridUnitType.Pixel);
        Mark.Width = options.HelpWidth;
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
    // values): what throws there shows on the row with the message, as a failure of the node does, and
    // the view goes on (3.11); the row tries again at the next change.
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

    // Writes through the node: a value, a press, an operation of the list. What the core refuses as a
    // mistake (a read-only node, a value of the wrong type, a disabled branch) shows on the row instead of
    // bringing the view down.
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

    // What the editor itself refused before anything reached the node (a text that is not a color), shown
    // as a refused write is.
    public void Refuse(string message)
    {
        Error = message;
        ShowState();
    }

    public void Dispose()
    {
        Scrub?.Dispose();

        if (Group != null)
            Group.PropertyChanged -= OnGroupChanged;

        AvaloniaInspectorView.Detach(Control);
        Panel?.Children.Clear();
    }

    // The label: the node's, with the type in a group's header, and italic when the objects hold
    // different values.
    private void ShowLabel()
    {
        if (!IsGroup)
        {
            Label.Text = Node.Label;

            if (Kind != EditorKind.Header)
                Label.FontStyle = Mixed ? FontStyle.Italic : FontStyle.Normal;

            if (Scrub != null)
                Label.Cursor = !Scrubs ? null : Node.ScrubAxis == ScrubAxis.Vertical ? UpAndDown : Across;

            return;
        }

        var text = Kind != EditorKind.Header && Node.ValueType is { } type ? $"{Node.Label} : {TypeNames.Of(type)}" : Node.Label;

        // A collection shows its failure under its selector or its list instead.
        if (Editor == null && Node.Failure is { } failure)
            text += $" <error: {failure.Reason}>";
        else if (Kind != EditorKind.Header && Node.Inspector.Instances.Count > 0 && !Node.IsCompromised && Node.ViewValue == null)
            text += " = null";

        Label.Text = text;
    }

    // The tooltips and the state of the editor: read-only, disabled in a branch whose group was replaced
    // (P3.4), and the error under it and in its tooltip after a failure (P7.11), the row's own included.
    // A row with no editor shows its failure in the label's tooltip, and a group's in its header.
    private void ShowEditorState()
    {
        Mark.IsVisible = Kind is not (EditorKind.Header or EditorKind.Separator) && !string.IsNullOrEmpty(Node.Help);

        var failure = Error ?? Fault ?? Describe(Node.Failure);
        ToolTip.SetTip(Label, LabelTip(Editor == null ? failure : null));

        if (Group != null)
        {
            Showing = true;

            try
            {
                Group.IsExpanded = !Node.Collapsed;
            }
            finally
            {
                Showing = false;
            }
        }

        if (Editor == null)
            return;

        Editor.ShowState(Node.ReadOnly, !Node.IsCompromised);
        ToolTip.SetTip(Editor.Control, failure ?? Node.Tooltip);

        if (!Editor.ShowsErrors)
        {
            Message.Text = failure;
            Message.IsVisible = failure != null;
        }
        else if (failure == null)
        {
            DataValidationErrors.ClearErrors(Editor.ErrorTarget);
        }
        else
        {
            DataValidationErrors.SetErrors(Editor.ErrorTarget, [failure]);
        }
    }

    // The short tooltip and the type and the path of the node, which the property grid always showed; a
    // failure goes first. The long help is the (?) mark's.
    private string LabelTip(string? failure)
    {
        var where = Node.ValueType is { } type ? $"{TypeNames.Of(type)}  {Node.Path}" : Node.Path;
        string?[] parts = [failure, Node.Tooltip, where];
        return string.Join("\n\n", parts.Where(part => !string.IsNullOrEmpty(part)));
    }

    private static string? Describe(InspectorFailureEventArgs? failure)
    {
        if (failure == null)
            return null;

        return failure.Message.Contains(failure.Reason) ? failure.Message : $"{failure.Message}\n{failure.Reason}";
    }

    // The long help over the window the view is in, which it blocks until it closes; in a top level that
    // is not a window, the help opens on its own.
    private void ShowHelp(object? sender, EventArgs e)
    {
        var dialog = new HelpDialog(Node.Label, Node.Help ?? string.Empty);

        if (TopLevel.GetTopLevel(Mark) is Window owner)
            _ = dialog.ShowDialog(owner);
        else
            dialog.Show();
    }

    // The user opened or closed the group.
    private void OnGroupChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!Showing && e.Property == Expander.IsExpandedProperty && Group != null)
            Node.Collapsed = !Group.IsExpanded;
    }
}
