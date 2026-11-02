using Verdant.Forge;
namespace Verdant.Battle;
public sealed record ActorRecipe(string Key,ForgeSpec? Hero,MonsterSpec? Monster)
{
    public FighterSpec Fighter(int team)
    {
        Role role;string group;int bonus=0;
        if(Hero is {} h){group=h.Class;role=h.Weapon switch{Weapon.Bow=>Role.Archer,Weapon.Staff=>Role.Mage,Weapon.Crook=>Role.Healer,Weapon.Daggers=>Role.Duelist,Weapon.Hammer=>Role.Tank,_=>Role.Melee};}
        else {var m=Monster!;group=m.Family.ToString();role=m.Family switch{MonsterFamily.Golem=>Role.Tank,MonsterFamily.Dragon=>Role.Charger,MonsterFamily.Aquatic=>Role.Spitter,MonsterFamily.Plant=>Role.Healer,MonsterFamily.Winged=>Role.Duelist,MonsterFamily.Arthropod=>Role.Swarm,_=>Role.Melee};bonus=(int)m.Rank*10+m.Mass;}
        var stats=role switch{
            Role.Tank=>new Stats(140,16,5,19,18,29),Role.Archer=>new Stats(90,19,1,24,75,28),Role.Mage=>new Stats(85,16,1,22,65,32),Role.Healer=>new Stats(105,14,2,22,55,30),Role.Duelist=>new Stats(95,14,2,34,19,19),Role.Charger=>new Stats(110,20,2,26,20,33),Role.Spitter=>new Stats(95,18,2,23,65,30),Role.Swarm=>new Stats(90,12,2,32,18,17),_=>new Stats(115,19,3,25,20,27)};
        string? ability=Hero!=null&&Abilities.HeroSignatures.TryGetValue(Hero.Id,out var signature)?signature:null;
        if(Monster is {} mon)ability=Abilities.MonsterSignatures[mon.Family.ToString()];
        if(Monster?.SpecialId is {} overrideId&&Abilities.All.ContainsKey(overrideId))ability=overrideId;
        EffectKind? element=Monster?.Element switch{Element.Fire=>EffectKind.Burn,Element.Ice=>EffectKind.Slow,Element.Plant=>EffectKind.Root,Element.Poison=>EffectKind.Poison,Element.Shadow=>EffectKind.Drain,_=>null};
        return new(team,Key,group,role,stats with{Health=stats.Health+bonus,Attack=stats.Attack+(Monster is {} m2?(int)m2.Element%2:0)},ability,Monster?.Rank==Rank.Boss?"rally":null,element);
    }
    public static ActorRecipe FromHero(int index)=>new("hero-"+index,ForgeRoster.All[index],null);
    public static ActorRecipe Generated(ulong seed)=>new("generated-"+seed,ForgeGenerator.Generate(seed),null);
    public static ActorRecipe FromMonster(ulong seed,MonsterFamily? family=null){var s=MonsterGenerator.Generate(seed,family);return new("monster-"+seed,null,s);}
}
