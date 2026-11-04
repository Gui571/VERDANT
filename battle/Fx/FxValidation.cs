using System.Numerics;
namespace Verdant.Battle;

/// <summary>Validações da camada de efeitos (sem engine): sprites em pixel art de verdade, determinismo, quatro fases, orçamento de partículas,
/// fim limpo e simulação intocada (eventos idênticos com e sem a camada de apresentação).</summary>
public static class FxValidation
{
    public static (int checks, List<string> failures, List<string> info) Run()
    {
        var failures = new List<string>(); var info = new List<string>(); int checks = 0;
        void Check(bool b, string label) { checks++; if (!b) failures.Add(label); }
        var atlas = FxAtlas.Shared; var img = atlas.Image;
        // 1. Atlas: alfa 0/255, ≤16 cores por quadro, dentro dos limites, sem quadros vazios
        foreach (var s in atlas.Sprites)
        {
            Check(s.X >= 0 && s.Y >= 0 && s.X + s.W * s.Frames <= img.Width && s.Y + s.H <= img.Height, $"{s.Name}:{s.Theme} dentro do atlas");
            for (int f = 0; f < s.Frames; f++)
            {
                var colors = new HashSet<int>(); int opaque = 0; bool badAlpha = false;
                for (int y = 0; y < s.H; y++) for (int x = 0; x < s.W; x++)
                {
                    int o = ((s.Y + y) * img.Width + s.X + f * s.W + x) * 4; int a = img.Pixels[o + 3];
                    if (a != 0 && a != 255) badAlpha = true; if (a == 255) { opaque++; colors.Add(img.Pixels[o] << 16 | img.Pixels[o + 1] << 8 | img.Pixels[o + 2]); }
                }
                Check(!badAlpha, $"{s.Name}:{s.Theme}#{f} alfa binário"); Check(colors.Count <= 16, $"{s.Name}:{s.Theme}#{f} ≤16 cores ({colors.Count})"); Check(opaque > 0, $"{s.Name}:{s.Theme}#{f} não vazio");
                Check(s.PivotX >= 0 && s.PivotX < s.W && s.PivotY >= 0 && s.PivotY < s.H, $"{s.Name}:{s.Theme} pivô dentro");
            }
        }
        info.Add($"atlas: {atlas.Sprites.Count} sprites, {img.Width}×{img.Height}");
        // 2. Atlas determinístico
        var again = FxAtlas.Build(); Check(again.Image.Pixels.AsSpan().SequenceEqual(img.Pixels), "atlas determinístico");
        // 3. Cada estado tem ícone e camada de corpo
        foreach (var k in Enum.GetValues<EffectKind>())
        {
            if (k is EffectKind.Damage or EffectKind.Heal or EffectKind.Push or EffectKind.Dash or EffectKind.Summon) continue;
            Check(StatusIcons.Priority.Contains(k), $"estado {k} tem ícone"); Check(atlas.Has("st_" + StatusIcons.Name(k), FxPalette.ForStatus(k)) || atlas.Sprites.Any(s => s.Name.Contains(StatusIcons.Name(k))), $"estado {k} no atlas");
        }
        // 4. Por habilidade: determinismo, fases, orçamento, fim limpo, simulação intacta
        FighterSpec Unit(int team, string ability = "nova") => new(team, "test", "test", Role.Melee, new(1000, 5, 0, 0, 18, 900), ability);
        BattleSimulation Create(string id) { var sim = new BattleSimulation(new(42, new[] { Unit(0, id), Unit(1), Unit(0), Unit(2), Unit(1), Unit(1) }, 900)); var pos = new Vector2[] { new(100, 90), new(125, 90), new(88, 80), new(115, 70), new(130, 100), new(122, 82) }; for (int i = 0; i < pos.Length; i++) { sim.SetManual(i); sim.Place(i, pos[i]); } sim.Fighters[0].SpecialCooldown = 0; return sim; }
        int peakAll = 0;
        foreach (var id in Abilities.All.Keys)
        {
            var plain = Create(id); var shown = Create(id); var fxA = new FxDirector(shown); var fxB = new FxDirector(Create(id), null);
            var simB = fxB.GetType() == typeof(FxDirector) ? (BattleSimulation)typeof(FxDirector).GetField("sim", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(fxB)! : plain;
            plain.CommandSpecial(0); shown.CommandSpecial(0); simB.CommandSpecial(0);
            int idleTele = 0, teleMax = 0, relMax = 0, impMax = 0, resMax = 0, lastBusy = -1, maxDraw = 0; bool det = true; var colorsUnion = new HashSet<int>();
            for (int t = 1; t <= 150; t++)
            {
                plain.Step(); shown.Step(); simB.Step(); fxA.Advance(); fxB.Advance(); fxA.Collect(); fxB.Collect();
                int n = fxA.Ground.Count + fxA.Over.Count + fxA.Top.Count; maxDraw = Math.Max(maxDraw, n);
                if (!Same(fxA.Ground, fxB.Ground) || !Same(fxA.Over, fxB.Over) || !Same(fxA.Top, fxB.Top)) det = false;
                if (t <= 12) teleMax = Math.Max(teleMax, n); else if (t <= 16) relMax = Math.Max(relMax, n); else if (t <= 40) impMax = Math.Max(impMax, n); else if (t <= 120) resMax = Math.Max(resMax, n);
                if (fxA.Active > 0) lastBusy = t; if (t == 1) idleTele = n;
                foreach (var d in fxA.Ground.Concat(fxA.Over).Concat(fxA.Top)) { var sp = atlas[d.Sprite]; for (int y = 0; y < sp.H; y++) for (int x = 0; x < sp.W; x++) { int o = ((sp.Y + y) * img.Width + sp.X + d.Frame * sp.W + x) * 4; if (img.Pixels[o + 3] == 255) colorsUnion.Add(img.Pixels[o] << 16 | img.Pixels[o + 1] << 8 | img.Pixels[o + 2]); } }
            }
            Check(det, $"{id}: determinismo (duas execuções = mesmas listas)");
            Check(plain.Events.SequenceEqual(shown.Events), $"{id}: simulação idêntica com a camada de efeitos");
            Check(teleMax > 0, $"{id}: telégrafo durante a conjuração"); Check(relMax > 0, $"{id}: liberação"); Check(impMax > 0, $"{id}: impacto"); Check(impMax >= 1, $"{id}: resíduo/impacto vivo");
            Check(fxA.PeakParticles <= FxDirector.MaxParticles, $"{id}: partículas ≤ {FxDirector.MaxParticles} ({fxA.PeakParticles})");
            Check(lastBusy < 150, $"{id}: fim limpo (último ativo no tick {lastBusy})");
            peakAll = Math.Max(peakAll, fxA.PeakParticles);
            info.Add($"{id,-14} tele={teleMax,3} lib={relMax,3} imp={impMax,3} resíduo={resMax,3} pico={maxDraw,3} partículas={fxA.PeakParticles,3} cores={colorsUnion.Count} fimLimpo@{lastBusy}");
        }
        // 5. Estresse: 40 unidades, todas lançando especiais; orçamento respeitado
        var specs = new List<FighterSpec>(); var ids = Abilities.All.Keys.Where(k => k != "rally").ToArray(); for (int i = 0; i < 40; i++) specs.Add(new(i % 2, "test", "test", Role.Melee, new(900, 12, 2, 30f, 22, 40), ids[i % ids.Length]));
        var big = new BattleSimulation(new(7, specs, 900));  var fxBig = new FxDirector(big); int worst = 0; for (int i = 0; i < 40; i++) big.Fighters[i].SpecialCooldown = 0;
        for (int t = 0; t < 600; t++) { big.Step(); fxBig.Advance(); if (t % 10 == 0) { fxBig.Collect(); worst = Math.Max(worst, fxBig.Ground.Count + fxBig.Over.Count + fxBig.Top.Count); } }
        info.Add($"estresse: tick={big.Tick} ev={big.Events.Count} casts={big.Events.Count(e => e.Kind.StartsWith("cast"))} alive={big.Fighters.Count(f => f.Health > 0)}");
        Check(fxBig.PeakParticles <= FxDirector.MaxParticles, $"estresse 40 unidades: partículas ≤ {FxDirector.MaxParticles}");
        info.Add($"estresse 40 unidades: pico de partículas={fxBig.PeakParticles}/{FxDirector.MaxParticles}, maior lista de desenho={worst}, pontos descartados={fxBig.DotsDropped}");
        return (checks, failures, info);
    }
    static bool Same(List<FxDraw> a, List<FxDraw> b) { if (a.Count != b.Count) return false; for (int i = 0; i < a.Count; i++) if (a[i].Sprite != b[i].Sprite || a[i].Frame != b[i].Frame || a[i].X != b[i].X || a[i].Y != b[i].Y || a[i].Flip != b[i].Flip) return false; return true; }
}
