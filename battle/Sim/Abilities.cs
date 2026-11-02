using System.Numerics;
namespace Verdant.Battle;
public enum TargetShape { Single, Line, Cone, Circle, Allies, Self }
public enum EffectKind { Damage, Heal, Shield, Stun, Slow, Push, Dash, Taunt, Poison, Burn, Bleed, Root, Drain, Buff, Weaken, Summon, Regen, Dodge }
public sealed record AbilityEffect(EffectKind Kind,int Power,int Duration=90);
public sealed record AbilitySpec(string Id,string Name,int Cooldown,int CastTicks,TargetShape Shape,float Radius,IReadOnlyList<AbilityEffect> Effects,string Trigger="enemy",int InterruptPercent=30,bool ResetOnKill=false);
public sealed record StatusEffect(EffectKind Kind,int Power,int Until,int Source,int Stacks=1);
public sealed record Obstacle(float X,float Y,float Width,float Height)
{
    public bool Contains(Vector2 p,float pad=0)=>p.X>=X-pad&&p.Y>=Y-pad&&p.X<=X+Width+pad&&p.Y<=Y+Height+pad;
}
public static class Abilities
{
    static AbilitySpec A(string id,string name,TargetShape shape,float radius,params AbilityEffect[] fx)=>new(id,name,210,12,shape,radius,Array.AsReadOnly(fx));
    public static readonly IReadOnlyDictionary<string,AbilitySpec> All=new Dictionary<string,AbilitySpec>{
        ["ice-wall"]=A("ice-wall","Muralha de gelo",TargetShape.Allies,65,new(EffectKind.Shield,28,120),new(EffectKind.Taunt,1,90)),
        ["lightning-cut"]=A("lightning-cut","Golpe-relâmpago",TargetShape.Single,70,new(EffectKind.Dash,24),new(EffectKind.Damage,27)) with{ResetOnKill=true},
        ["arrow-rain"]=A("arrow-rain","Chuva de flechas",TargetShape.Circle,28,new(EffectKind.Damage,22),new(EffectKind.Bleed,3,90)),
        ["nova"]=A("nova","Nova astral",TargetShape.Circle,30,new(EffectKind.Damage,25),new(EffectKind.Slow,35,75)),
        ["quake"]=A("quake","Tremor",TargetShape.Self,35,new(EffectKind.Damage,18),new(EffectKind.Stun,1,24),new(EffectKind.Taunt,1,90)),
        ["dagger-dance"]=A("dagger-dance","Dança das adagas",TargetShape.Self,26,new(EffectKind.Damage,10),new(EffectKind.Damage,10),new(EffectKind.Damage,10),new(EffectKind.Dodge,40,60)),
        ["blessing"]=A("blessing","Bênção",TargetShape.Allies,70,new(EffectKind.Heal,26),new(EffectKind.Shield,16,90)) with{Trigger="wounded"},
        ["acid"]=A("acid","Ácido vivo",TargetShape.Line,65,new(EffectKind.Damage,18),new(EffectKind.Poison,3,120)),
        ["roar"]=A("roar","Rugido de caça",TargetShape.Cone,55,new(EffectKind.Weaken,25,120),new(EffectKind.Dash,16),new(EffectKind.Damage,16)),
        ["breath"]=A("breath","Sopro ancestral",TargetShape.Cone,65,new(EffectKind.Damage,23),new(EffectKind.Burn,3,90)),
        ["web"]=A("web","Teia",TargetShape.Circle,24,new(EffectKind.Root,1,60),new(EffectKind.Damage,15)),
        ["dive"]=A("dive","Mergulho",TargetShape.Single,80,new(EffectKind.Dash,30),new(EffectKind.Damage,26),new(EffectKind.Push,7)),
        ["constrict"]=A("constrict","Constrição",TargetShape.Single,40,new(EffectKind.Root,1,45),new(EffectKind.Poison,4,90),new(EffectKind.Damage,20)),
        ["water-jet"]=A("water-jet","Jato d'água",TargetShape.Line,75,new(EffectKind.Damage,20),new(EffectKind.Push,12)),
        ["roots"]=A("roots","Raízes vivas",TargetShape.Self,38,new(EffectKind.Root,1,60),new(EffectKind.Damage,12),new(EffectKind.Regen,5,120)),
        ["rally"]=A("rally","Chamado da alcateia",TargetShape.Self,30,new(EffectKind.Summon,1),new(EffectKind.Buff,20,150))
    };
    public static AbilitySpec WithElement(AbilitySpec a,EffectKind? element)=>element is {} kind?a with{Effects=Array.AsReadOnly(a.Effects.Append(new AbilityEffect(kind,kind==EffectKind.Slow?30:kind==EffectKind.Drain?8:3,60)).ToArray())}:a;
    public static readonly IReadOnlyDictionary<Role,string> RoleSignatures=new Dictionary<Role,string>{{Role.Tank,"quake"},{Role.Archer,"arrow-rain"},{Role.Mage,"nova"},{Role.Healer,"blessing"},{Role.Duelist,"dagger-dance"},{Role.Charger,"roar"},{Role.Spitter,"acid"},{Role.Swarm,"web"},{Role.Melee,"lightning-cut"}};
    public static readonly IReadOnlyDictionary<string,string> HeroSignatures=new Dictionary<string,string>{{"bram","ice-wall"},{"nix","lightning-cut"},{"rowan","arrow-rain"},{"iria","nova"},{"boro","quake"},{"suri","dagger-dance"},{"mira","blessing"}};
    public static readonly IReadOnlyDictionary<string,string> MonsterSignatures=new Dictionary<string,string>{{"Slime","acid"},{"Beast","roar"},{"Golem","quake"},{"Dragon","breath"},{"Arthropod","web"},{"Winged","dive"},{"Serpent","constrict"},{"Aquatic","water-jet"},{"Plant","roots"}};
    public static readonly IReadOnlyDictionary<EffectKind,string> EffectNames=new Dictionary<EffectKind,string>{{EffectKind.Damage,"Dano"},{EffectKind.Heal,"Cura"},{EffectKind.Shield,"Escudo"},{EffectKind.Stun,"Atordoamento"},{EffectKind.Slow,"Lentidão"},{EffectKind.Push,"Empurrão"},{EffectKind.Dash,"Investida"},{EffectKind.Taunt,"Provocação"},{EffectKind.Poison,"Veneno"},{EffectKind.Burn,"Queimadura"},{EffectKind.Bleed,"Sangramento"},{EffectKind.Root,"Imobilização"},{EffectKind.Drain,"Drenar"},{EffectKind.Buff,"Vigor"},{EffectKind.Weaken,"Fraqueza"},{EffectKind.Summon,"Invocação"},{EffectKind.Regen,"Regeneração"},{EffectKind.Dodge,"Esquiva"}};
    public static string Describe(AbilityEffect effect){string value=effect.Kind is EffectKind.Stun or EffectKind.Root or EffectKind.Taunt?$"{effect.Duration/30.0:F1}s":effect.Power.ToString();if(effect.Kind is EffectKind.Slow or EffectKind.Buff or EffectKind.Weaken or EffectKind.Dodge)value+="%";if(effect.Kind is EffectKind.Poison or EffectKind.Burn or EffectKind.Bleed or EffectKind.Regen)value+="/s";return EffectNames[effect.Kind]+" "+value;}
    public static string ForRole(Role role)=>RoleSignatures[role];
    public static AbilitySpec Get(string? id,Role role)=>All[id??ForRole(role)];
}
