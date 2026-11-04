using System.Numerics;
using Verdant.Forge;
namespace Verdant.Battle;

/// <summary>Cada especial é desenhado em quatro fases: telégrafo (durante a conjuração), liberação, impacto (eventos de dano) e resíduo.</summary>
public sealed partial class FxDirector
{
    const float ConeHalfAngle = 0.6107f;   // atan(0,7): exatamente o cone usado pela simulação
    static Vector2 Rot(Vector2 v, float a) { float c = MathF.Cos(a), s = MathF.Sin(a); return new(v.X * c - v.Y * s, v.X * s + v.Y * c); }
    static Vector2 Unit(Vector2 v) => v.LengthSquared() < .01f ? Vector2.UnitX : Vector2.Normalize(v);

    // --- fase 1: telégrafo --------------------------------------------------------------------------------------------
    void Cast(BattleEvent e, int idx, string id)
    {
        var f = sim.Fighters[e.Source]; var a = Abilities.All[id]; var th = ThemeOf(id, f); var r = RngFor(idx); int size = sizeOf(e.Source);
        insts.Add(new Inst { Kind = "tele", Start = lastTick, Dur = a.CastTicks + 1, Owner = e.Source, X = f.Position.X, Y = f.Position.Y, X2 = e.X, Y2 = e.Y, Ability = a, Theme = th, Seed = idx });
        insts.Add(new Inst { Kind = "charge", Start = lastTick, Dur = a.CastTicks + 1, Owner = e.Source, X = f.Position.X, Y = f.Position.Y, Theme = th, R = size * .42f });
        float bx = f.Position.X, by = f.Position.Y - size * .42f;
        for (int i = 0; i < 8; i++) { float ang = i * MathF.Tau / 8 + r.Float() * .5f; float d = 15; Spawn("star4", th == FxTheme.Steel ? FxTheme.Light : th, bx + MathF.Cos(ang) * d, by + MathF.Sin(ang) * d * .8f, -MathF.Cos(ang) * d / 9f, -MathF.Sin(ang) * d * .8f / 9f, 9, delay: i % 4, prio: PrioImpact); }
    }

    void DrawTele(Inst i, int age)
    {
        var a = i.Ability!; var th = i.Theme; float R = a.Radius; var src = new Vector2(i.X, i.Y); var tgt = new Vector2(i.X2, i.Y2); var dir = Unit(tgt - src); bool strong = age % 4 < 3;
        int ta = strong ? 3 : 2, tb = 1; float p = age / (float)Math.Max(1, a.CastTicks);
        switch (a.Shape)
        {
            case TargetShape.Circle:
                EllipseDots(FxLayer.Ground, th, tgt.X, tgt.Y, R, R, ta, tb, 3, 2, age);
                EllipseDots(FxLayer.Ground, th, tgt.X, tgt.Y, R * p, R * p, 1, 0, 2, 3, -age * 2);
                if (a.Id == "arrow-rain") ArrowShadows(i, tgt, R, age, true);
                if (a.Id == "nova") { for (int k = 0; k < 4; k++) { float an = k * MathF.PI / 2 + age * .15f; Dot2(FxLayer.Ground, FxTheme.Light, 3, tgt.X + MathF.Cos(an) * R * .55f - .5f, tgt.Y + MathF.Sin(an) * R * .55f - .5f); } }
                break;
            case TargetShape.Self: EllipseDots(FxLayer.Ground, th, src.X, src.Y, R, R, ta, tb, 3, 2, age); EllipseDots(FxLayer.Ground, th, src.X, src.Y, R * (1 - p * .25f), R * (1 - p * .25f), 1, 0, 2, 4, -age); break;
            case TargetShape.Allies:
                EllipseDots(FxLayer.Ground, th, src.X, src.Y, Math.Min(R, 70) * Math.Min(1, p * 1.4f), Math.Min(R, 70) * Math.Min(1, p * 1.4f), ta, tb, 4, 3, age);
                if (a.Id == "ice-wall") WallMarks(i, src, dir, age, true);
                break;
            case TargetShape.Cone:
                {
                    var e1 = src + Rot(dir, ConeHalfAngle) * R; var e2 = src + Rot(dir, -ConeHalfAngle) * R;
                    LineDots(FxLayer.Ground, th, src.X, src.Y, e1.X, e1.Y, ta, tb, 3, 2, age); LineDots(FxLayer.Ground, th, src.X, src.Y, e2.X, e2.Y, ta, tb, 3, 2, age);
                    for (int k = 0; k <= 14; k++) { var q = src + Rot(dir, -ConeHalfAngle + 2 * ConeHalfAngle * k / 14f) * R; if ((k + age) % 3 != 0) Dot(FxLayer.Ground, th, ta, q.X, q.Y); }
                    var mid = src + dir * R * p; for (int k = -5; k <= 5; k += 2) { var q = src + Rot(dir, k * .08f) * R * p; Dot(FxLayer.Ground, th, 1, q.X, q.Y); }
                }
                break;
            case TargetShape.Line:
                {
                    var n = new Vector2(-dir.Y, dir.X) * 10; var end = src + dir * R;
                    LineDots(FxLayer.Ground, th, src.X + n.X, src.Y + n.Y, end.X + n.X, end.Y + n.Y, ta, tb, 3, 2, age); LineDots(FxLayer.Ground, th, src.X - n.X, src.Y - n.Y, end.X - n.X, end.Y - n.Y, ta, tb, 3, 2, age);
                    LineDots(FxLayer.Ground, th, end.X + n.X, end.Y + n.Y, end.X - n.X, end.Y - n.Y, ta, tb, 2, 1, age);
                    var head = src + dir * R * p; Dot2(FxLayer.Ground, th, 3, head.X - .5f, head.Y - .5f);
                }
                break;
            case TargetShape.Single:
                {
                    LineDots(FxLayer.Ground, th, src.X, src.Y, tgt.X, tgt.Y, 1, 0, 2, 3, age);
                    float s = 7 - p * 2; foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) }) { float cx = tgt.X + sx * s, cy = tgt.Y + sy * s * .8f - 2; Dot(FxLayer.Ground, th, ta, cx, cy); Dot(FxLayer.Ground, th, ta, cx - sx, cy); Dot(FxLayer.Ground, th, ta, cx, cy - sy); }
                }
                break;
        }
    }
    void ArrowShadows(Inst i, Vector2 c, float R, int age, bool tele)
    {
        var r = new Rng((ulong)i.Seed * 7919UL + 13); for (int k = 0; k < 9; k++) { float an = r.Float() * MathF.Tau, d = R * MathF.Sqrt(r.Float()) * .92f; if (tele && age < k) continue; Dot2(FxLayer.Ground, FxTheme.Earth, 0, c.X + MathF.Cos(an) * d - .5f, c.Y + MathF.Sin(an) * d - .5f); Dot(FxLayer.Ground, FxTheme.Earth, 0, c.X + MathF.Cos(an) * d + 2, c.Y + MathF.Sin(an) * d); }
    }
    void WallMarks(Inst i, Vector2 src, Vector2 dir, int age, bool tele)
    {
        for (int k = -2; k <= 2; k++) { var q = src + Rot(dir, k * .42f) * 17; Dot(FxLayer.Ground, FxTheme.Ice, age % 4 < 2 ? 3 : 2, q.X, q.Y); Dot(FxLayer.Ground, FxTheme.Ice, 1, q.X + 1, q.Y); Dot(FxLayer.Ground, FxTheme.Ice, 1, q.X - 1, q.Y); }
    }

    // --- fase 2 e 4: liberação e resíduo --------------------------------------------------------------------------------
    List<int> HitTargets(int idx, int tick, int source, params string[] kinds)
    {
        var list = new List<int>(); for (int k = idx + 1; k < sim.Events.Count && sim.Events[k].Tick == tick; k++) { var ev = sim.Events[k]; if (ev.Source == source && kinds.Any(x => ev.Kind == x || ev.Kind.StartsWith(x)) && !list.Contains(ev.Target)) list.Add(ev.Target); }
        return list;
    }
    void Ring(float x, float y, float R, int dur, FxTheme th, int delay = 0, int flat = 100) => insts.Add(new Inst { Kind = "ring", Start = lastTick + delay, Dur = dur, X = x, Y = y, R = R, Theme = th, Aux = flat });

    void Release(BattleEvent e, int idx, string id)
    {
        var caster = sim.Fighters[e.Source]; var a = Abilities.All[id]; var th = ThemeOf(id, caster); hitTheme[e.Source] = th; var r = RngFor(idx); int size = sizeOf(e.Source);
        var now = caster.Position; var old = e.Source < lastPos.Length && lastPos[e.Source] != default ? lastPos[e.Source] : now; var c = new Vector2(e.X, e.Y);
        var tPos = sim.Fighters[Math.Clamp(e.Target, 0, sim.Fighters.Length - 1)].Position; var dir = Unit(tPos - old); var mouth = old + dir * 8 + new Vector2(0, -size * .35f);
        var hits = HitTargets(idx, e.Tick, e.Source, "damage", "status", "heal"); var el = ElementOf(caster);
        bool damaging = a.Effects.Any(x => x.Kind is EffectKind.Damage);
        switch (id)
        {
            case "ice-wall":
                insts.Add(new Inst { Kind = "pillars", Start = lastTick, Dur = 66, X = now.X, Y = now.Y, X2 = dir.X, Y2 = dir.Y, Seed = idx });
                Ring(now.X, now.Y, 32, 10, FxTheme.Ice); Ring(now.X, now.Y, 66, 14, FxTheme.Shield, 2);
                for (int k = 0; k < 10; k++) Spawn("shard", FxTheme.Ice, now.X + dir.X * 17 + r.Int(-14, 15), now.Y + dir.Y * 17 + r.Int(-5, 6), r.Float() - .5f, -1.4f - r.Float(), 12, delay: k / 3, gy: .18f, loop: true, speed: 2);
                foreach (var f in sim.Fighters) if (f.Alive && f.Spec.Team == caster.Spec.Team && Vector2.Distance(f.Position, now) <= a.Radius) { Spawn("star4", FxTheme.Shield, f.Position.X, f.Position.Y - sizeOf(f.Id) * .5f, 0, -.3f, 12, delay: 2, prio: PrioImpact); Spawn("spark", FxTheme.Shield, f.Position.X, f.Position.Y - sizeOf(f.Id) * .5f, 0, 0, 9, delay: 3); }
                Shake(1, 4); break;
            case "lightning-cut":
                insts.Add(new Inst { Kind = "streak", Start = lastTick, Dur = 14, X = old.X, Y = old.Y - size * .35f, X2 = now.X, Y2 = now.Y - size * .35f, Theme = FxTheme.Light });
                for (int k = 0; k < 6; k++) { var q = Vector2.Lerp(old, now, k / 5f); Spawn("puff", FxTheme.Earth, q.X, q.Y - 1, -dir.X * .2f, -.15f, 10, delay: k, layer: FxLayer.Ground, prio: PrioDecor); }
                Spawn("slash", FxTheme.Light, tPos.X - 3, tPos.Y - size * .45f, 0, 0, 8, delay: 1); Spawn("slash", FxTheme.Steel, tPos.X + 3, tPos.Y - size * .45f - 2, 0, 0, 8, delay: 3, flip: true);
                Spawn("spark", FxTheme.Light, now.X + dir.X * 5, now.Y - size * .4f, 0, 0, 8, prio: PrioKey); Shake(1, 3); break;
            case "arrow-rain":
                insts.Add(new Inst { Kind = "arrows", Start = lastTick, Dur = 56, X = c.X, Y = c.Y, R = a.Radius, Seed = idx });
                for (int k = 0; k < 9; k++) Spawn("puff", FxTheme.Earth, c.X + r.Int(-(int)a.Radius / 2, (int)a.Radius / 2 + 1), c.Y + r.Int(-(int)a.Radius / 2, (int)a.Radius / 2 + 1), 0, -.12f, 10, delay: 5 + k, layer: FxLayer.Ground, prio: PrioDecor);
                break;
            case "nova":
                Ring(c.X, c.Y, a.Radius, 9, FxTheme.Ice); Ring(c.X, c.Y, a.Radius * .72f, 9, FxTheme.Light, 2);
                insts.Add(new Inst { Kind = "glow", Start = lastTick, Dur = 12, X = c.X, Y = c.Y, R = a.Radius, Theme = FxTheme.Ice });
                Spawn("star4", FxTheme.Light, c.X, c.Y - 6, 0, 0, 12, prio: PrioKey); Spawn("spark", FxTheme.Light, c.X, c.Y - 6, 0, 0, 10, prio: PrioKey);
                for (int k = 0; k < 10; k++) { float an = k * MathF.Tau / 10; Spawn("star4", k % 2 == 0 ? FxTheme.Light : FxTheme.Ice, c.X, c.Y - 4, MathF.Cos(an) * 2.3f, MathF.Sin(an) * 2.3f * .8f, 12, drag: .06f, prio: PrioImpact); }
                for (int k = 0; k < 8; k++) Spawn("snow", FxTheme.Ice, c.X + r.Int(-(int)a.Radius, (int)a.Radius + 1) * .8f, c.Y - 26 + r.Int(0, 8), 0, .55f, 20, delay: 3 + k, prio: PrioDecor);
                Shake(1, 5); break;
            case "quake":
                Ring(now.X, now.Y, a.Radius, 10, FxTheme.Earth); Ring(now.X, now.Y, a.Radius * .65f, 9, FxTheme.Light, 2);
                insts.Add(new Inst { Kind = "cracks", Start = lastTick, Dur = 50, X = now.X, Y = now.Y, R = a.Radius, Seed = idx });
                for (int k = 0; k < 8; k++) { float an = k * MathF.Tau / 8 + .2f; Spawn("puff", FxTheme.Earth, now.X + MathF.Cos(an) * 10, now.Y + MathF.Sin(an) * 6, MathF.Cos(an) * .7f, MathF.Sin(an) * .25f - .25f, 13, delay: k % 3, prio: PrioImpact); }
                for (int k = 0; k < 9; k++) { float an = r.Float() * MathF.Tau; Spawn("shard", FxTheme.Earth, now.X + MathF.Cos(an) * 8, now.Y - 2, MathF.Cos(an) * 1.3f, -2.4f - r.Float() * 1.2f, 18, gy: .24f, loop: true, speed: 2); }
                Shake(2, 7); HitStop = Math.Max(HitStop, 2); break;
            case "dagger-dance":
                for (int k = 0; k < 3; k++) { float ox = (k == 1 ? 1 : -1) * 6, oy = -size * .45f + (k - 1) * 5; Spawn("slash", k == 1 ? FxTheme.Light : FxTheme.Steel, now.X + ox, now.Y + oy, 0, 0, 7, delay: k * 3, flip: k == 1); Spawn("spark", FxTheme.Light, now.X + ox, now.Y + oy, 0, 0, 7, delay: k * 3 + 1, prio: PrioDecor); }
                insts.Add(new Inst { Kind = "arcs", Start = lastTick, Dur = 16, X = now.X, Y = now.Y - size * .4f, R = a.Radius, Theme = FxTheme.Light });
                Ring(now.X, now.Y, a.Radius, 9, FxTheme.Steel, 0); break;
            case "blessing":
                insts.Add(new Inst { Kind = "column", Start = lastTick, Dur = 18, X = now.X, Y = now.Y, Theme = FxTheme.Light }); insts.Add(new Inst { Kind = "glow", Start = lastTick, Dur = 16, X = now.X, Y = now.Y, R = 16, Theme = FxTheme.Light });
                Ring(now.X, now.Y, Math.Min(70, a.Radius), 14, FxTheme.Heal); Ring(now.X, now.Y, Math.Min(70, a.Radius) * .6f, 12, FxTheme.Light, 2);
                foreach (var f in sim.Fighters) if (f.Alive && f.Spec.Team == caster.Spec.Team && Vector2.Distance(f.Position, now) <= a.Radius) { int sz = sizeOf(f.Id); Spawn("star4", FxTheme.Light, f.Position.X, f.Position.Y - sz * .5f, 0, -.35f, 14, delay: 4, prio: PrioImpact); for (int k = 0; k < 3; k++) Spawn("plus", FxTheme.Heal, f.Position.X + (k - 1) * 6, f.Position.Y - sz * .3f, 0, -.5f, 16, delay: 3 + k * 2, prio: PrioDecor); }
                for (int k = 0; k < 8; k++) Spawn("star4", FxTheme.Light, now.X + r.Int(-8, 9), now.Y - 4 - r.Int(0, 12), 0, -.8f - r.Float() * .4f, 16, delay: k, prio: PrioDecor);
                break;
            case "acid":
                {
                    var end = old + dir * a.Radius; insts.Add(new Inst { Kind = "acidshot", Start = lastTick, Dur = 14, X = mouth.X, Y = mouth.Y, X2 = end.X, Y2 = end.Y - 4, Seed = idx });
                    foreach (var t in hits.Where(h => h != e.Source).Take(3)) { var tp = sim.Fighters[t].Position; insts.Add(new Inst { Kind = "puddle", Start = lastTick + 3, Dur = 100, X = tp.X, Y = tp.Y + 2, R = 12, Theme = FxTheme.Acid, Seed = idx + t }); }
                    insts.Add(new Inst { Kind = "puddle", Start = lastTick + 7, Dur = 100, X = end.X, Y = end.Y, R = 24, Theme = FxTheme.Acid, Seed = idx }); break;
                }
            case "roar":
                insts.Add(new Inst { Kind = "wave", Start = lastTick, Dur = 24, X = old.X, Y = old.Y - size * .3f, X2 = dir.X, Y2 = dir.Y, R = a.Radius, Theme = FxTheme.Damage });
                for (int k = 0; k < 6; k++) { var q = Vector2.Lerp(old, now, k / 5f); Spawn("puff", FxTheme.Earth, q.X, q.Y - 1, -dir.X * .25f, -.1f, 10, delay: k, layer: FxLayer.Ground, prio: PrioDecor); }
                Spawn("spark", FxTheme.Light, mouth.X + dir.X * 4, mouth.Y, 0, 0, 9, prio: PrioKey); Shake(2, 6); break;
            case "breath":
                insts.Add(new Inst { Kind = "stream", Start = lastTick, Dur = 26, X = mouth.X, Y = mouth.Y, X2 = dir.X, Y2 = dir.Y, R = a.Radius, Theme = th, Seed = idx });
                insts.Add(new Inst { Kind = "patches", Start = lastTick + 8, Dur = 70, X = old.X + dir.X * a.Radius * .8f, Y = old.Y + dir.Y * a.Radius * .8f, X2 = dir.X, Y2 = dir.Y, R = a.Radius, Theme = th, Seed = idx });
                Shake(2, 10); break;
            case "web":
                {
                    insts.Add(new Inst { Kind = "web", Start = lastTick, Dur = 64, X = c.X, Y = c.Y, R = a.Radius, Theme = FxTheme.Web, Seed = idx });
                    insts.Add(new Inst { Kind = "strand", Start = lastTick, Dur = 8, X = mouth.X, Y = mouth.Y, X2 = c.X, Y2 = c.Y - 6, Theme = FxTheme.Web });
                    foreach (var t in hits.Take(4)) { var tp = sim.Fighters[t].Position; Spawn("spark", FxTheme.Web, tp.X, tp.Y - sizeOf(t) * .45f, 0, 0, 9, delay: 3); }
                    break;
                }
            case "dive":
                for (int k = 0; k < 7; k++) { var q = Vector2.Lerp(old, now, k / 6f); Spawn("shard", FxTheme.Steel, q.X + r.Int(-2, 3), q.Y - size * .4f + r.Int(-4, 5), -dir.X * .5f, r.Float() * .6f, 12, delay: k / 2, gy: .06f, loop: true, speed: 2, prio: PrioDecor); }
                insts.Add(new Inst { Kind = "streak", Start = lastTick, Dur = 10, X = old.X, Y = old.Y - size * .5f, X2 = now.X, Y2 = now.Y - size * .5f, Theme = FxTheme.Steel });
                Ring(tPos.X, tPos.Y, 14, 8, FxTheme.Earth, 1, 60); Spawn("puff", FxTheme.Earth, tPos.X, tPos.Y - 2, 0, -.1f, 12, delay: 1, layer: FxLayer.Ground);
                for (int k = 0; k < 6; k++) Spawn("spark", FxTheme.Steel, tPos.X + r.Int(-9, 10), tPos.Y - size * .4f + r.Int(-6, 6), 0, 0, 7, delay: 1 + k % 3, prio: PrioDecor); Shake(1, 4); break;
            case "constrict":
                insts.Add(new Inst { Kind = "coil", Start = lastTick, Dur = 52, X = tPos.X, Y = tPos.Y, Owner = e.Target, Theme = FxTheme.Plant });
                for (int k = 0; k < 5; k++) Spawn("bubble", FxTheme.Poison, tPos.X + r.Int(-8, 9), tPos.Y - r.Int(0, 12), 0, -.4f, 14, delay: 8 + k * 2, prio: PrioDecor);
                Spawn("spark", FxTheme.Poison, tPos.X, tPos.Y - sizeOf(e.Target) * .4f, 0, 0, 9, delay: 9, prio: PrioKey); Shake(1, 3); break;
            case "water-jet":
                {
                    var end = old + dir * a.Radius; insts.Add(new Inst { Kind = "jet", Start = lastTick, Dur = 14, X = mouth.X, Y = mouth.Y, X2 = end.X, Y2 = end.Y - 6, R = a.Radius, Theme = FxTheme.Water, Seed = idx });
                    foreach (var t in hits.Take(4)) { var tp = sim.Fighters[t].Position; for (int k = 0; k < 4; k++) Spawn("drop", FxTheme.Water, tp.X, tp.Y - sizeOf(t) * .4f, dir.X * 1.2f + r.Float() - .5f, -1f - r.Float(), 11, delay: 2 + k / 2, gy: .14f); Ring(tp.X, tp.Y, 12, 8, FxTheme.Water, 2, 60); }
                    break;
                }
            case "roots":
                insts.Add(new Inst { Kind = "rootfield", Start = lastTick, Dur = 64, X = now.X, Y = now.Y, R = a.Radius, Theme = FxTheme.Plant, Seed = idx });
                Ring(now.X, now.Y, a.Radius, 10, FxTheme.Plant);
                for (int k = 0; k < 8; k++) Spawn("leaf", FxTheme.Plant, now.X + r.Int(-18, 19), now.Y - r.Int(2, 12), r.Float() - .5f, -.7f - r.Float() * .5f, 16, delay: k % 4, gy: .05f, loop: true, speed: 3); break;
            default: // rally e demais: runa sombria
                Ring(now.X, now.Y, a.Radius + 6, 12, FxTheme.Shadow); Spawn("spark", FxTheme.Light, now.X, now.Y - 12, 0, 0, 10, prio: PrioKey); break;
        }
        if (damaging && el != FxTheme.Steel) Garnish(el, now, tPos, r);
    }
    void Garnish(FxTheme el, Vector2 from, Vector2 to, Rng r)
    {
        for (int k = 0; k < 4; k++)
        {
            float x = to.X + r.Int(-9, 10), y = to.Y - 8 - r.Int(0, 10);
            switch (el)
            {
                case FxTheme.Fire: Spawn("flame-s", FxTheme.Fire, x, y, 0, -.4f, 12, delay: 2 + k, loop: true, prio: PrioDecor); break;
                case FxTheme.Ice: Spawn("snow", FxTheme.Ice, x, y - 6, 0, .4f, 14, delay: 2 + k, prio: PrioDecor); break;
                case FxTheme.Plant: Spawn("leaf", FxTheme.Plant, x, y, r.Float() - .5f, -.4f, 14, delay: 2 + k, gy: .04f, loop: true, prio: PrioDecor); break;
                case FxTheme.Poison: Spawn("bubble", FxTheme.Poison, x, y, 0, -.35f, 14, delay: 2 + k, prio: PrioDecor); break;
                case FxTheme.Shadow: Spawn("smoke", FxTheme.Shadow, x, y, 0, -.25f, 14, delay: 2 + k, prio: PrioDecor); break;
            }
        }
    }

    // --- emissores contínuos (a cada tick, enquanto a instância vive) ---------------------------------------------------
    void OngoingEmitters()
    {
        foreach (var i in insts)
        {
            int age = lastTick - i.Start; if (age < 0) continue; var r = new Rng((ulong)i.Seed * 2654435761UL + (ulong)age * 40503UL + 1);
            if (i.Kind == "stream" && age < 18)
            {
                var dir = new Vector2(i.X2, i.Y2); string[] sizes = { "flame-s", "flame-m", "flame-l" };
                for (int k = 0; k < 3; k++)
                {
                    float spread = (r.Float() - .5f) * 2 * ConeHalfAngle * .8f; var d = Rot(dir, spread); float speed = 3.2f + r.Float() * 1.2f; int life = (int)(i.R / speed) + 2 + r.Int(0, 3);
                    Spawn("flame-s", i.Theme, i.X, i.Y, d.X * speed, d.Y * speed * .85f + .05f, life, loop: true, speed: 2, then: "flame-m", swap: life / 3, then2: "flame-l", swap2: life * 2 / 3, prio: PrioImpact);
                }
                if (age % 2 == 0) Spawn("smoke", i.Theme == FxTheme.Fire ? FxTheme.Shadow : FxTheme.Steel, i.X + dir.X * 20, i.Y + dir.Y * 20 - 4, dir.X * 1.4f, -.35f, 14, prio: PrioDecor);
            }
            if (i.Kind == "jet" && age < 10) { var dir = Unit(new Vector2(i.X2 - i.X, i.Y2 - i.Y)); float len = Vector2.Distance(new(i.X, i.Y), new(i.X2, i.Y2)); for (int k = 0; k < 2; k++) { float d = len * r.Float(); Spawn("drop", FxTheme.Water, i.X + dir.X * d, i.Y + dir.Y * d + r.Int(-2, 3), 0, -.7f - r.Float() * .6f, 9, gy: .12f, prio: PrioDecor); } }
            if (i.Kind == "puddle" && age > 0 && age % 9 == 0 && age < i.Dur - 14) Spawn("bubble", i.Theme == FxTheme.Acid ? FxTheme.Acid : FxTheme.Poison, i.X + r.Int(-(int)i.R / 2, (int)i.R / 2 + 1), i.Y - 1 + r.Int(-2, 3), 0, -.3f, 12, prio: PrioDecor);
            if (i.Kind == "patches" && age % 4 == 0 && age < i.Dur - 16) Spawn("flame-s", i.Theme, i.X + r.Int(-12, 13) + 12 * (r.Chance(.5f) ? 1 : -1) * 0, i.Y + r.Int(-10, 11), 0, -.1f, 14, loop: true, speed: 2, prio: PrioDecor);
        }
    }

    // --- desenho das instâncias ------------------------------------------------------------------------------------------
    void DrawInst(Inst i, int age)
    {
        if (age < 0) return; var th = i.Theme; float t = Math.Clamp(age / (float)Math.Max(1, i.Dur), 0, 1);
        switch (i.Kind)
        {
            case "tele": DrawTele(i, age); break;
            case "charge":
                { var f = sim.Fighters[i.Owner]; if (!f.Alive) break; EllipseDots(FxLayer.Ground, th == FxTheme.Steel ? FxTheme.Light : th, f.Position.X, f.Position.Y, 7 + (age % 6), (7 + (age % 6)) * .45f, 3, 1, 2, 1, age * 2); break; }
            case "ring":
                {
                    float k = 1 - (1 - t) * (1 - t); float rr = Math.Max(1, i.R * k); float flat = (i.Aux == 0 ? 100 : i.Aux) / 100f; bool fade = t > .6f;
                    EllipseDots(FxLayer.Ground, th, i.X, i.Y, rr, rr * flat, fade ? 1 : 3, fade ? 0 : 2, fade ? 2 : 6, fade ? 3 : 0, age, 1);
                    if (!fade && rr > 3) EllipseDots(FxLayer.Ground, th, i.X, i.Y, rr - 1.5f, (rr - 1.5f) * flat, 2, 1, 4, 2, age);
                    break;
                }
            case "glow":
                { float rr = i.R * (1 - (1 - t) * (1 - t)); for (int y = -(int)rr; y <= rr; y += 2) for (int x = -(int)rr; x <= rr; x += 2) { if (x * x + y * y > rr * rr) continue; if ((x / 2 + y / 2 + age) % 3 != 0) continue; if (t > .5f && (x / 2 + y / 2) % 2 == 0) continue; Dot(FxLayer.Ground, th, 1, i.X + x, i.Y + y); } break; }
            case "num":
                {
                    string s = (i.Aux == 1 ? "+" : "") + i.Value; var sp = Id(i.Large ? "glyph-l" : "glyph-s", th); var sd = atlas[sp]; int step = sd.W - 1; float x0 = i.X - (s.Length * step) / 2f + step / 2f; float rise = Math.Min(i.Large ? 11 : 8, age * (i.Large ? 1.2f : 1f));
                    if (t > .75f && age % 2 == 0) break; for (int k = 0; k < s.Length; k++) Spr(FxLayer.Top, sp, s[k] == '+' ? 10 : s[k] == '-' ? 11 : s[k] - '0', x0 + k * step, i.Y - rise - sd.H / 2f); break;
                }
            case "pillars":
                {
                    var dir = new Vector2(i.X2, i.Y2); var src = new Vector2(i.X, i.Y); var pil = Id("pillar", FxTheme.Ice);
                    for (int k = -2; k <= 2; k++)
                    {
                        var q = src + Rot(dir, k * .42f) * 17; int start = Math.Abs(k), local = age - start; if (local < 0) continue; int frame = local < 10 ? Math.Min(4, local / 2) : age > i.Dur - 10 ? Math.Max(0, 4 - (age - (i.Dur - 10)) / 2) : 4;
                        bool breaking = age > i.Dur - 6; if (breaking && age % 2 == 0) continue; Spr(FxLayer.Over, pil, frame, q.X, q.Y + 2);
                        if (age == i.Dur - 8 + Math.Abs(k) % 2) { }
                    }
                    if (age == i.Dur - 8) for (int k = -2; k <= 2; k++) { var q = src + Rot(dir, k * .42f) * 17; for (int j = 0; j < 3; j++) Spawn("shard", FxTheme.Ice, q.X, q.Y - 8, (j - 1) * .9f, -1f - j * .3f, 11, gy: .16f, loop: true, speed: 2); }
                    break;
                }
            case "arrows":
                {
                    var rr = new Rng((ulong)i.Seed * 7919UL + 13); var arrow = Id("arrow", FxTheme.Steel);
                    for (int k = 0; k < 9; k++)
                    {
                        float an = rr.Float() * MathF.Tau, d = i.R * MathF.Sqrt(rr.Float()) * .92f; float lx = i.X + MathF.Cos(an) * d, ly = i.Y + MathF.Sin(an) * d; int off = k, local = age - off;
                        if (local < 0) { Dot2(FxLayer.Ground, FxTheme.Earth, 0, lx - .5f, ly - .5f); continue; }
                        if (local < 6) Spr(FxLayer.Over, arrow, 0, lx, ly - 40 + 40 * local / 6f);
                        else if (local < 46) { if (local > 38 && local % 2 == 0) continue; Spr(FxLayer.Over, arrow, 1, lx, ly + 1); }
                    }
                    break;
                }
            case "cracks":
                {
                    var rr = new Rng((ulong)i.Seed * 104729UL + 5); float grow = Math.Min(1, age / 8f); bool fading = t > .55f;
                    for (int k = 0; k < 8; k++)
                    {
                        float an = k * MathF.Tau / 8 + (rr.Float() - .5f) * .5f, len = i.R * (.65f + rr.Float() * .45f) * grow; float x = i.X, y = i.Y; int steps = (int)len;
                        for (int s = 2; s < steps; s++) { an += (rr.Float() - .5f) * .55f; x = i.X + MathF.Cos(an) * s; y = i.Y + MathF.Sin(an) * s * .8f; if (fading && (s + k) % 2 == 0) continue; Dot(FxLayer.Ground, FxTheme.Shadow, 0, x, y + 1); Dot(FxLayer.Ground, FxTheme.Shadow, 1, x, y); if (!fading && s % 3 == 0) Dot(FxLayer.Ground, FxTheme.Earth, 3, x, y - 1); }
                    }
                    break;
                }
            case "arcs":
                for (int k = 0; k < 3; k++)
                {
                    int local = age - k * 3; if (local < 0 || local > 7) continue; float baseA = k * 2.1f - .5f, head = baseA + 2.4f * Math.Min(1, (local + 1) / 4f), rr = i.R * .62f;
                    for (int s = 0; s < 14; s++) { float an = head - s * .17f; if (an < baseA) break; if (local > 5 && s % 2 == 1) continue; int tone = s < 2 ? 3 : s < 6 ? 2 : 1; float cx = i.X + MathF.Cos(an) * rr, cy = i.Y + MathF.Sin(an) * rr * .8f; if (s < 7) Dot2(FxLayer.Over, th, tone, cx - .5f, cy - .5f); else Dot(FxLayer.Over, th, tone, cx, cy); Dot(FxLayer.Over, th, Math.Max(1, tone - 1), i.X + MathF.Cos(an) * (rr + 2.5f), i.Y + MathF.Sin(an) * (rr + 2.5f) * .8f); Dot(FxLayer.Over, FxTheme.Steel, 1, i.X + MathF.Cos(an) * (rr - 1.5f), i.Y + MathF.Sin(an) * (rr - 1.5f) * .8f); }
                }
                break;
            case "swirl":
                for (int arc = 0; arc < 3; arc++) { float baseA = age * .8f + arc * MathF.Tau / 3; for (int s = 0; s < 7; s++) { float an = baseA - s * .12f; float rr = i.R * (.35f + t * .6f); Dot(FxLayer.Over, th, s > 4 ? 1 : s > 2 ? 2 : 3, i.X + MathF.Cos(an) * rr, i.Y + MathF.Sin(an) * rr * .8f); } }
                break;
            case "column":
                {
                    var col = Id("column", FxTheme.Light); int h = age < 4 ? age * 25 / 4 : 100; if (age > 11 && age % 2 == 0) break; Spr(FxLayer.Over, col, age / 2 % 2, i.X, i.Y + 2); break;
                }
            case "streak":
                {
                    float x0 = i.X, y0 = i.Y, x1 = i.X2, y1 = i.Y2; int on = age < 3 ? 1 : age < 6 ? 3 : 2, off = age < 3 ? 0 : age < 6 ? 1 : 3;
                    LineDots(FxLayer.Over, th, x0, y0, x1, y1, age < 3 ? 3 : 2, age < 6 ? 2 : 1, on, off, age); LineDots(FxLayer.Over, th, x0, y0 + 1, x1, y1 + 1, 2, 1, on, off + 1, age); LineDots(FxLayer.Over, th, x0, y0 - 1, x1, y1 - 1, 2, 1, on, off, age); LineDots(FxLayer.Over, th, x0, y0 + 2, x1, y1 + 2, 1, 0, 1, 2, age); LineDots(FxLayer.Over, th, x0, y0 - 2, x1, y1 - 2, 1, 0, 1, 3, age);
                    for (int k = 1; k <= 3; k++) { var q = Vector2.Lerp(new(x0, y0), new(x1, y1), k / 4f); if (age < 3 + k) Spr(FxLayer.Over, Id("star4", th == FxTheme.Steel ? FxTheme.Steel : FxTheme.Light), Math.Min(2, age / 2), q.X, q.Y); }
                    break;
                }
            case "wave":
                {
                    var dir = new Vector2(i.X2, i.Y2); var src = new Vector2(i.X, i.Y);
                    for (int w = 0; w < 3; w++)
                    {
                        int local = age - w * 4; if (local < 0 || local > 14) continue; float rr = i.R * (.25f + .75f * local / 14f); bool late = local > 9;
                        for (int k = -7; k <= 7; k++) { if (late && k % 2 == 0) continue; var q = src + Rot(dir, k * ConeHalfAngle / 7f) * rr; Dot(FxLayer.Over, th, late ? 1 : w == 0 ? 3 : 2, q.X, q.Y); var q2 = src + Rot(dir, k * ConeHalfAngle / 7f) * (rr - 1.5f); Dot(FxLayer.Over, th, 1, q2.X, q2.Y); }
                    }
                    break;
                }
            case "acidshot":
                {
                    var from = new Vector2(i.X, i.Y); var to = new Vector2(i.X2, i.Y2);
                    for (int k = 0; k < 6; k++) { float u = (age - k) / 6f; if (u <= 0 || u >= 1) continue; var q = Vector2.Lerp(from, to, u); Spr(FxLayer.Over, Id("bubble", FxTheme.Acid), u < .75f ? 1 + k % 2 : 3, q.X, q.Y - MathF.Sin(u * MathF.PI) * 5); Dot(FxLayer.Over, FxTheme.Acid, 1, q.X - (to.X - from.X) / 30f * 2, q.Y - MathF.Sin(u * MathF.PI) * 5 + 2); }
                    break;
                }
            case "puddle":
                {
                    string sz = i.R >= 22 ? "puddle-l" : i.R >= 15 ? "puddle-m" : "puddle-s"; int left = i.Dur - age; if (left < 16) sz = left < 6 ? "puddle-s" : "puddle-m"; if (i.R >= 22 && left >= 16 && age < 4) sz = "puddle-m"; if (left < 16 && left % 2 == 0) break;
                    Spr(FxLayer.Ground, Id(sz, th), age / 7 % 2, i.X, i.Y); break;
                }
            case "stream": break;
            case "patches":
                { var rr = new Rng((ulong)i.Seed + 99); for (int k = 0; k < 3; k++) { float d = i.R * (.2f + .25f * k) ; var dir = new Vector2(i.X2, i.Y2); var q = new Vector2(i.X, i.Y) - dir * i.R * .8f + dir * d * 1.1f + new Vector2(-dir.Y, dir.X) * (rr.Float() - .5f) * 14; int left = i.Dur - age; string sz = left < 18 ? "puddle-s" : "puddle-m"; if (age > 10 && left > 12 && Has(sz, th)) { Spr(FxLayer.Ground, Id(sz, th), age / 6 % 2, q.X, q.Y + 3); } } break; }
            case "web":
                {
                    float grow = Math.Min(1, age / 6f), rr = i.R * grow; bool fade = i.Dur - age < 14; if (fade && age % 2 == 0) break;
                    for (int s = 0; s < 8; s++) { float an = s * MathF.Tau / 8; LineDots(FxLayer.Ground, th, i.X, i.Y, i.X + MathF.Cos(an) * rr, i.Y + MathF.Sin(an) * rr, 3, 2, 1, 0); }
                    for (int ring = 1; ring <= 3; ring++) { float rrr = rr * ring / 3f; for (int s = 0; s < 8; s++) { float a0 = s * MathF.Tau / 8, a1 = (s + 1) * MathF.Tau / 8; LineDots(FxLayer.Ground, th, i.X + MathF.Cos(a0) * rrr, i.Y + MathF.Sin(a0) * rrr, i.X + MathF.Cos(a1) * rrr, i.Y + MathF.Sin(a1) * rrr, 2, 1); } }
                    break;
                }
            case "strand":
                { float u = Math.Min(1, (age + 1) / 4f); LineDots(FxLayer.Over, th, i.X, i.Y, i.X + (i.X2 - i.X) * u, i.Y + (i.Y2 - i.Y) * u, 3, 2, 2, 1, age); break; }
            case "coil":
                {
                    var f = sim.Fighters[i.Owner]; if (!f.Alive) break; int frame = age < 7 ? 0 : age < 12 ? 1 : 2; if (i.Dur - age < 8 && age % 2 == 0) break;
                    Spr(FxLayer.Over, Id("coil", FxTheme.Plant), frame, f.Position.X, f.Position.Y - 3); Spr(FxLayer.Over, Id("coil", FxTheme.Plant), frame, f.Position.X, f.Position.Y - sizeOf(f.Id) * .38f); break;
                }
            case "jet":
                {
                    float len = Vector2.Distance(new(i.X, i.Y), new(i.X2, i.Y2)); var dir = Unit(new Vector2(i.X2 - i.X, i.Y2 - i.Y)); float reach = Math.Min(1, .35f + age / 4f) * len; var n = new Vector2(-dir.Y, dir.X);
                    if (age > 9 && age % 2 == 0) break;
                    for (int w = -1; w <= 1; w++) LineDots(FxLayer.Over, th, i.X + n.X * w, i.Y + n.Y * w, i.X + dir.X * reach + n.X * w, i.Y + dir.Y * reach + n.Y * w, w == 0 ? 3 : 2, w == 0 ? 2 : 1, 4, 2, age * 2 + w);
                    Spr(FxLayer.Over, Id("puff", FxTheme.Water), Math.Min(3, age / 3), i.X + dir.X * reach, i.Y + dir.Y * reach); break;
                }
            case "rootfield":
                {
                    var rr = new Rng((ulong)i.Seed * 31UL + 7); var root = Id("root", FxTheme.Plant); int left = i.Dur - age;
                    Spr(FxLayer.Ground, Id("vine-ring", FxTheme.Plant), age / 8 % 2, i.X, i.Y);
                    for (int k = 0; k < 10; k++)
                    {
                        float an = k * MathF.Tau / 10 + rr.Float() * .4f, d = 12 + rr.Float() * (i.R - 14); int local = age - k / 2; if (local < 0) continue;
                        int frame = left < 10 ? Math.Max(0, 4 - (10 - left) / 2) : Math.Min(4, local / 2); if (left < 4 && age % 2 == 0) continue; Spr(FxLayer.Over, root, frame, i.X + MathF.Cos(an) * d, i.Y + MathF.Sin(an) * d * .8f + 1, k % 2 == 0);
                    }
                    break;
                }
        }
    }
}
