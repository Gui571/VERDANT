using System.Text.Json;
namespace Verdant;

// Explicit xorshift32 streams: deterministic on .NET platforms, independent of UI and frame rate.
public sealed class RandomStream
{
    uint state;
    public RandomStream(uint seed, uint stream = 0) { state = seed ^ (0x9E3779B9u * (stream + 1)); state ^= state >> 16; state *= 0x7feb352du; state ^= state >> 15; state *= 0x846ca68bu; state ^= state >> 16; if (state == 0) state = 1; }
    public uint Next() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return state; }
    public float Unit() => (Next() >> 8) / 16777216f;
    public float Range(float a, float b) => a + (b - a) * Unit();
}
public sealed record BodyPlan(string Id, string Label, string Motion, int Legs, bool Wings, bool LongBody, bool Fins, bool Soft, bool Upright, bool Leaves = false);
public static class Plans
{
    public static readonly List<BodyPlan> All = new() {
        new("beast","Mossback","walk",4,false,false,false,false,false),
        new("golem","Stonekin","walk",2,false,false,false,false,true),
        new("dragon","Emberdrake","walk",4,false,false,false,false,false),
        new("arthropod","Amberclaw","crawl",8,false,false,false,false,false),
        new("winged","Sunwing","fly",2,true,false,false,false,false),
        new("serpent","Reed serpent","slither",0,false,true,false,false,false),
        new("aquatic","Tidefin","swim",0,false,false,true,false,false),
        new("slime","Dewdrop","hop",0,false,false,false,true,false),
        new("plant","Brambleling","walk",2,false,false,false,false,true,true)
    };
    public static BodyPlan Get(string id) => All.Find(p => p.Id == id) ?? throw new ArgumentException($"Unknown body plan: {id}");
}
public sealed record Genome
{
    public int Version { get; init; } = 1;
    public string Generator { get; init; } = "verdant-1";
    public uint Seed { get; init; } = 1042;
    public string Family { get; init; } = "beast";
    public float Mass { get; init; } = 1;
    public float Length { get; init; } = 1;
    public float Legs { get; init; } = 1;
    public float Tail { get; init; } = 1;
    public float Head { get; init; } = 1;
    public float Horns { get; init; } = .5f;
    public float Hue { get; init; } = .28f;
    public float Pattern { get; init; } = .5f;
    public float Tempo { get; init; } = 1;
    public void Validate()
    {
        if (Version != 1 || Generator != "verdant-1") throw new ArgumentException("Unsupported genome or generator version; migration required.");
        Plans.Get(Family);
        foreach (var x in new[] { Mass, Length, Legs, Tail, Head, Tempo }) if (!float.IsFinite(x) || x < .55f || x > 1.5f) throw new ArgumentException("Anatomy and tempo must be finite, within 0.55–1.5.");
        foreach (var x in new[] { Horns, Hue, Pattern }) if (!float.IsFinite(x) || x < 0 || x > 1) throw new ArgumentException("Color and detail parameters must be within 0–1.");
    }
    public static Genome Generate(uint seed, string family)
    {
        var r = new RandomStream(seed, 0); var color = new RandomStream(seed, 1); var pattern = new RandomStream(seed, 2); var motion = new RandomStream(seed, 3);
        var g = new Genome { Seed = seed, Family = family, Mass = r.Range(.7f, 1.4f), Length = r.Range(.7f, 1.4f), Legs = r.Range(.65f, 1.35f), Tail = r.Range(.6f, 1.45f), Head = r.Range(.75f, 1.3f), Horns = r.Unit(), Hue = color.Unit(), Pattern = pattern.Unit(), Tempo = motion.Range(.7f, 1.3f) };
        g.Validate(); return g;
    }
    public Genome Mutate(uint seed, float strength)
    {
        if (!float.IsFinite(strength) || strength < 0 || strength > 1) throw new ArgumentException("Mutation strength must be 0–1.");
        var r = new RandomStream(seed, 4); float M(float x) => Math.Clamp(x + r.Range(-strength, strength), .55f, 1.5f);
        return this with { Seed = seed, Mass = M(Mass), Length = M(Length), Legs = M(Legs), Tail = M(Tail), Head = M(Head), Horns = Math.Clamp(Horns + r.Range(-strength, strength), 0, 1), Hue = (Hue + r.Range(-strength * .2f, strength * .2f) + 1) % 1, Pattern = Math.Clamp(Pattern + r.Range(-strength, strength), 0, 1), Tempo = M(Tempo) };
    }
    public static Genome Cross(Genome a, Genome b, uint seed)
    {
        a.Validate(); b.Validate(); if (a.Family != b.Family) throw new ArgumentException("Parents must share a body plan.");
        var r = new RandomStream(seed, 5); float Pick(float x, float y) => r.Unit() < .5f ? x : y;
        return a with { Seed = seed, Mass = Pick(a.Mass, b.Mass), Length = Pick(a.Length, b.Length), Legs = Pick(a.Legs, b.Legs), Tail = Pick(a.Tail, b.Tail), Head = Pick(a.Head, b.Head), Horns = Pick(a.Horns, b.Horns), Hue = Pick(a.Hue, b.Hue), Pattern = Pick(a.Pattern, b.Pattern), Tempo = Pick(a.Tempo, b.Tempo) };
    }
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
    public static Genome FromJson(string json) { using var document = JsonDocument.Parse(json); foreach (var field in new[] { "Version", "Generator", "Family", "Seed" }) if (!document.RootElement.TryGetProperty(field, out _)) throw new ArgumentException("Missing genome field: " + field); var g = JsonSerializer.Deserialize<Genome>(json) ?? throw new ArgumentException("Empty genome"); g.Validate(); return g; }
}
public sealed class History
{
    readonly Stack<Genome> undo = new(), redo = new();
    public void Push(Genome g) { undo.Push(g); redo.Clear(); }
    public Genome Undo(Genome now) { if (undo.Count == 0) return now; redo.Push(now); return undo.Pop(); }
    public Genome Redo(Genome now) { if (redo.Count == 0) return now; undo.Push(now); return redo.Pop(); }
}
