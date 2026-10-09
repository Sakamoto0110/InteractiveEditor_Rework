namespace InteractiveEditor.Events;

// A view made a control for a node (P7.4): the escape valve for what the agnostic configuration does
// not cover. The type of the control is the platform's (a WinForms Control, a WPF FrameworkElement),
// so the same args serve every view. The view keeps placing the control, showing the value and the
// state (enabled, the failure's color, the tooltip); the rest is the subscriber's.
public sealed class ControlCreatedEventArgs<TControl>(InspectorNode node, RowPart part, TControl control)
    : InspectorEventArgs(node.Inspector) where TControl : class
{
    public InspectorNode Node { get; } = node;
    public string Path => Node.Path;
    public RowPart Part { get; } = part;
    public TControl Control { get; } = control;
}
