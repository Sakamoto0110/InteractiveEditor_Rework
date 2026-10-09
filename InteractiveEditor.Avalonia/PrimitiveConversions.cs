using InteractiveEditor.Primitives;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaSize = Avalonia.Size;

namespace InteractiveEditor.Avalonia;

/// <summary>
/// Conversions between the core primitives and Avalonia types. They are extensions rather than implicit
/// operators on the primitives so that the core does not depend on Avalonia.
/// </summary>
public static class PrimitiveConversions
{
    public static AvaloniaColor ToAvalonia(this Color color) => AvaloniaColor.FromArgb(color.A, color.R, color.G, color.B);
    public static AvaloniaColor ToAvalonia(this ColorHSL color) => ((Color)color).ToAvalonia();
    public static AvaloniaPoint ToAvalonia(this Point p) => new AvaloniaPoint(p.X, p.Y);
    public static AvaloniaPoint ToAvalonia(this PointF p) => new AvaloniaPoint(p.X, p.Y);
    public static AvaloniaSize ToAvalonia(this Size size) => new AvaloniaSize(size.Width, size.Height);
    public static AvaloniaSize ToAvalonia(this SizeF size) => new AvaloniaSize(size.Width, size.Height);

    public static Color ToPrimitive(this AvaloniaColor color) => new Color(color.A, color.R, color.G, color.B);
}
