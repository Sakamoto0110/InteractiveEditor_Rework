namespace InteractiveEditor.Primitives;

// The conversions with the Point of WPF, in double like this one: nothing is lost, so both are implicit.
public partial struct PxPoint
{
    public static implicit operator PxPoint(System.Windows.Point p) => new PxPoint(p.X, p.Y);
    public static implicit operator System.Windows.Point(PxPoint p) => new System.Windows.Point(p.X, p.Y);
}
