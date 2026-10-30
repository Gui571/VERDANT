using Godot;
using System.Text.Json;
namespace Verdant;
public static class Exporter
{
    public static void Png(Genome g, Pose pose, string path) { using var image = new CreatureRenderer(g).Render(pose).Image(); var e = image.SavePng(path); if (e != Error.Ok) throw new IOException($"PNG export failed: {e}"); }
    public static void Sheet(Genome g, string path, MotionState state, int direction)
    {
        var renderer = new CreatureRenderer(g); using var sheet = Image.CreateEmpty(192 * 8, 160 * 2, false, Image.Format.Rgba8); var frames = new List<object>();
        for (int i = 0; i < 16; i++) { var pose = Pose.At(i / 12.0, state, direction); using var img = renderer.Render(pose).Image(); sheet.BlitRect(img, new Rect2I(0, 0, 192, 160), new Vector2I(i % 8 * 192, i / 8 * 160)); frames.Add(new { index = i, x = i % 8 * 192, y = i / 8 * 160, width = 192, height = 160, origin = new[] { 96, 120 }, time = i / 12.0, state = state.ToString(), direction }); }
        if (sheet.SavePng(path) != Error.Ok) throw new IOException("Spritesheet write failed"); File.WriteAllText(path + ".json", JsonSerializer.Serialize(new { version = 1, generator = g.Generator, genome = g, fps = 12, frames }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
