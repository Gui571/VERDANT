using Godot;
namespace Verdant;

public sealed class Raster
{
    public readonly int Width, Height;
    public readonly byte[] Pixels;
    public int Clipped { get; private set; }
    public Raster(int w, int h) { Width = w; Height = h; Pixels = new byte[w * h * 4]; }
    public void Clear() { Array.Clear(Pixels); Clipped = 0; }
    public void Pixel(int x, int y, Color c) { if (x < 0 || y < 0 || x >= Width || y >= Height) { Clipped++; return; } int i = (y * Width + x) * 4; Pixels[i] = (byte)(Math.Clamp(c.R, 0, 1) * 255); Pixels[i + 1] = (byte)(Math.Clamp(c.G, 0, 1) * 255); Pixels[i + 2] = (byte)(Math.Clamp(c.B, 0, 1) * 255); Pixels[i + 3] = (byte)(Math.Clamp(c.A, 0, 1) * 255); }
    public void Rect(int x, int y, int w, int h, Color c) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Pixel(i, j, c); }
    public void Ellipse(float x, float y, float rx, float ry, Color c, Color? edge = null)
    {
        for (int j = (int)(y - ry) - 1; j <= y + ry + 1; j++) for (int i = (int)(x - rx) - 1; i <= x + rx + 1; i++)
            {
                float u = (i - x) / rx, v = (j - y) / ry, d = u * u + v * v; if (d > 1) continue;
                var col = edge.HasValue && d > .83f ? edge.Value : c;
                Pixel(i, j, col);
            }
    }
    public void Line(Vector2 a, Vector2 b, float width, Color c) { var delta = b - a; float len = delta.LengthSquared(); for (int y = (int)MathF.Floor(Math.Min(a.Y, b.Y) - width); y <= Math.Max(a.Y, b.Y) + width; y++) for (int x = (int)MathF.Floor(Math.Min(a.X, b.X) - width); x <= Math.Max(a.X, b.X) + width; x++) { float u = len == 0 ? 0 : Math.Clamp(((x - a.X) * delta.X + (y - a.Y) * delta.Y) / len, 0, 1); float dx = x - a.X - u * delta.X, dy = y - a.Y - u * delta.Y; if (dx * dx + dy * dy <= width * width) Pixel(x, y, c); } }
    public void Polygon(Vector2[] points, Color color) { float min = points.Min(p => p.Y), max = points.Max(p => p.Y); var cuts = new List<float>(); for (int y = (int)min; y <= max; y++) { cuts.Clear(); for (int i = 0, j = points.Length - 1; i < points.Length; j = i++) { var a = points[i]; var b = points[j]; if ((a.Y > y) != (b.Y > y)) cuts.Add(a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y)); } cuts.Sort(); for (int i = 0; i + 1 < cuts.Count; i += 2) for (int x = (int)Math.Ceiling(cuts[i]); x <= cuts[i + 1]; x++) Pixel(x, y, color); } }
    public Image Image() => Godot.Image.CreateFromData(Width, Height, false, Godot.Image.Format.Rgba8, Pixels);
}
