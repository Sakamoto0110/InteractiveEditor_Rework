using PixieLib;

namespace InteractiveEditor.WinForms;

// The conversions between the PixieLib primitives and the WinForms types that PixieLib does not know
// (it has the System.Drawing ones itself, for the double types and the color). They are extensions,
// so neither PixieLib nor the core depends on WinForms (PixieLib 4.4).
public static class PrimitiveConversions
{
    // The Padding of WinForms is in int: to it the sides are rounded, as PixieLib does to a Point.
    public static System.Windows.Forms.Padding ToWinForms(this PxPadding padding) =>
        new(Round(padding.Left), Round(padding.Top), Round(padding.Right), Round(padding.Bottom));

    public static PxPadding ToPrimitive(this System.Windows.Forms.Padding padding) =>
        new(padding.Left, padding.Top, padding.Right, padding.Bottom);

    private static int Round(double value) => (int)Math.Round(value);
}
