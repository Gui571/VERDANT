using Godot;
namespace Verdant;

/// <summary>Original ice-crest adventurer. Shapes are rasterized directly onto
/// the logical grid, with one-pixel contours and stable, body-local highlights.</summary>
public sealed class AzureSprite
{
    public const int Size = 48;
    public readonly Raster Buffer = new(Size, Size);
    static readonly Color Ink = new("20263e"), Navy = new("303f71"), BlueShadow = new("315daf"),
        Blue = new("528ee5"), Ice = new("97c9f0"), White = new("edf5ed"), Steel = new("a4b4c8"),
        Skin = new("f3c66f"), SkinShadow = new("c78b49"), Brown = new("77573f"), Gold = new("c29858");
    readonly int headWidth;
    public AzureSprite(float headScale = 1) { headWidth = (int)Math.Round(9 * Math.Clamp(headScale, .85f, 1.15f)); }
    void R(int x, int y, int w, int h, Color c) => Buffer.Rect(x, y, w, h, c);
    void P(Color c, params Vector2[] p) => Buffer.Polygon(p, c);
    public Raster Render(double seconds, bool walking = false, bool back = false)
    {
        Buffer.Clear();
        int phase = (int)Math.Floor(seconds * 8) % 32;
        int b = phase >= 10 && phase < 20 ? -1 : 0;
        int sway = phase >= 18 && phase < 27 ? 1 : 0;
        int step = walking ? ((int)(seconds * 6) % 4 < 2 ? 1 : -1) : 0;
        int left = 24-headWidth, right = 24+headWidth;
        // Short, angular cloak. The moving hem stays above the planted boots.
        P(Ink, new(17,25+b),new(31,25+b),new(34+sway,34),new(30,37),new(14,35));
        P(Navy,new(18,26+b),new(30,26+b),new(32+sway,33),new(29,35),new(16,34));
        R(17,30+b,2,4,BlueShadow);R(30+sway,31,2,3,Blue);
        // Axe haft is behind the body; bevels use discrete two-pixel clusters.
        P(Ink,new(9,14+b),new(12,14+b),new(17,36+b),new(14,37+b));
        P(Brown,new(10,17+b),new(11,17+b),new(16,35+b),new(15,35+b));
        P(Gold,new(10,17+b),new(11,17+b),new(14,28+b),new(13,28+b));
        P(Ink,new(9,10+b),new(13,11+b),new(16,16+b),new(13,20+b),new(8,19+b),new(5,15+b),new(7,13+b));
        P(Steel,new(9,12+b),new(12,12+b),new(14,16+b),new(12,18+b),new(8,17+b),new(7,15+b));
        P(White,new(9,12+b),new(11,12+b),new(12,15+b),new(9,17+b),new(7,15+b));
        R(12,15+b,2,2,Ice);
        // Stable feet, short legs and small patches of reflected ice light.
        R(19,35-step,6,5,Ink);R(27,35+step,6,5,Ink);
        R(19,36-step,5,2,Navy);R(27,36+step,4,2,BlueShadow);
        R(20,35-step,4,1,Steel);R(28,35+step,3,1,Ice);
        R(18,39-step,7,1,Ink);R(27,39+step,7,1,Ink);
        // Tunic, scarf and compact breastplate: deliberate asymmetry, no neck.
        P(Ink,new(18,26+b),new(30,26+b),new(33,30+b),new(32,35),new(28,37),new(19,36),new(17,32+b));
        P(BlueShadow,new(19,27+b),new(29,27+b),new(31,30+b),new(30,35),new(20,35),new(19,32+b));
        R(21,29+b,7,4,Blue);R(27,30+b,3,4,Navy);
        P(White,new(19,27+b),new(28,27+b),new(29,29+b),new(25,31+b),new(21,30+b));
        R(22,31+b,4,2,Ice);R(22,33+b,2,1,White);
        R(19,34,12,2,Brown);R(24,34,3,2,Gold);R(25,34,1,1,White);
        // Near gauntlet and raised curved shoulder plate.
        P(Ink,new(32,26+b),new(35,24+b),new(38,25+b),new(39,29+b),new(37,33+b),new(32,33+b),new(30,30+b));
        P(Steel,new(33,27+b),new(35,25+b),new(37,26+b),new(38,29+b),new(36,32+b),new(33,32+b),new(32,30+b));
        P(White,new(34,27+b),new(35,25+b),new(37,26+b),new(37,29+b),new(35,30+b),new(33,29+b));
        R(32,31+b,3,2,Blue);R(33,32+b,2,1,Ice);
        // Far mitten closes around the haft.
        P(Ink,new(14,27+b),new(18,26+b),new(20,29+b),new(18,33+b),new(15,33+b),new(13,30+b));
        P(Steel,new(15,28+b),new(18,27+b),new(19,29+b),new(17,32+b),new(15,31+b));
        R(15,28+b,3,2,White);R(16,30+b,2,1,Ice);
        // Broad hood, visible top plane, and an asymmetric swept ice crest.
        P(Ink,new(left+1,14+b),new(left+4,11+b),new(right-3,11+b),new(right,14+b),new(right+1,22+b),new(right-1,27+b),new(left+2,28+b),new(left-1,24+b),new(left-2,18+b));
        P(BlueShadow,new(left+1,15+b),new(left+4,12+b),new(right-3,12+b),new(right-1,15+b),new(right,22+b),new(right-2,26+b),new(left+2,26+b),new(left,23+b),new(left-1,18+b));
        P(Blue,new(left+1,15+b),new(left+5,12+b),new(right-4,12+b),new(right-1,16+b),new(right-2,19+b),new(left,19+b));
        R(left+3,14+b,right-left-8,2,Ice);
        P(Ink,new(left+1,14+b),new(left-1,8+b),new(left+3,9+b),new(left+5,12+b),new(left+8,9+b),new(left+11,9+b),new(left+8,14+b));
        P(Blue,new(left+2,13+b),new(left,9+b),new(left+2,10+b),new(left+5,13+b),new(left+8,10+b),new(left+9,10+b),new(left+7,14+b));
        R(left+1,10+b,1,2,Ice);R(left+7,11+b,2,1,Ice);
        P(Ink,new(right-8,12+b),new(right-4,8+b),new(right-1,8+b),new(right-3,11+b),new(right+3,10+b),new(right+1,14+b),new(right-3,16+b));
        P(Blue,new(right-7,13+b),new(right-3,9+b),new(right-2,9+b),new(right-4,12+b),new(right+1,11+b),new(right,13+b),new(right-3,15+b));
        R(right-4,11+b,2,1,Ice);R(right-1,12+b,2,1,Ice);
        R(left+3,15+b,6,1,Ice);R(left+5,14+b,4,1,Ice);
        // Pale ear fins frame the face without a heavy black frame inside it.
        P(Ink,new(left-2,15+b),new(left,14+b),new(left+2,19+b),new(left+1,23+b),new(left-2,22+b),new(left-4,19+b));
        P(White,new(left-2,16+b),new(left-1,16+b),new(left+1,19+b),new(left,21+b),new(left-2,20+b),new(left-3,18+b));
        R(left-1,19+b,2,3,Ice);
        if(!back)
        {
            P(SkinShadow,new(left+3,19+b),new(right-1,19+b),new(right,21+b),new(right-1,25+b),new(left+3,25+b),new(left+2,23+b));
            R(left+3,19+b,right-left-4,4,Skin);
            R(left+2,18+b,right-left-2,1,White);R(left+2,19+b,1,3,Ice);
            if(phase!=29){R(24,21+b,1,2,Ink);R(30,21+b,1,2,Ink);}
            else{R(24,22+b,1,1,Ink);R(30,22+b,1,1,Ink);}
            R(left+4,25+b,right-left-6,1,Ice);R(left+6,26+b,5,1,White);
        }
        else {R(left+3,19+b,right-left-5,4,Blue);R(left+4,22+b,right-left-8,2,BlueShadow);}
        return Buffer;
    }
}
