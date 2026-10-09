using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;

namespace InteractiveEditor.Avalonia;

// The Avalonia view of an inspector, a property grid: a row for each node the inspector shows (Rows), the
// label in a column on the left and the editor on the right, and an expander for each group, with the rows
// of the group inside (P7.3). Avalonia's own panels lay the rows out, so the rectangles of the layout step
// are not used. The view receives the inspector and observes it, with no state of its own about the
// objects: a value that changed shows on its row, and whatever changes the rows (an option, the bind, a
// rule of visibility) builds them again. What comes from another thread moves to the UI thread first
// (P7.12). A control has no Dispose, so the view has one of its own: disposing it lets the inspector go and
// leaves it alone; when the inspector is disposed, the view empties (P7.13). The escape valves tell what
// the view made for each node (P7.4), and RowFailed what failed in the view itself (3.11).
public sealed class AvaloniaInspectorView : UserControl, IDisposable
{
    // The rows at the top; the rows of a group go in the group's own panel. The margin keeps the overlay
    // scroll bar off the spinner buttons of the editors.
    private readonly StackPanel Area = new() { Spacing = 4, Margin = new Thickness(0, 0, 16, 0) };
    private readonly Dictionary<InspectorNode, AvaloniaRow> RowsByNode = [];
    private readonly HashSet<InspectorNode> Watched = [];
    private bool BuildPending;
    private bool IsDisposed;

    internal AvaloniaInspectorView(Inspector inspector)
    {
        Inspector = inspector;
        Content = new ScrollViewer { Content = Area };

        inspector.OptionChanged += OnOptionChanged;
        inspector.BindRegistered += OnBindChanged;
        inspector.BindRemoved += OnBindChanged;
        inspector.Unbound += OnBindChanged;
        inspector.ForcedApply += OnBindChanged;
        inspector.ForcedReload += OnBindChanged;
        inspector.ForcedClear += OnBindChanged;
        inspector.Disposed += OnInspectorDisposed;

        // The rows are made when the view is loaded, as in the WPF view, so whoever subscribes to the
        // valves right after CreateAvaloniaView sees every control.
        BuildPending = true;
    }

    // The escape valves (P7.4): a control the view made for a node, and then the node's row, whole, in
    // its panel and showing its value. Each comes once, and again only for a row made anew (a node that
    // changed its editor, or that shows again after it was hidden). An exception of a subscriber goes up
    // through the view, as one of the inspector's events does (3.11).
    public event EventHandler<ControlCreatedEventArgs<Control>>? ControlCreated;

    public event EventHandler<RowCreatedEventArgs<Control>>? RowCreated;

    // What failed in the view itself (3.11): a row that could not be made is left out with its branch
    // until the next build (critical); one that could not show its objects keeps what it showed, with the
    // message (worked around).
    public event EventHandler<InspectorFailureEventArgs>? RowFailed;

    // The inspector shown; null once it was disposed.
    public Inspector? Inspector { get; private set; }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        Release();
        IsDisposed = true;
    }

    // Takes a control out of the panel it is in.
    internal static void Detach(Control control)
    {
        if (control.Parent is Panel parent)
            parent.Children.Remove(control);
    }

    internal void OnRowFailed(InspectorNode node, FailureSeverity severity, Exception exception, string message)
    {
        RowFailed?.Invoke(this, new InspectorFailureEventArgs(node.Inspector, node.Path, severity, exception, message, suggestion: null));
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (BuildPending)
            Build();
    }

    // Changes come in bursts (a handler that hides three rows, a bind), so the rows are built once, when
    // the UI thread gets to it; before the view is loaded, the build waits for it.
    private void ScheduleBuild()
    {
        if (BuildPending || Inspector == null)
            return;

        BuildPending = true;

        if (IsLoaded)
            Dispatcher.UIThread.Post(Build);
    }

    // Puts a row for every node the inspector shows in the panel of the group it belongs to, keeping the
    // controls of a row while its node keeps the same editor, so building again does not take the text
    // being typed; then every row shows its node.
    private void Build()
    {
        BuildPending = false;

        if (Inspector == null || IsDisposed)
            return;

        Watch(Inspector);

        var shown = new List<AvaloniaRow>();
        var made = new List<AvaloniaRow>();
        var wanted = new Dictionary<Panel, List<Control>> { [Area] = [] };

        foreach (var node in Inspector.Rows)
        {
            // The rows come parent first, so the panel of the group above is known by now; a node right
            // below the root goes at the top.
            var panel = node.Parent?.Parent == null
                ? Area
                : RowsByNode.TryGetValue(node.Parent, out var group) && group.Panel is { } inner && wanted.ContainsKey(inner) ? inner : null;

            if (panel == null)
                continue;

            if (!RowsByNode.TryGetValue(node, out var row) || !row.Fits())
            {
                Discard(node);

                // A row that cannot be made (a control the platform refuses) is left out with its
                // branch, and made again at the next build (3.11).
                try
                {
                    row = new AvaloniaRow(this, node);
                }
                catch (Exception e)
                {
                    OnRowFailed(node, FailureSeverity.Critical, e, $"The row of '{node.Path}' could not be made; it is left out until the next build.");
                    continue;
                }

                RowsByNode[node] = row;
                made.Add(row);
            }

            shown.Add(row);
            wanted[panel].Add(row.Control);

            if (row.Panel is { } own)
                wanted[own] = [];
        }

        foreach (var node in RowsByNode.Keys.Except(shown.Select(row => row.Node)).ToList())
            Discard(node);

        foreach (var (panel, controls) in wanted)
            Arrange(panel, controls);

        var marks = Inspector.Any(node => !string.IsNullOrEmpty(node.Help));

        foreach (var row in shown)
        {
            row.ShowMarks(marks);
            row.Show();
        }

        // A valve that throws stops here; the view stays usable, and the rows it did not announce are
        // not announced again.
        foreach (var row in made)
            Announce(row);
    }

    // The valves for a row this build made (P7.4): each of its controls, and then the row.
    private void Announce(AvaloniaRow row)
    {
        var node = row.Node;
        ControlCreated?.Invoke(this, new(node, RowPart.Label, row.LabelControl));
        ControlCreated?.Invoke(this, new(node, RowPart.Help, row.HelpControl));

        if (row.EditorControl is { } editor)
            ControlCreated?.Invoke(this, new(node, RowPart.Editor, editor));

        if (row.Panel is { } panel)
            ControlCreated?.Invoke(this, new(node, RowPart.Panel, panel));

        RowCreated?.Invoke(this, new(node, row.LabelControl, row.HelpControl, row.EditorControl, row.Panel));
    }

    // Puts the rows a panel should hold in it, in their order. A control that is already in its place is
    // left there, so it keeps the focus; a control sits in one panel at a time, and only ever in its
    // group's, so what a panel lets go of was never wanted anywhere else.
    private static void Arrange(Panel panel, List<Control> controls)
    {
        var keep = controls.ToHashSet();

        for (var i = panel.Children.Count - 1; i >= 0; i--)
        {
            if (!keep.Contains(panel.Children[i]))
                panel.Children.RemoveAt(i);
        }

        for (var i = 0; i < controls.Count; i++)
        {
            if (i < panel.Children.Count && panel.Children[i] == controls[i])
                continue;

            panel.Children.Remove(controls[i]);
            panel.Children.Insert(i, controls[i]);
        }
    }

    // Takes a node's row off the view.
    private void Discard(InspectorNode node)
    {
        if (RowsByNode.Remove(node, out var row))
            row.Dispose();
    }

    // Every node of the tree, also the hidden ones, whose rule can show them again; nodes added by hand
    // after the view was built are taken at the next build.
    private void Watch(Inspector inspector)
    {
        foreach (var node in inspector)
        {
            if (!Watched.Add(node))
                continue;

            node.ValueChanged += OnValueChanged;
            node.BindFailed += OnBindFailed;
            node.VisibleChanged += OnVisibleChanged;
            node.ObjectReplaced += OnObjectReplaced;
        }
    }

    // Lets the inspector go: no more events from it, and no rows.
    private void Release()
    {
        if (Inspector == null)
            return;

        Inspector.OptionChanged -= OnOptionChanged;
        Inspector.BindRegistered -= OnBindChanged;
        Inspector.BindRemoved -= OnBindChanged;
        Inspector.Unbound -= OnBindChanged;
        Inspector.ForcedApply -= OnBindChanged;
        Inspector.ForcedReload -= OnBindChanged;
        Inspector.ForcedClear -= OnBindChanged;
        Inspector.Disposed -= OnInspectorDisposed;

        foreach (var node in Watched)
        {
            node.ValueChanged -= OnValueChanged;
            node.BindFailed -= OnBindFailed;
            node.VisibleChanged -= OnVisibleChanged;
            node.ObjectReplaced -= OnObjectReplaced;
        }

        Watched.Clear();

        foreach (var row in RowsByNode.Values)
            row.Dispose();

        RowsByNode.Clear();
        Area.Children.Clear();
        Inspector = null;
    }

    // The inspector raises its events on the thread that changed something; Avalonia only lets the UI
    // thread touch a control (P7.12).
    private void OnUiThread(Action action)
    {
        if (IsDisposed)
            return;

        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }

    // An option can change the rows (Visible, Order, Editor) or only what one shows (Label, Collapsed);
    // building again keeps the rows that still fit, so it covers both.
    private void OnOptionChanged(object? sender, OptionChangedEventArgs e) => OnUiThread(ScheduleBuild);

    // A bind starts every node over in silence, and the forced operations can change any row.
    private void OnBindChanged(object? sender, EventArgs e) => OnUiThread(ScheduleBuild);

    private void OnVisibleChanged(object? sender, VisibleChangedEventArgs e) => OnUiThread(ScheduleBuild);

    // The branch of a replaced group is disabled until a Rebind (P3.4).
    private void OnObjectReplaced(object? sender, ObjectReplacedEventArgs e) => OnUiThread(ScheduleBuild);

    private void OnInspectorDisposed(object? sender, InspectorEventArgs e) => OnUiThread(Release);

    private void OnValueChanged(object? sender, ValueChangedEventArgs e) => OnUiThread(() =>
    {
        if (RowsByNode.TryGetValue(e.Node, out var row))
            row.Show();
    });

    // A failure keeps what the row shows (a text that did not convert stays as typed, P2.12); only the
    // state changes.
    private void OnBindFailed(object? sender, InspectorFailureEventArgs e) => OnUiThread(() =>
    {
        if (sender is InspectorNode node && RowsByNode.TryGetValue(node, out var row))
            row.ShowState();
    });
}
