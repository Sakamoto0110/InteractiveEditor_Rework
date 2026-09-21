using System;
using System.Collections.Generic;
 

namespace InteractiveEditor.Primitives;

public struct Point : IEquatable<Point>
{
    public static readonly Point Empty = new Point(0, 0);

    private int x;
    private int y;

    public Point(int x, int y)
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

    public static implicit operator System.Drawing.Point(Point p) => new System.Drawing.Point(p.X, p.Y);
    public static implicit operator System.Windows.Point(Point p) => new System.Windows.Point(p.X, p.Y);
    public static explicit operator Size(Point p) => new Size(p.X, p.Y);

    public static Point operator +(Point left, Point right) => new Point(left.X + right.X, left.Y + right.Y);
    public static Point operator -(Point left, Point right) => new Point(left.X - right.X, left.Y - right.Y);

    public static Point operator +(Point pt, Size sz) => Add(pt, sz);
    public static Point operator -(Point pt, Size sz) => Subtract(pt, sz);

    public static Point operator *(Point pt, int scalar) => new Point(pt.X * scalar, pt.Y * scalar);
    public static Point operator *(int scalar, Point pt) => pt * scalar;
    public static Point operator /(Point pt, int scalar) => new Point(pt.X / scalar, pt.Y / scalar);

    public static Point operator +(Point pt) => pt;
    public static Point operator -(Point pt) => new Point(-pt.X, -pt.Y);

    public static bool operator ==(Point left, Point right) => left.X == right.X && left.Y == right.Y;
    public static bool operator !=(Point left, Point right) => !(left == right);

    public static Point Add(Point pt, Size sz) => new Point(pt.X + sz.Width, pt.Y + sz.Height);
    public static Point Subtract(Point pt, Size sz) => new Point(pt.X - sz.Width, pt.Y - sz.Height);

    public readonly bool Equals(Point other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is Point other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => $"{X},{Y}";
}
 