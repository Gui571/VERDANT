namespace Verdant.Battle;

/// <summary>Ícones de estado 9×9 (base 7×7 + contorno). Cada estado tem forma própria, além da cor.</summary>
public static class StatusIcons
{
    /// <summary>Estados na ordem de prioridade de exibição: no máximo 3 por unidade (os primeiros presentes).</summary>
    public static readonly EffectKind[] Priority = { EffectKind.Stun, EffectKind.Root, EffectKind.Taunt, EffectKind.Burn, EffectKind.Poison, EffectKind.Bleed, EffectKind.Slow, EffectKind.Weaken, EffectKind.Drain, EffectKind.Shield, EffectKind.Dodge, EffectKind.Regen, EffectKind.Buff };
    public static string Name(EffectKind k) => k switch { EffectKind.Stun => "stun", EffectKind.Root => "root", EffectKind.Slow => "slow", EffectKind.Burn => "burn", EffectKind.Poison => "poison", EffectKind.Bleed => "bleed", EffectKind.Taunt => "taunt", EffectKind.Shield => "shield", EffectKind.Regen => "regen", EffectKind.Dodge => "dodge", EffectKind.Weaken => "weaken", EffectKind.Buff => "buff", _ => "drain" };

    public static IEnumerable<(string name, FxTheme theme, Pic pic)> All()
    {
        foreach (var k in Priority) yield return (Name(k), FxPalette.ForStatus(k), Draw(k).Outlined());
    }

    static Pic Draw(EffectKind kind)
    {
        var p = new Pic(7, 7);
        switch (kind)
        {
            case EffectKind.Stun: p.Line(3, 0, 3, 6, 'l'); p.Line(0, 3, 6, 3, 'l'); p.Line(1, 1, 5, 5, 'm'); p.Line(5, 1, 1, 5, 'm'); p.Set(3, 3, 'w'); p.Set(3, 2, 'w'); p.Set(2, 3, 'w'); p.Set(4, 3, 'w'); p.Set(3, 4, 'w'); break;
            case EffectKind.Root: p.Line(0, 0, 6, 6, 'm'); p.Line(6, 0, 0, 6, 'm'); p.Line(3, 0, 3, 6, 'l'); p.Set(0, 3, 'l'); p.Set(6, 3, 'l'); p.Set(3, 3, 'w'); p.Set(1, 5, 'd'); p.Set(5, 5, 'd'); break;
            case EffectKind.Slow: p.Line(3, 0, 3, 6, 'l'); p.Line(0, 3, 6, 3, 'l'); p.Line(1, 1, 5, 5, 'm'); p.Line(5, 1, 1, 5, 'm'); p.Set(3, 3, 'w'); break;
            case EffectKind.Burn: for (int y = 0; y < 7; y++) { float t = y / 6f; int hw = (int)MathF.Round(3 * MathF.Sqrt(Math.Min(1, t * 1.5f + .1f)) * (t > .85f ? .7f : 1)); for (int x = 3 - hw; x <= 3 + hw; x++) p.Set(x, y, Math.Abs(x - 3) < hw / 2f && t > .4f ? 'w' : Math.Abs(x - 3) <= hw * .7f && t > .25f ? 'l' : 'm'); } p.Set(2, 0, '.'); break;
            case EffectKind.Poison: p.Ellipse(2, 4, 2.2f, 2.2f, 'm', 'd'); p.Ellipse(5, 2.5f, 1.6f, 1.6f, 'l', 'm'); p.Set(2, 3, 'w'); p.Set(5, 2, 'w'); p.Set(4, 5, 'm'); p.Set(1, 1, 'l'); break;
            case EffectKind.Bleed: p.Set(3, 0, 'l'); p.Line(2, 1, 2, 2, 'm'); p.Line(4, 1, 4, 2, 'm'); p.Set(3, 1, 'm'); p.Ellipse(3, 4, 2.6f, 2.4f, 'm', 'd'); p.Set(2, 3, 'w'); p.Set(2, 4, 'l'); p.Line(5, 5, 6, 6, 'l'); break;
            case EffectKind.Taunt: p.Rect(2, 0, 3, 4, 'm'); p.Rect(3, 0, 1, 4, 'w'); p.Rect(2, 5, 3, 2, 'm'); p.Set(3, 5, 'w'); break;
            case EffectKind.Shield: for (int y = 0; y < 7; y++) { int hw = y < 4 ? 3 : 3 - (y - 3); for (int x = 3 - hw; x <= 3 + hw; x++) p.Set(x, y, x == 3 - hw || x == 3 + hw ? 'd' : x < 3 && y < 4 ? 'w' : 'l'); } p.Set(3, 6, 'd'); p.Set(2, 1, 'w'); p.Set(2, 2, 'w'); break;
            case EffectKind.Regen: p.Rect(2, 0, 3, 7, 'm'); p.Rect(0, 2, 7, 3, 'm'); p.Rect(3, 1, 1, 5, 'l'); p.Rect(1, 3, 5, 1, 'l'); p.Set(3, 3, 'w'); break;
            case EffectKind.Dodge: p.Line(0, 1, 5, 1, 'l'); p.Set(6, 2, 'l'); p.Line(1, 3, 6, 3, 'w'); p.Line(0, 5, 4, 5, 'l'); p.Set(5, 6, 'l'); p.Set(0, 3, 'm'); break;
            case EffectKind.Weaken: p.Line(0, 1, 3, 4, 'l'); p.Line(6, 1, 3, 4, 'l'); p.Line(0, 3, 3, 6, 'm'); p.Line(6, 3, 3, 6, 'm'); p.Set(3, 4, 'w'); break;
            case EffectKind.Buff: p.Line(0, 5, 3, 2, 'l'); p.Line(6, 5, 3, 2, 'l'); p.Line(0, 3, 3, 0, 'm'); p.Line(6, 3, 3, 0, 'm'); p.Set(3, 2, 'w'); break;
            default: p.Ellipse(3, 3, 3.2f, 3.2f, 'm', 'd'); p.Ellipse(4.5f, 2.5f, 2.6f, 2.8f, '.'); p.Set(1, 2, 'w'); p.Set(1, 3, 'l'); p.Set(2, 5, 'l'); break;
        }
        return p;
    }
}
