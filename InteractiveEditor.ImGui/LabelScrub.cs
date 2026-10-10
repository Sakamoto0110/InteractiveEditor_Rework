using System.Numerics;
using Hexa.NET.ImGui;
using InteractiveEditor.Options;
using InteractiveEditor.Views;
using Gui = Hexa.NET.ImGui.ImGui;

namespace InteractiveEditor.ImGui;

// Scrubbing on a row's label (P7.18): dragging the label moves the number by the distance times the node's
// ScrubMultiplier, across or up and down (ScrubAxis), and writes while it drags (P2.12). The label is an
// ImGui item, active while the mouse holds it, and ImGui measures the drag from where it was pressed. The
// values the objects held when the drag started are kept here, and each write is "the start of each object
// plus the whole delta" (P7.16): an int with a small multiplier still moves, and with several objects bound
// each moves by the same delta (P2.4). The drag starts past a few pixels, so a click changes nothing;
// Shift makes the step ten times larger and Ctrl ten times smaller, and Esc puts every object back where
// it started.
internal sealed class LabelScrub(ImGuiRow row)
{
    private const float Threshold = 3f;

    // The values when the drag started; null while there is no drag.
    private object?[]? From;

    // The last delta written, so a move that does not change it writes nothing.
    private double Written;

    // Set by Esc during a drag, until the mouse lets go of the label.
    private bool Cancelled;

    private InspectorNode Node => row.Node;

    // Follows the mouse on the label, which has to be the item drawn last.
    public void Update()
    {
        var active = Gui.IsItemActive();

        // Looked at before the label's state: with keyboard navigation on, ImGui lets go of the active
        // item on Esc itself, before the row is drawn.
        if (From is { } start && Gui.IsKeyPressed(ImGuiKey.Escape, false))
        {
            From = null;
            Cancelled = active;
            row.Write(() => Node.SetValues(start));
            return;
        }

        var scrubs = ViewRules.CanScrub(Node);

        if (!active)
            Cancelled = false;

        if (!active || !scrubs)
            From = null;

        if (!scrubs)
            return;

        var vertical = Node.ScrubAxis == ScrubAxis.Vertical;

        if (From != null || Gui.IsItemHovered())
            Gui.SetMouseCursor(vertical ? ImGuiMouseCursor.ResizeNs : ImGuiMouseCursor.ResizeEw);

        if (!active || Cancelled)
            return;

        // Zero until the mouse went past the threshold once.
        var drag = Gui.GetMouseDragDelta(ImGuiMouseButton.Left, Threshold);

        if (From == null)
        {
            if (drag == Vector2.Zero)
                return;

            From = Node.ViewValues.ToArray();
            Written = 0;
        }

        var io = Gui.GetIO();
        var step = (Node.ScrubMultiplier ?? 0) * (io.KeyShift ? 10 : io.KeyCtrl ? 0.1 : 1);
        var delta = (vertical ? -drag.Y : drag.X) * step;

        if (delta == Written)
            return;

        Written = delta;
        var values = From.Select(value => (object?)(ViewRules.ToDouble(value) + delta)).ToArray();
        row.Write(() => Node.SetValues(values));
    }
}
