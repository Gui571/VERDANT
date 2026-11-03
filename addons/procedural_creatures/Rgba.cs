using System.Globalization;
namespace Verdant;

// Mesmos floats e quantização do raster anterior: nenhuma conversão de gama.
public readonly record struct Rgba(float R, float G, float B, float A = 1f)
{
    public Rgba(string hex) : this(0,0,0,0)
    {
        hex = hex.TrimStart('#');
        if (hex.Length is not (6 or 8)) throw new ArgumentException("Use RRGGBB ou RRGGBBAA", nameof(hex));
        R = byte.Parse(hex.AsSpan(0,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture)/255f;
        G = byte.Parse(hex.AsSpan(2,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture)/255f;
        B = byte.Parse(hex.AsSpan(4,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture)/255f;
        A = hex.Length==8 ? byte.Parse(hex.AsSpan(6,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture)/255f : 1f;
    }
}
public readonly record struct PixelPoint(int X, int Y);
