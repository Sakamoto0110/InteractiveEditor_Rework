using PixieLib;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaSize = Avalonia.Size;
using AvaloniaThickness = Avalonia.Thickness;

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

    // In double, PixieLib's default precision and Avalonia's, so these go both ways without loss.
    public static AvaloniaPoint ToAvalonia(this PxPoint p) => new AvaloniaPoint(p.X, p.Y);
    public static AvaloniaSize ToAvalonia(this PxSize size) => new AvaloniaSize(size.Width, size.Height);
    public static AvaloniaRect ToAvalonia(this PxRect rect) => new AvaloniaRect(rect.X, rect.Y, rect.Width, rect.Height);

    public static PxPoint ToPrimitive(this AvaloniaPoint p) => new PxPoint(p.X, p.Y);
    public static PxSize ToPrimitive(this AvaloniaSize size) => new PxSize(size.Width, size.Height);
    public static PxRect ToPrimitive(this AvaloniaRect rect) => new PxRect(rect.X, rect.Y, rect.Width, rect.Height);

    // The padding of the layout options (InspectorOptions.Padding) and Avalonia's thickness, as in the
    // other views.
    public static AvaloniaThickness ToAvalonia(this PxPadding padding) =>
        new AvaloniaThickness(padding.Left, padding.Top, padding.Right, padding.Bottom);
    public static PxPadding ToPrimitive(this AvaloniaThickness thickness) =>
        new PxPadding(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

    // PxColorRgba takes the alpha last, the other way around from Color.FromArgb.
    public static PxColorRgba ToPrimitive(this AvaloniaColor color) => new PxColorRgba(color.R, color.G, color.B, color.A);
}
