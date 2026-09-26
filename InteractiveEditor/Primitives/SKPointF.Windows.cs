namespace InteractiveEditor.Primitives;

public partial struct SKPointF
{
    public static implicit operator System.Windows.Point(SKPointF p) => new System.Windows.Point(p.X, p.Y);
}
