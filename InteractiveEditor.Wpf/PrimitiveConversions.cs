using PixieLib;

namespace InteractiveEditor.Wpf;

/// <summary>
/// Conversions between the PixieLib primitives and WPF types. They are extensions rather than implicit
/// operators so that neither PixieLib nor the core depends on WPF.
/// </summary>
public static class PrimitiveConversions
{
    public static System.Windows.Media.Color ToWpf(this PxColorRgba color) => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    public static System.Windows.Media.Color ToWpf(this PxColorHsl color) => PxColorHsl.ToRgba(color).ToWpf();
    public static System.Windows.Point ToWpf(this PxPointi p) => new System.Windows.Point(p.X, p.Y);
    public static System.Windows.Point ToWpf(this PxPointf p) => new System.Windows.Point(p.X, p.Y);
    public static System.Windows.Size ToWpf(this PxSizei size) => new System.Windows.Size(size.Width, size.Height);
    public static System.Windows.Size ToWpf(this PxSizef size) => new System.Windows.Size(size.Width, size.Height);

    // PxColorRgba takes the alpha last, the other way around from Color.FromArgb.
    public static PxColorRgba ToPrimitive(this System.Windows.Media.Color color) => new PxColorRgba(color.R, color.G, color.B, color.A);
}
