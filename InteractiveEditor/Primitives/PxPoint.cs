using System;
using System.Collections.Generic;
 

namespace InteractiveEditor.Primitives;

public struct PxPoint : IEquatable<PxPoint>
{
    public static readonly PxPoint Empty = new PxPoint(0, 0);

    private int x;
    private int y;

    public PxPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X
    {
        readonly get => x;
        set => x = value;
    }

    public int Y
    {
        readonly get => y;
        set => y = value;
    }

    public readonly bool IsEmpty => X == 0 && Y == 0;

    public static implicit operator System.Drawing.Point(PxPoint p) => new System.Drawing.Point(p.X, p.Y);
    public static explicit operator PxSize(PxPoint p) => new PxSize(p.X, p.Y);

    public static PxPoint operator +(PxPoint left, PxPoint right) => new PxPoint(left.X + right.X, left.Y + right.Y);
    public static PxPoint operator -(PxPoint left, PxPoint right) => new PxPoint(left.X - right.X, left.Y - right.Y);

    public static PxPoint operator +(PxPoint pt, PxSize sz) => Add(pt, sz);
    public static PxPoint operator -(PxPoint pt, PxSize sz) => Subtract(pt, sz);

    public static PxPoint operator *(PxPoint pt, int scalar) => new PxPoint(pt.X * scalar, pt.Y * scalar);
    public static PxPoint operator *(int scalar, PxPoint pt) => pt * scalar;
    public static PxPoint operator /(PxPoint pt, int scalar) => new PxPoint(pt.X / scalar, pt.Y / scalar);

    public static PxPoint operator +(PxPoint pt) => pt;
    public static PxPoint operator -(PxPoint pt) => new PxPoint(-pt.X, -pt.Y);

    public static bool operator ==(PxPoint left, PxPoint right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(PxPoint left, PxPoint right) => !(left == right);

    public static PxPoint Add(PxPoint pt, PxSize sz) => new PxPoint(pt.X + sz.Width, pt.Y + sz.Height);
    public static PxPoint Subtract(PxPoint pt, PxSize sz) => new PxPoint(pt.X - sz.Width, pt.Y - sz.Height);

    public readonly bool Equals(PxPoint other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is PxPoint other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => $"{X},{Y}";
}
 