namespace Verdant.Forge;
public enum MonsterFamily { Slime, Beast, Golem, Dragon, Arthropod, Winged, Serpent, Aquatic, Plant }
public enum Element { Fire, Ice, Plant, Shadow, Poison }
public enum Rank { Minion, Elite, Boss }
public enum MonsterMotion { Idle, Walk, Run, Attack, Hurt, Death, Special, Cast }
public sealed record MonsterSpec(ulong Seed, MonsterFamily Family, Element Element, Rank Rank, int Mass, int Head, int Appendages, int Pattern, int Tint,string? SpecialId=null)
{
    public int Size => Rank == Rank.Boss ? 48 : 32;
    public string Id => $"{Family.ToString().ToLowerInvariant()}-{Seed}";
    public void Validate() { if (!Enum.IsDefined(Family)||!Enum.IsDefined(Element)||!Enum.IsDefined(Rank)||new[]{Mass,Head,Appendages,Pattern,Tint}.Any(v=>v<0||v>3)) throw new ArgumentException("Receita de monstro inválida"); }
}
public static class MonsterGenerator
{
    public static MonsterSpec Generate(ulong seed, MonsterFamily? family=null)
    {
        var r=new Rng(seed); return new(seed, family??(MonsterFamily)r.Int(0,9),(Element)r.Int(0,5),(Rank)(r.Int(0,10)==0?2:r.Int(0,4)==0?1:0),r.Int(0,4),r.Int(0,4),r.Int(0,4),r.Int(0,4),r.Int(0,4));
    }
    public static MonsterSpec Mutate(MonsterSpec a,ulong seed) { var b=Generate(seed,a.Family);return a with{Seed=seed,Mass=b.Mass,Head=b.Head,Appendages=b.Appendages,Pattern=b.Pattern,Tint=b.Tint}; }
    public static MonsterSpec Cross(MonsterSpec a,MonsterSpec b,ulong seed)
    {
        if(a.Family!=b.Family)throw new ArgumentException("Cruze pais da mesma família");var r=new Rng(seed);
        return a with{Seed=seed,Mass=r.Chance(.5f)?a.Mass:b.Mass,Head=r.Chance(.5f)?a.Head:b.Head,Appendages=r.Chance(.5f)?a.Appendages:b.Appendages,Pattern=r.Chance(.5f)?a.Pattern:b.Pattern,Element=r.Chance(.5f)?a.Element:b.Element,Tint=r.Chance(.5f)?a.Tint:b.Tint};
    }
    public static ForgePalette Palette(MonsterSpec s)
    {
        var colors=new[]{"df8755","79bed1","94b568","a187bb","a8bd64"};var ramp=Ramp.From(new Godot.Color(colors[(int)s.Element]));
        var (h,sat,v)=Hsv.From(ramp.M);ramp=Ramp.From(Hsv.To(h+(s.Tint-1)*.015f,sat,v-(s.Tint*.025f)));
        return ForgePalette.Make(ramp,ForgeLook.Blue,Ramp.Of("896348","d6ad70","efdcab"),ForgeLook.Leather);
    }
}
