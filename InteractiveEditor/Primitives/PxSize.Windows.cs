namespace InteractiveEditor.Primitives;

// The conversions with the Size of WPF. To it the conversion is explicit, because that Size throws
// ArgumentException on a negative width or height, which PxSize takes.
public partial struct PxSize
{
    public static implicit operator PxSize(System.Windows.Size size) => new PxSize(size.Width, size.Height);
    public static explicit operator System.Windows.Size(PxSize size) => new System.Windows.Size(size.Width, size.Height);
}
