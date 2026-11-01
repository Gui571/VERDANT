using Godot;
namespace Verdant.Forge;

/// <summary>As receitas do elenco fixo. Cada uma é só dados: espécie + equipamento + paleta.</summary>
public static class ForgeRoster
{
    static ForgePalette Pal(Species sp, Ramp cloth, Ramp acc, Ramp? leather = null) => ForgePalette.Make(ForgeLook.Fur(sp), cloth, acc, leather ?? ForgeLook.Leather);

    public static readonly IReadOnlyList<ForgeSpec> All = Array.AsReadOnly(new[]
    {
        new ForgeSpec("bram", "Bram", "Guerreiro", "Capacete de gelo, machado e escudo. Guarda firme e silhueta compacta.",
            Species.Duck, Headwear.IceHelm, Weapon.Axe, Offhand.RoundShield, BackGear.Cape, Outfit.Tunic, false, false, true,
            Pal(Species.Duck, ForgeLook.Blue, Ramp.Of("98572e", "e5953d", "f2b872"))),
        new ForgeSpec("nix", "Nix", "Batedora", "Orelhas longas, lenço cobre e espada curta. Leve e alerta.",
            Species.Rabbit, Headwear.Headband, Weapon.Sword, Offhand.None, BackGear.Cape, Outfit.Tunic, true, false, false,
            Pal(Species.Rabbit, ForgeLook.Blue, Ramp.Of("98572e", "e5953d", "f2b872"))),
        new ForgeSpec("rowan", "Rowan", "Arqueiro", "Capuz verde, arco e aljava. Couro e tecido em blocos simples.",
            Species.Human, Headwear.Hood, Weapon.Bow, Offhand.None, BackGear.Quiver, Outfit.Plain, false, false, false,
            Pal(Species.Human, Ramp.Of("4e6754", "849967", "a7ba84"), Ramp.Of("8a7a43", "b5a15d", "dfce80"))),
        new ForgeSpec("iria", "Iria", "Maga astral", "Chapéu violeta, grimório e cristal suspenso. Curiosa e metódica.",
            Species.Human, Headwear.WizardHat, Weapon.Staff, Offhand.Tome, BackGear.None, Outfit.Robe, false, false, false,
            Pal(Species.Human, Ramp.Of("514777", "9678cf", "b79ae6"), Ramp.Of("b9729a", "edacd3", "f7cde6"))),
        new ForgeSpec("boro", "Boro", "Guardião", "Carapaça de bronze, escudo-torre e martelo. Calmo e inabalável.",
            Species.Turtle, Headwear.WideHat, Weapon.Hammer, Offhand.TowerShield, BackGear.Shell, Outfit.Plate, false, false, false,
            Pal(Species.Turtle, Ramp.Of("765740", "c29454", "dfb27a"), Ramp.Of("b5a15d", "dfce80", "f1e7ae"))),
        new ForgeSpec("suri", "Suri", "Duelista", "Raposa de lenço vinho, cauda clara e duas adagas. Ágil e provocadora.",
            Species.Fox, Headwear.None, Weapon.Daggers, Offhand.None, BackGear.Tail, Outfit.Plain, true, false, false,
            Pal(Species.Fox, Ramp.Of("5c2d49", "8c4665", "b0678a"), Ramp.Of("b5a58a", "f0e6cd", "ffffff"))),
        new ForgeSpec("mira", "Mira", "Curandeira", "Coruja de manto jade e báculo circular. Atenta e acolhedora.",
            Species.Owl, Headwear.Cowl, Weapon.Crook, Offhand.None, BackGear.None, Outfit.Robe, false, false, false,
            Pal(Species.Owl, Ramp.Of("356e78", "57afa4", "8ad0c4"), Ramp.Of("b5ac96", "e5dcc2", "f6f0dc")))
    });

    public static ForgeSpec For(AdventurerKind kind) => All[(int)kind];
}
