using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.Layout;

namespace InteractiveEditor.Views.Wpf;

// The WPF view of an inspector, the WinForms one on the other platform (P7.7). It receives the inspector
// and observes it, with no state of its own about the objects: the rows go on the rectangles of the
// layout step, a canvas for each group (P7.3), inside a scroll viewer, and whatever the inspector reports
// (a value, an option, the bind, a failure) shows. What comes from another thread moves to the UI thread
// first (P7.12). A WPF control has no Dispose, so the view has one of its own: disposing it lets the
// inspector go and leaves it alone; when the inspector is disposed, the view empties (P7.13).
public sealed class WpfInspectorView : UserControl, IDisposable
{
    // The light background of the row under the mouse (P7.14): a little of the highlight over the
    // background, so it follows the system's colors.
    internal static readonly Color HoverColor = Blend(SystemColors.ControlColor, SystemColors.HighlightColor, 0.15);

    private readonly ScrollViewer Scroller = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Focusable = false,
    };

    // The canvas the rows go on, at the top left: with the size of the layout, a stretched one would be
    // centred in a larger view.
    private readonly Canvas Area = new()
    {
        Background = Brushes.Transparent,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
    };
    private readonly Rectangle Hover = new() { Fill = Frozen(HoverColor), IsHitTestVisible = false };
    private readonly Dictionary<InspectorNode, WpfRow> RowsByNode = [];
    private readonly Dictionary<InspectorNode, Canvas> Panels = [];
    private readonly HashSet<InspectorNode> Watched = [];
    private bool LayoutPending;
    private bool IsDisposed;

    internal WpfInspectorView(Inspector inspector)
    {
        Inspector = inspector;
        Background = SystemColors.ControlBrush;
        Scroller.Content = Area;
        Area.SetValue(KeyboardNavigation.TabNavigationProperty, KeyboardNavigationMode.Local);
        AutomationId(Area, "content");
        Content = Scroller;

        Scroller.ScrollChanged += OnScrollChanged;
        Loaded += OnLoaded;
        AddHandler(MouseMoveEvent, new MouseEventHandler(OnMouseMoved), handledEventsToo: true);
        MouseLeave += OnMouseLeft;

        inspector.OptionChanged += OnOptionChanged;
        inspector.BindRegistered += OnBindChanged;
        inspector.BindRemoved += OnBindChanged;
        inspector.Unbound += OnBindChanged;
        inspector.ForcedApply += OnBindChanged;
        inspector.ForcedReload += OnBindChanged;
        inspector.ForcedClear += OnBindChanged;
        inspector.Disposed += OnInspectorDisposed;

        // The rows are made when the view is loaded, with the width it got, so whoever subscribes to
        // the valves right after CreateWpfView sees every control.
        LayoutPending = true;
    }

    // The escape valves (P7.4): a control the view made for a node, and then the node's row, whole,
    // placed and showing its value. Each comes once, and again only for a row made anew (a node that
    // changed its editor). An exception of a subscriber goes up through the view, as one of the
    // inspector's events does (3.11).
    public event EventHandler<ControlCreatedEventArgs<FrameworkElement>>? ControlCreated;

    public event EventHandler<RowCreatedEventArgs<FrameworkElement>>? RowCreated;

    // What failed in the view itself (3.11): a row that could not be made or placed is left out until
    // the next layout (critical); one that could not show its objects keeps what it showed, in light
    // red (worked around).
    public event EventHandler<InspectorFailureEventArgs>? RowFailed;

    // The inspector shown; null once it was disposed.
    public Inspector? Inspector { get; private set; }

    // The row under the mouse, whose background is lit (P7.14).
    internal WpfRow? Hovered { get; private set; }

    // The view's own ids, for UI automation and tests: a WPF Name cannot hold the dots of a path, and the
    // Tag stays free for whoever uses the library (P7.16).
    internal static void AutomationId(DependencyObject element, string id)
    {
        System.Windows.Automation.AutomationProperties.SetAutomationId(element, id);
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        Release();
        IsDisposed = true;
    }

    internal static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    // Changes come in bursts (a handler that hides three rows, a bind), so they are laid out once, when
    // the UI thread gets to it.
    private void ScheduleLayOut()
    {
        if (LayoutPending || Inspector == null)
            return;

        LayoutPending = true;

        if (IsLoaded)
            Dispatcher.BeginInvoke(LayOutRows);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (LayoutPending)
            LayOutRows();
    }

    // The width of the editors follows the view's; the rows keep their height.
    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ViewportWidthChange != 0)
            ScheduleLayOut();
    }

    // Puts every row on the rectangles of the layout step, keeping the controls of a row while its node
    // keeps the same editor, so a relayout does not take the focus or the text being typed.
    private void LayOutRows()
    {
        LayoutPending = false;

        if (Inspector == null || IsDisposed)
            return;

        Watch(Inspector);

        var layout = Inspector.Layout(Math.Max(0, Scroller.ViewportWidth));
        var seen = new HashSet<InspectorNode>();
        var grouped = new HashSet<InspectorNode>();

        // A valve that throws stops the layout where it is; the view stays usable, and the next change
        // lays out the rest.
        Place(Area, layout.Rows, seen, grouped);

        foreach (var node in RowsByNode.Keys.Where(node => !seen.Contains(node)).ToList())
            Discard(node);

        foreach (var node in Panels.Keys.Where(node => !grouped.Contains(node)).ToList())
        {
            Detach(Panels[node]);
            Panels.Remove(node);
        }

        var size = (Size)layout.Size;
        Area.Width = size.Width;
        Area.Height = size.Height;

        // The rows may have moved under the mouse.
        UpdateHover();
    }

    private void Place(Canvas container, IReadOnlyList<LayoutRow> rows, HashSet<InspectorNode> seen, HashSet<InspectorNode> grouped)
    {
        var tab = 0;

        foreach (var layout in rows)
        {
            var node = layout.Node;
            seen.Add(node);

            var made = !RowsByNode.TryGetValue(node, out var row) || !row.Fits();
            var madePanel = node.IsGroup && !Panels.ContainsKey(node);
            Canvas? panel;

            // A row that cannot be made or placed (a control the platform refuses) is left out with its
            // branch, and made again at the next layout (3.11).
            try
            {
                if (made)
                {
                    Discard(node);
                    row = new WpfRow(this, node);
                    RowsByNode[node] = row;
                }

                row!.Place(container, layout, ref tab);
                panel = node.IsGroup ? PlacePanel(container, layout, ref tab) : null;
            }
            catch (Exception e)
            {
                Discard(node);
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

    // The canvas that holds a group's rows (P7.3), collapsed with the group.
    private Canvas PlacePanel(Canvas container, LayoutRow layout, ref int tab)
    {
        var node = layout.Node;

        if (!Panels.TryGetValue(node, out var panel))
        {
            panel = new Canvas { Background = Brushes.Transparent };
            panel.SetValue(KeyboardNavigation.TabNavigationProperty, KeyboardNavigationMode.Local);
            AutomationId(panel, node.Path + "#panel");
            Panels[node] = panel;
        }

        Move(panel, container);

        var bounds = (Rect)layout.Panel;
        Canvas.SetLeft(panel, bounds.X);
        Canvas.SetTop(panel, bounds.Y);
        panel.Width = bounds.Width;
        panel.Height = bounds.Height;
        panel.Visibility = node.Collapsed ? Visibility.Collapsed : Visibility.Visible;
        KeyboardNavigation.SetTabIndex(panel, tab++);
        return panel;
    }

    // Puts an element in a canvas, out of the one it was in.
    internal static void Move(UIElement element, Canvas container)
    {
        if (VisualTreeHelper.GetParent(element) == container)
            return;

        Detach(element);
        container.Children.Add(element);
    }

    internal static void Detach(UIElement element)
    {
        if (VisualTreeHelper.GetParent(element) is Panel parent)
            parent.Children.Remove(element);
    }

    // Takes a node's row off the view.
    private void Discard(InspectorNode node)
    {
        if (!RowsByNode.Remove(node, out var row))
            return;

        if (Hovered == row)
            ShowHover(null);

        row.Dispose();
    }

    // The valves for what this layout made for a node (P7.4): each new control, and then the new row.
    private void Announce(InspectorNode node, WpfRow? row, Canvas? panel, bool madePanel)
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
        ShowHover(null);

        foreach (var row in RowsByNode.Values)
            row.Dispose();

        foreach (var panel in Panels.Values)
            Detach(panel);

        RowsByNode.Clear();
        Panels.Clear();
        Area.Width = 0;
        Area.Height = 0;
        Inspector = null;
    }

    private void OnMouseMoved(object sender, MouseEventArgs e)
    {
        UpdateHover();
    }

    // Out of its windows, WPF keeps the last position the mouse had in them, so leaving the view puts
    // the row out at once instead of asking where the mouse is.
    private void OnMouseLeft(object sender, MouseEventArgs e)
    {
        if (Hovered != null)
            ShowHover(null);
    }

    // Finds the row under the mouse, inside the view and in a container on screen (not in a collapsed
    // group), and lights it instead of the last one.
    private void UpdateHover()
    {
        var inView = Mouse.GetPosition(Scroller);
        var inside = IsVisible && inView.X >= 0 && inView.Y >= 0 && inView.X < Scroller.ViewportWidth && inView.Y < Scroller.ViewportHeight;
        var found = inside
            ? RowsByNode.Values.FirstOrDefault(row => row.Container is { IsVisible: true } container
                && row.Bounds.Contains(Mouse.GetPosition(container)))
            : null;

        if (found != Hovered)
            ShowHover(found);
    }

    // The background goes behind the row's controls, in its container.
    private void ShowHover(WpfRow? row)
    {
        Hovered?.ShowHover(false);
        Hovered = row;

        if (row?.Container is not { } container)
        {
            Detach(Hover);
            return;
        }

        Move(Hover, container);
        Panel.SetZIndex(Hover, -1);
        Canvas.SetLeft(Hover, row.Bounds.X);
        Canvas.SetTop(Hover, row.Bounds.Y);
        Hover.Width = row.Bounds.Width;
        Hover.Height = row.Bounds.Height;
        row.ShowHover(true);
    }

    private static Color Blend(Color under, Color over, double amount)
    {
        byte Mix(byte a, byte b) => (byte)Math.Round(a + (b - a) * amount);
        return Color.FromRgb(Mix(under.R, over.R), Mix(under.G, over.G), Mix(under.B, over.B));
    }

    // The inspector raises its events on the thread that changed something; WPF only lets the UI thread
    // touch a control (P7.12).
    private void OnUiThread(Action action)
    {
        if (IsDisposed)
            return;

        if (Dispatcher.CheckAccess())
            action();
        else
            Dispatcher.BeginInvoke(action);
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
