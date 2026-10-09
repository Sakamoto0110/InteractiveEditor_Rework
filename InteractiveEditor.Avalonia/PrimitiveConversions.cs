using PixieLib;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaSize = Avalonia.Size;

namespace InteractiveEditor.Avalonia;

/// <summary>
/// Conversions between the PixieLib primitives and Avalonia types. They are extensions rather than implicit
/// operators so that neither PixieLib nor the core depends on Avalonia.
/// </summary>
public static class PrimitiveConversions
{
    public static AvaloniaColor ToAvalonia(this PxColorRgba color) => AvaloniaColor.FromArgb(color.A, color.R, color.G, color.B);
    public static AvaloniaColor ToAvalonia(this PxColorHsl color) => PxColorHsl.ToRgba(color).ToAvalonia();
    public static AvaloniaPoint ToAvalonia(this PxPointi p) => new AvaloniaPoint(p.X, p.Y);
    public static AvaloniaPoint ToAvalonia(this PxPointf p) => new AvaloniaPoint(p.X, p.Y);
    public static AvaloniaSize ToAvalonia(this PxSizei size) => new AvaloniaSize(size.Width, size.Height);
    public static AvaloniaSize ToAvalonia(this PxSizef size) => new AvaloniaSize(size.Width, size.Height);

    // PxColorRgba takes the alpha last, the other way around from Color.FromArgb.
    public static PxColorRgba ToPrimitive(this AvaloniaColor color) => new PxColorRgba(color.R, color.G, color.B, color.A);
}
