namespace InteractiveEditor.Primitives;

public partial struct ArgbColor
{
    public static implicit operator System.Windows.Media.Color(ArgbColor color) => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    public static implicit operator ArgbColor(System.Windows.Media.Color color) => new ArgbColor(color.A, color.R, color.G, color.B);
}
