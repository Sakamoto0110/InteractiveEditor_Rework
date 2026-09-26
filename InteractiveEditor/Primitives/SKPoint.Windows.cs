namespace InteractiveEditor.Primitives;

public partial struct SKPoint
{
    public static implicit operator System.Windows.Point(SKPoint p) => new System.Windows.Point(p.X, p.Y);
}
