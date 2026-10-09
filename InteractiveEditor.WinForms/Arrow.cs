using System.Drawing;
using System.Windows.Forms;

namespace InteractiveEditor.WinForms;

// A small triangle in the middle of an area, pointing one way: the arrow of a group (P7.10) and those of
// the list's buttons. Drawn, not a character, so it does not depend on the font.
internal static class Arrow
{
    private const int Size = 8;

    public static void Paint(Graphics graphics, Rectangle area, ArrowDirection direction, Color color)
    {
        var half = Size / 2;
        var x = area.Left + area.Width / 2;
        var y = area.Top + area.Height / 2;

        Point[] points = direction switch
        {
            ArrowDirection.Right => [new(x - half / 2, y - half), new(x + half / 2 + 1, y), new(x - half / 2, y + half)],
            ArrowDirection.Left => [new(x + half / 2, y - half), new(x - half / 2 - 1, y), new(x + half / 2, y + half)],
            ArrowDirection.Up => [new(x - half, y + half / 2), new(x + half, y + half / 2), new(x, y - half / 2 - 1)],
            _ => [new(x - half, y - half / 2), new(x + half, y - half / 2), new(x, y + half / 2 + 1)],
        };

        using var brush = new SolidBrush(color);
        graphics.FillPolygon(brush, points);
    }
}
