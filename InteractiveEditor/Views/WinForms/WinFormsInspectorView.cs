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
    // The light background of the row under the mouse (P7.14): a little of the highlight over the
    // background, so it follows the system's colors.
    internal static readonly Color HoverColor = Blend(SystemColors.Control, SystemColors.Highlight, 0.15);

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
        Content.Paint += PaintHover;
        Follow(Content);

        inspector.OptionChanged += OnOptionChanged;
        inspector.BindRegistered += OnBindChanged;
        inspector.BindRemoved += OnBindChanged;
        inspector.Unbound += OnBindChanged;
        inspector.ForcedApply += OnBindChanged;
        inspector.ForcedReload += OnBindChanged;
        inspector.ForcedClear += OnBindChanged;
        inspector.Disposed += OnInspectorDisposed;

        // The rows are made when the view gets its window, so whoever subscribes to the valves right
        // after CreateWinFormsView sees every control.
        LayoutPending = true;
    }

    // The escape valves (P7.4): a control the view made for a node, and then the node's row, whole,
    // placed and showing its value. Each comes once, and again only for a row made anew (a node that
    // changed its editor). An exception of a subscriber goes up through the view, as one of the
    // inspector's events does (3.11).
    public event EventHandler<ControlCreatedEventArgs<Control>>? ControlCreated;

    public event EventHandler<RowCreatedEventArgs<Control>>? RowCreated;

    // What failed in the view itself (3.11): a row that could not be made or placed is left out until
    // the next layout (critical); one that could not show its objects keeps what it showed, in light
    // red (worked around).
    public event EventHandler<InspectorFailureEventArgs>? RowFailed;

    // The inspector shown; null once it was disposed.
    public Inspector? Inspector { get; private set; }

    // The tooltips of every row: the node's Tooltip, or the failure of the row (P7.11).
    internal ToolTip Tips { get; } = new();

    // The row under the mouse, whose background is lit (P7.14).
    internal WinFormsRow? Hovered { get; private set; }

    // Follows the mouse over a control and the ones inside it: a control covers its container, so the
    // container alone would not know when the mouse is over a row's label or editor.
    internal void Follow(Control control)
    {
        control.MouseMove += OnMouseMoved;
        control.MouseLeave += OnMouseMoved;

        foreach (Control child in control.Controls)
            Follow(child);
    }

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

        // A valve that throws stops the layout where it is; the view stays usable, and the next change
        // lays out the rest.
        try
        {
            Place(Content, layout.Rows, seen, grouped);

            foreach (var node in RowsByNode.Keys.Where(node => !seen.Contains(node)).ToList())
                Drop(node);

            foreach (var node in Panels.Keys.Where(node => !grouped.Contains(node)).ToList())
            {
                Panels[node].Dispose();
                Panels.Remove(node);
            }

            Content.Bounds = new Rectangle(AutoScrollPosition, (Size)layout.Size);
        }
        finally
        {
            Content.ResumeLayout();
            ResumeLayout();
        }

        // The rows may have moved under the mouse.
        UpdateHover();
    }

    private void Place(Control container, IReadOnlyList<LayoutRow> rows, HashSet<InspectorNode> seen, HashSet<InspectorNode> grouped)
    {
        var tab = 0;

        foreach (var layout in rows)
        {
            var node = layout.Node;
            seen.Add(node);

            var made = !RowsByNode.TryGetValue(node, out var row) || !row.Fits();
            var madePanel = node.IsGroup && !Panels.ContainsKey(node);
            Panel? panel;

            // A row that cannot be made or placed (a control the platform refuses) is left out with its
            // branch, and made again at the next layout (3.11).
            try
            {
                if (made)
                {
                    Drop(node);
                    row = new WinFormsRow(this, node);
                    RowsByNode[node] = row;
                }

                row!.Place(container, layout, ref tab);
                panel = node.IsGroup ? PlacePanel(container, layout, ref tab) : null;
            }
            catch (Exception e)
            {
                Drop(node);
                OnRowFailed(node, FailureSeverity.Critical, e, $"The row of '{node.Path}' could not be made; it is left out until the next layout.");
                continue;
            }

            row.Show();

            if (made || madePanel)
                Announce(node, made ? row : null, panel, madePanel);

            if (panel == null)
                continue;

            grouped.Add(node);
            Place(panel, layout.Rows, seen, grouped);
        }
    }

    // The panel that holds a group's rows (P7.3), hidden while the group is collapsed.
    private Panel PlacePanel(Control container, LayoutRow layout, ref int tab)
    {
        var node = layout.Node;

        if (!Panels.TryGetValue(node, out var panel))
        {
            panel = new Panel { Name = node.Path + "#panel" };
            panel.Paint += PaintHover;
            Follow(panel);
            Panels[node] = panel;
        }

        if (panel.Parent != container)
            container.Controls.Add(panel);

        panel.Bounds = (Rectangle)layout.Panel;
        panel.Visible = !node.Collapsed;
        panel.TabIndex = tab++;
        return panel;
    }

    // Takes a node's row off the view.
    private void Drop(InspectorNode node)
    {
        if (!RowsByNode.Remove(node, out var row))
            return;

        if (Hovered == row)
            Hovered = null;

        row.Dispose();
    }

    // The valves for what this layout made for a node (P7.4): each new control, and then the new row.
    private void Announce(InspectorNode node, WinFormsRow? row, Panel? panel, bool madePanel)
    {
        if (row != null)
        {
            ControlCreated?.Invoke(this, new(node, RowPart.Label, row.LabelControl));
            ControlCreated?.Invoke(this, new(node, RowPart.Help, row.HelpControl));

            if (row.EditorControl is { } editor)
                ControlCreated?.Invoke(this, new(node, RowPart.Editor, editor));
        }

        if (madePanel && panel != null)
            ControlCreated?.Invoke(this, new(node, RowPart.Panel, panel));

        if (row != null)
            RowCreated?.Invoke(this, new(node, row.LabelControl, row.HelpControl, row.EditorControl, panel));
    }

    internal void OnRowFailed(InspectorNode node, FailureSeverity severity, Exception exception, string message)
    {
        RowFailed?.Invoke(this, new InspectorFailureEventArgs(node.Inspector, node.Path, severity, exception, message, suggestion: null));
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
        Hovered = null;
        Content.Size = Size.Empty;
        Inspector = null;
    }

    private void OnMouseMoved(object? sender, EventArgs e)
    {
        UpdateHover();
    }

    // Finds the row under the mouse, inside the view and in a container on screen (not in a collapsed
    // group), and lights it instead of the last one.
    private void UpdateHover()
    {
        var point = Cursor.Position;
        var inside = IsHandleCreated && RectangleToScreen(ClientRectangle).Contains(point);
        var found = inside
            ? RowsByNode.Values.FirstOrDefault(row => row.Container is { Visible: true } container
                && container.RectangleToScreen(row.Bounds).Contains(point))
            : null;

        if (found == Hovered)
            return;

        var last = Hovered;
        Hovered = found;

        foreach (var row in new[] { last, found })
        {
            if (row?.Container == null)
                continue;

            row.ShowHover(row == found);
            row.Container.Invalidate(row.Bounds, true);
        }
    }

    private void PaintHover(object? sender, PaintEventArgs e)
    {
        if (Hovered is not { } row || row.Container != sender)
            return;

        using var brush = new SolidBrush(HoverColor);
        e.Graphics.FillRectangle(brush, row.Bounds);
    }

    private static Color Blend(Color under, Color over, double amount)
    {
        int Mix(int a, int b) => (int)Math.Round(a + (b - a) * amount);
        return Color.FromArgb(Mix(under.R, over.R), Mix(under.G, over.G), Mix(under.B, over.B));
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
            row.Show();

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
