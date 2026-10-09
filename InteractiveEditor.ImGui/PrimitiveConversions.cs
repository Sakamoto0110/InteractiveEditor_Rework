using System.Numerics;
using InteractiveEditor.Primitives;

namespace InteractiveEditor.ImGui;

/// <summary>
/// Conversions between the core primitives and the System.Numerics vectors ImGui works with: positions and
/// sizes are Vector2, colors are RGBA Vector4 with components from 0 to 1.
/// </summary>
public static class PrimitiveConversions
{
    public static Vector4 ToVector4(this Color color) => new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    public static Vector4 ToVector4(this ColorHSL color) => ((Color)color).ToVector4();
    public static Vector2 ToVector2(this Point p) => new(p.X, p.Y);
    public static Vector2 ToVector2(this PointF p) => new(p.X, p.Y);
    public static Vector2 ToVector2(this Size size) => new(size.Width, size.Height);
    public static Vector2 ToVector2(this SizeF size) => new(size.Width, size.Height);

    // From an ImGui RGBA color.
    public static Color ToPrimitive(this Vector4 color) => new Color(ToByte(color.W), ToByte(color.X), ToByte(color.Y), ToByte(color.Z));

    private static byte ToByte(float component) => (byte)MathF.Round(Math.Clamp(component, 0f, 1f) * 255f);
}
