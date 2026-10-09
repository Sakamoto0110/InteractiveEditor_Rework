using System.Windows;
using System.Windows.Input;
using InteractiveEditor.Options;
using InteractiveEditor.Views;

namespace InteractiveEditor.Wpf;

// Scrubbing on a row's label (P7.18), as in the WinForms view: dragging it moves the number by the
// distance times the node's ScrubMultiplier, across or up and down (ScrubAxis), and writes while it drags
// (P2.12). The values the objects held when the drag started are kept here, and each write is "the
// start of each object plus the whole delta" (P7.16). The drag starts past a few pixels, so a click
// changes nothing; Shift makes the step ten times larger and Ctrl ten times smaller, and Esc puts every
// object back where it started. The label has no keyboard focus, so Esc is taken from the input manager,
// only while dragging. The distance is in the view's units, measured from the view, which does not move.
internal sealed class LabelScrub : IDisposable
{
    private const double Threshold = 3;

    private readonly WpfRow Row;
    private readonly UIElement Label;
    private readonly UIElement View;

    private Point Start;
    private bool Pressed;
    private bool Dragging;
    private object?[] From = [];

    // The last delta written, so a move that does not change it writes nothing.
    private double Written;

    public LabelScrub(WpfRow row, UIElement label, UIElement view)
    {
        Row = row;
        Label = label;
        View = view;

        label.MouseLeftButtonDown += OnMouseDown;
        label.MouseMove += OnMouseMove;
        label.MouseLeftButtonUp += (_, _) => End();
        label.LostMouseCapture += (_, _) => End();
    }

    private InspectorNode Node => Row.Node;

    public void Dispose()
    {
        End();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ViewRules.CanScrub(Node))
            return;

        Start = e.GetPosition(View);
        Pressed = true;
        Dragging = false;
        Label.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!Pressed)
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
            InputManager.Current.PreProcessInput += OnPreProcessInput;
        }

        var keys = Keyboard.Modifiers;
        var step = (Node.ScrubMultiplier ?? 0) * (keys.HasFlag(ModifierKeys.Shift) ? 10 : keys.HasFlag(ModifierKeys.Control) ? 0.1 : 1);
        var delta = Math.Round(distance) * step;

        if (delta == Written)
            return;

        Written = delta;
        var values = From.Select(value => (object?)(ViewRules.ToDouble(value) + delta)).ToArray();
        Row.Write(() => Node.SetValues(values));
    }

    // Esc while dragging puts every object back where the drag started, and goes no further.
    private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
    {
        if (!Dragging || e.StagingItem.Input is not KeyEventArgs { Key: Key.Escape } key || key.RoutedEvent != Keyboard.PreviewKeyDownEvent)
            return;

        e.Cancel();

        var from = From;
        End();
        Row.Write(() => Node.SetValues(from));
    }

    private void End()
    {
        if (!Pressed)
            return;

        Pressed = false;

        if (Dragging)
        {
            Dragging = false;
            InputManager.Current.PreProcessInput -= OnPreProcessInput;
        }

        if (Label.IsMouseCaptured)
            Label.ReleaseMouseCapture();
    }
}
