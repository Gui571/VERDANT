using Godot;
using Verdant.Forge;
namespace Verdant.Battle;

/// <summary>Quadro de pixel art em caracteres: '.' vazio, 'o' tinta, 'd' sombra, 'm' base, 'l' luz, 'w' brilho (tons do tema).</summary>
public sealed class Pic
{
    public readonly int W, H; public readonly char[] P;
    public Pic(int w, int h) { W = w; H = h; P = new string('.', w * h).ToCharArray(); }
    public static Pic Map(params string[] rows) { var p = new Pic(rows.Max(r => r.Length), rows.Length); for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++) p.P[y * p.W + x] = rows[y][x]; return p; }
    public char Get(int x, int y) => x < 0 || y < 0 || x >= W || y >= H ? '.' : P[y * W + x];
    public void Set(int x, int y, char c) { if (x >= 0 && y >= 0 && x < W && y < H) P[y * W + x] = c; }
    public void Rect(int x, int y, int w, int h, char c) { for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) Set(x + i, y + j, c); }
    public void Line(int x0, int y0, int x1, int y1, char c) { foreach (var (x, y) in Layer.LinePoints(x0, y0, x1, y1)) Set(x, y, c); }
    public void Ellipse(float cx, float cy, float rx, float ry, char fill, char? edge = null)
    {
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { float u = (x - cx) / rx, v = (y - cy) / ry, d = u * u + v * v; if (d <= 1) Set(x, y, edge.HasValue && d > .6f ? edge.Value : fill); }
    }
    /// <summary>Contorno de tinta de 1 px (vizinhança de 4) em volta de tudo que está preenchido; amplia a imagem em 1 px por lado.</summary>
    public Pic Outlined()
    {
        var q = new Pic(W + 2, H + 2);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (P[y * W + x] != '.') q.Set(x + 1, y + 1, P[y * W + x]);
        for (int y = 0; y < q.H; y++) for (int x = 0; x < q.W; x++)
            if (q.Get(x, y) == '.' && (Filled(q, x + 1, y) || Filled(q, x - 1, y) || Filled(q, x, y + 1) || Filled(q, x, y - 1))) q.P[y * q.W + x] = 'p';
        for (int i = 0; i < q.P.Length; i++) if (q.P[i] == 'p') q.P[i] = 'o';
        return q;
    }
    static bool Filled(Pic q, int x, int y) { char c = q.Get(x, y); return c != '.' && c != 'p'; }
    public Raster ToRaster(FxTheme theme) { var r = new Raster(W, H); for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { char c = P[y * W + x]; if (c != '.') r.Pixel(x, y, FxPalette.Of(theme, c)); } return r; }
}

public sealed record FxSprite(int Id, string Name, FxTheme Theme, int W, int H, int Frames, int X, int Y, int PivotX, int PivotY);

/// <summary>Atlas único dos efeitos, montado uma vez em C# (sem Godot) e enviado à GPU como uma textura só.
/// Nada é rasterizado durante a batalha: o <see cref="FxDirector"/> só escolhe sprite, quadro e posição.</summary>
public sealed class FxAtlas
{
    public const int Width = 256;
    public Raster Image { get; private set; } = null!;
    public readonly List<FxSprite> Sprites = new();
    readonly Dictionary<(string, FxTheme), int> ids = new();
    public int Id(string name, FxTheme theme = FxTheme.Steel) => ids.TryGetValue((name, theme), out var id) ? id : throw new KeyNotFoundException($"sprite {name}:{theme}");
    public bool Has(string name, FxTheme theme) => ids.ContainsKey((name, theme));
    /// <summary>Procura o sprite no tema pedido; se o tema não existir para esse sprite, usa o mais próximo (Luz, Aço, depois o primeiro registrado).</summary>
    public int IdNear(string name, FxTheme theme)
    {
        if (ids.TryGetValue((name, theme), out var id)) return id;
        foreach (var t in new[] { FxTheme.Light, FxTheme.Steel }) if (ids.TryGetValue((name, t), out id)) return id;
        foreach (var s in Sprites) if (s.Name == name) return s.Id;
        throw new KeyNotFoundException("sprite " + name);
    }
    public FxSprite this[int id] => Sprites[id];
    static FxAtlas? shared;
    public static FxAtlas Shared => shared ??= Build();

    static readonly FxTheme[] All = Enum.GetValues<FxTheme>();

    sealed record Def(string Name, FxTheme[] Themes, Func<FxTheme, Pic[]> Make, bool Outline, bool BottomPivot);
    static readonly List<Def> Defs = new();
    static void Reg(string name, FxTheme[] themes, Func<FxTheme, Pic[]> make, bool outline = false, bool bottom = false) => Defs.Add(new(name, themes, make, outline, bottom));
    static FxTheme[] T(params FxTheme[] t) => t;

    static FxAtlas()
    {
        // --- partículas básicas -------------------------------------------------------------------------------------
        Reg("px", All, _ => new[] { Pic.Map("d"), Pic.Map("m"), Pic.Map("l"), Pic.Map("w") });
        Reg("spark", All, _ => new[] {
            Pic.Map("...l...", "...w...", "..mwm..", "lwwwwwl", "..mwm..", "...w...", "...l..."),
            Pic.Map(".......", "...l...", "..lwl..", ".lwwwl.", "..lwl..", "...l...", "......."),
            Pic.Map(".......", ".......", "...m...", "..mlm..", "...m...", ".......", "......."),
            Pic.Map(".......", "..d.d..", ".......", ".d...d.", ".......", "..d.d..", ".......") });
        Reg("slash", T(FxTheme.Steel, FxTheme.Damage, FxTheme.Fire, FxTheme.Ice, FxTheme.Shadow, FxTheme.Light, FxTheme.Blood), _ => new[] {
            Pic.Map("........w..", ".......ww..", "......ww...", ".....ww....", "....ww.....", "...ww......", "..ww.......", ".ww........", "w.........."),
            Pic.Map("..........w", ".........wl", "........wl.", ".......wl..", "......wl...", ".....wl....", "....wl.....", "...wl......", "..wl.......", ".wl........", "wl........."),
            Pic.Map("..........l", "..........m", ".........l.", "...........", ".......m...", "......l....", "...........", "...m.......", "..l........", "...........", "l.........."),
            Pic.Map("...........", "..........d", "...........", "........d..", "...........", "...........", "..d........", "...........", "...........", "...........", "...........") });
        Reg("puff", T(FxTheme.Earth, FxTheme.Ice, FxTheme.Shadow, FxTheme.Water, FxTheme.Steel, FxTheme.Poison, FxTheme.Web, FxTheme.Plant), t => Puff());
        Reg("shard", T(FxTheme.Earth, FxTheme.Ice, FxTheme.Steel, FxTheme.Plant, FxTheme.Fire, FxTheme.Web), _ => new[] {
            Pic.Map(".l..", "lml.", ".md.", "..d."), Pic.Map("..l.", ".lm.", "lmd.", ".d.."), Pic.Map("l...", "mll.", ".mdd", "..d."), Pic.Map(".ll.", "lmmd", ".dd.", "....") }, true);
        Reg("flame-s", T(FxTheme.Fire, FxTheme.Poison, FxTheme.Shadow, FxTheme.Light, FxTheme.Ice), t => Enumerable.Range(0, 4).Select(f => Flame(5, 7, f)).ToArray());
        Reg("flame-m", T(FxTheme.Fire, FxTheme.Poison, FxTheme.Shadow, FxTheme.Light, FxTheme.Ice), t => Enumerable.Range(0, 4).Select(f => Flame(7, 10, f)).ToArray());
        Reg("flame-l", T(FxTheme.Fire, FxTheme.Poison, FxTheme.Shadow, FxTheme.Light, FxTheme.Ice), t => Enumerable.Range(0, 4).Select(f => Flame(10, 14, f)).ToArray(), false, true);
        Reg("leaf", T(FxTheme.Plant), _ => new[] { Pic.Map("..ll.", ".lmm.", "lmmd.", ".mdd.", "d...."), Pic.Map("ll...", "lmm..", ".mmd.", "..mdd", "...d."), Pic.Map("..d..", ".lmd.", "lmmd.", ".lmd.", "..d..") }, true);
        Reg("bubble", T(FxTheme.Poison, FxTheme.Acid, FxTheme.Water), _ => new[] {
            Pic.Map(".m.", "mlm", ".m."), Pic.Map(".mm.", "mlwm", "mlmm", ".mm."), Pic.Map(".mmm.", "mlwwm", "mlmmm", "mmmmd", ".mdd."), Pic.Map("l...l", ".....", "..m..", ".....", "l...l") });
        Reg("crystal", T(FxTheme.Ice), _ => new[] {
            Pic.Map("..w..", ".lwm.", ".lwmd", ".lmmd", ".lmmd", ".mmdd", "..md."), Pic.Map("..l..", ".lwm.", ".lmmd", ".lmmd", ".lmmd", ".mmdd", "..md."), Pic.Map("..m..", ".lmm.", ".lmmd", ".lmmd", ".mmmd", ".mmdd", "..dd.") }, true, true);
        Reg("snow", T(FxTheme.Ice), _ => new[] { Pic.Map(".w.", "wlw", ".w."), Pic.Map("w.w", ".l.", "w.w"), Pic.Map(".l.", "lwl", ".l.") });
        Reg("star4", T(FxTheme.Light, FxTheme.Steel, FxTheme.Stun, FxTheme.Ice, FxTheme.Shield, FxTheme.Heal, FxTheme.Shadow), _ => new[] {
            Pic.Map("...m...", "...l...", "...w...", "mlwwwlm", "...w...", "...l...", "...m..."), Pic.Map(".......", "...l...", "..lwl..", ".lwwwl.", "..lwl..", "...l...", "......."), Pic.Map(".......", ".......", "...l...", "..lwl..", "...l...", ".......", ".......") });
        Reg("drop", T(FxTheme.Blood, FxTheme.Acid, FxTheme.Water, FxTheme.Poison, FxTheme.Heal), _ => new[] { Pic.Map(".l.", ".m.", "lmm", "mmd", ".d."), Pic.Map(".l.", "lmm", "mmd", ".d.", "..."), Pic.Map("...", "...", "m.m", "lmd", "...") });
        Reg("smoke", T(FxTheme.Shadow, FxTheme.Poison, FxTheme.Steel, FxTheme.Earth), _ => Smoke());
        Reg("plus", T(FxTheme.Heal, FxTheme.Light, FxTheme.Shield), _ => new[] { Pic.Map("..m..", "..l..", "mlwlm", "..l..", "..m.."), Pic.Map(".....", "..l..", ".lwl.", "..l..", "....."), Pic.Map(".....", ".....", "..l..", ".....", ".....") });
        // --- peças de habilidade ------------------------------------------------------------------------------------
        Reg("arrow", T(FxTheme.Steel), _ => new[] { Pic.Map(".w.", ".m.", ".m.", ".m.", ".m.", ".m.", "lml", ".l.", ".w."), Pic.Map("w.w", ".m.", ".m.", ".m.", ".m.", "lml", ".l.") }, true, true);
        Reg("pillar", T(FxTheme.Ice), _ => Enumerable.Range(0, 5).Select(f => Pillar(f)).ToArray(), true, true);
        Reg("column", T(FxTheme.Light), _ => new[] { Column(0), Column(1) }, false, true);
        Reg("root", T(FxTheme.Plant), _ => Enumerable.Range(0, 5).Select(Root).ToArray(), true, true);
        Reg("vine-ring", T(FxTheme.Plant), _ => new[] { VineRing(0), VineRing(1) });
        Reg("web-knot", T(FxTheme.Web), _ => new[] { Pic.Map("o...o...o", ".o..o..o.", "..o.o.o..", "...ooo...", "ooooooooo", "...ooo...", "..o.o.o..", ".o..o..o.", "o...o...o").Recolor('o', 'l') });
        Reg("puddle-s", T(FxTheme.Acid, FxTheme.Poison, FxTheme.Fire, FxTheme.Water, FxTheme.Ice), _ => Puddle(13, 6));
        Reg("puddle-m", T(FxTheme.Acid, FxTheme.Poison, FxTheme.Fire, FxTheme.Water, FxTheme.Ice), _ => Puddle(19, 8));
        Reg("puddle-l", T(FxTheme.Acid, FxTheme.Poison, FxTheme.Fire, FxTheme.Water, FxTheme.Ice), _ => Puddle(25, 10));
        Reg("coil", T(FxTheme.Plant, FxTheme.Poison), _ => new[] { Coil(17, 7), Coil(13, 6), Coil(9, 5) }, true);
        Reg("shield-bubble", T(FxTheme.Shield), _ => new[] { ShieldBubble(0), ShieldBubble(1) });
        // --- ícones de estado 9×9 e numerais ------------------------------------------------------------------------
        foreach (var (name, theme, pic) in StatusIcons.All()) Reg("st-" + name, T(theme), _ => new[] { pic });
        Reg("glyph-s", T(FxTheme.Damage, FxTheme.Heal, FxTheme.Shield, FxTheme.Poison, FxTheme.Fire, FxTheme.Blood, FxTheme.Light), _ => Glyphs(1), true);
        Reg("glyph-l", T(FxTheme.Damage, FxTheme.Heal, FxTheme.Shield, FxTheme.Light), _ => Glyphs(2), true);
    }

    static Pic[] Puff()
    {
        var f = new Pic[4]; int[] rx = { 2, 4, 5, 5 }, ry = { 2, 3, 4, 4 };
        for (int i = 0; i < 4; i++) { var p = new Pic(11, 9); p.Ellipse(5, 4, rx[i], ry[i], i < 2 ? 'l' : 'm', i < 3 ? 'm' : 'd'); if (i >= 2) for (int y = 0; y < 9; y++) for (int x = 0; x < 11; x++) if ((x + y) % (i == 2 ? 4 : 2) == 0 && p.Get(x, y) != '.') p.Set(x, y, '.'); f[i] = p; }
        return f;
    }
    static Pic[] Smoke()
    {
        var f = new Pic[4]; int[] r = { 2, 3, 4, 4 };
        for (int i = 0; i < 4; i++) { var p = new Pic(9, 9); p.Ellipse(4, 4 - i / 2, r[i], r[i], i == 3 ? 'd' : 'm', 'd'); p.Ellipse(3, 3 - i / 2, r[i] - 1.5f, r[i] - 1.5f, 'l'); if (i >= 2) for (int y = 0; y < 9; y++) for (int x = 0; x < 9; x++) if ((x * 3 + y * 5 + i) % 3 == 0) p.Set(x, y, '.'); f[i] = p; }
        return f;
    }
    /// <summary>Chama em forma de gota; o topo balança por quadro. Núcleo claro na parte de baixo.</summary>
    public static Pic FlameForIcon() => Flame(10, 13, 0);
    internal static Pic Flame(int w, int h, int frame)
    {
        var p = new Pic(w, h);
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1); float hw = w / 2f * MathF.Sqrt(Math.Min(1, t * 1.5f + .05f)) * (t > .8f ? MathF.Sqrt(1 - (t - .8f) * 2.2f) : 1);
            float sway = MathF.Sin(frame * 1.57f + t * 3f) * (1 - t) * (w / 5f);
            for (int x = 0; x < w; x++)
            {
                float dx = x + .5f - w / 2f - sway; if (MathF.Abs(dx) > hw) continue;
                char c = MathF.Abs(dx) < hw * .35f && t > .45f ? 'w' : MathF.Abs(dx) < hw * .65f && t > .25f ? 'l' : 'm';
                p.Set(x, y, c);
            }
        }
        return p;
    }
    static Pic Pillar(int f)
    {
        int h = new[] { 4, 8, 12, 15, 16 }[f], w = 9; var p = new Pic(w, 16);
        for (int y = 0; y < h; y++) { int row = 16 - h + y; float t = y / (float)Math.Max(1, h - 1); int hw = (int)MathF.Round(1 + t * 3.4f); for (int x = 4 - hw; x <= 4 + hw; x++) p.Set(x, row, x < 4 - hw / 3 ? 'l' : x > 4 + hw / 3 ? 'd' : 'm'); if (y == 0) p.Set(4, row, 'w'); if (y > 1 && y < h - 2) p.Set(4 - hw / 2 - 1 < 0 ? 0 : 4 - hw / 2, row, 'w'); }
        return p;
    }
    static Pic Column(int f)
    {
        var p = new Pic(15, 42);
        for (int y = 0; y < 42; y++) { float t = y / 41f; int hw = (int)MathF.Round(2 + t * 5); for (int x = 7 - hw; x <= 7 + hw; x++) { bool edge = x == 7 - hw || x == 7 + hw; if (edge && (y + f) % 2 == 0) continue; if (!edge && Math.Abs(x - 7) > hw - 2 && (x + y + f) % 2 == 0) continue; p.Set(x, y, edge ? 'm' : Math.Abs(x - 7) <= 1 ? 'w' : 'l'); } }
        return p;
    }
    static Pic Root(int f)
    {
        int h = new[] { 3, 6, 9, 11, 12 }[f]; var p = new Pic(7, 12); int x = 3;
        for (int i = 0; i < h; i++) { int y = 11 - i; x = 3 + (int)MathF.Round(MathF.Sin(i * .8f) * 1.6f); p.Set(x, y, i > h - 3 ? 'l' : 'm'); if (i % 3 == 1) p.Set(x + 1, y, 'd'); if (i > 3 && i % 3 == 0) { p.Set(x - 1, y, 'm'); } }
        return p;
    }
    static Pic VineRing(int f)
    {
        var p = new Pic(26, 10);
        for (int k = 0; k < 40; k++) { float a = k * MathF.Tau / 40; int x = (int)MathF.Round(12.5f + MathF.Cos(a) * 12), y = (int)MathF.Round(4.5f + MathF.Sin(a) * 4); p.Set(x, y, ((k + f) % 3 == 0) ? 'l' : 'm'); if (k % 4 == 0) p.Set(x, y + (y < 5 ? -1 : 1), 'd'); }
        return p;
    }
    static Pic[] Puddle(int w, int h)
    {
        var f = new Pic[2];
        for (int i = 0; i < 2; i++) { var p = new Pic(w, h); p.Ellipse((w - 1) / 2f, (h - 1) / 2f, w / 2f, h / 2f, 'm', 'd'); p.Ellipse((w - 1) / 2f - w * .12f, (h - 1) / 2f - h * .12f, w * .26f, h * .22f, 'l'); for (int k = 0; k < 3; k++) { int x = 2 + ((k * 7 + i * 5) % (w - 4)), y = 1 + ((k * 3 + i * 2) % (h - 2)); if (p.Get(x, y) != '.') p.Set(x, y, 'w'); } f[i] = p; }
        return f;
    }
    static Pic Coil(int w, int h)
    {
        var p = new Pic(w, h); p.Ellipse((w - 1) / 2f, (h - 1) / 2f, w / 2f, h / 2f, 'm', 'd'); p.Ellipse((w - 1) / 2f, (h - 1) / 2f - 1, w / 2f - 2.2f, h / 2f - 1.8f, '.');
        for (int x = 1; x < w - 1; x += 3) { if (p.Get(x, 1) != '.') p.Set(x, 1, 'l'); if (p.Get(x + 1, h - 2) != '.') p.Set(x + 1, h - 2, 'd'); }
        return p;
    }
    static Pic ShieldBubble(int f)
    {
        var p = new Pic(30, 34);
        for (int k = 0; k < 90; k++) { float a = k * MathF.Tau / 90; int x = (int)MathF.Round(14.5f + MathF.Cos(a) * 14), y = (int)MathF.Round(16.5f + MathF.Sin(a) * 16); if (((k + f * 2) / 2) % 3 != 2) p.Set(x, y, MathF.Sin(a) < -.3f && MathF.Cos(a) < .3f ? 'w' : MathF.Sin(a) > .3f ? 'd' : 'l'); }
        for (int k = 0; k < 4; k++) { int y = 5 + k * 6 + (f * 2) % 6; for (int x = 6; x < 10; x++) if (p.Get(x, y) == '.' && (x + y) % 2 == 0 && x - 5 + y > 8) { } }
        p.Set(7 + f, 8, 'w'); p.Set(8 + f, 7, 'w'); p.Set(7 + f, 9, 'w');
        return p;
    }
    static readonly string[] Font = { "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001", "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111", "000010111010000", "000000111000000" };
    static Pic[] Glyphs(int scale)
    {
        var frames = new Pic[Font.Length];
        for (int g = 0; g < Font.Length; g++)
        {
            var p = new Pic(3 * scale, 5 * scale);
            for (int y = 0; y < 5; y++) for (int x = 0; x < 3; x++) if (Font[g][y * 3 + x] == '1') for (int j = 0; j < scale; j++) for (int i = 0; i < scale; i++) p.Set(x * scale + i, y * scale + j, y * scale + j < 2 * scale ? 'w' : 'l');
            frames[g] = p;
        }
        return frames;
    }

    /// <summary>Quadros de um mesmo sprite têm o mesmo tamanho: o menor fica centralizado (ou apoiado embaixo, nos sprites de pivô inferior).</summary>
    static Pic Pad(Pic p, int w, int h, bool bottom)
    {
        if (p.W == w && p.H == h) return p; var q = new Pic(w, h); int ox = (w - p.W) / 2, oy = bottom ? h - p.H : (h - p.H) / 2;
        for (int y = 0; y < p.H; y++) for (int x = 0; x < p.W; x++) q.Set(x + ox, y + oy, p.P[y * p.W + x]); return q;
    }

    // --- montagem do atlas ----------------------------------------------------------------------------------------
    public static FxAtlas Build()
    {
        var atlas = new FxAtlas(); var packed = new List<(FxSprite s, Raster[] frames)>(); int x = 0, y = 0, rowH = 0;
        foreach (var d in Defs) foreach (var theme in d.Themes)
        {
            var raw = d.Make(theme).Select(p => d.Outline ? p.Outlined() : p).ToArray(); int w = raw.Max(p => p.W), h = raw.Max(p => p.H);
            var pics = raw.Select(p => Pad(p, w, h, d.BottomPivot)).ToArray(); int total = w * pics.Length;
            if (x + total > Width) { x = 0; y += rowH + 1; rowH = 0; }
            var s = new FxSprite(atlas.Sprites.Count, d.Name, theme, w, h, pics.Length, x, y, w / 2, d.BottomPivot ? h - 1 : h / 2);
            atlas.Sprites.Add(s); atlas.ids[(d.Name, theme)] = s.Id; packed.Add((s, pics.Select(p => p.ToRaster(theme)).ToArray()));
            x += total + 1; rowH = Math.Max(rowH, h);
        }
        int height = y + rowH; atlas.Image = new Raster(Width, Math.Max(1, height));
        foreach (var (s, frames) in packed) for (int f = 0; f < frames.Length; f++) for (int j = 0; j < s.H; j++) for (int i = 0; i < s.W; i++)
        { int a = (j * s.W + i) * 4, b = ((s.Y + j) * Width + s.X + f * s.W + i) * 4; Array.Copy(frames[f].Pixels, a, atlas.Image.Pixels, b, 4); }
        return atlas;
    }
}

static class PicExt { public static Pic Recolor(this Pic p, char from, char to) { for (int i = 0; i < p.P.Length; i++) if (p.P[i] == from) p.P[i] = to; return p; } }
