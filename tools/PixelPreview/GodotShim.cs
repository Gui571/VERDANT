// Mínimo da API do Godot usado pelo código dos sprites, para compilar e visualizar sem o motor.
using System.Globalization;
namespace Godot;

public struct Color : IEquatable<Color>
{
    public float R, G, B, A;
    public Color(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
    public Color(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6 && hex.Length != 8) throw new ArgumentException("hex: " + hex);
        R = int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber) / 255f;
        G = int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber) / 255f;
        B = int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber) / 255f;
        A = hex.Length == 8 ? int.Parse(hex.AsSpan(6, 2), NumberStyles.HexNumber) / 255f : 1f;
    }
    public Color Lerp(Color to, float t) => new(R + (to.R - R) * t, G + (to.G - G) * t, B + (to.B - B) * t, A + (to.A - A) * t);
    public bool Equals(Color o) => R == o.R && G == o.G && B == o.B && A == o.A;
    public override bool Equals(object? o) => o is Color c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);
}
public static class Colors { public static readonly Color White = new(1, 1, 1), Transparent = new(0, 0, 0, 0); }

public struct Vector2
{
    public float X, Y;
    public Vector2(float x, float y) { X = x; Y = y; }
    public static readonly Vector2 Zero = new(0, 0);
    public float LengthSquared() => X * X + Y * Y;
    public float Length() => MathF.Sqrt(LengthSquared());
    public Vector2 Normalized() { float l = Length(); return l == 0 ? this : new(X / l, Y / l); }
    public Vector2 Lerp(Vector2 to, float t) => new(X + (to.X - X) * t, Y + (to.Y - Y) * t);
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float k) => new(a.X * k, a.Y * k);
    public static Vector2 operator /(Vector2 a, float k) => new(a.X / k, a.Y / k);
}
public struct Vector2I { public int X, Y; public Vector2I(int x, int y) { X = x; Y = y; } }
public static class Mathf { public static float Lerp(float a, float b, float t) => a + (b - a) * t; }
public sealed class Image { public enum Format { Rgba8 } public static Image CreateFromData(int w, int h, bool mipmaps, Format f, byte[] data) => new(); }
