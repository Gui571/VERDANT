using Godot;
namespace Verdant;
public partial class WorldView : Control
{
    public bool Landscape = true, FieldMode, Paused, Rig;
    public int Zoom = 3;
    public Genome Genome = new();
    public readonly Pose Pose = new();
    CreatureRenderer renderer = new(new Genome());
    readonly GenerationQueue generation = new();
    ImageTexture? creature, terrain;
    readonly List<(CreatureRenderer renderer, ImageTexture texture)> neighbors = new();
    Vector2 position = new(0, 0);
    double accumulator, textureClock;
    public override void _Ready() { ClipContents = true; TextureFilter = TextureFilterEnum.Nearest; MouseFilter = MouseFilterEnum.Ignore; BuildTerrain(); for (int i = 0; i < 4; i++) { var r = new CreatureRenderer(Genome.Generate((uint)(200 + i), Plans.All[i * 2].Id)); using var img = r.Render(Verdant.Pose.At(i, MotionState.Walk)).Image(); neighbors.Add((r, ImageTexture.CreateFromImage(img))); } }
    public void SetGenome(Genome g) { Genome = g; generation.Request(g); }
    public void Step() { Pose.Tick(1.0 / 60); Refresh(); }
    void Refresh() { using var img = renderer.Render(Pose, Rig).Image(); if (creature == null) creature = ImageTexture.CreateFromImage(img); else creature.Update(img); QueueRedraw(); }
    void BuildTerrain()
    {
        var r = new Raster(480, 270); var random = new RandomStream(991); r.Rect(0, 0, 480, 270, new Color("597d58")); for (int y = 0; y < 270; y++) { for (int x = 0; x < 480; x++) { float river = 355 + MathF.Sin(y * .019f) * 32; if (x > river) { r.Pixel(x, y, new Color(x < river + 5 ? "99ae72" : x < river + 9 ? "456f69" : "315f69")); if (x > river + 10 && (x + (int)(Math.Sin(y) * 7)) % 31 < 9 && y % 9 == 0) r.Pixel(x, y, new Color("528b8c")); } } }
        for (int i = 0; i < 1500; i++) { int x = (int)random.Range(0, 340), y = (int)random.Range(0, 270); r.Rect(x, y, 2, 1, new Color(i % 2 == 0 ? "6e9061" : "4e714f")); if (i % 11 == 0) r.Rect(x, y - 2, 1, 3, new Color("87a66c")); }
        for (int i = 0; i < 21; i++) { int x = (int)random.Range(5, 330), y = (int)random.Range(8, 260); if (x > 90 && x < 285 && y > 65 && y < 215) continue; r.Ellipse(x + 5, y + 8, 17, 6, new Color("48654c")); r.Rect(x - 2, y - 11, 5, 20, new Color("485045")); r.Ellipse(x, y - 17, 18, 19, new Color("294e44")); r.Ellipse(x - 4, y - 23, 15, 16, new Color("3b6550")); r.Ellipse(x - 7, y - 27, 10, 10, new Color("63885b")); }
        for (int i = 0; i < 90; i++) { int x = (int)random.Range(0, 330), y = (int)random.Range(0, 270); r.Rect(x, y, 2, 2, new Color(i % 2 == 0 ? "e9c873" : "b0c68b")); }
        using var img = r.Image(); terrain = ImageTexture.CreateFromImage(img);
    }
    public override void _Process(double delta)
    {
        var latest = generation.TakeLatest(); if (latest != null) { renderer = latest; Refresh(); }
        if (Paused) return; accumulator += Math.Min(delta, .25); while (accumulator >= 1.0 / 60) { if (FieldMode) { var dir = new Vector2((Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0), (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0)); position += dir.Normalized() * 1.1f; position = position.Clamp(new Vector2(-100, -55), new Vector2(100, 55)); Pose.SetState(dir.Length() > 0 ? MotionState.Walk : MotionState.Idle); if (dir.X != 0) Pose.Direction = dir.X < 0 ? 1 : 0; else if (dir.Y != 0) Pose.Direction = dir.Y < 0 ? 2 : 3; } Pose.Tick(1.0 / 60); accumulator -= 1.0 / 60; }
        textureClock += delta; if (textureClock < 1.0 / 30) return; textureClock = 0; Refresh(); if (FieldMode) for (int i = 0; i < neighbors.Count; i++) { using var img = neighbors[i].renderer.Render(Verdant.Pose.At(Pose.Time + i, MotionState.Walk, i % 2)).Image(); neighbors[i].texture.Update(img); }
    }
    public override void _Draw()
    {
        DrawStyleBox(new StyleBoxFlat { BgColor = new Color("172725"), CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 }, new Rect2(Vector2.Zero, Size)); float scale = FieldMode ? 2 : Zoom; var size = new Vector2(480, 270) * scale; var origin = (Size - size) / 2; if (Landscape && terrain != null) DrawTextureRect(terrain, new Rect2(origin, size), false); else { for (int i = 0; i < Size.X; i += 24) DrawLine(new Vector2(i, 0), new Vector2(i, Size.Y), new Color("1e312e")); for (int j = 0; j < Size.Y; j += 24) DrawLine(new Vector2(0, j), new Vector2(Size.X, j), new Color("1e312e")); }
        float z = FieldMode ? 2 : Zoom; var center = Size / 2 + new Vector2(-25, 15) + position * (FieldMode ? scale : 0);
        if (FieldMode) for (int i = 0; i < neighbors.Count; i++) { var pos = Size / 2 + new Vector2((i % 2 == 0 ? -180 : 170), (i / 2 == 0 ? -75 : 100)); DrawTextureRect(neighbors[i].texture, new Rect2(pos - new Vector2(96, 120), new Vector2(192, 160)), false); }
        DrawEllipseShadow(center + new Vector2(0, (-12 + 7 * Genome.Mass + 17 * Genome.Legs + 2.6f) * z), new Vector2(36, 7) * z); if (creature != null) DrawTextureRect(creature, new Rect2(center - new Vector2(96, 100) * z, new Vector2(192, 160) * z), false);
    }
    void DrawEllipseShadow(Vector2 center, Vector2 radius) { var pts = Enumerable.Range(0, 32).Select(i => center + new Vector2(MathF.Cos(i * MathF.Tau / 32) * radius.X, MathF.Sin(i * MathF.Tau / 32) * radius.Y)).ToArray(); DrawColoredPolygon(pts, new Color(0.06f, .16f, .14f, .28f)); }
    public override void _ExitTree() { generation.Dispose(); creature?.Dispose(); terrain?.Dispose(); foreach (var n in neighbors) n.texture.Dispose(); }
}
