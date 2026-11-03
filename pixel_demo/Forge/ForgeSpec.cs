namespace Verdant.Forge;

public enum Species { Duck, Rabbit, Human, Turtle, Fox, Owl, Mouse, Cat, Frog, Elf, Bear }
public enum Headwear { None, Headband, IceHelm, Hood, WizardHat, WideHat, Cowl, Crown, Beret }
public enum Weapon { None, Axe, Sword, Staff, Bow, Hammer, Daggers, Crook, Spear }
public enum Offhand { None, RoundShield, TowerShield, Tome }
public enum BackGear { None, Cape, Shell, Quiver, Tail }
public enum Outfit { Tunic, Plain, Robe, Plate }

/// <summary>Receita completa de um personagem. Duas receitas iguais produzem pixels idênticos.</summary>
public sealed record ForgeSpec(
    string Id, string Name, string Class, string Description,
    Species Species, Headwear Headwear, Weapon Weapon, Offhand Offhand, BackGear Back, Outfit Outfit,
    bool Scarf, bool Beard, bool Mirror, ForgePalette Palette)
{
    /// <summary>Lado da mão da arma: por padrão a arma fica na mão da frente (direita) e o item secundário na de trás.</summary>
    public bool WeaponFar => Mirror;
}

public static class ForgeLook
{
    // Cores-base por espécie (rampas já ajustadas à mão para o estilo do elenco).
    public static Ramp Fur(Species s) => s switch
    {
        Species.Duck => Ramp.Of("b5a15d", "dfce80", "f1e7ae"),
        Species.Rabbit => Ramp.Of("b0b5ba", "f0f0e5", "ffffff"),
        Species.Human => Ramp.Of("c99a76", "edc297", "f7dcbc"),
        Species.Turtle => Ramp.Of("718453", "a6b477", "c6d09a"),
        Species.Fox => Ramp.Of("aa633e", "df9855", "f0e6cd"),
        Species.Owl => Ramp.Of("b5ac96", "e5dcc2", "f6f0dc"),
        Species.Mouse => Ramp.Of("6f5d58", "9b8478", "c7b3a6"),
        Species.Cat => Ramp.Of("7f8596", "b0b5ba", "e4e7ea"),
        Species.Frog => Ramp.Of("4e7a45", "7fa65a", "b4cf7c"),
        Species.Bear => Ramp.Of("69483c", "aa7950", "d6ac78"),
        Species.Elf => Ramp.Of("5c8a4a", "8fb162", "bed58c"),
        _ => Ramp.Of("c99a76", "edc297", "f7dcbc")
    };
    public static readonly Ramp Blue = Ramp.Of("3455a4", "5098ef", "8ec2f6");
    public static readonly Ramp Leather = Ramp.Of("6b4a34", "947052", "b58f69");
}
