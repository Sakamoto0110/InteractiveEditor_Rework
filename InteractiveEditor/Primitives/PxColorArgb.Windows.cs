namespace InteractiveEditor.Primitives;

// The conversions with the Color of WPF, in ARGB bytes like this one: both implicit. That Color also
// keeps the channels as scRGB floats; the conversion takes the bytes, which are what a control shows.
public partial struct PxColorArgb
{
    public static implicit operator System.Windows.Media.Color(PxColorArgb color) =>
        System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);

    public static implicit operator PxColorArgb(System.Windows.Media.Color color) => new PxColorArgb(color.A, color.R, color.G, color.B);
}
