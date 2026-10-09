using System.Numerics;
using PixieLib;

namespace InteractiveEditor.ImGui;

// Conversions between the PixieLib primitives and the System.Numerics vectors ImGui works with: positions
// and sizes are Vector2, colors are RGBA Vector4 with components from 0 to 1.
public static class PrimitiveConversions
{
    public static Vector4 ToVector4(this PxColorRgba color) => new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    public static Vector4 ToVector4(this PxColorHsl color) => PxColorHsl.ToRgba(color).ToVector4();
    public static Vector2 ToVector2(this PxPointi p) => new(p.X, p.Y);
    public static Vector2 ToVector2(this PxPointf p) => new(p.X, p.Y);
    public static Vector2 ToVector2(this PxSizei size) => new(size.Width, size.Height);
    public static Vector2 ToVector2(this PxSizef size) => new(size.Width, size.Height);

    // From double, narrowed to ImGui's float.
    public static Vector2 ToVector2(this PxPoint p) => new((float)p.X, (float)p.Y);
    public static Vector2 ToVector2(this PxSize size) => new((float)size.Width, (float)size.Height);

    // From an ImGui RGBA color; X, Y, Z and W are R, G, B and A, the order PxColorRgba takes them in.
    public static PxColorRgba ToPrimitive(this Vector4 color) => new PxColorRgba(ToByte(color.X), ToByte(color.Y), ToByte(color.Z), ToByte(color.W));

    private static byte ToByte(float component) => (byte)MathF.Round(Math.Clamp(component, 0f, 1f) * 255f);
}
