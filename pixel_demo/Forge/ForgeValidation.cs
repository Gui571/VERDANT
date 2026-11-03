using System.Security.Cryptography;
namespace Verdant.Forge;

/// <summary>Regras objetivas do estilo, verificáveis sem abrir o Godot (usadas pelo --pixel-test e pela ferramenta de prévia).</summary>
public static class ForgeValidation
{
    public sealed record Result(int Checks, IReadOnlyList<string> Failures);

    static readonly (AdventurerMotion m, int frames, double fps)[] Motions =
    {
        (AdventurerMotion.Idle, 32, 8), (AdventurerMotion.Walk, 8, 8), (AdventurerMotion.Run, 8, 12), (AdventurerMotion.Attack, 6, 8), (AdventurerMotion.Hurt, 4, 8), (AdventurerMotion.Special, 8, 8), (AdventurerMotion.Cast, 8, 8)
    };

    public static Result Run(int generatedSeeds = 200)
    {
        int checks = 0; var fails = new List<string>();
        void Check(bool ok, string name) { checks++; if (!ok && fails.Count < 4000) fails.Add(name); }

        var specs = ForgeRoster.All.Concat(Enumerable.Range(1, generatedSeeds).Select(i => ForgeGenerator.Generate((ulong)i))).ToList();
        foreach (var spec in specs)
        {
            var r = new ForgeRenderer(spec); var twin = new ForgeRenderer(spec);
            foreach (var (m, frames, fps) in Motions) foreach (bool back in new[] { false, true })
                for (int f = 0; f < frames; f++)
                {
                    var a = r.Render(f / fps, m, back); string tag = $"{spec.Id}/{m}/{(back ? "back" : "front")}/{f}";
                    Check(a.Clipped == 0, "recorte: " + tag);
                    var colors = new HashSet<uint>(); bool binary = true;
                    for (int p = 0; p < a.Pixels.Length; p += 4) { byte al = a.Pixels[p + 3]; if (al != 0 && al != 255) binary = false; if (al != 0) colors.Add(BitConverter.ToUInt32(a.Pixels, p)); }
                    Check(binary, "antisserrilhado: " + tag); Check(colors.Count <= RosterVisualRules.MaxColors, $"paleta ({colors.Count}) : " + tag);
                    Check(a.Pixels.SequenceEqual(twin.Render(f / fps, m, back).Pixels), "determinismo: " + tag);
                    if (m == AdventurerMotion.Idle && !back) Check(Orphans(a) == 0, "pixel órfão: " + tag);
                }
            // Loops: idle de 4 s e passadas de 1 s fecham sem salto.
            Check(r.Render(0).Pixels.SequenceEqual(r.Render(4).Pixels), "loop idle: " + spec.Id);
            Check(r.Render(0, AdventurerMotion.Walk).Pixels.SequenceEqual(r.Render(1, AdventurerMotion.Walk).Pixels), "loop caminhada: " + spec.Id);
            Check(r.Render(0, AdventurerMotion.Run).Pixels.SequenceEqual(r.Render(8 / 12.0, AdventurerMotion.Run).Pixels), "loop corrida: " + spec.Id);
            // O idle realmente anima, e os pés não saem do lugar (contato fixo com o chão).
            var variants = new HashSet<string>(); var foot = r.Render(0).Pixels.AsSpan((28 * 32 + 12) * 4, 8 * 4).ToArray();
            for (int f = 0; f < 32; f++) { var px = r.Render(f / 8.0).Pixels; variants.Add(Convert.ToHexString(SHA256.HashData(px))); Check(px.AsSpan((28 * 32 + 12) * 4, 8 * 4).SequenceEqual(foot), $"pés móveis no idle: {spec.Id}/{f}"); }
            Check(variants.Count >= 4, "idle com poses distintas: " + spec.Id);
        }
        // Silhuetas únicas no elenco fixo.
        var sil = new HashSet<string>();
        foreach (var spec in ForgeRoster.All) { var px = new ForgeRenderer(spec).Render(0).Pixels; var al = new byte[1024]; for (int i = 0; i < 1024; i++) al[i] = px[i * 4 + 3]; Check(sil.Add(Convert.ToHexString(SHA256.HashData(al))), "silhueta única: " + spec.Id); }
        // Geração: sementes distintas dão personagens distintos; mesma semente repete exatamente.
        var seen = new HashSet<string>(); for (ulong s = 1; s <= 120; s++) { var a = ForgeGenerator.Generate(s); Check(a == ForgeGenerator.Generate(s), "semente determinística " + s); seen.Add(Convert.ToHexString(SHA256.HashData(new ForgeRenderer(a).Render(0).Pixels))); }
        Check(seen.Count >= 110, $"variedade de sementes ({seen.Count}/120)");
        return new Result(checks, fails);
    }

    /// <summary>Conta pixels opacos sem nenhum vizinho opaco (4-vizinhança), exceto brilhos de 1 px previstos nas armas.</summary>
    public static int Orphans(Raster a)
    {
        int n = 0;
        for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++)
        {
            if (a.Pixels[(y * a.Width + x) * 4 + 3] == 0) continue; bool any = false;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) { int nx = x + dx, ny = y + dy; if (nx >= 0 && ny >= 0 && nx < a.Width && ny < a.Height && a.Pixels[(ny * a.Width + nx) * 4 + 3] != 0) any = true; }
            if (!any) n++;
        }
        return n;
    }
}
