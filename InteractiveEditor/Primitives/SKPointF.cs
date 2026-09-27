

namespace InteractiveEditor.Primitives;

public struct SKPointF : IEquatable<SKPointF>
{
    public static readonly SKPointF Empty = new SKPointF(0f, 0f);

    private float x;
    private float y;

    public SKPointF(float x, float y)
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

    public static implicit operator System.Drawing.PointF(SKPointF p) => new System.Drawing.PointF(p.X, p.Y);

    public static SKPointF operator +(SKPointF pt, SKSize sz) => Add(pt, sz);
    public static SKPointF operator -(SKPointF pt, SKSize sz) => Subtract(pt, sz);
    public static SKPointF operator +(SKPointF pt, SKSizeF sz) => Add(pt, sz);
    public static SKPointF operator -(SKPointF pt, SKSizeF sz) => Subtract(pt, sz);

    public static bool operator ==(SKPointF left, SKPointF right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(SKPointF left, SKPointF right) => !(left == right);

    public static SKPointF Add(SKPointF pt, SKSize sz) => new SKPointF(pt.X + sz.Width, pt.Y + sz.Height);
    public static SKPointF Subtract(SKPointF pt, SKSize sz) => new SKPointF(pt.X - sz.Width, pt.Y - sz.Height);
    public static SKPointF Add(SKPointF pt, SKSizeF sz) => new SKPointF(pt.X + sz.Width, pt.Y + sz.Height);
    public static SKPointF Subtract(SKPointF pt, SKSizeF sz) => new SKPointF(pt.X - sz.Width, pt.Y - sz.Height);

    public readonly bool Equals(SKPointF other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is SKPointF other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => $"{X},{Y}";
}
 