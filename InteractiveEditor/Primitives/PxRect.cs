namespace InteractiveEditor.Primitives;

// A rectangle in double, what the layout step gives each part of a row (P8.2). The conversions with
// System.Drawing follow PxPoint: implicit from Rectangle and RectangleF, explicit back. The ones with
// the Rect of WPF follow PxSize, for the same reason (PxRect.Windows.cs).
public partial struct PxRect : IEquatable<PxRect>
{
    public static readonly PxRect Empty = new PxRect(0, 0, 0, 0);

    private double x;
    private double y;
    private double width;
    private double height;

    public PxRect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public PxRect(PxPoint location, PxSize size) : this(location.X, location.Y, size.Width, size.Height)
    {
    }

    public double X
    {
        readonly get => x;
        set => x = value;
    }

    public double Y
    {
        readonly get => y;
        set => y = value;
    }

    public double Width
    {
        readonly get => width;
        set => width = value;
    }

    public double Height
    {
        readonly get => height;
        set => height = value;
    }

    public readonly double Right => X + Width;
    public readonly double Bottom => Y + Height;

    public readonly bool IsEmpty => X == 0 && Y == 0 && Width == 0 && Height == 0;

    // The left and top edges are inside, the right and bottom ones are not.
    public readonly bool Contains(PxPoint point) => point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    public static implicit operator PxRect(System.Drawing.Rectangle rect) => new PxRect(rect.X, rect.Y, rect.Width, rect.Height);
    public static implicit operator PxRect(System.Drawing.RectangleF rect) => new PxRect(rect.X, rect.Y, rect.Width, rect.Height);

    public static explicit operator System.Drawing.Rectangle(PxRect rect) =>
        new System.Drawing.Rectangle((int)Math.Round(rect.X), (int)Math.Round(rect.Y), (int)Math.Round(rect.Width), (int)Math.Round(rect.Height));

    public static explicit operator System.Drawing.RectangleF(PxRect rect) =>
        new System.Drawing.RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);

    public static bool operator ==(PxRect left, PxRect right) => left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;
    public static bool operator !=(PxRect left, PxRect right) => !(left == right);

    public readonly bool Equals(PxRect other) => this == other;
    public override readonly bool Equals(object? obj) => obj is PxRect other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
    public override readonly string ToString() => FormattableString.Invariant($"{X},{Y},{Width},{Height}");
}
