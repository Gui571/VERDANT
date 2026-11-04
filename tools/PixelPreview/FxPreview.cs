using Godot;
using Verdant;
using Verdant.Battle;
using Verdant.Forge;
namespace PixelPreview;

public static class FxPreview
{
    // Janela de revisão: logical 120×64 ao redor da ação, ampliada com pixels inteiros.
    public const int CropX = 46, CropY = 46, CropW = 116, CropH = 76;
    public static readonly int[] Offsets = { 2, 8, 13, 15, 19, 26, 40, 62 };

    public static Canvas Zoom(Raster frame, int zoom, int x = CropX, int y = CropY, int w = CropW, int h = CropH)
    {
        var crop = new Raster(w, h);
        for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) { int sx = x + i, sy = y + j; if (sx < 0 || sy < 0 || sx >= frame.Width || sy >= frame.Height) continue; int a = (sy * frame.Width + sx) * 4, b = (j * w + i) * 4; Array.Copy(frame.Pixels, a, crop.Pixels, b, 4); }
        var c = new Canvas(w * zoom, h * zoom, 0x000000); c.Blit(crop, 0, 0, zoom); return c;
    }

    /// <summary>Executa o cenário e salva uma folha 4×2 com os instantes-chave (+ quadros soltos para GIFs).</summary>
    public static void Sheet(string dir, string name, string ability, Func<FxScene, (Action step, Func<Raster> compose)> setup, int zoom = 4, Element? element = null, bool frames = false, int span = 70)
    {
        var scene = new FxScene(ability, element); var (afterStep, compose) = setup(scene);
        var captured = new Dictionary<int, Raster>(); var all = new List<Raster>();
        for (int t = 0; t < scene.CastTick + span; t++)
        {
            scene.Step(); afterStep(); int rel = t + 1 - scene.CastTick;
            var frame = compose();
            if (Offsets.Contains(rel)) captured[rel] = frame;
            if (frames && rel >= -2) all.Add(frame);
        }
        int cols = 4, rows = 2; var sheet = new Canvas(cols * CropW * zoom + (cols + 1) * 4, rows * CropH * zoom + (rows + 1) * 4, 0x1c2031);
        for (int i = 0; i < Offsets.Length; i++)
        {
            var z = Zoom(captured[Offsets[i]], zoom); int ox = 4 + (i % cols) * (CropW * zoom + 4), oy = 4 + (i / cols) * (CropH * zoom + 4);
            for (int y = 0; y < z.H; y++) Array.Copy(z.Data, y * z.W * 4, sheet.Data, ((oy + y) * sheet.W + ox) * 4, z.W * 4);
        }
        Directory.CreateDirectory(dir); sheet.Save(Path.Combine(dir, name + ".png"));
        if (frames)
        {
            var fdir = Path.Combine(dir, name + "-frames"); Directory.CreateDirectory(fdir);
            for (int i = 0; i < all.Count; i++) Zoom(all[i], zoom).Save(Path.Combine(fdir, $"f{i:D3}.png"));
        }
    }

    public static void DrawList(Raster dst, FxAtlas atlas, List<FxDraw> list)
    {
        var img = atlas.Image;
        foreach (var d in list)
        {
            var sp = atlas[d.Sprite]; int sx = sp.X + d.Frame * sp.W;
            for (int y = 0; y < sp.H; y++) for (int x = 0; x < sp.W; x++)
            {
                int ax = sx + (d.Flip ? sp.W - 1 - x : x), ay = sp.Y + y; int a = (ay * img.Width + ax) * 4; if (img.Pixels[a + 3] == 0) continue;
                int px = d.X + x, py = d.Y + y; if (px < 0 || py < 0 || px >= dst.Width || py >= dst.Height) continue; int o = (py * dst.Width + px) * 4;
                dst.Pixels[o] = img.Pixels[a]; dst.Pixels[o + 1] = img.Pixels[a + 1]; dst.Pixels[o + 2] = img.Pixels[a + 2]; dst.Pixels[o + 3] = 255;
            }
        }
    }

    public static (Action, Func<Raster>) WithFx(FxScene scene, out FxDirector fx)
    {
        var d = new FxDirector(scene.Sim, id => id < scene.Recipes.Count ? scene.Recipes[id].Monster?.Size ?? 32 : 32); fx = d;
        return (d.Advance, () => { d.Collect(); return scene.Compose(f => DrawList(f, FxAtlas.Shared, d.Ground), f => { DrawList(f, FxAtlas.Shared, d.Over); DrawList(f, FxAtlas.Shared, d.Top); }); });
    }

    public static int After(string dir, bool frames, string? only = null, Element? element = null)
    {
        Directory.CreateDirectory(dir);
        foreach (var id in Abilities.All.Keys.Where(k => k != "rally" && (only == null || k == only)))
            Sheet(dir, id + "-depois" + (element != null ? "-" + element.ToString()!.ToLowerInvariant() : ""), id, scene => WithFx(scene, out _), 4, element, frames);
        return 0;
    }

    public static int Before(string dir, bool frames)
    {
        Directory.CreateDirectory(dir);
        foreach (var id in Abilities.All.Keys.Where(k => k != "rally"))
            Sheet(dir, id + "-antes", id, scene => (() => { }, () => scene.Compose(null, scene.LegacyOverlay)), 4, null, frames);
        return 0;
    }
}
