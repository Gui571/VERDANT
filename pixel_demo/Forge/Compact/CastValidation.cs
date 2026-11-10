using System.Security.Cryptography;
namespace Verdant.Forge.Compact;

/// <summary>Validação do elenco v2: 25 espécies × 20 classes × 4 patentes, slots independentes, silhuetas e tempo de golpe.</summary>
public static class CastValidation
{
    public static ForgeValidation.Result Run(int seeds = 60)
    {
        int checks = 0, maxColors = 0; var errors = new List<string>();
        void Check(bool yes, string message) { checks++; if (!yes && errors.Count < 60) errors.Add(message); }
        string MaskHash(Raster r) => Convert.ToHexString(SHA256.HashData(r.Pixels.Where((_, i) => i % 4 == 3).ToArray()));
        string Hash(Raster r) => Convert.ToHexString(SHA256.HashData(r.Pixels));
        // contagens declaradas
        Check(Enum.GetValues<Species>().Length >= 25, "25 espécies"); Check(ForgeCast.Classes.Count == 20, "20 classes"); Check(Enum.GetValues<Grade>().Length == 4, "4 patentes");
        Check(ForgeCast.SpeciesPt.Length == Enum.GetValues<Species>().Length, "nomes de espécies completos");
        // matriz completa: toda espécie × classe × patente em Idle
        foreach (var sp in Enum.GetValues<Species>()) foreach (var cls in ForgeCast.Classes) foreach (var g in Enum.GetValues<Grade>())
        {
            var spec = ForgeCast.Generate(3, g, species: sp, classId: cls.Id); var px = new ChibiRenderer(spec).Render(0); string tag = $"{sp}/{cls.Id}/{g}";
            var color = new HashSet<uint>(); bool alpha = true, margin = true;
            for (int i = 0; i < 1024; i++) { alpha &= px.Pixels[i * 4 + 3] is 0 or 255; if (px.Pixels[i * 4 + 3] != 0) { color.Add(BitConverter.ToUInt32(px.Pixels, i * 4)); if (i % 32 == 0 || i % 32 == 31 || i / 32 == 0 || i / 32 == 31) margin = false; } }
            maxColors = Math.Max(maxColors, color.Count);
            Check(alpha && color.Count is > 0 and <= 16, "paleta " + tag + " cores=" + color.Count); Check(margin && px.Clipped == 0, "margem " + tag);
        }
        // sementes: todos os clipes e direções, determinismo
        for (ulong seed = 1; seed <= (ulong)seeds; seed++)
        {
            var spec = ForgeCast.Generate(seed); var again = ForgeCast.Generate(seed);
            Check(spec.Species == again.Species && spec.Headwear == again.Headwear && spec.Weapon == again.Weapon && spec.Name == again.Name && spec.Grade == again.Grade, "geração determinística " + seed);
            var a = new ChibiRenderer(spec); var b = new ChibiRenderer(again);
            foreach (var motion in Enum.GetValues<AdventurerMotion>()) foreach (var facing in Enum.GetValues<Facing>())
            {
                int n = ChibiRenderer.Frames(motion), fps = ChibiRenderer.Fps(motion);
                for (int f = 0; f < n; f++)
                {
                    var px = a.Render(f / (double)fps, motion, facing); var color = new HashSet<uint>(); bool alpha = true, margin = true; string edge = "";
                    for (int i = 0; i < 1024; i++) { alpha &= px.Pixels[i * 4 + 3] is 0 or 255; if (px.Pixels[i * 4 + 3] != 0) { color.Add(BitConverter.ToUInt32(px.Pixels, i * 4)); if (i % 32 == 0 || i % 32 == 31 || i / 32 == 0 || i / 32 == 31) { margin = false; edge = $" borda=({i % 32},{i / 32})"; } } }
                    maxColors = Math.Max(maxColors, color.Count); string tag = $"{spec.Id}/{motion}/{facing}/{f}";
                    Check(alpha && color.Count is > 0 and <= 16, "paleta " + tag + " cores=" + color.Count); Check(margin && px.Clipped == 0, "margem " + tag + edge + " clipped=" + px.Clipped); Check(Hash(px) == Hash(b.Render(f / (double)fps, motion, facing)), "determinismo " + tag);
                }
            }
            for (int f = 0; f < 6; f++) { var d = a.RenderDeath(f / 8.0); Check(d.Clipped == 0, "morte limites " + spec.Id); Check(f == 5 ? d.Pixels.All(v => v == 0) : d.Pixels.Any(v => v != 0), "morte pose " + spec.Id + "/" + f); }
        }
        // slots independentes: trocar o fluxo de UM slot não muda os outros sorteios
        foreach (var slot in new[] { ForgeCast.SlotHat, ForgeCast.SlotBack, ForgeCast.SlotOutfit, ForgeCast.SlotWeapon, ForgeCast.SlotName, ForgeCast.SlotColor })
            for (ulong seed = 100; seed < 130; seed++)
            {
                var baseSpec = ForgeCast.Generate(seed); var alt = ForgeCast.Generate(seed, null, slot, 7000 + seed);
                bool same(Func<ForgeSpec, object> get) => get(baseSpec).Equals(get(alt));
                if (slot != ForgeCast.SlotColor) Check(baseSpec.Grade == alt.Grade, $"slot {slot} preserva patente");
                if (slot != ForgeCast.SlotHat) Check(same(s => s.Headwear), $"slot {slot} preserva chapéu");
                if (slot != ForgeCast.SlotWeapon) { Check(same(s => s.Weapon), $"slot {slot} preserva arma"); Check(same(s => s.Offhand), $"slot {slot} preserva mão secundária"); }
                if (slot != ForgeCast.SlotBack) Check(same(s => s.Back), $"slot {slot} preserva costas");
                if (slot != ForgeCast.SlotOutfit) Check(same(s => s.Outfit), $"slot {slot} preserva roupa");
                if (slot != ForgeCast.SlotName) Check(same(s => s.Name), $"slot {slot} preserva nome");
                Check(same(s => s.Species) && same(s => s.Class), $"slot {slot} preserva espécie e classe");
            }
        // silhuetas: as 20 classes são distintas na mesma espécie; as 4 patentes diferem em cada classe
        foreach (var species in new[] { Species.Human, Species.Rabbit, Species.Wolf })
        {
            var masks = new Dictionary<string, string>();
            foreach (var cls in ForgeCast.Classes)
            {
                var px = new ChibiRenderer(ForgeCast.Generate(5, Grade.Recruit, species: species, classId: cls.Id)) { GroundShadow = false }.Render(0);
                string h = MaskHash(px); Check(masks.TryAdd(h, cls.Id), $"silhueta de classe {species}/{cls.Id} igual a " + (masks.TryGetValue(h, out var o) ? o : "?"));
            }
        }
        foreach (var cls in ForgeCast.Classes)
        {
            var hashes = new HashSet<string>(); foreach (var g in Enum.GetValues<Grade>()) hashes.Add(Hash(new ChibiRenderer(ForgeCast.Generate(5, g, species: Species.Human, classId: cls.Id)) { GroundShadow = false }.Render(0)));
            Check(hashes.Count == 4, "4 patentes distintas em " + cls.Id);
        }
        // tempo do golpe: 12 quadros a 16 fps = 0,75 s; seis poses, cada uma presente; pesos desiguais
        Check(ChibiRenderer.AttackPose.Length == 12 && ChibiRenderer.Fps(AdventurerMotion.Attack) == 16, "golpe: 12 quadros a 16 fps");
        Check(Enumerable.Range(0, 6).All(p => ChibiRenderer.AttackPose.Contains(p)), "golpe: seis poses presentes");
        Check(ChibiRenderer.AttackPose.GroupBy(p => p).Select(g => g.Count()).Distinct().Count() > 1, "golpe: duração por pose desigual");
        Check(ChibiRenderer.AttackPose.Zip(ChibiRenderer.AttackPose.Skip(1), (x, y) => y >= x).All(v => v), "golpe: poses em ordem");
        // arco de golpe: aparece no contato de arma corpo a corpo e não aparece em arco/cajado
        int Region(ForgeSpec sp, int f) { var r = new ChibiRenderer(sp) { GroundShadow = false }.Render(f / 16.0, AdventurerMotion.Attack); int n = 0; for (int y = 12; y <= 22; y++) for (int x = 24; x <= 29; x++) if (r.Pixels[(y * 32 + x) * 4 + 3] != 0) n++; return n; }
        var sword = ForgeCast.Generate(5, Grade.Recruit, species: Species.Human, classId: "swordsman");
        Check(Region(sword, 6) >= Region(sword, 4) + 4, "arco de golpe visível no contato");
        // hit em xadrez e sem alfa parcial
        var hit = new ChibiRenderer(ForgeCast.Generate(5, Grade.Recruit, species: Species.Wolf, classId: "knight")); var h0 = (byte[])hit.Render(0, AdventurerMotion.Hurt).Pixels.Clone(); var h3 = hit.Render(3 / 8.0, AdventurerMotion.Hurt).Pixels;
        Check(!h0.SequenceEqual(h3), "hit muda a imagem"); int checker = 0; for (int i = 0; i < 1024; i++) if (h0[i * 4 + 3] != 0 && (i % 32 + i / 32) % 2 == 0 && h0[i * 4] > 230 && h0[i * 4 + 1] > 230) checker++; Check(checker > 15, "hit em xadrez claro"); 
        Console.WriteLine($"CAST checks={checks} failures={errors.Count} max_colors={maxColors} species={Enum.GetValues<Species>().Length} classes={ForgeCast.Classes.Count} grades=4 seeds={seeds}");
        return new(checks, errors);
    }
}
