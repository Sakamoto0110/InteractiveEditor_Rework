namespace InteractiveEditor.Primitives;

// The conversions with the Padding of WinForms, in int (explicit back to it, rounded, as PxPoint does
// to Point), and with the Thickness of WPF, in double (implicit both ways).
public partial struct PxPadding
{
    public static implicit operator PxPadding(System.Windows.Forms.Padding padding) =>
        new PxPadding(padding.Left, padding.Top, padding.Right, padding.Bottom);

    public static explicit operator System.Windows.Forms.Padding(PxPadding padding) =>
        new System.Windows.Forms.Padding((int)Math.Round(padding.Left), (int)Math.Round(padding.Top),
            (int)Math.Round(padding.Right), (int)Math.Round(padding.Bottom));

    public static implicit operator PxPadding(System.Windows.Thickness thickness) =>
        new PxPadding(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

    public static implicit operator System.Windows.Thickness(PxPadding padding) =>
        new System.Windows.Thickness(padding.Left, padding.Top, padding.Right, padding.Bottom);
}
