using Godot;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
namespace Verdant;
public static class SelfTests
{
    public static int Run()
    {
        int checks = 0; var failures = new List<string>(); var metrics = new List<object>(); void Check(bool ok, string name) { checks++; if (!ok) failures.Add(name); }
        var outDir = ProjectSettings.GlobalizePath("user://verification"); Directory.CreateDirectory(outDir);
        foreach (var plan in Plans.All)
        {
            for (uint seed = 0; seed < 100; seed++)
            {
                var g = Genome.Generate(seed, plan.Id); g.Validate(); Check(g == Genome.Generate(seed, plan.Id), "genome deterministic"); Check(g == Genome.FromJson(g.ToJson()), "JSON roundtrip"); var renderer = new CreatureRenderer(g); var pose = Pose.At(.625, MotionState.Walk, (int)(seed % 4)); var a = SHA256.HashData(renderer.Render(pose).Pixels); var b = SHA256.HashData(renderer.Render(pose).Pixels); Check(a.SequenceEqual(b), "image deterministic"); Check(renderer.Buffer.Clipped == 0, $"clipped {plan.Id}/{seed}"); Check(renderer.Buffer.Pixels.Any(x => x != 0), "nonempty render");
                foreach (var state in Enum.GetValues<MotionState>()) { renderer.Render(Pose.At(.125, state)); Check(renderer.Buffer.Clipped == 0, $"state bounds {plan.Id}/{seed}/{state}"); }
            }
            for (uint i = 0; i < 6; i++) { var g = Genome.Generate(600 + i, plan.Id); Check(Genome.FromJson(File.ReadAllText(ProjectSettings.GlobalizePath($"res://samples/{plan.Id}-{i}.json"))) == g, "saved sample matches generator"); Exporter.Png(g, Pose.At(.3, MotionState.Walk), Path.Combine(outDir, $"{plan.Id}-{i}.png")); }
        }
        using (var queue = new GenerationQueue()) { for (uint i = 0; i < 50; i++) queue.Request(Genome.Generate(i, "beast")); queue.Cancel(); Check(queue.TakeLatest() == null, "cancel discards result"); var last = Genome.Generate(123456, "slime"); queue.Request(last); CreatureRenderer? result = null; var wait = Stopwatch.StartNew(); while (result == null && wait.ElapsedMilliseconds < 5000) { result = queue.TakeLatest(); System.Threading.Thread.Sleep(1); } Check(result != null, "latest generation completes"); if (result != null) Check(result.Render(Pose.At(0, MotionState.Idle)).Pixels.SequenceEqual(new CreatureRenderer(last).Render(Pose.At(0, MotionState.Idle)).Pixels), "obsolete result never published"); }
        foreach (var plan in Plans.All) foreach (float extreme in new[] { .55f, 1.5f }) { var g = Genome.Generate(42, plan.Id) with { Mass = extreme, Length = extreme, Head = extreme, Legs = extreme, Tail = extreme, Horns = 1 }; var r = new CreatureRenderer(g); for (int direction = 0; direction < 4; direction++) { r.Render(Pose.At(.7, MotionState.Run, direction)); Check(r.Buffer.Clipped == 0, $"extreme bounds {plan.Id}/{extreme}/{direction}"); } }
        var sample = Genome.Generate(1042, "beast"); Check(sample.Mutate(9, 0) with { Seed = sample.Seed } == sample, "zero mutation preserves traits"); try { Genome.Cross(sample, Genome.Generate(1, "slime"), 2); Check(false, "incompatible crossing"); } catch (ArgumentException) { Check(true, "cross rejected"); }
        foreach (var bad in new[] { "{}", "{\"Version\":2}", "{\"Mass\":0}", "{\"Family\":\"bad\"}" }) { try { var g = Genome.FromJson(bad); Check(false, "reject invalid"); } catch (ArgumentException) { Check(true, "invalid rejected"); } }
        var history = new History(); history.Push(sample); var mutated = sample.Mutate(3, .2f); Check(history.Undo(mutated) == sample, "undo"); Check(history.Redo(sample) == mutated, "redo");
        var p60 = new Pose(); p60.SetState(MotionState.Walk); for (int i = 0; i < 120; i++) p60.Tick(1.0 / 60); var p120 = new Pose(); p120.SetState(MotionState.Walk); for (int i = 0; i < 240; i++) p120.Tick(1.0 / 120); Check(Math.Abs(p60.Time - p120.Time) < 1e-10 && Math.Abs(p60.Activity - p120.Activity) < 1e-6, "time independent");
        Exporter.Sheet(sample, Path.Combine(outDir, "sheet.png"), MotionState.Walk, 0); using (var sheet = Image.LoadFromFile(Path.Combine(outDir, "sheet.png"))) { using var expected = new CreatureRenderer(sample).Render(Pose.At(0, MotionState.Walk)).Image(); using var first = sheet.GetRegion(new Rect2I(0, 0, 192, 160)); Check(expected.GetData().SequenceEqual(first.GetData()), "sheet matches live renderer"); }
        using (var contact = Image.CreateEmpty(192 * 6, 160 * 9, false, Image.Format.Rgba8)) { for (int row = 0; row < Plans.All.Count; row++) for (int col = 0; col < 6; col++) { using var img = Image.LoadFromFile(Path.Combine(outDir, $"{Plans.All[row].Id}-{col}.png")); contact.BlitRect(img, new Rect2I(0, 0, 192, 160), new Vector2I(col * 192, row * 160)); } contact.SavePng(Path.Combine(outDir, "contact-sheet.png")); }
        foreach (int count in new[] { 1, 25, 100 }) { var timer = Stopwatch.StartNew(); var renderers = Enumerable.Range(0, count).Select(i => new CreatureRenderer(Genome.Generate((uint)i, Plans.All[i % 9].Id))).ToArray(); double cold = timer.Elapsed.TotalMilliseconds; var times = new List<double>(); long alloc = GC.GetAllocatedBytesForCurrentThread(); for (int frame = 0; frame < 30; frame++) { timer.Restart(); foreach (var r in renderers) r.Render(Pose.At(frame / 30.0, MotionState.Walk)); times.Add(timer.Elapsed.TotalMilliseconds); } times.Sort(); metrics.Add(new { population = count, coldMs = cold, p50Ms = times[15], p95Ms = times[28], p99Ms = times[29], allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - alloc, workingSet = Process.GetCurrentProcess().WorkingSet64 }); }
        var report = new { checks, passed = checks - failures.Count, failures, metrics, scope = "CPU raster only; no GPU texture upload or desktop FPS claim" }; var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(Path.Combine(outDir, "report.json"), json); GD.Print(json); GD.Print("Artifacts: " + outDir); return failures.Count == 0 ? 0 : 1;
    }
}
