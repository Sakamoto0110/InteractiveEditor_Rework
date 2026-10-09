using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Options;
using InteractiveEditor.Views;

namespace InteractiveEditor.Avalonia;

// One row of the property grid: the label and the editor of a node side by side, the label in a column of
// its own. A group is an expander instead, with the label and the type in its header and the rows of the
// group in a panel inside, which opens and closes with the node's Collapsed (P7.10); a collection's
// selector or list goes on top of them. The label goes italic when the objects hold different values
// (P7.19), and its tooltip has the node's tooltip, its long help, and its type and path. What fails while
// the row shows its objects, and what the core refuses as a mistake, stays in the row (P7.11, 3.11).
internal sealed class AvaloniaRow : IDisposable
{
    // The column of the labels, the same at every level, as the property grid had it.
    private const double LabelWidth = 160;

    private readonly TextBlock Label = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    // The message of a failure under an editor that does not show one itself (a check box, a button, a
    // display, a list), in the theme's color for errors.
    private readonly TextBlock Message = new() { TextWrapping = TextWrapping.Wrap, IsVisible = false };

    private readonly Expander? Group;
    private readonly AvaloniaEditor? Editor;

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

    public AvaloniaRow(InspectorNode node)
    {
        Node = node;
        Kind = ViewRules.KindFor(node);
        IsGroup = node.IsGroup;
        MadeFor = node.ValueType;
        Editor = AvaloniaEditor.For(this, Kind);

        // The node's path names the controls for UI automation and tests, as in the WPF view (P7.16); the
        // Name and the Tag stay free for whoever uses the library.
        AutomationProperties.SetAutomationId(Label, node.Path + "#label");

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
                Header = Label,
                Content = Editor == null ? Panel : new StackPanel { Spacing = 4, Children = { Editor.Control, Message, Panel } },
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            Group.PropertyChanged += OnGroupChanged;
            Control = Group;
            return;
        }

        var line = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(LabelWidth, GridUnitType.Pixel),
                new ColumnDefinition(1, GridUnitType.Star),
            },
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };

        // A header (the group of a type with no members) is its bold label, and a separator its line,
        // each across the whole row.
        if (Kind == EditorKind.Header)
        {
            Label.FontWeight = FontWeight.Bold;
            Grid.SetColumnSpan(Label, 2);
        }

        if (Kind != EditorKind.Separator)
            line.Children.Add(Label);

        if (Editor != null)
        {
            if (Kind == EditorKind.Separator)
                Grid.SetColumnSpan(Editor.Control, 2);
            else
                Grid.SetColumn(Editor.Control, 1);

            line.Children.Add(Editor.Control);

            if (!Editor.ShowsErrors)
            {
                Grid.SetRow(Message, 1);
                Grid.SetColumn(Message, 1);
                line.Children.Add(Message);
            }
        }

        Control = line;
    }

    public InspectorNode Node { get; }

    // What the row puts in the panel it sits in: the line of a leaf, or the expander of a group.
    public Control Control { get; }

    // Where the rows of a group go; null for a leaf.
    public StackPanel? Panel { get; }

    // The editor the row was made for, null for none; a node that changes it gets a new row.
    public EditorKind? Kind { get; }

    public bool IsGroup { get; }

    // Whether the row shows its objects as holding different values (P7.19).
    public bool Mixed => ViewRules.ShowsMixed(Kind, Node);

    public bool Fits() => ViewRules.KindFor(Node) == Kind && Node.IsGroup == IsGroup && Node.ValueType == MadeFor;

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
    // values): what throws there shows on the row with the message, as a failure of the node does, and
    // the view goes on (3.11); the row tries again at the next change.
    public void Try(Action show)
    {
        try
        {
            show();
        }
        catch (Exception e)
        {
            Fault = $"This row could not show its objects.\n{e.Message}";
            ShowEditorState();
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

    // The short tooltip, the long help (the view has no (?) mark for it), and the type and the path of
    // the node, which the property grid always showed; a failure goes first.
    private string LabelTip(string? failure)
    {
        var where = Node.ValueType is { } type ? $"{TypeNames.Of(type)}  {Node.Path}" : Node.Path;
        string?[] parts = [failure, Node.Tooltip, Node.Help, where];
        return string.Join("\n\n", parts.Where(part => !string.IsNullOrEmpty(part)));
    }

    private static string? Describe(InspectorFailureEventArgs? failure)
    {
        if (failure == null)
            return null;

        return failure.Message.Contains(failure.Reason) ? failure.Message : $"{failure.Message}\n{failure.Reason}";
    }

    // The user opened or closed the group.
    private void OnGroupChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!Showing && e.Property == Expander.IsExpandedProperty && Group != null)
            Node.Collapsed = !Group.IsExpanded;
    }
}
