

namespace InteractiveEditor.Primitives;

public struct PxPointF : IEquatable<PxPointF>
{
    public static readonly PxPointF Empty = new PxPointF(0f, 0f);

    private float x;
    private float y;

    public PxPointF(float x, float y)
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

    public static implicit operator System.Drawing.PointF(PxPointF p) => new System.Drawing.PointF(p.X, p.Y);

    public static PxPointF operator +(PxPointF pt, PxSize sz) => Add(pt, sz);
    public static PxPointF operator -(PxPointF pt, PxSize sz) => Subtract(pt, sz);
    public static PxPointF operator +(PxPointF pt, PxSizeF sz) => Add(pt, sz);
    public static PxPointF operator -(PxPointF pt, PxSizeF sz) => Subtract(pt, sz);

    public static bool operator ==(PxPointF left, PxPointF right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(PxPointF left, PxPointF right) => !(left == right);

    public static PxPointF Add(PxPointF pt, PxSize sz) => new PxPointF(pt.X + sz.Width, pt.Y + sz.Height);
    public static PxPointF Subtract(PxPointF pt, PxSize sz) => new PxPointF(pt.X - sz.Width, pt.Y - sz.Height);
    public static PxPointF Add(PxPointF pt, PxSizeF sz) => new PxPointF(pt.X + sz.Width, pt.Y + sz.Height);
    public static PxPointF Subtract(PxPointF pt, PxSizeF sz) => new PxPointF(pt.X - sz.Width, pt.Y - sz.Height);

    public readonly bool Equals(PxPointF other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is PxPointF other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => $"{X},{Y}";
}
 