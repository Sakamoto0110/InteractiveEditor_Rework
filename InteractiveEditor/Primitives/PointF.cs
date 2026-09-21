

namespace InteractiveEditor.Primitives;

public struct PointF : IEquatable<PointF>
{
    public static readonly PointF Empty = new PointF(0f, 0f);

    private float x;
    private float y;

    public PointF(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X
    {
        readonly get => x;
        set => x = value;
    }

    public float Y
    {
        readonly get => y;
        set => y = value;
    }

    public readonly bool IsEmpty => X == 0f && Y == 0f;

    public static implicit operator System.Drawing.PointF(PointF p) => new System.Drawing.PointF(p.X, p.Y);
    public static implicit operator System.Windows.Point(PointF p) => new System.Windows.Point(p.X, p.Y);

    public static PointF operator +(PointF pt, Size sz) => Add(pt, sz);
    public static PointF operator -(PointF pt, Size sz) => Subtract(pt, sz);
    public static PointF operator +(PointF pt, SizeF sz) => Add(pt, sz);
    public static PointF operator -(PointF pt, SizeF sz) => Subtract(pt, sz);

    public static bool operator ==(PointF left, PointF right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(PointF left, PointF right) => !(left == right);

    public static PointF Add(PointF pt, Size sz) => new PointF(pt.X + sz.Width, pt.Y + sz.Height);
    public static PointF Subtract(PointF pt, Size sz) => new PointF(pt.X - sz.Width, pt.Y - sz.Height);
    public static PointF Add(PointF pt, SizeF sz) => new PointF(pt.X + sz.Width, pt.Y + sz.Height);
    public static PointF Subtract(PointF pt, SizeF sz) => new PointF(pt.X - sz.Width, pt.Y - sz.Height);

    public readonly bool Equals(PointF other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is PointF other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => $"{X},{Y}";
}
 