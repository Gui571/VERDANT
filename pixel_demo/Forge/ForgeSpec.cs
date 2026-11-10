namespace Verdant.Forge;

public enum Species { Duck, Rabbit, Human, Turtle, Fox, Owl, Mouse, Cat, Frog, Elf, Bear,
    // Segunda leva (estilo v2): só o ChibiRenderer desenha estas; o ForgeRenderer antigo as trata como humanos.
    Wolf, Dog, Panda, Boar, Pig, Goat, Deer, Raccoon, Monkey, Orc, Goblin, Lizard, Skeleton, Dwarf }
/// <summary>Patente visual. Não altera atributos de combate: é só equipamento e brilho.</summary>
public enum Grade { Recruit, Veteran, Elite, Hero }
public enum Headwear { None, Headband, IceHelm, Hood, WizardHat, WideHat, Cowl, Crown, Beret, Helm, Horns, Mitre, Leaves, Feather }
public enum Weapon { None, Axe, Sword, Staff, Bow, Hammer, Daggers, Crook, Spear, Mace, Halberd, Crossbow, Wand, Lute, Flask }
public enum Offhand { None, RoundShield, TowerShield, Tome, Lantern }
public enum BackGear { None, Cape, Shell, Quiver, Tail, Banner }
public enum Outfit { Tunic, Plain, Robe, Plate }

/// <summary>Receita completa de um personagem. Duas receitas iguais produzem pixels idênticos.</summary>
public sealed record ForgeSpec(
    string Id, string Name, string Class, string Description,
    Species Species, Headwear Headwear, Weapon Weapon, Offhand Offhand, BackGear Back, Outfit Outfit,
    bool Scarf, bool Beard, bool Mirror, ForgePalette Palette)
{
    /// <summary>Lado da mão da arma: por padrão a arma fica na mão da frente (direita) e o item secundário na de trás.</summary>
    public bool WeaponFar => Mirror;
    /// <summary>Patente visual (Recruta por padrão: os pixels do elenco original não mudam).</summary>
    public Grade Grade { get; init; }
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
        Species.Wolf => Ramp.Of("5d6470", "8e96a3", "c9ced6"),
        Species.Dog => Ramp.Of("8b5a3a", "c28a58", "e6c498"),
        Species.Panda => Ramp.Of("aeb2b8", "eeeeee", "ffffff"),
        Species.Boar => Ramp.Of("5a4338", "846352", "b08c76"),
        Species.Pig => Ramp.Of("c07a86", "efa8ae", "ffd0d0"),
        Species.Goat => Ramp.Of("a9a394", "dcd6c6", "f5f1e6"),
        Species.Deer => Ramp.Of("8d5a36", "c88e54", "e8c698"),
        Species.Raccoon => Ramp.Of("5f6068", "8d8e98", "bcbdc6"),
        Species.Monkey => Ramp.Of("7a4e34", "b27f55", "dcb48a"),
        Species.Orc => Ramp.Of("3f6a3a", "6e9a4e", "9fc274"),
        Species.Goblin => Ramp.Of("4e7a2e", "86b043", "b8d86e"),
        Species.Lizard => Ramp.Of("2f7a60", "4fb08a", "8adab5"),
        Species.Skeleton => Ramp.Of("a8a69a", "e0ddcf", "f7f5ec"),
        Species.Dwarf => Ramp.Of("b8795a", "e0a07c", "f4c9a8"),
        _ => Ramp.Of("c99a76", "edc297", "f7dcbc")
    };
    public static readonly Ramp Blue = Ramp.Of("3455a4", "5098ef", "8ec2f6");
    public static readonly Ramp Leather = Ramp.Of("6b4a34", "947052", "b58f69");
}
