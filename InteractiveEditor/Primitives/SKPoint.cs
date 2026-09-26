using System;
using System.Collections.Generic;
 

namespace InteractiveEditor.Primitives;

public struct SKPoint : IEquatable<SKPoint>
{
    public static readonly SKPoint Empty = new SKPoint(0, 0);

    private int x;
    private int y;

    public SKPoint(int x, int y)
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

    public static implicit operator System.Drawing.Point(SKPoint p) => new System.Drawing.Point(p.X, p.Y);
    public static implicit operator System.Windows.Point(SKPoint p) => new System.Windows.Point(p.X, p.Y);
    public static explicit operator SKSize(SKPoint p) => new SKSize(p.X, p.Y);

    public static SKPoint operator +(SKPoint left, SKPoint right) => new SKPoint(left.X + right.X, left.Y + right.Y);
    public static SKPoint operator -(SKPoint left, SKPoint right) => new SKPoint(left.X - right.X, left.Y - right.Y);

    public static SKPoint operator +(SKPoint pt, SKSize sz) => Add(pt, sz);
    public static SKPoint operator -(SKPoint pt, SKSize sz) => Subtract(pt, sz);

    public static SKPoint operator *(SKPoint pt, int scalar) => new SKPoint(pt.X * scalar, pt.Y * scalar);
    public static SKPoint operator *(int scalar, SKPoint pt) => pt * scalar;
    public static SKPoint operator /(SKPoint pt, int scalar) => new SKPoint(pt.X / scalar, pt.Y / scalar);

    public static SKPoint operator +(SKPoint pt) => pt;
    public static SKPoint operator -(SKPoint pt) => new SKPoint(-pt.X, -pt.Y);

    public static bool operator ==(SKPoint left, SKPoint right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(SKPoint left, SKPoint right) => !(left == right);

    public static SKPoint Add(SKPoint pt, SKSize sz) => new SKPoint(pt.X + sz.Width, pt.Y + sz.Height);
    public static SKPoint Subtract(SKPoint pt, SKSize sz) => new SKPoint(pt.X - sz.Width, pt.Y - sz.Height);

    public readonly bool Equals(SKPoint other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is SKPoint other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => $"{X},{Y}";
}
 