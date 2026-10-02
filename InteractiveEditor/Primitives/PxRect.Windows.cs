namespace InteractiveEditor.Primitives;

// The conversions with the Rect of WPF, as with its Size: explicit to it, because that Rect throws
// ArgumentException on a negative width or height, which PxRect takes.
public partial struct PxRect
{
    public static implicit operator PxRect(System.Windows.Rect rect) => new PxRect(rect.X, rect.Y, rect.Width, rect.Height);

    public static explicit operator System.Windows.Rect(PxRect rect) =>
        new System.Windows.Rect(rect.X, rect.Y, rect.Width, rect.Height);
}
