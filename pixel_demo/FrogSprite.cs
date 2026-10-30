using Godot;
namespace Verdant;

/// <summary>A 48px, flat-palette procedural micro-chibi sprite. All movement snaps
/// to this logical pixel grid; the feet stay planted during the four-second idle.</summary>
public sealed class FrogSprite
{
    public const int Size = 48;
    public readonly Raster Buffer = new(Size, Size);
    static readonly Color Ink = new("171923"), Green = new("91c66c"), GreenShadow = new("507d4e"),
        Skin = new("f7d89b"), SkinShadow = new("d8a86a"), Blue = new("478ce8"), BlueShadow = new("294d99"),
        Iron = new("d9e7ee"), IronShadow = new("8296ad"), Leather = new("956342");
    readonly float headScale;
    public FrogSprite(float headScale = 1) { this.headScale = Math.Clamp(headScale, .85f, 1.15f); }
    void R(int x, int y, int w, int h, Color c) => Buffer.Rect(x, y, w, h, c);
    void P(Color c, params Vector2[] points) => Buffer.Polygon(points, c);
    public Raster Render(double seconds, bool walking = false, bool back = false)
    {
        Buffer.Clear();
        int frame = (int)Math.Floor(seconds * 8) % 32;
        int breath = frame >= 9 && frame < 20 ? -1 : 0;
        int sway = frame >= 18 && frame < 27 ? 1 : 0;
        int step = walking ? ((int)(seconds * 6) % 4 < 2 ? 1 : -1) : 0;
        int b = breath;
        // Short cloak behind the torso, with only one shadow color.
        P(Ink, new(14, 25+b), new(29, 25+b), new(32+sway, 36), new(29, 38), new(12+sway, 37));
        P(BlueShadow, new(16, 27+b), new(27, 27+b), new(29+sway, 35), new(15+sway, 35));
        // Planted boots are independent of the breathing offset.
        R(16, 35-step, 6, 5, Ink); R(24, 35+step, 6, 5, Ink);
        R(17, 36-step, 3, 2, Leather); R(25, 36+step, 3, 2, Leather);
        // Tiny pill torso and very short arms.
        R(15, 25+b, 16, 11, Ink); R(17, 26+b, 12, 8, Blue);
        R(17, 31+b, 12, 3, BlueShadow); R(17, 34+b, 12, 2, Leather);
        R(22, 34+b, 3, 2, Iron);
        R(12, 27+b, 5, 7, Ink); R(13, 28+b, 3, 3, Blue); R(13, 31+b, 3, 2, Skin);
        // Oversized iron sword: sturdy stepped tip, two flat material tones.
        int weapon = breath;
        P(Ink, new(38, 10+weapon), new(42, 14+weapon), new(42, 29+weapon), new(39, 32+weapon), new(35, 29+weapon), new(35, 15+weapon));
        P(Iron, new(38, 13+weapon), new(40, 15+weapon), new(40, 28+weapon), new(38, 30+weapon), new(37, 28+weapon), new(37, 16+weapon));
        R(39, 16+weapon, 2, 13, IronShadow);
        R(32, 30+weapon, 13, 4, Ink); R(34, 31+weapon, 9, 1, IronShadow);
        R(36, 34+weapon, 5, 5, Ink); R(38, 34+weapon, 1, 3, Leather);
        R(29, 29+b, 8, 6, Ink); R(30, 30+b, 5, 3, BlueShadow); R(34, 30+b, 3, 3, Skin);
        // Huge stepped hood: parameterized silhouette, no source sprite or mask.
        int half = (int)Math.Round(11 * headScale), left = 22-half, right = 22+half;
        P(Ink, new(left+3, 6+b), new(right-3, 6+b), new(right-3, 8+b), new(right, 8+b), new(right+2, 12+b), new(right+2, 23+b), new(right-1, 27+b), new(left+1, 27+b), new(left-2, 23+b), new(left-2, 12+b), new(left+1, 8+b), new(left+3, 8+b));
        P(Green, new(left+2, 10+b), new(right-2, 10+b), new(right, 13+b), new(right, 22+b), new(right-2, 25+b), new(left+2, 25+b), new(left, 22+b), new(left, 13+b));
        R(left+1, 21+b, right-left-2, 4, GreenShadow);
        // Frog eye lobes form the hood's characteristic top silhouette.
        foreach(int x in new[]{left+1,right-7}) { R(x, 5+b, 7, 8, Ink); R(x+1, 6+b, 5, 5, Green); R(x+2, 7+b, 2, 2, Ink); }
        if (!back)
        {
            P(Ink, new(left+3, 14+b), new(right-3, 14+b), new(right-1, 17+b), new(right-1, 23+b), new(right-4, 26+b), new(left+4, 26+b), new(left+1, 23+b), new(left+1, 17+b));
            P(Skin, new(left+4, 16+b), new(right-4, 16+b), new(right-3, 18+b), new(right-3, 23+b), new(left+3, 23+b), new(left+3, 18+b));
            R(left+4, 23+b, right-left-8, 1, SkinShadow);
            // Two pixel dots; no nose, mouth, cheek or facial shading noise.
            if(frame != 29) { R(18, 20+b, 1, 2, Ink); R(25, 20+b, 1, 2, Ink); }
            else { R(18, 21+b, 1, 1, Ink); R(25, 21+b, 1, 1, Ink); }
        }
        return Buffer;
    }
}
