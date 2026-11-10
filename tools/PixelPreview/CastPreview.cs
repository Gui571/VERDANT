using Verdant;
using Verdant.Forge;
using Verdant.Forge.Compact;
namespace PixelPreview;
/// <summary>Exporta sprites soltos (32×32) e tiras de quadros do elenco v2; a composição com rótulos é feita por script Python.</summary>
public static class CastPreview
{
    static void Sprite(string dir, string name, Raster r) => Png.Save(Path.Combine(dir, name + ".png"), 32, 32, r.Pixels);
    static void Strip(string dir, string name, ChibiRenderer rr, AdventurerMotion motion, Facing facing = Facing.East, bool death = false)
    {
        int n = death ? 6 : ChibiRenderer.Frames(motion), fps = death ? 8 : ChibiRenderer.Fps(motion);
        var c = new Canvas(32 * n, 32, 0); Array.Clear(c.Data);
        for (int f = 0; f < n; f++) c.Blit(death ? rr.RenderDeath(f / (double)fps, facing) : rr.Render(f / (double)fps, motion, facing), f * 32, 0, 1);
        c.Save(Path.Combine(dir, name + ".png"));
    }
    public static int Export(string dir)
    {
        Directory.CreateDirectory(dir);
        // 1) espécies (mesma classe, para comparar só a espécie)
        foreach (var sp in Enum.GetValues<Species>()) Sprite(dir, "species-" + sp, new ChibiRenderer(ForgeCast.Generate(7, Grade.Recruit, species: sp, classId: "swordsman")) { GroundShadow = false }.Render(0));
        // 2) classes (espécies variadas, mesma patente)
        var spp = new[] { Species.Human, Species.Rabbit, Species.Duck, Species.Fox, Species.Wolf, Species.Bear, Species.Orc, Species.Cat, Species.Elf, Species.Owl };
        for (int i = 0; i < ForgeCast.Classes.Count; i++) { var c = ForgeCast.Classes[i]; Sprite(dir, "class-" + i.ToString("D2") + "-" + c.Id, new ChibiRenderer(ForgeCast.Generate(11 + (ulong)i, Grade.Recruit, species: spp[i % spp.Length], classId: c.Id)) { GroundShadow = false }.Render(0)); }
        // 3) patentes
        foreach (var cls in new[] { "knight", "mage", "archer", "cleric" }) foreach (var g in Enum.GetValues<Grade>())
            Sprite(dir, $"rank-{cls}-{(int)g}", new ChibiRenderer(ForgeCast.Generate(5, g, species: cls == "mage" ? Species.Rabbit : cls == "archer" ? Species.Fox : Species.Human, classId: cls)) { GroundShadow = false }.Render(0));
        // 4) "mude uma parte, mantenha o personagem"
        var baseSpec = ForgeCast.Generate(2042, Grade.Veteran, species: Species.Human, classId: "free");
        Sprite(dir, "slot-base", new ChibiRenderer(baseSpec) { GroundShadow = false }.Render(0));
        foreach (var slot in new[] { ForgeCast.SlotHat, ForgeCast.SlotWeapon, ForgeCast.SlotOffhand, ForgeCast.SlotBack, ForgeCast.SlotOutfit })
            for (ulong k = 1; k <= 2; k++) Sprite(dir, $"slot-{slot}-{k}", new ChibiRenderer(ForgeCast.Generate(2042, Grade.Veteran, slot, 900 + k * 37, Species.Human, "free")) { GroundShadow = false }.Render(0));
        // 5) movimento: tiras por clipe e quatro direções
        foreach (var (id, spec) in new[] { ("bram", ForgeRoster.All[0]), ("suri", ForgeRoster.All[5]), ("wolf-barb", ForgeCast.Generate(3, Grade.Elite, species: Species.Wolf, classId: "barbarian")), ("orc-hal", ForgeCast.Generate(4, Grade.Hero, species: Species.Orc, classId: "halberdier")) })
        {
            var rr = new ChibiRenderer(spec) { GroundShadow = false };
            foreach (var m in new[] { AdventurerMotion.Idle, AdventurerMotion.Walk, AdventurerMotion.Attack, AdventurerMotion.Hurt, AdventurerMotion.Special }) Strip(dir, $"strip-{id}-{m}", rr, m);
            Strip(dir, $"strip-{id}-Death", rr, AdventurerMotion.Idle, Facing.East, true);
            foreach (var f in Enum.GetValues<Facing>()) Sprite(dir, $"dir-{id}-{f}", rr.Render(0, AdventurerMotion.Idle, f));
        }
        // 6) seis equipes
        var nix = ForgeCast.Generate(9, Grade.Veteran, species: Species.Rabbit, classId: "rogue");
        for (int t = 0; t < 6; t++) Sprite(dir, "team-" + t, new ChibiRenderer(nix) { GroundShadow = false, Style = ChibiRenderer.Look(nix, t) }.Render(0));
        // 7) silhueta chapada + 24 sementes
        Sprite(dir, "sil-hero", new ChibiRenderer(baseSpec) { GroundShadow = false, Style = ChibiRenderer.Look(baseSpec) with { Pass = Verdant.Forge.StyleV2.ProofPass.Silhouette } }.Render(0));
        for (ulong s = 1; s <= 45; s++) Sprite(dir, "seed-" + s.ToString("D2"), new ChibiRenderer(ForgeCast.Generate(s)) { GroundShadow = false }.Render(0));
        return 0;
    }
}
