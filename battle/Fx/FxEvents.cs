using System.Numerics;
using Verdant.Forge;
namespace Verdant.Battle;

public sealed partial class FxDirector
{
    readonly Dictionary<int, int> specialAt = new();   // fighter -> tick em que o último especial foi liberado
    readonly Dictionary<int, FxTheme> hitTheme = new(); // fighter -> tema visual do especial liberado

    static readonly FxTheme[] ShardThemes = { FxTheme.Earth, FxTheme.Ice, FxTheme.Steel, FxTheme.Plant, FxTheme.Fire, FxTheme.Web };
    static FxTheme Shard(FxTheme t) => ShardThemes.Contains(t) ? t : t == FxTheme.Water ? FxTheme.Ice : t == FxTheme.Damage || t == FxTheme.Blood ? FxTheme.Fire : FxTheme.Steel;
    static FxTheme GlyphTheme(FxTheme t, bool large) => large ? (t is FxTheme.Heal or FxTheme.Shield or FxTheme.Light ? t : FxTheme.Damage) : (t is FxTheme.Heal or FxTheme.Shield or FxTheme.Poison or FxTheme.Fire or FxTheme.Blood or FxTheme.Light ? t : FxTheme.Damage);

    /// <summary>Tema do especial: define a paleta; o elemento do monstro tinge os detalhes.</summary>
    public FxTheme ThemeOf(string id, Fighter f)
    {
        var element = FxPalette.ForElement(f.Spec.ElementEffect);
        return id switch
        {
            "ice-wall" => FxTheme.Ice, "lightning-cut" => FxTheme.Light, "arrow-rain" => FxTheme.Steel, "nova" => FxTheme.Ice, "quake" => FxTheme.Earth,
            "dagger-dance" => FxTheme.Steel, "blessing" => FxTheme.Light, "acid" => FxTheme.Acid, "roar" => FxTheme.Damage,
            "breath" => element == FxTheme.Steel || element == FxTheme.Plant ? FxTheme.Fire : element, "web" => FxTheme.Web, "dive" => FxTheme.Steel,
            "constrict" => FxTheme.Plant, "water-jet" => FxTheme.Water, "roots" => FxTheme.Plant, _ => FxTheme.Shadow
        };
    }
    FxTheme ElementOf(Fighter f) { var t = FxPalette.ForElement(f.Spec.ElementEffect); return t; }

    void Shake(int amp, int ticks = 6) { ShakeAmp = Math.Max(ShakeAmp, amp); ShakeLeft = Math.Max(ShakeLeft, ticks); }

    void OnEvent(BattleEvent e, int idx)
    {
        string k = e.Kind;
        if (k.StartsWith("cast:")) Cast(e, idx, k[5..]);
        else if (k.StartsWith("effect:")) { specialAt[e.Source] = e.Tick; Release(e, idx, k[7..]); }
        else if (k == "damage") Damage(e, idx);
        else if (k == "heal") Healed(e, idx);
        else if (k.StartsWith("status:")) Status(e, idx, Enum.Parse<EffectKind>(k[7..]));
        else if (k == "death") Death(e, idx);
        else if (k == "interrupt") Interrupt(e, idx);
        else if (k == "summon") SummonFx(e, idx);
        else if (k == "phase") PhaseFx(e, idx);
    }

    // ------------------------------------------------------------------------------------------------------------------
    //  Impacto e números
    // ------------------------------------------------------------------------------------------------------------------
    void Damage(BattleEvent e, int idx)
    {
        var r = RngFor(idx); var src = sim.Fighters[e.Source]; var tgt = sim.Fighters[e.Target]; int size = sizeOf(e.Target);
        float x = e.X, y = e.Y - size * .4f; bool special = specialAt.TryGetValue(e.Source, out var t) && t == e.Tick;
        bool dot = !special && e.Tick % 30 == 0 && DotOf(tgt, e.Source, out _);
        if (special)
        {
            var th = hitTheme.GetValueOrDefault(e.Source, FxTheme.Damage);
            Spawn("spark", FxTheme.Light, x, y, 0, 0, 9, prio: PrioKey);
            Spawn("spark", th, x, y - 2, 0, 0, 8, delay: 1, prio: PrioImpact);
            int n = 5; for (int i = 0; i < n; i++) { float a = i * MathF.Tau / n + r.Float() * .8f; float sp = 1.1f + r.Float() * 1.2f; Spawn("shard", Shard(th), x, y, MathF.Cos(a) * sp, MathF.Sin(a) * sp - .8f, 12 + r.Int(0, 5), gy: .16f, prio: PrioImpact, speed: 2, loop: true); }
            insts.Add(new Inst { Kind = "ring", Start = lastTick, Dur = 8, X = e.X, Y = e.Y, R = 11, Theme = th });
            HitStop = Math.Max(HitStop, 2); Shake(1, 4);
            Number(e, idx, -(r.Int(0, 5)), e.Amount.ToString(), FxTheme.Damage, true);
        }
        else if (dot)
        {
            DotOf(tgt, e.Source, out var kind); var th = FxPalette.ForStatus(kind);
            Spawn("spark", th, x, y, 0, -.3f, 8, prio: PrioDecor); Number(e, idx, r.Int(-4, 5), e.Amount.ToString(), th == FxTheme.Blood ? FxTheme.Blood : th == FxTheme.Fire ? FxTheme.Fire : FxTheme.Poison, false);
        }
        else
        {
            bool left = tgt.Position.X < src.Position.X; var role = src.Spec.Role;
            if (role == Role.Archer) { Spawn("spark", FxTheme.Steel, x, y, 0, 0, 8, prio: PrioImpact); Spawn("spark", FxTheme.Earth, x, y + 6, 0, 0, 6, delay: 1, prio: PrioDecor); }
            else if (role is Role.Mage or Role.Healer) { Spawn("star4", FxTheme.Light, x, y, 0, 0, 9); Spawn("spark", FxTheme.Ice, x + 3, y - 2, 0, 0, 7, delay: 1, prio: PrioDecor); }
            else if (role == Role.Spitter) { Spawn("drop", FxTheme.Water, x, y, r.Float() - .5f, -1f, 9, gy: .15f); Spawn("spark", FxTheme.Water, x, y, 0, 0, 7); }
            else { Spawn("slash", role == Role.Charger ? FxTheme.Damage : FxTheme.Steel, x + (left ? -2 : 2), y, 0, 0, 7, flip: left); Spawn("spark", FxTheme.Light, x, y, 0, 0, 6, delay: 1, prio: PrioDecor); }
            Number(e, idx, r.Int(-3, 4), e.Amount.ToString(), FxTheme.Damage, false);
        }
    }
    bool DotOf(Fighter target, int source, out EffectKind kind)
    {
        foreach (var k in new[] { EffectKind.Burn, EffectKind.Poison, EffectKind.Bleed }) if (target.Statuses.TryGetValue(k, out var s) && s.Source == source) { kind = k; return true; }
        kind = EffectKind.Poison; return false;
    }
    void Number(BattleEvent e, int idx, int jitter, string text, FxTheme theme, bool large)
    {
        int size = sizeOf(e.Target);
        insts.Add(new Inst { Kind = "num", Start = lastTick, Dur = large ? 30 : 24, X = e.X + jitter, Y = e.Y - size - 2, Theme = GlyphTheme(theme, large), Large = large, Value = int.Parse(text), Aux = text.StartsWith("+") ? 1 : 0 });
    }
    void Healed(BattleEvent e, int idx)
    {
        var r = RngFor(idx); int size = sizeOf(e.Target); float x = e.X, y = e.Y - size * .45f;
        for (int i = 0; i < 3; i++) Spawn("plus", FxTheme.Heal, x + r.Int(-7, 8), y + r.Int(-2, 7), 0, -.45f - r.Float() * .25f, 18, delay: i * 2, prio: PrioImpact);
        Spawn("star4", FxTheme.Light, x, y - 4, 0, 0, 9, delay: 1, prio: PrioDecor);
        Number(e, idx, 0, "+" + e.Amount, FxTheme.Heal, specialAt.TryGetValue(e.Source, out var t) && t == e.Tick);
        insts[^1].Aux = 1;
    }
    void Status(BattleEvent e, int idx, EffectKind kind)
    {
        var r = RngFor(idx); var th = FxPalette.ForStatus(kind); int size = sizeOf(e.Target); float x = e.X, y = e.Y - size * .5f;
        Spawn("spark", th, x, y, 0, 0, 9, prio: PrioKey); Spawn("star4", th == FxTheme.Steel ? FxTheme.Light : (Has("star4", th) ? th : FxTheme.Light), x, y - size * .35f, 0, -.2f, 12, delay: 1, prio: PrioImpact);
        for (int i = 0; i < 3; i++) Spawn("px", th, x + r.Int(-6, 7), y + r.Int(-4, 6), 0, -.5f, 8, delay: i, prio: PrioDecor);
    }
    bool Has(string n, FxTheme t) => atlas.Has(n, t);
    void Death(BattleEvent e, int idx)
    {
        var r = RngFor(idx); int size = sizeOf(e.Target); float x = e.X, y = e.Y - size * .3f;
        Spawn("puff", FxTheme.Steel, x, y, 0, -.1f, 12, prio: PrioImpact); Spawn("puff", FxTheme.Earth, x + 4, y + 4, .1f, 0, 12, delay: 2, layer: FxLayer.Ground, prio: PrioDecor);
        for (int i = 0; i < 5; i++) Spawn("spark", FxTheme.Light, x + r.Int(-8, 9), y + r.Int(-8, 5), 0, -.55f - r.Float() * .3f, 14, delay: i, prio: PrioDecor);
    }
    void Interrupt(BattleEvent e, int idx)
    {
        int size = sizeOf(e.Target); float x = e.X, y = e.Y - size * .8f;
        Spawn("spark", FxTheme.Damage, x, y, 0, 0, 10, prio: PrioKey); for (int i = 0; i < 4; i++) { float a = i * MathF.PI / 2 + .6f; Spawn("star4", FxTheme.Stun, x, y, MathF.Cos(a) * 1.4f, MathF.Sin(a) * 1.2f, 10, prio: PrioImpact); }
        Shake(1, 3);
    }
    void SummonFx(BattleEvent e, int idx)
    {
        var r = RngFor(idx); insts.Add(new Inst { Kind = "ring", Start = lastTick, Dur = 14, X = e.X, Y = e.Y, R = 16, Theme = FxTheme.Shadow });
        for (int i = 0; i < 8; i++) { float a = i * MathF.Tau / 8; Spawn("smoke", FxTheme.Shadow, e.X + MathF.Cos(a) * 4, e.Y - 4 + MathF.Sin(a) * 2, MathF.Cos(a) * .6f, -.5f, 14, delay: i / 2, prio: PrioImpact); }
        Spawn("spark", FxTheme.Light, e.X, e.Y - 10, 0, 0, 10, prio: PrioKey);
    }
    void PhaseFx(BattleEvent e, int idx)
    {
        var r = RngFor(idx); int size = sizeOf(e.Source);
        insts.Add(new Inst { Kind = "ring", Start = lastTick, Dur = 16, X = e.X, Y = e.Y, R = 34, Theme = FxTheme.Damage });
        insts.Add(new Inst { Kind = "ring", Start = lastTick + 3, Dur = 16, X = e.X, Y = e.Y, R = 24, Theme = FxTheme.Fire });
        for (int i = 0; i < 10; i++) Spawn("flame-s", FxTheme.Fire, e.X + r.Int(-12, 13), e.Y - 4 + r.Int(-3, 4), 0, -.8f - r.Float() * .5f, 16, delay: i % 5, loop: true, prio: PrioImpact);
        Spawn("spark", FxTheme.Light, e.X, e.Y - size * .5f, 0, 0, 10, prio: PrioKey); Shake(2, 8); HitStop = Math.Max(HitStop, 3);
    }

    // ------------------------------------------------------------------------------------------------------------------
    //  Sobreposições contínuas por unidade (estado lido da simulação a cada quadro) e projéteis
    // ------------------------------------------------------------------------------------------------------------------
    void DrawProjectiles()
    {
        foreach (var s in sim.Projectiles)
        {
            var role = sim.Fighters[s.Source].Spec.Role; var v = s.Velocity; var d = v.LengthSquared() < .01f ? Vector2.UnitX : Vector2.Normalize(v); float x = s.Position.X, y = s.Position.Y - 8;
            if (role == Role.Archer)
            {   // flecha: ponta clara, haste, pena; 2 px de espessura na ponta
                for (int i = 0; i < 7; i++) Dot(FxLayer.Over, FxTheme.Steel, i < 2 ? 3 : i < 5 ? 1 : 2, x - d.X * i, y - d.Y * i);
                Dot(FxLayer.Over, FxTheme.Steel, 3, x + d.X, y + d.Y); Dot(FxLayer.Over, FxTheme.Steel, 0, x - d.X * 6 + 0, y - d.Y * 6 + 1); Dot(FxLayer.Over, FxTheme.Steel, 0, x - d.X * 6, y - d.Y * 6 - 1);
            }
            else if (role is Role.Mage or Role.Healer)
            {
                Spr(FxLayer.Over, Id("star4", FxTheme.Light), (lastTick / 2) % 2, x, y);
                for (int i = 1; i < 5; i++) Dot(FxLayer.Over, FxTheme.Ice, i < 3 ? 2 : 1, x - d.X * (i * 2 + 2), y - d.Y * (i * 2 + 2));
            }
            else { Spr(FxLayer.Over, Id("bubble", FxTheme.Water), 1 + (lastTick / 3) % 2, x, y); for (int i = 1; i < 4; i++) Dot(FxLayer.Over, FxTheme.Water, 1, x - d.X * (i * 2 + 2), y - d.Y * (i * 2 + 2)); }
        }
    }

    void DrawUnitOverlays()
    {
        int t = lastTick;
        foreach (var f in sim.Fighters)
        {
            if (!f.Alive || f.Retreated) continue; int size = sizeOf(f.Id); float px = f.Position.X, py = f.Position.Y, body = py - size * .42f, headY = py - size + 3;
            // Escudo: bolha facetada ao redor do corpo, pisca quando está acabando.
            if (f.Shield > 0 && (f.ShieldUntil - t > 24 || t % 6 < 4)) Spr(FxLayer.Over, Id("shield-bubble", FxTheme.Shield), (t / 5 + f.Id) % 2, px, body);
            int shown = 0; var icons = new List<EffectKind>(3);
            foreach (var kind in StatusIcons.Priority)
            {
                bool on = kind == EffectKind.Shield ? f.Shield > 0 : f.Statuses.ContainsKey(kind); if (!on) continue;
                if (icons.Count < 3) icons.Add(kind);
                if (kind == EffectKind.Shield) continue; BodyLayer(f, kind, px, py, body, headY, size, t); shown++;
            }
            // Ícones acima da barra de vida; no máximo 3, na ordem de prioridade.
            float ix = px - (icons.Count * 10 - 1) / 2f + 4.5f; float iy = headY - 9;
            foreach (var kind in icons) { Spr(FxLayer.Top, Id("st-" + StatusIcons.Name(kind), FxPalette.ForStatus(kind)), 0, ix, iy + (kind == EffectKind.Stun || kind == EffectKind.Taunt ? (t / 4 % 2) : 0)); ix += 10; }
        }
    }

    void BodyLayer(Fighter f, EffectKind kind, float px, float py, float body, float headY, int size, int t)
    {
        int id = f.Id;
        switch (kind)
        {
            case EffectKind.Burn:
                Spr(FxLayer.Over, Id("flame-s", FxTheme.Fire), (t / 3 + id) % 4, px - 7, body + 2); Spr(FxLayer.Over, Id("flame-s", FxTheme.Fire), (t / 3 + id + 2) % 4, px + 7, body - 3);
                break;
            case EffectKind.Poison:
                for (int i = 0; i < 2; i++) { int ph = (t + id * 5 + i * 11) % 22; Spr(FxLayer.Over, Id("bubble", FxTheme.Poison), Math.Min(3, ph / 6), px + (i == 0 ? -6 : 6) + (ph % 4 < 2 ? 0 : 1), body + 6 - ph * .9f); }
                break;
            case EffectKind.Bleed:
                for (int i = 0; i < 2; i++) { int ph = (t + id * 3 + i * 9) % 18; Spr(FxLayer.Over, Id("drop", FxTheme.Blood), ph < 12 ? 0 : 2, px + (i == 0 ? -4 : 5), body - 2 + ph * .8f); }
                break;
            case EffectKind.Slow:
                Spr(FxLayer.Over, Id("crystal", FxTheme.Ice), (t / 6 + id) % 3, px - 9, py); Spr(FxLayer.Over, Id("crystal", FxTheme.Ice), (t / 6 + id + 1) % 3, px + 9, py + 1);
                if (t % 9 == id % 9) Spawn("snow", FxTheme.Ice, px + (t % 7) - 3, headY + 4, 0, .5f, 14, prio: PrioDecor);
                break;
            case EffectKind.Root:
                Spr(FxLayer.Ground, Id("vine-ring", FxTheme.Plant), (t / 8) % 2, px, py - 1); Spr(FxLayer.Over, Id("root", FxTheme.Plant), 4, px - 8, py + 1); Spr(FxLayer.Over, Id("root", FxTheme.Plant), 3, px + 8, py + 2, true);
                break;
            case EffectKind.Stun:
                for (int i = 0; i < 3; i++) { float a = t * .32f + i * MathF.Tau / 3; Spr(a % MathF.Tau < MathF.PI ? FxLayer.Over : FxLayer.Over, Id("star4", FxTheme.Stun), 1 + (t / 4 + i) % 2, px + MathF.Cos(a) * 9, headY + 3 + MathF.Sin(a) * 2.5f); }
                break;
            case EffectKind.Regen:
                if ((t + id * 4) % 14 == 0) Spawn("plus", FxTheme.Heal, px + ((t / 14 + id) % 3 - 1) * 6, body + 6, 0, -.5f, 16, prio: PrioDecor);
                break;
            case EffectKind.Weaken:
                { int ph = (t + id * 6) % 20; Spr(FxLayer.Over, Id("st-weaken", FxTheme.Shadow), 0, px + 8, headY + 2 + ph * .5f); }
                break;
            case EffectKind.Buff:
                { int ph = (t + id * 4) % 16; Spr(FxLayer.Over, Id("st-buff", FxTheme.Damage), 0, px - 8, body + 4 - ph * .9f); }
                break;
            case EffectKind.Drain:
                { int ph = (t + id * 3) % 24; Spr(FxLayer.Over, Id("smoke", FxTheme.Shadow), Math.Min(3, ph / 6), px + (ph % 8 < 4 ? -8 : 8), py - ph * .5f); }
                break;
            case EffectKind.Dodge:
                for (int i = 0; i < 3; i++) { int dx = 10 + i * 2 + (t % 4); LineDots(FxLayer.Over, FxTheme.Steel, px - dx, body - 6 + i * 5, px - dx - 4, body - 6 + i * 5, 2, 1); }
                break;
        }
    }
}
