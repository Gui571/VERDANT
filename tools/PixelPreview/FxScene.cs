using System.Numerics;
using Vector2 = System.Numerics.Vector2;
using Godot;
using Verdant;
using Verdant.Battle;
using Verdant.Forge;
namespace PixelPreview;

/// <summary>Cena de revisão: monta uma batalha pequena em torno de um especial e compõe cada tick em 320×150 (pixel exato),
/// do mesmo jeito que a arena do jogo. Serve para revisar efeitos e gerar as comparações "antes × depois" sem o Godot.</summary>
public sealed class FxScene
{
    public readonly BattleSimulation Sim;
    public readonly string AbilityId;
    readonly Dictionary<string, object> renderers = new();
    readonly Raster terrain;
    public int CasterId => 0;
    public int CastTick { get; private set; }

    static readonly string[] HeroOf = { "ice-wall", "lightning-cut", "arrow-rain", "nova", "quake", "dagger-dance", "blessing" };

    public static ActorRecipe CasterFor(string ability, EffectKind? elementOverride = null)
    {
        int hero = Array.IndexOf(HeroOf, ability);
        if (hero >= 0) return ActorRecipe.FromHero(hero);
        var family = Abilities.MonsterSignatures.First(p => p.Value == ability).Key;
        var fam = Enum.Parse<MonsterFamily>(family);
        var spec = MonsterGenerator.Generate(100 + (ulong)fam, fam) with { Rank = Rank.Minion };
        return ActorRecipe.FromMonster(100 + (ulong)fam, fam) with { Monster = spec };
    }

    public FxScene(string ability, Element? element = null, int castAt = 24)
    {
        AbilityId = ability; CastTick = castAt;
        var casterRecipe = CasterFor(ability);
        if (element != null && casterRecipe.Monster != null) casterRecipe = casterRecipe with { Monster = casterRecipe.Monster with { Element = element.Value } };
        var recipes = new List<(ActorRecipe r, int team)> { (casterRecipe, 0), (ActorRecipe.FromHero(6), 0), (ActorRecipe.FromHero(0), 0), (ActorRecipe.FromMonster(7, MonsterFamily.Beast), 1), (ActorRecipe.FromMonster(11, MonsterFamily.Golem), 1), (ActorRecipe.FromMonster(15, MonsterFamily.Arthropod), 1) };
        if (casterRecipe.Hero != null && casterRecipe.Hero.Id == "mira") recipes[1] = (ActorRecipe.FromHero(3), 0);
        var fighters = recipes.Select(x => { var f = x.r.Fighter(x.team); return f with { Stats = f.Stats with { Health = f.Stats.Health * 9, Attack = x.team == 1 ? 6 : f.Stats.Attack, Speed = 0.01f } }; }).ToArray();
        // O caster do elenco fica com a assinatura declarada; o de monstro herda a da família (e o elemento da receita).
        Sim = new BattleSimulation(new(77, fighters, 3000));
        Sim.SetManual(0); Sim.SetManual(1);
        // Distância do primeiro inimigo conforme o alcance do especial, para o conjurador poder lançá-lo.
        float d = Math.Clamp(Abilities.All[ability].Radius * .6f, 22, 50);
        Sim.Place(0, new(84, 84)); Sim.Place(1, new(64, 98)); Sim.Place(2, new(68, 70));
        Sim.Place(3, new(84 + d, 84)); Sim.Place(4, new(84 + d + 12, 84 - 20)); Sim.Place(5, new(84 + d + 10, 84 + 20));
        Sim.Fighters[0].SpecialCooldown = 0;
        for (int i = 0; i < Sim.Fighters.Length; i++) Recipes.Add(recipes[i].r);
        if (ability == "blessing") { Sim.Fighters[1].Health = Sim.Fighters[1].Spec.Stats.Health / 5; Sim.Fighters[2].Health = Sim.Fighters[2].Spec.Stats.Health / 3; }
        terrain = BuildTerrain();
    }
    public readonly List<ActorRecipe> Recipes = new();

    /// <summary>Avança um tick; no tick configurado o caster dispara o especial.</summary>
    public void Step()
    {
        if (Sim.Tick == CastTick) Sim.CommandSpecial(0);
        Sim.Step();
    }

    static Raster BuildTerrain()
    {
        var px = new Raster(320, 150); px.Rect(0, 0, 320, 150, new Color("82985a")); px.Rect(8, 8, 304, 136, new Color("92784c")); var r = new Rng(902);
        for (int i = 0; i < 700; i++) { int x = r.Int(0, 320), y = r.Int(0, 150); px.Rect(x, y, 2, 1, new Color(i % 3 == 0 ? "a58a5a" : "7b693f")); }
        for (int i = 0; i < 18; i++) { int x = r.Int(0, 320), y = i % 2 == 0 ? 3 : 145; px.Rect(x, y, 4, 2, new Color("555b58")); px.Rect(x, y, 3, 1, new Color("9da38b")); }
        return px;
    }

    object RendererOf(int id)
    {
        var r = Recipes[id]; string key = r.Key + ":" + id;
        if (!renderers.TryGetValue(key, out var o)) renderers[key] = o = r.Hero != null ? new CompactAdventurer(r.Hero) : new MonsterRenderer(r.Monster!);
        return o;
    }

    public (Raster sprite, int size, bool flip, Vector2 pos) SpriteOf(Fighter f)
    {
        int elapsed = Sim.Tick - f.DeathTick; MonsterMotion m; double t;
        if (!f.Alive) { m = MonsterMotion.Death; t = elapsed / 30.0; }
        else if (f.CastRemaining > 0) { m = MonsterMotion.Cast; t = (Sim.Tick - f.LastSpecial) / 30.0; }
        else if (Sim.Tick - f.LastSpecial < 30) { m = MonsterMotion.Special; t = (Sim.Tick - f.LastSpecial) / 30.0; }
        else if (Sim.Tick - f.LastHit < 12) { m = MonsterMotion.Hurt; t = (Sim.Tick - f.LastHit) / 30.0; }
        else if (Sim.Tick - f.LastAttack < 22) { m = MonsterMotion.Attack; t = (Sim.Tick - f.LastAttack) / 30.0; }
        else { m = f.Moving ? MonsterMotion.Walk : MonsterMotion.Idle; t = Sim.Tick / 30.0; }
        var rr = RendererOf(f.Id); Raster px; int size;
        if (rr is MonsterRenderer mr) { px = mr.Render(Math.Max(0, t), m); size = Recipes[f.Id].Monster!.Size; }
        else
        {
            var hero = (CompactAdventurer)rr; size = 32;
            px = m == MonsterMotion.Death ? DeathFrame(hero, (int)(t * 8)) : hero.Render(Math.Max(0, t), m == MonsterMotion.Special ? AdventurerMotion.Special : m == MonsterMotion.Cast ? AdventurerMotion.Cast : (AdventurerMotion)m);
        }
        bool flip = f.Target >= 0 && Sim.Fighters[f.Target].Position.X < f.Position.X;
        return (px, size, flip, f.Position);
    }
    static Raster DeathFrame(CompactAdventurer hero, int f)
    {
        var original = hero.Render(0, AdventurerMotion.Hurt); var px = new Raster(32, 32);
        if (f < 5) for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) { int k = (y * 32 + x) * 4; if (original.Pixels[k + 3] != 0) px.Pixel(x, 29 - (29 - y) * (5 - f) / 5, new Color(original.Pixels[k] / 255f, original.Pixels[k + 1] / 255f, original.Pixels[k + 2] / 255f, 1)); }
        return px;
    }

    public static void Blit(Raster dst, Raster src, int ox, int oy, bool flip = false)
    {
        for (int y = 0; y < src.Height; y++) for (int x = 0; x < src.Width; x++)
        {
            int sx = flip ? src.Width - 1 - x : x; int i = (y * src.Width + sx) * 4; if (src.Pixels[i + 3] == 0) continue;
            int px = ox + x, py = oy + y; if (px < 0 || py < 0 || px >= dst.Width || py >= dst.Height) continue;
            int o = (py * dst.Width + px) * 4; dst.Pixels[o] = src.Pixels[i]; dst.Pixels[o + 1] = src.Pixels[i + 1]; dst.Pixels[o + 2] = src.Pixels[i + 2]; dst.Pixels[o + 3] = 255;
        }
    }

    /// <summary>Compõe o tick atual: terreno, unidades ordenadas por Y e as camadas de efeito dadas (chão antes, topo depois).</summary>
    public Raster Compose(Action<Raster>? ground = null, Action<Raster>? over = null)
    {
        var frame = new Raster(320, 150); Array.Copy(terrain.Pixels, frame.Pixels, frame.Pixels.Length);
        ground?.Invoke(frame);
        foreach (var f in Sim.Fighters.OrderBy(f => f.Position.Y))
        {
            if (f.Retreated) continue; int elapsed = Sim.Tick - f.DeathTick; if (!f.Alive && elapsed >= 23) continue;
            var (px, size, flip, pos) = SpriteOf(f);
            Blit(frame, px, (int)MathF.Round(pos.X) - size / 2, (int)MathF.Round(pos.Y) - (size - 3), flip);
        }
        over?.Invoke(frame);
        return frame;
    }

    // --- Efeitos antigos da arena (cópia fiel de Arena._Draw antes desta entrega) para as comparações "antes × depois". ---
    public void LegacyOverlay(Raster frame)
    {
        var energy = new Color("c1dceb"); var hit = new Color("fff0c7");
        for (int i = Sim.Events.Count - 1; i >= 0; i--)
        {
            var e = Sim.Events[i]; int age = Sim.Tick - e.Tick; if (age > 25) break; int ex = (int)MathF.Round(e.X), ey = (int)MathF.Round(e.Y);
            if (e.Kind == "damage" || e.Kind == "heal") { if (age < 4) { frame.Rect(ex - 2, ey - 10, 4, 1, hit); frame.Rect(ex, ey - 11, 1, 4, hit); } }
            if (e.Kind.StartsWith("effect:") && age < 12)
            {
                var ability = Sim.Ability(Sim.Fighters[e.Source]); var src = Sim.Fighters[e.Source].Position; var c = new Vector2(e.X, e.Y);
                var dir = c - src; if (dir.LengthSquared() < 1) dir = Vector2.UnitX; else dir = Vector2.Normalize(dir);
                float radius = ability.Radius * Math.Min(1, (age + 3) / 8f);
                for (int k = 0; k < 24; k++)
                {
                    float fr = k / 23f; Vector2 p;
                    if (ability.Shape == TargetShape.Line) p = src + dir * radius * fr;
                    else if (ability.Shape == TargetShape.Cone) { float a = MathF.Atan2(dir.Y, dir.X) + (-.6f + 1.2f * fr); p = src + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius; }
                    else { float a = k * MathF.Tau / 24; p = c + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius / 2); }
                    frame.Rect((int)MathF.Round(p.X), (int)MathF.Round(p.Y), 1, 1, energy);
                }
            }
        }
        foreach (var shot in Sim.Projectiles) frame.Rect((int)MathF.Round(shot.Position.X), (int)MathF.Round(shot.Position.Y) - 7, 2, 1, new Color("f0f0e5"));
    }
}
