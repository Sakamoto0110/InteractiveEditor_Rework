using System.Drawing;
using System.Windows.Forms;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.Layout;

namespace InteractiveEditor.Views.WinForms;

// The WinForms view of an inspector (P7.7). It receives the inspector and observes it, with no state of
// its own about the objects: the rows go on the rectangles of the layout step, a panel for each group
// (P7.3), and whatever the inspector reports (a value, an option, the bind, a failure) shows. What comes
// from another thread moves to the UI thread first (P7.12). Disposing the view leaves the inspector
// alone; when the inspector is disposed, the view empties (P7.13).
public sealed class WinFormsInspectorView : UserControl
{
    private readonly Panel Content = new() { Name = "content" };
    private readonly Dictionary<InspectorNode, WinFormsRow> RowsByNode = [];
    private readonly Dictionary<InspectorNode, Panel> Panels = [];
    private readonly HashSet<InspectorNode> Watched = [];
    private bool LayoutPending;

    internal WinFormsInspectorView(Inspector inspector)
    {
        Inspector = inspector;
        AutoScroll = true;
        Controls.Add(Content);

        inspector.OptionChanged += OnOptionChanged;
        inspector.BindRegistered += OnBindChanged;
        inspector.BindRemoved += OnBindChanged;
        inspector.Unbound += OnBindChanged;
        inspector.ForcedApply += OnBindChanged;
        inspector.ForcedReload += OnBindChanged;
        inspector.ForcedClear += OnBindChanged;
        inspector.Disposed += OnInspectorDisposed;

        LayOutRows();
    }

    // The inspector shown; null once it was disposed.
    public Inspector? Inspector { get; private set; }

    // The tooltips of every row: the node's Tooltip, or the failure of the row (P7.11).
    internal ToolTip Tips { get; } = new();

    // At once, with the width the view got from its parent: laid out later, a focused text box would
    // keep the scroll it took while it was still narrow, showing only the end of its text.
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (LayoutPending)
            LayOutRows();
    }

    // The width of the editors follows the view's; the rows keep their height.
    protected override void OnClientSizeChanged(EventArgs e)
    {
        base.OnClientSizeChanged(e);
        ScheduleLayOut();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Release();
            Tips.Dispose();
        }

        base.Dispose(disposing);
    }

    // Changes come in bursts (a handler that hides three rows, a bind), so they are laid out once, when
    // the UI thread gets to it.
    private void ScheduleLayOut()
    {
        if (LayoutPending || Inspector == null)
            return;

        LayoutPending = true;

        if (IsHandleCreated)
            BeginInvoke(LayOutRows);
    }

    // Puts every row on the rectangles of the layout step, keeping the controls of a row while its node
    // keeps the same editor, so a relayout does not take the focus or the text being typed.
    private void LayOutRows()
    {
        LayoutPending = false;

        if (Inspector == null || IsDisposed)
            return;

        Watch(Inspector);

        var layout = Inspector.Layout(Math.Max(0, ClientSize.Width));
        var seen = new HashSet<InspectorNode>();
        var grouped = new HashSet<InspectorNode>();

        SuspendLayout();
        Content.SuspendLayout();

        Place(Content, layout.Rows, seen, grouped);

        foreach (var node in RowsByNode.Keys.Where(node => !seen.Contains(node)).ToList())
        {
            RowsByNode[node].Dispose();
            RowsByNode.Remove(node);
        }

        foreach (var node in Panels.Keys.Where(node => !grouped.Contains(node)).ToList())
        {
            Panels[node].Dispose();
            Panels.Remove(node);
        }

        Content.Bounds = new Rectangle(AutoScrollPosition, (Size)layout.Size);

        Content.ResumeLayout();
        ResumeLayout();
    }

    private void Place(Control container, IReadOnlyList<LayoutRow> rows, HashSet<InspectorNode> seen, HashSet<InspectorNode> grouped)
    {
        var tab = 0;

        foreach (var layout in rows)
        {
            var node = layout.Node;
            seen.Add(node);

            if (!RowsByNode.TryGetValue(node, out var row) || !row.Fits())
            {
                row?.Dispose();
                row = new WinFormsRow(this, node);
                RowsByNode[node] = row;
            }

            row.Place(container, layout, ref tab);
            row.ShowValue();
            row.ShowState();

            if (!node.IsGroup)
                continue;

            grouped.Add(node);

            if (!Panels.TryGetValue(node, out var panel))
            {
                panel = new Panel { Name = node.Path + "#panel" };
                Panels[node] = panel;
            }

            if (panel.Parent != container)
                container.Controls.Add(panel);

            panel.Bounds = (Rectangle)layout.Panel;
            panel.Visible = !node.Collapsed;
            panel.TabIndex = tab++;

            Place(panel, layout.Rows, seen, grouped);
        }
    }

    // Every node of the tree, also the hidden ones, whose rule can show them again; nodes added by hand
    // after the view was built are taken at the next layout.
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

        foreach (var panel in Panels.Values)
            panel.Dispose();

        RowsByNode.Clear();
        Panels.Clear();
        Content.Size = Size.Empty;
        Inspector = null;
    }

    // The inspector raises its events on the thread that changed something; WinForms only lets the UI
    // thread touch a control (P7.12).
    private void OnUiThread(Action action)
    {
        if (IsDisposed || Disposing)
            return;

        if (InvokeRequired)
            BeginInvoke(action);
        else
            action();
    }

    private void OnOptionChanged(object? sender, OptionChangedEventArgs e) => OnUiThread(ScheduleLayOut);

    // A bind starts every node over in silence, and the forced operations can change any row.
    private void OnBindChanged(object? sender, EventArgs e) => OnUiThread(ScheduleLayOut);

    private void OnVisibleChanged(object? sender, VisibleChangedEventArgs e) => OnUiThread(ScheduleLayOut);

    // The branch of a replaced group is disabled until a Rebind (P3.4).
    private void OnObjectReplaced(object? sender, ObjectReplacedEventArgs e) => OnUiThread(ScheduleLayOut);

    private void OnInspectorDisposed(object? sender, InspectorEventArgs e) => OnUiThread(Release);

    private void OnValueChanged(object? sender, ValueChangedEventArgs e) => OnUiThread(() =>
    {
        if (RowsByNode.TryGetValue(e.Node, out var row))
        {
            row.ShowValue();
            row.ShowState();
        }

        // The items of a collection decide the height of its list editor.
        if (e.Node is CollectionNode)
            ScheduleLayOut();
    });

    // A failure keeps what the row shows (a text that did not convert stays as typed, P2.12); only the
    // state changes.
    private void OnBindFailed(object? sender, InspectorFailureEventArgs e) => OnUiThread(() =>
    {
        if (sender is InspectorNode node && RowsByNode.TryGetValue(node, out var row))
            row.ShowState();
    });
}
