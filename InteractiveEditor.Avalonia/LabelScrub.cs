using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using InteractiveEditor.Options;
using InteractiveEditor.Views;

namespace InteractiveEditor.Avalonia;

// Scrubbing on a row's label (P7.18), as in the WinForms and WPF views: dragging it moves the number by
// the distance times the node's ScrubMultiplier, across or up and down (ScrubAxis), and writes while it
// drags (P2.12). The values the objects held when the drag started are kept here, and each write is "the
// start of each object plus the whole delta" (P7.16). The drag starts past a few pixels, so a click
// changes nothing; Shift makes the step ten times larger and Ctrl ten times smaller, and Esc puts every
// object back where it started. The label has no keyboard focus, so Esc is taken from the top level, only
// while dragging. The distance is in the view's units, measured from the view, which does not move.
internal sealed class LabelScrub : IDisposable
{
    private const double Threshold = 3;

    private readonly AvaloniaRow Row;
    private readonly Control Label;
    private readonly Control View;

    private Point Start;
    private IPointer? Pressed;
    private bool Dragging;
    private object?[] From = [];

    // Where Esc is taken while dragging.
    private TopLevel? Keys;

    // The last delta written, so a move that does not change it writes nothing.
    private double Written;

    public LabelScrub(AvaloniaRow row, Control label, Control view)
    {
        Row = row;
        Label = label;
        View = view;

        label.PointerPressed += OnPointerPressed;
        label.PointerMoved += OnPointerMoved;
        label.PointerReleased += OnPointerReleased;
        label.PointerCaptureLost += OnPointerCaptureLost;
    }

    private InspectorNode Node => Row.Node;

    public void Dispose()
    {
        End();
        Label.PointerPressed -= OnPointerPressed;
        Label.PointerMoved -= OnPointerMoved;
        Label.PointerReleased -= OnPointerReleased;
        Label.PointerCaptureLost -= OnPointerCaptureLost;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(Label).Properties.IsLeftButtonPressed || !ViewRules.CanScrub(Node))
            return;

        Start = e.GetPosition(View);
        Pressed = e.Pointer;
        Dragging = false;
        e.Pointer.Capture(Label);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (Pressed == null || e.Pointer != Pressed)
            return;

        var point = e.GetPosition(View);
        var distance = Node.ScrubAxis == ScrubAxis.Vertical ? Start.Y - point.Y : point.X - Start.X;

        if (!Dragging)
        {
            if (Math.Abs(distance) < Threshold)
                return;

            Dragging = true;
            From = Node.ViewValues.ToArray();
            Written = 0;
            Keys = TopLevel.GetTopLevel(Label);
            Keys?.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        }

        var keys = e.KeyModifiers;
        var step = (Node.ScrubMultiplier ?? 0) * (keys.HasFlag(KeyModifiers.Shift) ? 10 : keys.HasFlag(KeyModifiers.Control) ? 0.1 : 1);
        var delta = Math.Round(distance) * step;

        if (delta == Written)
            return;

        Written = delta;
        var values = From.Select(value => (object?)(ViewRules.ToDouble(value) + delta)).ToArray();
        Row.Write(() => Node.SetValues(values));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer == Pressed)
            End();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        End();
    }

    // Esc while dragging puts every object back where the drag started, and goes no further.
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!Dragging || e.Key != Key.Escape)
            return;

        e.Handled = true;

        var from = From;
        End();
        Row.Write(() => Node.SetValues(from));
    }

    private void End()
    {
        if (Pressed is not { } pointer)
            return;

        Pressed = null;

        if (Dragging)
        {
            Dragging = false;
            Keys?.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            Keys = null;
        }

        if (pointer.Captured == Label)
            pointer.Capture(null);
    }
}
