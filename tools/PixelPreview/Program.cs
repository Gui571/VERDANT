using Verdant;
namespace PixelPreview;

public static class Program
{
    static AdventurerMotion ParseMotion(string s) => Enum.Parse<AdventurerMotion>(s, true);
    public static int Main(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine("comandos: roster <saida.png> [zoom] [motion] | strip <saida.png> <personagem> <motion> [zoom] | frames <pasta> <personagem> <motion> [zoom]"); return 1; }
        switch (args[0])
        {
            case "roster": return Roster(args[1], args.Length > 2 ? int.Parse(args[2]) : 6, args.Length > 3 ? ParseMotion(args[3]) : AdventurerMotion.Idle);
            case "strip": return Strip(args[1], Kind(args[2]), ParseMotion(args[3]), args.Length > 4 ? int.Parse(args[4]) : 6);
            case "motions": return Motions(args[1], args.Length > 2 ? int.Parse(args[2]) : 4);
            case "gen": return Gen(args[1], ulong.Parse(args[2]), args.Length > 3 ? int.Parse(args[3]) : 5, args.Length > 4 ? AdventurerMotion.Idle : AdventurerMotion.Idle);
            case "check": { var res = Verdant.Forge.ForgeValidation.Run(args.Length > 1 ? int.Parse(args[1]) : 200); foreach (var g in res.Failures.GroupBy(f => f.Split(':')[0])) { Console.WriteLine($"FALHA [{g.Count()}] {g.Key}  ex.: {string.Join(", ", g.Take(12).Select(x => x.Split(':').Last().Trim()))}"); } Console.WriteLine($"checks={res.Checks} falhas={res.Failures.Count}"); return res.Failures.Count == 0 ? 0 : 1; }
            case "export": return Export(args[1]);
            case "pose": { var r = new Verdant.Forge.ForgeRenderer(Verdant.Forge.ForgeRoster.All.First(x => x.Id == args[1])); var m = ParseMotion(args[2]); for (int f = 0; f < FrameCount(m); f++) { var q = r.Compute(Time(m, f), m); Console.WriteLine($"f{f} dx={q.Dx} dy={q.Dy} hdy={q.HeadDy} near={q.NearAngle} ndx={q.NearDx} ndy={q.NearDy} stepL={q.StepL} stepR={q.StepR}"); } return 0; }
            case "frames": return Frames(args[1], Kind(args[2]), ParseMotion(args[3]), args.Length > 4 ? int.Parse(args[4]) : 8);
        }
        return 1;
    }
    // Folha de revisão: uma linha por personagem, colunas = walk(8) | run(8)... em quadros espaçados.
    static int Motions(string path, int zoom)
    {
        var all = RosterCatalog.All; int cell = 32 * zoom; var ms = new[] { (AdventurerMotion.Walk, 8), (AdventurerMotion.Attack, 6), (AdventurerMotion.Hurt, 4) };
        int cols = ms.Sum(m => m.Item2); var c = new Canvas(cols * cell, all.Count * cell, 0x1c2031);
        for (int r = 0; r < all.Count; r++) { var a = new CompactAdventurer(1, all[r].Kind); int col = 0; foreach (var (m, n) in ms) for (int f = 0; f < n; f++, col++) c.Blit(a.Render(Time(m, f), m), col * cell, r * cell, zoom); }
        c.Save(path); return 0;
    }
    static int Gen(string path, ulong first, int zoom, AdventurerMotion m)
    {
        int cols = 6, rows = 4, cell = 32 * zoom; var c = new Canvas(cols * cell, rows * cell, 0x1c2031);
        for (int i = 0; i < cols * rows; i++) { var spec = Verdant.Forge.ForgeGenerator.Generate(first + (ulong)i); var a = new CompactAdventurer(spec); c.Blit(a.Render(0, m), (i % cols) * cell, (i / cols) * cell, zoom); }
        c.Save(path); return 0;
    }
    // Regenera docs/roster no mesmo formato do exportador do Godot (PixelDemo.ExportParty): sprites, atlases, JSON de tempo, catálogo e galeria.
    static int Export(string dir)
    {
        Directory.CreateDirectory(dir);
        var json = new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        void Sheet(string path, Func<int, Raster> cell, int frames, int cols)
        {
            int rows = (frames + cols - 1) / cols; var c = new Canvas(cols * 32, rows * 32, 0); Array.Clear(c.Data);
            for (int f = 0; f < frames; f++) c.Blit(cell(f), (f % cols) * 32, (f / cols) * 32, 1);
            c.Save(path);
        }
        foreach (var spec in Verdant.Forge.ForgeRoster.All)
        {
            var a = new CompactAdventurer(spec); string id = spec.Id;
            Sheet(Path.Combine(dir, id + ".png"), _ => a.Render(0), 1, 1);
            Sheet(Path.Combine(dir, id + "-idle.png"), f => a.Render(f / 8.0), 32, 8);
            File.WriteAllText(Path.Combine(dir, id + "-idle.json"), System.Text.Json.JsonSerializer.Serialize(new { version = 1, character = a.Name, width = 32, height = 32, columns = 8, frames = 32, fps = 8, duration = 4, origin = new[] { 16, 29 }, state = "idle", loop = true }));
            foreach (var m in new[] { AdventurerMotion.Walk, AdventurerMotion.Run, AdventurerMotion.Attack, AdventurerMotion.Hurt })
            {
                int frames = m == AdventurerMotion.Attack ? 6 : m == AdventurerMotion.Hurt ? 4 : 8; int fps = m == AdventurerMotion.Run ? 12 : 8; string act = m.ToString().ToLowerInvariant();
                Sheet(Path.Combine(dir, $"{id}-{act}.png"), f => a.Render(f / (double)fps, m), frames, 8);
                File.WriteAllText(Path.Combine(dir, $"{id}-{act}.json"), System.Text.Json.JsonSerializer.Serialize(new { version = 1, character = a.Name, width = 32, height = 32, columns = 8, frames, fps, duration = frames / (double)fps, origin = new[] { 16, 29 }, state = act, loop = m is AdventurerMotion.Walk or AdventurerMotion.Run }));
            }
        }
        var characters = RosterCatalog.All.Select(e => { var sp = Verdant.Forge.ForgeRoster.All.First(x => x.Id == e.Id); return new { e.Kind, e.Id, e.Name, e.Class, e.Description, e.Silhouette, e.Colors, Recipe = new { sp.Species, sp.Headwear, sp.Weapon, sp.Offhand, sp.Back, sp.Outfit } }; });
        File.WriteAllText(Path.Combine(dir, "catalog.json"), System.Text.Json.JsonSerializer.Serialize(new { version = 2, cell = 32, origin = new[] { 16, 29 }, maxColors = 16, engine = "VERDANT Forge", characters }, json));
        // Linha do elenco (frames 0..31 para o GIF) e galeria procedural (sementes 1..24).
        Directory.CreateDirectory(Path.Combine(dir, "frames"));
        for (int i = 0; i < 32; i++)
        {
            var c = new Canvas(160, 112, 0x1c2031); int k = 0;
            foreach (var spec in Verdant.Forge.ForgeRoster.All) { c.Blit(new CompactAdventurer(spec).Render(i / 8.0), 10 + (k % 4) * 38, 8 + (k / 4) * 48, 1); k++; }
            Zoom(c, 6).Save(Path.Combine(dir, "frames", $"lineup-{i:D2}.png"));
        }
        var g = new Canvas(6 * 40, 4 * 40, 0x1c2031); var gen = new List<object>();
        for (int i = 0; i < 24; i++) { var spec = Verdant.Forge.ForgeGenerator.Generate((ulong)(i + 1)); g.Blit(new CompactAdventurer(spec).Render(0), (i % 6) * 40 + 4, (i / 6) * 40 + 4, 1); gen.Add(new { seed = i + 1, spec.Id, spec.Name, spec.Class, spec.Species, spec.Headwear, spec.Weapon, spec.Description }); }
        Zoom(g, 4).Save(Path.Combine(dir, "generated.png"));
        File.WriteAllText(Path.Combine(dir, "generated-catalog.json"), System.Text.Json.JsonSerializer.Serialize(new { version = 1, generator = "VERDANT Forge", characters = gen }, json));
        for (int i = 0; i < 16; i++) { var gc = new Canvas(6 * 40, 4 * 40, 0x1c2031); for (int j = 0; j < 24; j++) gc.Blit(new CompactAdventurer(Verdant.Forge.ForgeGenerator.Generate((ulong)(j + 1))).Render(i / 4.0), (j % 6) * 40 + 4, (j / 6) * 40 + 4, 1); Zoom(gc, 4).Save(Path.Combine(dir, "frames", $"generated-{i:D2}.png")); }
        return 0;
    }
    static Canvas Zoom(Canvas c, int z)
    {
        var o = new Canvas(c.W * z, c.H * z, 0);
        for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++) { int i = (y * c.W + x) * 4; for (int j = 0; j < z; j++) for (int k = 0; k < z; k++) { int d = ((y * z + j) * o.W + x * z + k) * 4; o.Data[d] = c.Data[i]; o.Data[d + 1] = c.Data[i + 1]; o.Data[d + 2] = c.Data[i + 2]; o.Data[d + 3] = c.Data[i + 3]; } }
        return o;
    }
    static AdventurerKind Kind(string id) => RosterCatalog.All.First(e => e.Id == id).Kind;
    static int FrameCount(AdventurerMotion m) => m == AdventurerMotion.Attack ? 6 : m == AdventurerMotion.Hurt ? 4 : m == AdventurerMotion.Idle ? 16 : 8;
    static double Time(AdventurerMotion m, int f) => m == AdventurerMotion.Idle ? f / 4.0 : f / (m == AdventurerMotion.Run ? 12.0 : 8.0);
    static int Roster(string path, int zoom, AdventurerMotion motion)
    {
        var all = RosterCatalog.All; int cell = 32 * zoom; var c = new Canvas(cell * 4 + 40, cell * 2 + 30, 0x1c2031);
        for (int i = 0; i < all.Count; i++) { var a = new CompactAdventurer(1, all[i].Kind); var r = a.Render(Time(motion, 0), motion); c.Blit(r, 10 + (i % 4) * cell, 10 + (i / 4) * (cell + 10), zoom); }
        c.Save(path); return 0;
    }
    static int Strip(string path, AdventurerKind kind, AdventurerMotion motion, int zoom)
    {
        int n = FrameCount(motion); if (n > 8) n = 8; int step = FrameCount(motion) / n; int cell = 32 * zoom; var c = new Canvas(cell * n, cell, 0x1c2031);
        var a = new CompactAdventurer(1, kind); for (int f = 0; f < n; f++) c.Blit(a.Render(Time(motion, f * step), motion), f * cell, 0, zoom);
        c.Save(path); return 0;
    }
    static int Frames(string dir, AdventurerKind kind, AdventurerMotion motion, int zoom)
    {
        Directory.CreateDirectory(dir); var a = new CompactAdventurer(1, kind);
        for (int f = 0; f < FrameCount(motion); f++) { var c = new Canvas(32 * zoom, 32 * zoom, 0x1c2031); c.Blit(a.Render(Time(motion, f), motion), 0, 0, zoom); c.Save(Path.Combine(dir, $"f{f:D2}.png")); }
        return 0;
    }
}
