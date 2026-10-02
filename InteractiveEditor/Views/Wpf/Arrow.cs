using System.Windows;
using System.Windows.Media;

namespace InteractiveEditor.Views.Wpf;

// A small triangle pointing one way: the arrow of a group (P7.10) and those of the list's buttons. Drawn,
// not a character, so it does not depend on the font, the same size as the WinForms view's.
internal static class Arrow
{
    private const double Size = 8;

    public enum Pointing
    {
        Right,
        Down,
        Up,
    }

    // The corners of the triangle, centred across the given width and on its own height.
    public static PointCollection Points(Pointing pointing, double width)
    {
        var half = Size / 2;
        var x = Math.Floor(width / 2);
        var y = half;

        Point[] points = pointing switch
        {
            Pointing.Right => [new(x - half / 2, y - half), new(x + half / 2 + 1, y), new(x - half / 2, y + half)],
            Pointing.Up => [new(x - half, y + half / 2), new(x + half, y + half / 2), new(x, y - half / 2 - 1)],
            _ => [new(x - half, y - half / 2), new(x + half, y - half / 2), new(x, y + half / 2 + 1)],
        };

        var collection = new PointCollection(points);
        collection.Freeze();
        return collection;
    }
}
