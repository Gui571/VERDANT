namespace Verdant.Forge;
public static class ForgeEffects
{
    // Somente tons já existentes na paleta; a margem de segurança é de dois pixels.
    public static void Special(Raster buffer,ForgePalette palette,int frame,bool cast)
    {
        if(frame<2||frame>6)return;int cx=buffer.Width/2,cy=buffer.Height-18,r=3+(frame-2)%3;
        for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++)if(Math.Abs(x)+Math.Abs(y)==r){int px=cx+x,py=cy+y;buffer.Pixel(px,py,palette.MetalL);}
        if(!cast){for(int i=0;i<5;i++)buffer.Pixel(cx+5+i,cy+i/2,palette.White);}
    }
    public static ForgeValidation.Result ValidateIcons(IEnumerable<string> ids){int checks=0;var failures=new List<string>();foreach(var id in ids){var a=Icon(id);var b=Icon(id);checks+=3;if(a.Clipped!=0)failures.Add("icon bounds "+id);var colors=new HashSet<uint>();bool alpha=true;for(int i=0;i<a.Pixels.Length;i+=4){alpha&=a.Pixels[i+3] is 0 or 255;if(a.Pixels[i+3]>0)colors.Add(BitConverter.ToUInt32(a.Pixels,i));}if(!alpha||colors.Count>5||colors.Count==0)failures.Add("icon palette "+id);if(!a.Pixels.SequenceEqual(b.Pixels))failures.Add("icon determinism "+id);}return new(checks,failures);}
    /// <summary>Ícone 16×16 do especial: arte própria por habilidade, 4 tons de um tema + tinta (contorno de 1 px).</summary>
    public static Raster Icon(string id)
    {
        var (theme, pic) = IconArt(id); return pic.Outlined().ToRaster(theme);
    }
    static void Crystal(Verdant.Battle.Pic p, int cx, int h, int baseY)
    {
        for (int y = 0; y < h; y++) { int row = baseY - h + 1 + y; float t = y / (float)Math.Max(1, h - 1); int hw = (int)MathF.Round(.6f + t * 2f); for (int x = cx - hw; x <= cx + hw; x++) p.Set(x, row, y == 0 ? 'w' : x < cx ? 'l' : x > cx ? 'd' : 'm'); }
    }
    static (Verdant.Battle.FxTheme, Verdant.Battle.Pic) IconArt(string id)
    {
        var p = new Verdant.Battle.Pic(14, 14); var T = Verdant.Battle.FxTheme.Steel;
        switch (id)
        {
            case "ice-wall": T = Verdant.Battle.FxTheme.Ice; Crystal(p, 3, 8, 12); Crystal(p, 7, 12, 12); Crystal(p, 11, 9, 12); p.Line(0, 13, 13, 13, 'd'); break;
            case "lightning-cut": T = Verdant.Battle.FxTheme.Light; p.Line(10, 0, 4, 6, 'l'); p.Line(4, 6, 9, 6, 'l'); p.Line(9, 6, 3, 13, 'l'); p.Line(11, 0, 5, 6, 'w'); p.Line(5, 7, 10, 7, 'w'); p.Line(10, 7, 4, 13, 'w'); p.Line(0, 12, 6, 12, 'm'); break;
            case "arrow-rain": for (int x = 3; x <= 11; x += 4) { p.Line(x, 1, x - 1, 9, 'm'); p.Set(x - 1, 10, 'w'); p.Set(x - 2, 9, 'l'); p.Set(x, 9, 'l'); p.Set(x + 1, 1, 'l'); p.Set(x - 1, 0, 'l'); } for (int x = 0; x < 14; x += 3) p.Set(x, 13, 'd'); break;
            case "nova": T = Verdant.Battle.FxTheme.Ice; for (int i = 0; i < 4; i++) { p.Line(7, 7, 7 + (int)MathF.Round(MathF.Cos(i * MathF.PI / 2) * 6), 7 + (int)MathF.Round(MathF.Sin(i * MathF.PI / 2) * 6), 'l'); p.Line(7, 7, 7 + (int)MathF.Round(MathF.Cos(i * MathF.PI / 2 + .785f) * 4), 7 + (int)MathF.Round(MathF.Sin(i * MathF.PI / 2 + .785f) * 4), 'm'); } p.Ellipse(7, 7, 2.2f, 2.2f, 'w'); break;
            case "quake": T = Verdant.Battle.FxTheme.Earth; p.Ellipse(4, 5, 3, 2.6f, 'l', 'm'); p.Ellipse(9, 4, 2.4f, 2.2f, 'l', 'm'); p.Set(3, 4, 'w'); p.Set(8, 3, 'w'); p.Line(0, 9, 5, 10, 'd'); p.Line(5, 10, 7, 13, 'd'); p.Line(5, 10, 13, 8, 'd'); p.Line(0, 10, 5, 11, 'm'); break;
            case "dagger-dance": p.Line(2, 2, 10, 10, 'w'); p.Line(3, 2, 11, 10, 'l'); p.Line(11, 2, 3, 10, 'w'); p.Line(10, 2, 2, 10, 'l'); p.Line(10, 11, 13, 13, 'm'); p.Line(3, 11, 0, 13, 'm'); p.Set(7, 6, 'm'); break;
            case "blessing": T = Verdant.Battle.FxTheme.Heal; p.Rect(5, 1, 4, 12, 'm'); p.Rect(1, 5, 12, 4, 'm'); p.Rect(6, 2, 2, 10, 'l'); p.Rect(2, 6, 10, 2, 'l'); p.Rect(6, 6, 2, 2, 'w'); p.Set(0, 0, 'w'); p.Set(13, 0, 'w'); p.Set(13, 13, 'w'); p.Set(0, 13, 'w'); break;
            case "acid": T = Verdant.Battle.FxTheme.Acid; p.Ellipse(7, 9, 4.4f, 4, 'm', 'd'); for (int y = 0; y < 5; y++) p.Set(7, y + 1, 'm'); p.Line(7, 1, 4, 6, 'm'); p.Line(7, 1, 10, 6, 'm'); p.Ellipse(5.5f, 8, 1.6f, 1.8f, 'l'); p.Set(5, 8, 'w'); p.Ellipse(11, 3, 1.4f, 1.4f, 'l'); p.Ellipse(2.5f, 3.5f, 1.2f, 1.2f, 'l'); break;
            case "roar": T = Verdant.Battle.FxTheme.Damage; for (int k = 0; k < 3; k++) for (int a = -4; a <= 4; a++) { float an = a * .2f; int x = (int)MathF.Round(2 + MathF.Cos(an) * (4 + k * 3)), y = (int)MathF.Round(7 + MathF.Sin(an) * (4 + k * 3)); p.Set(x, y, k == 0 ? 'w' : k == 1 ? 'l' : 'm'); } p.Rect(0, 5, 2, 4, 'l'); break;
            case "breath": T = Verdant.Battle.FxTheme.Fire; { var f = Verdant.Battle.FxAtlas.FlameForIcon(); for (int y = 0; y < f.H; y++) for (int x = 0; x < f.W; x++) p.Set(x + 2, y, f.Get(x, y)); } p.Line(0, 12, 3, 12, 'l'); break;
            case "web": T = Verdant.Battle.FxTheme.Web; for (int i = 0; i < 4; i++) { float an = i * MathF.PI / 4; p.Line(7 - (int)MathF.Round(MathF.Cos(an) * 6), 7 - (int)MathF.Round(MathF.Sin(an) * 6), 7 + (int)MathF.Round(MathF.Cos(an) * 6), 7 + (int)MathF.Round(MathF.Sin(an) * 6), 'l'); } p.Ellipse(7, 7, 3.4f, 3.4f, '.', 'm'); p.Ellipse(7, 7, 3.4f, 3.4f, '.'); for (int k = 0; k < 16; k++) { float an = k * MathF.Tau / 16; p.Set((int)MathF.Round(7 + MathF.Cos(an) * 3.5f), (int)MathF.Round(7 + MathF.Sin(an) * 3.5f), 'm'); } p.Set(7, 7, 'w'); break;
            case "dive": p.Line(1, 1, 7, 9, 'l'); p.Line(13, 1, 7, 9, 'l'); p.Line(0, 3, 6, 11, 'm'); p.Line(13, 3, 7, 11, 'm'); p.Line(7, 2, 7, 12, 'w'); p.Line(5, 10, 7, 13, 'w'); p.Line(9, 10, 7, 13, 'w'); break;
            case "constrict": T = Verdant.Battle.FxTheme.Plant; for (int k = 0; k < 3; k++) { p.Ellipse(7, 3 + k * 4, 6, 2, 'm', 'd'); p.Ellipse(7, 2.6f + k * 4, 3.8f, .9f, '.'); p.Line(2 + k, 3 + k * 4, 5 + k, 2 + k * 4, 'l'); } break;
            case "water-jet": T = Verdant.Battle.FxTheme.Water; for (int r = 0; r < 3; r++) for (int x = 0; x < 11; x++) p.Set(x, 3 + r * 4 + (int)MathF.Round(MathF.Sin(x * .9f + r) * 1.2f), r == 1 ? 'w' : 'l'); p.Set(12, 2, 'm'); p.Set(13, 6, 'm'); p.Set(12, 10, 'm'); p.Set(13, 4, 'l'); p.Set(12, 8, 'l'); break;
            case "roots": T = Verdant.Battle.FxTheme.Plant; p.Line(7, 0, 7, 6, 'l'); p.Line(7, 6, 2, 9, 'm'); p.Line(7, 6, 12, 9, 'm'); p.Line(2, 9, 1, 13, 'm'); p.Line(12, 9, 13, 13, 'm'); p.Line(7, 6, 7, 13, 'm'); p.Line(5, 8, 4, 13, 'd'); p.Line(9, 8, 10, 13, 'd'); p.Set(6, 1, 'w'); p.Set(8, 2, 'w'); break;
            default: T = Verdant.Battle.FxTheme.Shadow; p.Ellipse(7, 6, 5, 5, 'm', 'd'); p.Rect(4, 6, 2, 2, 'w'); p.Rect(8, 6, 2, 2, 'w'); p.Rect(5, 10, 4, 2, 'l'); p.Line(2, 0, 4, 2, 'l'); p.Line(11, 0, 9, 2, 'l'); break;
        }
        return (T, p);
    }
}
