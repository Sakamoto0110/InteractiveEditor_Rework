using InteractiveEditor.Primitives;

namespace InteractiveEditor.Wpf;

/// <summary>
/// Conversions between the core primitives and WPF types. They are extensions rather than implicit
/// operators on the primitives so that the core does not depend on WPF.
/// </summary>
public static class PrimitiveConversions
{
    public static System.Windows.Media.Color ToWpf(this Color color) => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    public static System.Windows.Media.Color ToWpf(this ColorHSL color) => ((Color)color).ToWpf();
    public static System.Windows.Point ToWpf(this Point p) => new System.Windows.Point(p.X, p.Y);
    public static System.Windows.Point ToWpf(this PointF p) => new System.Windows.Point(p.X, p.Y);
    public static System.Windows.Size ToWpf(this Size size) => new System.Windows.Size(size.Width, size.Height);
    public static System.Windows.Size ToWpf(this SizeF size) => new System.Windows.Size(size.Width, size.Height);

    public static Color ToPrimitive(this System.Windows.Media.Color color) => new Color(color.A, color.R, color.G, color.B);
}
