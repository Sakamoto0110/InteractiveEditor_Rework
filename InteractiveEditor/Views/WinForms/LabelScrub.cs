using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using InteractiveEditor.Options;

namespace InteractiveEditor.Views.WinForms;

// Scrubbing on a row's label (P7.18): dragging it moves the number by the distance times the node's
// ScrubMultiplier, across or up and down (ScrubAxis), and writes while it drags (P2.12). The values the
// objects held when the drag started are kept here, and each write is "the start of each object plus
// the whole delta" (P7.16): an int with a small multiplier still moves, and with several objects bound
// each moves by the same delta (P2.4). The drag starts past a few pixels, so a click changes nothing;
// Shift makes the step ten times larger and Ctrl ten times smaller, and Esc puts every object back
// where it started.
internal sealed class LabelScrub : IMessageFilter, IDisposable
{
    private const int Threshold = 3;
    private const int KeyDownMessage = 0x0100;

    private readonly WinFormsRow Row;
    private readonly Control Label;

    private Point Start;
    private bool Pressed;
    private bool Dragging;
    private object?[] From = [];

    // The last delta written, so a move that does not change it writes nothing.
    private double Written;

    public LabelScrub(WinFormsRow row, Control label)
    {
        Row = row;
        Label = label;

        label.MouseDown += OnMouseDown;
        label.MouseMove += OnMouseMove;
        label.MouseUp += (_, _) => End();
        label.MouseCaptureChanged += (_, _) =>
        {
            if (!Label.Capture)
                End();
        };
    }

    private InspectorNode Node => Row.Node;

    // Whether a node scrubs now: a number with a ScrubMultiplier (P7.17), which can be written.
    public static bool CanScrub(InspectorNode node)
    {
        return !node.IsGroup && node.ScrubMultiplier is { } multiplier && multiplier != 0 && IsNumber(node.ValueType)
            && !node.ReadOnly && !node.IsCompromised;
    }

    // Esc while dragging puts every object back where the drag started, and goes no further.
    public bool PreFilterMessage(ref Message m)
    {
        if (!Dragging || m.Msg != KeyDownMessage || (Keys)(int)m.WParam != Keys.Escape)
            return false;

        var from = From;
        End();
        Row.Write(() => Node.SetValues(from));
        return true;
    }

    public void Dispose()
    {
        End();
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !CanScrub(Node))
            return;

        Start = Cursor.Position;
        Pressed = true;
        Dragging = false;
        Label.Capture = true;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!Pressed)
            return;

        var point = Cursor.Position;
        var distance = Node.ScrubAxis == ScrubAxis.Vertical ? Start.Y - point.Y : point.X - Start.X;

        if (!Dragging)
        {
            if (Math.Abs(distance) < Threshold)
                return;

            Dragging = true;
            From = Node.ViewValues.ToArray();
            Written = 0;
            Application.AddMessageFilter(this);
        }

        var keys = Control.ModifierKeys;
        var step = (Node.ScrubMultiplier ?? 0) * (keys.HasFlag(Keys.Shift) ? 10 : keys.HasFlag(Keys.Control) ? 0.1 : 1);
        var delta = distance * step;

        if (delta == Written)
            return;

        Written = delta;
        var values = From.Select(value => (object?)(ToDouble(value) + delta)).ToArray();
        Row.Write(() => Node.SetValues(values));
    }

    private void End()
    {
        if (!Pressed)
            return;

        Pressed = false;

        if (Dragging)
        {
            Dragging = false;
            Application.RemoveMessageFilter(this);
        }

        if (Label.Capture)
            Label.Capture = false;
    }

    // The numeric types and their nullable forms; an enum is a choice, not a number.
    private static bool IsNumber(Type? type)
    {
        if (type == null)
            return false;

        type = Nullable.GetUnderlyingType(type) ?? type;

        return !type.IsEnum && Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
            or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double
            or TypeCode.Decimal;
    }

    // A null start (a nullable number with no value) counts as zero.
    private static double ToDouble(object? value)
    {
        return value is IConvertible convertible ? convertible.ToDouble(CultureInfo.InvariantCulture) : 0;
    }
}
