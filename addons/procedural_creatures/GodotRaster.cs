using Godot;
namespace Verdant;

// Fronteira com o motor: o núcleo só conhece RGBA e System.Numerics.
public static class GodotRaster
{
    public static Rgba ToRgba(this Color c) => new(c.R,c.G,c.B,c.A);
    public static Color ToGodot(this Rgba c) => new(c.R,c.G,c.B,c.A);
    public static Image Image(this Raster r) => Godot.Image.CreateFromData(r.Width,r.Height,false,Godot.Image.Format.Rgba8,r.Pixels);
    // Compatibilidade do renderer legado e dos fundos, sem alterar suas fórmulas.
    public static void Pixel(this Raster r,int x,int y,Color c) => r.Pixel(x,y,c.ToRgba());
    public static void Rect(this Raster r,int x,int y,int w,int h,Color c) => r.Rect(x,y,w,h,c.ToRgba());
    public static void Ellipse(this Raster r,float x,float y,float rx,float ry,Color c,Color? edge=null) => r.Ellipse(x,y,rx,ry,c.ToRgba(),edge?.ToRgba());
    public static void Line(this Raster r,Vector2 a,Vector2 b,float width,Color c) => r.Line(new(a.X,a.Y),new(b.X,b.Y),width,c.ToRgba());
    public static void Polygon(this Raster r,Vector2[] points,Color c) => r.Polygon(points.Select(p=>new System.Numerics.Vector2(p.X,p.Y)).ToArray(),c.ToRgba());
}
