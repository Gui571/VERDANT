using Godot;
namespace Verdant.Forge;

/// <summary>Gera personagens novos no mesmo estilo do elenco a partir de uma semente. Mesma semente = mesmo personagem, sempre.
/// A geração sorteia uma classe (que define equipamento coerente), uma espécie compatível com os chapéus da classe e uma paleta
/// derivada das rampas do elenco — nunca cores soltas — para a família visual se manter.</summary>
public static class ForgeGenerator
{
    sealed record Archetype(string Name, Headwear[] Hats, Weapon[] Weapons, Offhand[] Offhands, BackGear[] Backs, Outfit[] Outfits, bool Scarf);

    static readonly Archetype[] Classes =
    {
        new("Guerreiro", new[] { Headwear.IceHelm, Headwear.Headband, Headwear.WideHat }, new[] { Weapon.Axe, Weapon.Sword, Weapon.Hammer }, new[] { Offhand.RoundShield, Offhand.TowerShield }, new[] { BackGear.Cape, BackGear.None }, new[] { Outfit.Tunic, Outfit.Plate }, false),
        new("Batedor", new[] { Headwear.Headband, Headwear.Hood, Headwear.WideHat }, new[] { Weapon.Sword, Weapon.Daggers, Weapon.Spear }, new[] { Offhand.None }, new[] { BackGear.Cape, BackGear.Tail, BackGear.None }, new[] { Outfit.Tunic, Outfit.Plain }, true),
        new("Arqueiro", new[] { Headwear.Hood, Headwear.Headband, Headwear.WideHat }, new[] { Weapon.Bow }, new[] { Offhand.None }, new[] { BackGear.Quiver }, new[] { Outfit.Plain }, false),
        new("Mago", new[] { Headwear.WizardHat, Headwear.WideHat }, new[] { Weapon.Staff }, new[] { Offhand.Tome, Offhand.None }, new[] { BackGear.None, BackGear.Cape }, new[] { Outfit.Robe }, false),
        new("Curandeiro", new[] { Headwear.Cowl, Headwear.Hood, Headwear.Crown }, new[] { Weapon.Crook, Weapon.Staff }, new[] { Offhand.Tome, Offhand.None }, new[] { BackGear.None, BackGear.Cape }, new[] { Outfit.Robe }, false),
        new("Guardião", new[] { Headwear.WideHat, Headwear.IceHelm }, new[] { Weapon.Hammer, Weapon.Spear }, new[] { Offhand.TowerShield, Offhand.RoundShield }, new[] { BackGear.Shell, BackGear.Cape }, new[] { Outfit.Plate }, false),
        new("Duelista", new[] { Headwear.Headband, Headwear.None }, new[] { Weapon.Daggers, Weapon.Sword }, new[] { Offhand.None }, new[] { BackGear.Tail, BackGear.Cape }, new[] { Outfit.Plain, Outfit.Tunic }, true),
        new("Lanceiro", new[] { Headwear.Headband, Headwear.IceHelm, Headwear.Crown }, new[] { Weapon.Spear }, new[] { Offhand.RoundShield, Offhand.None }, new[] { BackGear.Cape, BackGear.Tail, BackGear.None }, new[] { Outfit.Tunic, Outfit.Plain }, true),
    };

    // Espécies com orelhas altas só aceitam adornos que não as cobrem, para a silhueta continuar legível.
    static Headwear[] Allowed(Species sp, Headwear[] hats)
    {
        Headwear[] safe = sp switch
        {
            Species.Rabbit or Species.Fox or Species.Cat or Species.Mouse => new[] { Headwear.Headband, Headwear.Crown, Headwear.None },
            Species.Elf => new[] { Headwear.Headband, Headwear.Crown, Headwear.WideHat, Headwear.WizardHat, Headwear.None },
            Species.Owl => new[] { Headwear.Headband, Headwear.Cowl, Headwear.Crown, Headwear.None },
            _ => (Headwear[])Enum.GetValues(typeof(Headwear))
        };
        var ok = hats.Where(h => safe.Contains(h)).ToArray();
        return ok.Length > 0 ? ok : new[] { Headwear.Headband };
    }

    // Tecidos: a rampa azul "do time" aparece em ~40% dos sorteios, como nas referências; o resto vem de uma lista curada.
    static readonly Ramp[] Cloths =
    {
        ForgeLook.Blue, ForgeLook.Blue, ForgeLook.Blue,
        Ramp.Of("4e6754", "849967", "a7ba84"), Ramp.Of("514777", "9678cf", "b79ae6"), Ramp.Of("5c2d49", "8c4665", "b0678a"),
        Ramp.Of("356e78", "57afa4", "8ad0c4"), Ramp.Of("765740", "c29454", "dfb27a"), Ramp.Of("7a2f3a", "c4575a", "e48a82"),
        Ramp.Of("2f3f6e", "4a6fc0", "7da2e8")
    };
    static readonly Ramp[] Accents =
    {
        Ramp.Of("98572e", "e5953d", "f2b872"), Ramp.Of("b5a15d", "dfce80", "f1e7ae"), Ramp.Of("b9729a", "edacd3", "f7cde6"),
        Ramp.Of("b5ac96", "e5dcc2", "f6f0dc"), Ramp.Of("356e78", "57afa4", "8ad0c4")
    };
    static readonly string[] Syl = { "Bo", "Ki", "Ra", "Lu", "Ze", "Mo", "Ti", "Fa", "Ne", "Do", "Sa", "Vi", "Pe", "Gru", "Ola", "Tu" };
    static readonly string[] End = { "", "", "n", "ro", "ni", "la", "ki", "to" };

    public static readonly string[] SpeciesPt = { "Pato", "Coelho", "Humano", "Tartaruga", "Raposa", "Coruja", "Rato", "Gato", "Sapo", "Elfo" };
    static readonly Dictionary<Weapon, string> WeaponPt = new() { [Weapon.Axe] = "machado", [Weapon.Sword] = "espada curta", [Weapon.Staff] = "cajado de cristal", [Weapon.Bow] = "arco", [Weapon.Hammer] = "martelo", [Weapon.Daggers] = "duas adagas", [Weapon.Crook] = "báculo circular", [Weapon.Spear] = "lança" };
    static readonly Dictionary<Headwear, string> HatPt = new() { [Headwear.None] = "cabeça descoberta", [Headwear.Headband] = "faixa de tecido", [Headwear.IceHelm] = "elmo de gelo", [Headwear.Hood] = "capuz", [Headwear.WizardHat] = "chapéu pontudo", [Headwear.WideHat] = "chapéu de aba larga", [Headwear.Cowl] = "capuz claro", [Headwear.Crown] = "coroa pequena" };

    public static ForgeSpec Generate(ulong seed)
    {
        var r = new Rng(seed);
        var cls = r.Pick(Classes);
        // 1) espécie sorteada; 2) chapéu escolhido entre os da classe que a espécie aceita.
        var sp = (Species)r.Int(0, 10);
        var hat = r.Pick(Allowed(sp, cls.Hats));
        var weapon = r.Pick(cls.Weapons); var off = r.Pick(cls.Offhands); var back = r.Pick(cls.Backs); var outfit = r.Pick(cls.Outfits);
        if (weapon is Weapon.Bow or Weapon.Daggers) off = Offhand.None;                 // mãos ocupadas
        if (weapon == Weapon.Spear && off == Offhand.TowerShield) off = Offhand.RoundShield;
        // Cauda natural das espécies que têm uma, quando a classe não ocupa as costas.
        if (back == BackGear.None && sp is Species.Fox or Species.Cat or Species.Mouse or Species.Rabbit && r.Chance(.6f)) back = BackGear.Tail;
        if (back == BackGear.Tail && sp is Species.Human or Species.Elf or Species.Owl or Species.Turtle) back = BackGear.Cape;
        if (back == BackGear.Shell && sp != Species.Turtle) back = BackGear.Cape;
        if (sp == Species.Turtle && back == BackGear.None && r.Chance(.7f)) back = BackGear.Shell;
        bool beard = sp is Species.Human or Species.Elf && cls.Outfits.Contains(Outfit.Robe) && r.Chance(.45f);
        bool mirror = weapon is Weapon.Axe or Weapon.Hammer or Weapon.Sword && off == Offhand.RoundShield && r.Chance(.5f);

        // Pelagem: rampa da espécie com leve deslocamento de brilho para dar variedade sem sair do estilo.
        var fur = ForgeLook.Fur(sp);
        if (sp is Species.Duck or Species.Rabbit or Species.Fox or Species.Cat or Species.Mouse or Species.Frog && r.Chance(.5f))
        {
            float shift = (r.Float() - .5f) * .12f, vs = .88f + r.Float() * .22f; var (h, s, v) = Hsv.From(fur.M);
            if (sp != Species.Rabbit) fur = Ramp.From(Hsv.To(h + shift, Math.Clamp(s * (.8f + r.Float() * .4f), 0, 1), Math.Clamp(v * vs, 0, 1)));
        }
        var cloth = r.Pick(Cloths); var acc = r.Pick(Accents);
        var palette = ForgePalette.Make(fur, cloth, acc, ForgeLook.Leather);

        string name = r.Pick(Syl) + r.Pick(Syl).ToLowerInvariant() + r.Pick(End);
        string desc = $"{SpeciesPt[(int)sp]} {cls.Name.ToLowerInvariant()}: {WeaponPt[weapon]}, {HatPt[hat]}" + (beard ? " e barba branca" : "") + ". Gerado pela semente " + seed + ".";
        return new ForgeSpec("g" + seed, char.ToUpperInvariant(name[0]) + name[1..], cls.Name, desc, sp, hat, weapon, off, back, outfit, cls.Scarf, beard, mirror, palette);
    }
}
