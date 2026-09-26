namespace InteractiveEditor.Primitives;

public partial struct HslColor
{
    public static implicit operator System.Windows.Media.Color(HslColor color) => (ArgbColor)color;
    public static implicit operator HslColor(System.Windows.Media.Color color) => new HslColor(new ArgbColor(color.A, color.R, color.G, color.B));
}
