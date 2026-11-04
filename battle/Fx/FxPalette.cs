using Godot;
namespace Verdant.Battle;

/// <summary>Temas de cor dos efeitos. Cada tema é uma rampa de 4 tons (sombra, base, luz, brilho) + a tinta do projeto.
/// Formas e ícones carregam o significado; a cor só reforça (daltonismo): veneno e planta, por exemplo, têm cores e formas diferentes.</summary>
public enum FxTheme { Steel, Damage, Fire, Ice, Heal, Shield, Plant, Poison, Acid, Shadow, Water, Earth, Light, Blood, Stun, Web }

public static class FxPalette
{
    public static readonly Color Ink = new("151829");
    static readonly Dictionary<FxTheme, Color[]> Ramps = new()
    {
        [FxTheme.Steel] = Of("5f7b94", "829fb9", "c1dceb", "f0f0e5"),
        [FxTheme.Damage] = Of("8c3040", "e5593f", "f2a65a", "fff0c7"),
        [FxTheme.Fire] = Of("a8402a", "e8762f", "f7c35a", "fff0c7"),
        [FxTheme.Ice] = Of("3a6e9a", "6fb5e0", "b5e4f5", "f0fbff"),
        [FxTheme.Heal] = Of("3f8a5a", "7fcf8a", "bff0a8", "f4ffe8"),
        [FxTheme.Shield] = Of("3a7fa0", "5fc7d9", "a8eef0", "f0ffff"),
        [FxTheme.Plant] = Of("3a6e3f", "6fae50", "a5d889", "e6f5c8"),
        [FxTheme.Poison] = Of("5a3a7a", "9a6bd0", "cfa8f0", "f6eaff"),
        [FxTheme.Acid] = Of("5a7a1f", "a0cf2f", "d6f06a", "f6ffc8"),
        [FxTheme.Shadow] = Of("251c3a", "4a3a6e", "8a6fb0", "d6c8f0"),
        [FxTheme.Water] = Of("2f5f9a", "4f9fe0", "9fe0f5", "f0ffff"),
        [FxTheme.Earth] = Of("6b5638", "a58a5a", "d2b98a", "efe4c4"),
        [FxTheme.Light] = Of("c9a85a", "f0d98a", "fff0c7", "ffffff"),
        [FxTheme.Blood] = Of("7a2438", "c4455a", "e88a8a", "ffd6d0"),
        [FxTheme.Stun] = Of("b89a3a", "f2d25a", "fff0a0", "ffffff"),
        [FxTheme.Web] = Of("8d9ab0", "c8d4e0", "eef3f8", "ffffff"),
    };
    static Color[] Of(string d, string m, string l, string w) => new[] { new Color(d), new Color(m), new Color(l), new Color(w) };
    /// <summary>Tom 0..3 (sombra, base, luz, brilho) do tema.</summary>
    public static Color Tone(FxTheme t, int tone) => Ramps[t][Math.Clamp(tone, 0, 3)];
    public static Color Of(FxTheme t, char c) => c switch { 'o' => Ink, 'd' => Ramps[t][0], 'm' => Ramps[t][1], 'l' => Ramps[t][2], 'w' => Ramps[t][3], _ => new Color(0, 0, 0, 0) };

    public static FxTheme ForElement(EffectKind? element) => element switch
    {
        EffectKind.Burn => FxTheme.Fire, EffectKind.Slow => FxTheme.Ice, EffectKind.Root => FxTheme.Plant,
        EffectKind.Poison => FxTheme.Poison, EffectKind.Drain => FxTheme.Shadow, _ => FxTheme.Steel
    };
    public static FxTheme ForStatus(EffectKind k) => k switch
    {
        EffectKind.Burn => FxTheme.Fire, EffectKind.Slow => FxTheme.Ice, EffectKind.Root => FxTheme.Plant, EffectKind.Poison => FxTheme.Poison,
        EffectKind.Bleed => FxTheme.Blood, EffectKind.Stun => FxTheme.Stun, EffectKind.Taunt => FxTheme.Damage, EffectKind.Shield => FxTheme.Shield,
        EffectKind.Regen => FxTheme.Heal, EffectKind.Dodge => FxTheme.Steel, EffectKind.Weaken => FxTheme.Shadow, EffectKind.Buff => FxTheme.Damage,
        EffectKind.Drain => FxTheme.Shadow, EffectKind.Heal => FxTheme.Heal, _ => FxTheme.Steel
    };
}
