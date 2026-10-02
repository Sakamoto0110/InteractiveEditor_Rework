using System;
using System.Collections.Generic;
 

namespace InteractiveEditor.Primitives;

// A point in double, the only precision (P8.1). From System.Drawing nothing is lost, so the conversion
// is implicit; back to it the value is rounded (Point) or narrowed (PointF), so it is explicit. The
// Point of WPF is in double too, so both ways are implicit (PxPoint.Windows.cs, windows target only).
public partial struct PxPoint : IEquatable<PxPoint>
{
    public static readonly PxPoint Empty = new PxPoint(0, 0);

    private double x;
    private double y;

    public PxPoint(double x, double y)
    {
        X = x;
        Y = y;
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

    public readonly bool IsEmpty => X == 0 && Y == 0;

    public static implicit operator PxPoint(System.Drawing.Point p) => new PxPoint(p.X, p.Y);
    public static implicit operator PxPoint(System.Drawing.PointF p) => new PxPoint(p.X, p.Y);
    public static explicit operator System.Drawing.Point(PxPoint p) => new System.Drawing.Point((int)Math.Round(p.X), (int)Math.Round(p.Y));
    public static explicit operator System.Drawing.PointF(PxPoint p) => new System.Drawing.PointF((float)p.X, (float)p.Y);
    public static explicit operator PxSize(PxPoint p) => new PxSize(p.X, p.Y);

    public static PxPoint operator +(PxPoint left, PxPoint right) => new PxPoint(left.X + right.X, left.Y + right.Y);
    public static PxPoint operator -(PxPoint left, PxPoint right) => new PxPoint(left.X - right.X, left.Y - right.Y);

    public static PxPoint operator +(PxPoint pt, PxSize sz) => Add(pt, sz);
    public static PxPoint operator -(PxPoint pt, PxSize sz) => Subtract(pt, sz);

    public static PxPoint operator *(PxPoint pt, double scalar) => new PxPoint(pt.X * scalar, pt.Y * scalar);
    public static PxPoint operator *(double scalar, PxPoint pt) => pt * scalar;
    public static PxPoint operator /(PxPoint pt, double scalar) => new PxPoint(pt.X / scalar, pt.Y / scalar);

    public static PxPoint operator +(PxPoint pt) => pt;
    public static PxPoint operator -(PxPoint pt) => new PxPoint(-pt.X, -pt.Y);

    public static bool operator ==(PxPoint left, PxPoint right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(PxPoint left, PxPoint right) => !(left == right);

    public static PxPoint Add(PxPoint pt, PxSize sz) => new PxPoint(pt.X + sz.Width, pt.Y + sz.Height);
    public static PxPoint Subtract(PxPoint pt, PxSize sz) => new PxPoint(pt.X - sz.Width, pt.Y - sz.Height);

    public readonly bool Equals(PxPoint other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is PxPoint other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => FormattableString.Invariant($"{X},{Y}");
}
 