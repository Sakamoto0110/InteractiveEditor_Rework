using System.Numerics;
using Hexa.NET.ImGui;
using PixieInspector.Views;
using Gui = Hexa.NET.ImGui.ImGui;

namespace PixieInspector.ImGui;

// An arc (EditorKind.Arc): a dial with the sector of the node, its center, and of the sibling its ArcSpread
// names, the width around that center, and a number field for each next to the dial. Dragging on the dial
// turns the center to the mouse; with Shift, it opens or closes the width around the center instead. Both
// are held in radians and shown in each node's Unit, and angles go counterclockwise from the right, with y
// up. With objects that hold different values, the dial and the fields show the first object's, as a row
// that scrubs does, and a change sets the same value in all of them. Each change is written as it is made.
internal sealed class ArcEditor(ImGuiRow row) : ImGuiEditor(row)
{
    private const float Turn = MathF.PI * 2;
    private const int Segments = 64;

    public override void Draw()
    {
        var spread = Node.ArcSpreadNode!;
        var center = Held(Node);
        var width = Held(spread);

        DrawDial(spread, center, width);
        Gui.SameLine();
        Gui.BeginGroup();
        DrawField(Node, "##center");
        DrawField(spread, "##spread");
        Gui.EndGroup();
    }

    private void DrawDial(InspectorNode spread, double center, double width)
    {
        var size = Gui.GetFrameHeight() * 2 + Gui.GetStyle().ItemSpacing.Y;
        var corner = Gui.GetCursorScreenPos();
        Gui.InvisibleButton("##dial", new Vector2(size, size));

        var hub = corner + new Vector2(size / 2);
        var radius = size / 2 - 1;
        var draw = Gui.GetWindowDrawList();

        draw.AddCircleFilled(hub, radius, Gui.GetColorU32(ImGuiCol.FrameBg), Segments);

        // The sector as a fan of triangles: wider than half a turn, it is not convex.
        var from = (float)(center - width / 2);
        var steps = Math.Max(1, (int)Math.Ceiling(Segments * Math.Min(width, Turn) / Turn));
        var step = (float)Math.Min(width, Turn) / steps;
        var fill = Gui.GetColorU32(ImGuiCol.SliderGrab, 0.45f);

        for (var i = 0; i < steps && width > 0; i++)
            draw.AddTriangleFilled(hub, Point(hub, radius, from + step * i), Point(hub, radius, from + step * (i + 1)), fill);

        draw.AddCircle(hub, radius, Gui.GetColorU32(ImGuiCol.Border), Segments);
        draw.AddLine(hub, Point(hub, radius, (float)center), Gui.GetColorU32(ImGuiCol.SliderGrabActive), 2f);

        if (Gui.IsItemHovered() && !Gui.IsItemActive())
            Gui.SetTooltip("Drag to turn the center; Shift and drag to open or close the arc.");

        if (!Gui.IsItemActive())
            return;

        var mouse = Gui.GetIO().MousePos - hub;
        var angle = Math.Atan2(-mouse.Y, mouse.X);

        if (Gui.GetIO().KeyShift)
        {
            if (spread.ReadOnly)
                return;

            var opened = Math.Min(Math.Abs(Wrap(angle - center)) * 2, Turn);

            if (Math.Abs(opened - width) > 1e-9)
                Row.Write(() => spread.SetValue(Shown(spread, opened)));
        }
        else if (Math.Abs(Wrap(angle - center)) > 1e-9)
        {
            Row.Write(() => Node.SetValue(Shown(Node, angle)));
        }
    }

    // A number field for one of the two angles, in its node's unit, with the unit's suffix after it.
    private void DrawField(InspectorNode node, string id)
    {
        var shown = ViewRules.ToDouble(node.ViewValue);
        var suffix = node.Unit?.Suffix;
        var room = Gui.GetContentRegionAvail().X;

        if (!string.IsNullOrEmpty(suffix))
            room -= Gui.CalcTextSize(suffix).X + Gui.GetStyle().ItemInnerSpacing.X;

        if (node.ReadOnly)
            Gui.BeginDisabled();

        Gui.SetNextItemWidth(room);

        if (Gui.InputDouble(id, ref shown, 0, 0, "%g"))
            Row.Write(() => node.SetValue(shown));

        if (!string.IsNullOrEmpty(suffix))
        {
            Gui.SameLine(0, Gui.GetStyle().ItemInnerSpacing.X);
            Gui.TextUnformatted(suffix);
        }

        if (node.ReadOnly)
            Gui.EndDisabled();
    }

    // The angle a node holds, in radians, from what its view shows.
    private static double Held(InspectorNode node)
    {
        var shown = ViewRules.ToDouble(node.ViewValue);
        return node.Unit?.ToHeld(shown) ?? shown;
    }

    // An angle in radians as the node takes it, in its unit.
    private static double Shown(InspectorNode node, double held) => node.Unit?.ToShown(held) ?? held;

    // An angle brought into (-π, π].
    private static double Wrap(double angle)
    {
        angle = Math.IEEERemainder(angle, Turn);
        return angle <= -Math.PI ? angle + Turn : angle;
    }

    // The point at an angle on the circle, with y up on the screen's y down.
    private static Vector2 Point(Vector2 hub, float radius, float angle) =>
        hub + new Vector2(MathF.Cos(angle), -MathF.Sin(angle)) * radius;
}
