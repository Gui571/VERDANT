namespace Verdant.Forge.Compact;

/// <summary>Uma classe visual: define equipamento coerente. Não cria papéis novos na simulação: <see cref="Role"/> só diz com qual papel de combate existente ela combina.</summary>
public sealed record ClassDef(string Id, string Name, string Role, Headwear[] Hats, Weapon[] Weapons, Offhand[] Offhands, BackGear[] Backs, Outfit[] Outfits, bool Scarf);

/// <summary>Elenco procedural do estilo v2: 25 espécies × 20 classes × 4 patentes. Cada peça sorteia em um fluxo de RNG próprio
/// (semente-base + nome do slot), então trocar o fluxo de UM slot não muda as escolhas dos outros.</summary>
public static class ForgeCast
{
    public const string SlotSpecies = "species", SlotClass = "class", SlotHat = "hat", SlotWeapon = "weapon", SlotOffhand = "offhand", SlotBack = "back", SlotOutfit = "outfit", SlotName = "name", SlotColor = "color";
    public static readonly string[] Slots = { SlotSpecies, SlotClass, SlotHat, SlotWeapon, SlotOffhand, SlotBack, SlotOutfit, SlotName, SlotColor };
    static ClassDef C(string id, string name, string role, Headwear[] h, Weapon[] w, Offhand[] o, BackGear[] b, Outfit[] f, bool scarf = false) => new(id, name, role, h, w, o, b, f, scarf);
    static readonly Headwear[] H0 = { Headwear.None };
    static readonly Offhand[] O0 = { Offhand.None };
    static readonly BackGear[] B0 = { BackGear.None };

    public static readonly IReadOnlyList<ClassDef> Classes = Array.AsReadOnly(new[]
    {
        C("knight", "Cavaleiro", "Melee", new[] { Headwear.Helm }, new[] { Weapon.Sword }, new[] { Offhand.RoundShield }, new[] { BackGear.Cape }, new[] { Outfit.Plate }),
        C("paladin", "Paladino", "Tank", new[] { Headwear.Helm, Headwear.IceHelm }, new[] { Weapon.Mace }, new[] { Offhand.RoundShield, Offhand.TowerShield }, new[] { BackGear.Cape }, new[] { Outfit.Plate }),
        C("guardian", "Guardião", "Tank", new[] { Headwear.IceHelm, Headwear.WideHat }, new[] { Weapon.Hammer }, new[] { Offhand.TowerShield }, new[] { BackGear.None, BackGear.Cape }, new[] { Outfit.Plate }),
        C("swordsman", "Espadachim", "Melee", new[] { Headwear.Headband, Headwear.Beret }, new[] { Weapon.Sword }, O0, new[] { BackGear.Cape }, new[] { Outfit.Tunic }, true),
        C("barbarian", "Bárbaro", "Charger", new[] { Headwear.Horns }, new[] { Weapon.Axe }, O0, B0, new[] { Outfit.Plain }),
        C("halberdier", "Alabardeiro", "Melee", new[] { Headwear.Helm }, new[] { Weapon.Halberd }, O0, B0, new[] { Outfit.Tunic }),
        C("rogue", "Ladino", "Melee", new[] { Headwear.Hood, Headwear.Cowl }, new[] { Weapon.Daggers }, O0, new[] { BackGear.None, BackGear.Tail }, new[] { Outfit.Plain }, true),
        C("monk", "Monge", "Melee", new[] { Headwear.Headband }, new[] { Weapon.None }, O0, B0, new[] { Outfit.Plain }, true),
        C("militia", "Miliciano", "Melee", new[] { Headwear.Beret, Headwear.Headband }, new[] { Weapon.Spear }, new[] { Offhand.RoundShield, Offhand.None }, B0, new[] { Outfit.Tunic }),
        C("archer", "Arqueiro", "Archer", new[] { Headwear.Hood, Headwear.Headband }, new[] { Weapon.Bow }, O0, new[] { BackGear.Quiver }, new[] { Outfit.Plain }),
        C("crossbowman", "Besteiro", "Archer", new[] { Headwear.WideHat, Headwear.Beret }, new[] { Weapon.Crossbow }, O0, new[] { BackGear.Quiver }, new[] { Outfit.Tunic }),
        C("alchemist", "Alquimista", "Spitter", new[] { Headwear.Beret, Headwear.Headband }, new[] { Weapon.Flask }, O0, B0, new[] { Outfit.Tunic, Outfit.Plain }),
        C("mage", "Mago", "Mage", new[] { Headwear.WizardHat }, new[] { Weapon.Staff }, new[] { Offhand.Tome }, B0, new[] { Outfit.Robe }),
        C("witch", "Bruxa", "Mage", new[] { Headwear.WideHat, Headwear.WizardHat }, new[] { Weapon.Wand }, new[] { Offhand.Lantern }, B0, new[] { Outfit.Robe }),
        C("necromancer", "Necromante", "Mage", new[] { Headwear.Hood, Headwear.Cowl }, new[] { Weapon.Staff }, new[] { Offhand.Lantern }, B0, new[] { Outfit.Robe }),
        C("shaman", "Xamã", "Healer", new[] { Headwear.Feather, Headwear.Horns }, new[] { Weapon.Crook }, O0, new[] { BackGear.None, BackGear.Cape }, new[] { Outfit.Robe, Outfit.Plain }),
        C("cleric", "Clérigo", "Healer", new[] { Headwear.Mitre }, new[] { Weapon.Mace }, new[] { Offhand.Tome }, B0, new[] { Outfit.Robe }),
        C("druid", "Druida", "Healer", new[] { Headwear.Leaves }, new[] { Weapon.Staff, Weapon.Crook }, O0, B0, new[] { Outfit.Robe }),
        C("bard", "Bardo", "Healer", new[] { Headwear.Feather }, new[] { Weapon.Lute }, O0, new[] { BackGear.Cape }, new[] { Outfit.Tunic }),
        C("bannerman", "Porta-estandarte", "Melee", new[] { Headwear.Helm, Headwear.IceHelm }, new[] { Weapon.Sword }, new[] { Offhand.RoundShield }, new[] { BackGear.Banner }, new[] { Outfit.Tunic }),
    });
    /// <summary>Classe "livre" (só para demonstrar slots): mistura todas as opções, então trocar o fluxo de um slot sempre aparece.</summary>
    public static readonly ClassDef Free = C("free", "Livre", "Melee",
        new[] { Headwear.None, Headwear.Headband, Headwear.Helm, Headwear.Horns, Headwear.Hood, Headwear.WizardHat, Headwear.Beret, Headwear.Mitre },
        new[] { Weapon.Sword, Weapon.Axe, Weapon.Mace, Weapon.Spear, Weapon.Staff, Weapon.Hammer, Weapon.Daggers },
        new[] { Offhand.None, Offhand.RoundShield, Offhand.TowerShield, Offhand.Tome, Offhand.Lantern },
        new[] { BackGear.None, BackGear.Cape, BackGear.Quiver, BackGear.Banner }, new[] { Outfit.Plain, Outfit.Tunic, Outfit.Robe, Outfit.Plate });
    public static ClassDef ClassById(string id) => id == "free" ? Free : Classes.First(c => c.Id == id);

    public static readonly string[] SpeciesPt =
    {
        "Pato", "Coelho", "Humano", "Tartaruga", "Raposa", "Coruja", "Rato", "Gato", "Sapo", "Elfo", "Urso",
        "Lobo", "Cão", "Panda", "Javali", "Porco", "Bode", "Cervo", "Guaxinim", "Macaco", "Orc", "Goblin", "Lagarto", "Esqueleto", "Anão"
    };
    // Espécies de orelhas/chifres altos só aceitam adornos que não as cobrem (a silhueta continua legível).
    static readonly HashSet<Species> Eared = new() { Species.Rabbit, Species.Fox, Species.Cat, Species.Mouse, Species.Wolf, Species.Goat, Species.Deer, Species.Goblin, Species.Lizard, Species.Owl };
    static readonly Headwear[] EaredSafe = { Headwear.Headband, Headwear.Crown, Headwear.Feather, Headwear.None, Headwear.Leaves };
    static readonly HashSet<Species> Tailed = new() { Species.Fox, Species.Cat, Species.Mouse, Species.Rabbit, Species.Wolf, Species.Dog, Species.Raccoon, Species.Monkey, Species.Lizard };
    static readonly Ramp[] Cloths = { ForgeLook.Blue, Ramp.Of("4e6754", "849967", "a7ba84"), Ramp.Of("514777", "9678cf", "b79ae6"), Ramp.Of("5c2d49", "8c4665", "b0678a"), Ramp.Of("356e78", "57afa4", "8ad0c4"), Ramp.Of("765740", "c29454", "dfb27a") };
    static readonly Ramp[] Accents = { Ramp.Of("98572e", "e5953d", "f2b872"), Ramp.Of("b5a15d", "dfce80", "f1e7ae"), Ramp.Of("b9729a", "edacd3", "f7cde6"), Ramp.Of("356e78", "57afa4", "8ad0c4") };
    static readonly string[] Syl = { "Bo", "Ki", "Ra", "Lu", "Ze", "Mo", "Ti", "Fa", "Ne", "Do", "Sa", "Vi", "Pe", "Gru", "Ola", "Tu" };
    static readonly string[] End = { "", "", "n", "ro", "ni", "la", "ki", "to" };

    /// <summary>Hash estável (FNV-1a) do nome do slot: <c>string.GetHashCode</c> muda a cada execução e quebraria o determinismo.</summary>
    static ulong Fnv(string s) { ulong h = 14695981039346656037UL; foreach (char c in s) { h ^= c; h *= 1099511628211UL; } return h; }
    static Rng Stream(ulong seed, string slot, string? rerollSlot, ulong rerollSeed) => new(slot == rerollSlot ? rerollSeed * 0x9E3779B97F4A7C15UL ^ Fnv(slot) : seed * 0xD6E8FEB86659FD93UL ^ Fnv(slot));

    public static ForgeSpec Generate(ulong seed, Grade? grade = null, string? rerollSlot = null, ulong rerollSeed = 0, Species? species = null, string? classId = null)
    {
        Rng S(string slot) => Stream(seed, slot, rerollSlot, rerollSeed);
        var sp = species ?? (Species)S(SlotSpecies).Int(0, Enum.GetValues<Species>().Length);
        var cls = classId != null ? ClassById(classId) : Classes[S(SlotClass).Int(0, Classes.Count)];
        var hats = Eared.Contains(sp) ? cls.Hats.Where(EaredSafe.Contains).ToArray() : cls.Hats;
        var hat = hats.Length > 0 ? S(SlotHat).Pick(hats) : S(SlotHat).Pick(new[] { Headwear.Headband, Headwear.Feather });
        var weapon = S(SlotWeapon).Pick(cls.Weapons); var off = S(SlotOffhand).Pick(cls.Offhands);
        var back = S(SlotBack).Pick(cls.Backs); var outfit = S(SlotOutfit).Pick(cls.Outfits);
        if (weapon is Weapon.Bow or Weapon.Daggers or Weapon.Crossbow or Weapon.Lute or Weapon.Halberd or Weapon.None) off = Offhand.None;   // duas mãos ou mãos livres
        if (back == BackGear.Shell && sp != Species.Turtle) back = BackGear.Cape;
        if (sp == Species.Turtle && back == BackGear.None) back = BackGear.Shell;
        if (back == BackGear.Tail && !Tailed.Contains(sp)) back = BackGear.Cape;
        var g = grade ?? (Grade)S(SlotColor).Int(0, 4);
        var cr = S(SlotColor); cr.Next(); var cloth = cr.Pick(Cloths); var acc = cr.Pick(Accents);
        var nr = S(SlotName); string name = nr.Pick(Syl) + nr.Pick(Syl).ToLowerInvariant() + nr.Pick(End); name = char.ToUpperInvariant(name[0]) + name[1..];
        var palette = ForgePalette.Make(ForgeLook.Fur(sp), cloth, acc, ForgeLook.Leather);
        string desc = $"{SpeciesPt[(int)sp]} {cls.Name.ToLowerInvariant()} ({GradePt(g).ToLowerInvariant()}). Semente {seed}.";
        return new ForgeSpec($"v{seed}-{sp}-{cls.Id}-{g}", name, cls.Name, desc, sp, hat, weapon, off, back, outfit, cls.Scarf, false, false, palette) { Grade = g };
    }
    public static string GradePt(Grade g) => g switch { Grade.Recruit => "Recruta", Grade.Veteran => "Veterano", Grade.Elite => "Elite", _ => "Herói" };
}
