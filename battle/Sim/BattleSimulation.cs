using System.Numerics;
namespace Verdant.Battle;
public enum Role { Melee, Archer, Mage, Healer, Tank, Duelist, Charger, Spitter, Swarm }
public sealed record Stats(int Health,int Attack,int Defense,float Speed,float Range,int Cooldown);
public sealed record FighterSpec(int Team,string Key,string Group,Role Role,Stats Stats);
public sealed record BattleSpec(ulong Seed,IReadOnlyList<FighterSpec> Fighters,int MaxTicks=2700);
public sealed record BattleEvent(int Tick,int Source,int Target,string Kind,int Amount,float X,float Y);
public sealed class Fighter
{
    public int Id {get;} public FighterSpec Spec {get;} public Vector2 Position;
    public int Health,Cooldown,Invulnerable,LastHit=-100,LastAttack=-100,DeathTick=-1,Damage,Kills;
    public bool Moving; public int Target=-1; public bool Alive=>Health>0;
    public Fighter(int id,FighterSpec spec,Vector2 pos){Id=id;Spec=spec;Position=pos;Health=spec.Stats.Health;Cooldown=id%12;}
}
public sealed class BattleSimulation
{
    public const int Hz=30; public const int Width=320,Height=150;
    public BattleSpec Spec {get;} public Fighter[] Fighters {get;}
    public List<BattleEvent> Events {get;}=new();
    public int Tick {get;private set;} public bool Finished {get;private set;} public int Winner {get;private set;}=-1;
    readonly List<(int Due,int Source,int Target)> shots=new();
    public BattleSimulation(BattleSpec spec)
    {
        if(spec.Fighters.Count<2||spec.Fighters.Count>40||spec.Fighters.Any(f=>f.Team<0||f.Stats.Health<=0||f.Stats.Cooldown<=0)||spec.MaxTicks<=0)throw new ArgumentException("Invalid battle");
        Spec=spec with{Fighters=Array.AsReadOnly(spec.Fighters.ToArray())};var counts=new Dictionary<int,int>();
        Fighters=Spec.Fighters.Select((s,i)=>{int slot=counts.GetValueOrDefault(s.Team);counts[s.Team]=slot+1;int jitter=(int)((spec.Seed*6364136223846793005UL+(ulong)i*1442695040888963407UL)>>32)%7;return new Fighter(i,s,new(s.Team==0?35+(slot/6)*18:285-(slot/6)*18,55+slot%6*15+jitter)){Cooldown=(i+jitter+7)%12};}).ToArray();
    }
    public void Step()
    {
        if(Finished)return;Tick++;
        foreach(var f in Fighters){f.Moving=false;if(f.Cooldown>0)f.Cooldown--;if(f.Invulnerable>0)f.Invulnerable--;}
        for(int i=shots.Count-1;i>=0;i--)if(shots[i].Due<=Tick){var s=shots[i];Resolve(Fighters[s.Source],Fighters[s.Target]);shots.RemoveAt(i);}
        foreach(var f in Fighters){if(!f.Alive)continue;var st=f.Spec.Stats;
            Fighter? target=null;float score=float.MaxValue;
            foreach(var t in Fighters){if(!t.Alive||t.Spec.Team==f.Spec.Team)continue;float d=Vector2.DistanceSquared(f.Position,t.Position);if(t.Spec.Role==Role.Tank&&d<3600)d*=.55f;if(d<score){score=d;target=t;}}
            if(target==null)continue;f.Target=target.Id;
            if(f.Spec.Role==Role.Healer&&f.Cooldown==0){var ally=Fighters.Where(a=>a.Alive&&a.Spec.Team==f.Spec.Team&&a.Health<a.Spec.Stats.Health&&Vector2.DistanceSquared(a.Position,f.Position)<6400).OrderBy(a=>(float)a.Health/a.Spec.Stats.Health).FirstOrDefault();if(ally!=null){int heal=Math.Min(st.Attack,ally.Spec.Stats.Health-ally.Health);ally.Health+=heal;f.Cooldown=st.Cooldown;f.LastAttack=Tick;Events.Add(new(Tick,f.Id,ally.Id,"heal",heal,ally.Position.X,ally.Position.Y));continue;}}
            var delta=target.Position-f.Position;float dist=delta.Length();
            bool ranged=f.Spec.Role is Role.Archer or Role.Mage or Role.Spitter;
            if(dist>st.Range){Move(f,delta,st.Speed/Hz*(f.Spec.Role==Role.Charger&&dist>50?1.35f:1));}
            else if(ranged&&dist<20&&f.Health<st.Health/2){Move(f,-delta,st.Speed/Hz*.7f);}
            if(dist<=st.Range&&f.Cooldown==0){f.Cooldown=st.Cooldown;f.LastAttack=Tick;Events.Add(new(Tick,f.Id,target.Id,ranged?"projectile":"attack",0,target.Position.X,target.Position.Y));if(ranged)shots.Add((Tick+8,f.Id,target.Id));else Resolve(f,target);}
        }
        // Separação simétrica: ordem de ids estável, sem RNG dependente do framerate.
        for(int pass=0;pass<2;pass++)for(int i=0;i<Fighters.Length;i++)for(int j=i+1;j<Fighters.Length;j++){
            var a=Fighters[i];var b=Fighters[j];if(!a.Alive||!b.Alive)continue;var d=b.Position-a.Position;float length=d.Length();if(length<11){var axis=length<.001f?new Vector2(0,i%2==0?1:-1):d/length;float push=(11-length)/2;a.Position=Clamp(a.Position-axis*push);b.Position=Clamp(b.Position+axis*push);}}
        int team=-1;bool mixed=false;foreach(var f in Fighters.Where(f=>f.Alive)){if(team<0)team=f.Spec.Team;else if(team!=f.Spec.Team)mixed=true;}
        if(!mixed||Tick>=Spec.MaxTicks){Finished=true;Winner=mixed?-1:team;Events.Add(new(Tick,-1,-1,"end",Winner,0,0));}
    }
    static Vector2 Clamp(Vector2 p)=>Vector2.Clamp(p,new(20,54),new(Width-20,Height-8));
    static void Move(Fighter f,Vector2 direction,float distance){if(direction.LengthSquared()<.0001f)return;f.Position=Clamp(f.Position+Vector2.Normalize(direction)*distance);f.Moving=true;}
    void Resolve(Fighter a,Fighter b)
    {
        if(!b.Alive)return;
        Hit(a,b,a.Spec.Stats.Attack);
        if(a.Spec.Role==Role.Mage)foreach(var other in Fighters)if(other!=b&&other.Alive&&other.Spec.Team!=a.Spec.Team&&Vector2.DistanceSquared(other.Position,b.Position)<400)Hit(a,other,a.Spec.Stats.Attack/2);
    }
    void Hit(Fighter a,Fighter b,int power)
    {
        if(b.Invulnerable>0)return;int damage=Math.Max(1,power-b.Spec.Stats.Defense);damage=Math.Min(damage,b.Health);b.Health-=damage;a.Damage+=damage;b.Invulnerable=4;b.LastHit=Tick;
        var d=b.Position-a.Position;if(d.LengthSquared()>0)b.Position=Clamp(b.Position+Vector2.Normalize(d)*2);
        Events.Add(new(Tick,a.Id,b.Id,"damage",damage,b.Position.X,b.Position.Y));if(!b.Alive){b.DeathTick=Tick;a.Kills++;Events.Add(new(Tick,a.Id,b.Id,"death",0,b.Position.X,b.Position.Y));}
    }
    public void Run(){while(!Finished)Step();}
}
