using System.Numerics;
namespace Verdant.Battle;
public enum Role { Melee, Archer, Mage, Healer, Tank, Duelist, Charger, Spitter, Swarm }
public sealed record Stats(int Health,int Attack,int Defense,float Speed,float Range,int Cooldown);
public sealed record FighterSpec(int Team,string Key,string Group,Role Role,Stats Stats,string? AbilityId=null,string? SecondAbility=null,EffectKind? ElementEffect=null);
public sealed record BattleSpec(ulong Seed,IReadOnlyList<FighterSpec> Fighters,int MaxTicks=2700,IReadOnlyList<Obstacle>? Obstacles=null,bool Specials=true);
public sealed record BattleEvent(int Tick,int Source,int Target,string Kind,int Amount,float X,float Y);
public sealed class Fighter
{
    public int Id {get;} public FighterSpec Spec {get;} public Vector2 Position;
    public int Health,Cooldown,Invulnerable,LastHit=-100,LastAttack=-100,DeathTick=-1,Damage,Kills;
    public readonly Dictionary<EffectKind,StatusEffect> Statuses=new();
    public int Shield,ShieldUntil,SpecialCooldown=60,CastRemaining,CastTarget,LastSpecial=-100,Phase,Summons,ParentId=-1,SpawnTick=0,IdleTicks,MaxIdleTicks;
    public readonly AbilitySpec PrimaryAbility;public readonly AbilitySpec? PhaseAbility;
    public AbilitySpec? Casting;public bool Retreating,Retreated;
    public bool Moving; public int Target=-1; public bool Alive=>Health>0;
    public Fighter(int id,FighterSpec spec,Vector2 pos){Id=id;Spec=spec;Position=pos;Health=spec.Stats.Health;Cooldown=id%12;PrimaryAbility=Abilities.WithElement(Abilities.Get(spec.AbilityId,spec.Role),spec.ElementEffect);PhaseAbility=spec.SecondAbility==null?null:Abilities.WithElement(Abilities.Get(spec.SecondAbility,spec.Role),spec.ElementEffect);}
}
public sealed partial class BattleSimulation
{
    public const int Hz=30; public const int Width=320,Height=150;
    public BattleSpec Spec {get;} public Fighter[] Fighters {get;private set;}
    public List<BattleEvent> Events {get;}=new();
    public int Tick {get;private set;} public bool Finished {get;private set;} public int Winner {get;private set;}=-1;

    public BattleSimulation(BattleSpec spec)
    {
        if(spec.Fighters.Count<2||spec.Fighters.Count>256||spec.Fighters.Any(f=>f.Team<0||f.Stats.Health<=0||f.Stats.Cooldown<=0)||spec.MaxTicks<=0)throw new ArgumentException("Invalid battle");
        Spec=spec with{Fighters=Array.AsReadOnly(spec.Fighters.ToArray()),Obstacles=spec.Obstacles==null?null:Array.AsReadOnly(spec.Obstacles.ToArray())};var counts=new Dictionary<int,int>();
        Fighters=Spec.Fighters.Select((s,i)=>{int slot=counts.GetValueOrDefault(s.Team);counts[s.Team]=slot+1;int jitter=(int)((spec.Seed*6364136223846793005UL+(ulong)i*1442695040888963407UL)>>32)%7;var teams=spec.Fighters.Select(f=>f.Team).Distinct().OrderBy(x=>x).ToArray();int ti=Array.IndexOf(teams,s.Team);float angle=MathF.Tau*ti/teams.Length;var center=teams.Length==2?new Vector2(ti==0?35:285,96):new Vector2(160+MathF.Cos(angle)*120,98+MathF.Sin(angle)*30);var pos=Clamp(center+new Vector2((slot/6)*9*(ti%2==0?1:-1),-34+slot%6*13+jitter));return new Fighter(i,s,SafePosition(pos)){Cooldown=(i+jitter+7)%12};}).ToArray();
    }
    public void Step()
    {
        if(Finished)return;ApplyReplay();Tick++;
        foreach(var f in Fighters){f.Moving=false;if(f.Cooldown>0)f.Cooldown--;if(f.Invulnerable>0)f.Invulnerable--;}
        TickSpecials();
        foreach(var f in Fighters){if(!f.Alive||Controlled(f))continue;var st=f.Spec.Stats;
            Fighter? target=null;float score=float.MaxValue;
            foreach(var t in Fighters){if(!t.Alive||t.Retreated||t.Spec.Team==f.Spec.Team)continue;float d=Vector2.DistanceSquared(f.Position,t.Position);if((t.Spec.Role==Role.Tank||t.Statuses.ContainsKey(EffectKind.Taunt))&&d<3600)d*=.55f;if(d<score){score=d;target=t;}}
            if(target==null)continue;f.Target=target.Id;
            if(Spec.Specials&&!manualTeams.Contains(f.Spec.Team)&&BeginCast(f,false))continue;
            if(f.Spec.Role==Role.Healer&&f.Cooldown==0){var ally=Fighters.Where(a=>a.Alive&&a.Spec.Team==f.Spec.Team&&a.Health<a.Spec.Stats.Health&&Vector2.DistanceSquared(a.Position,f.Position)<6400).OrderBy(a=>(float)a.Health/a.Spec.Stats.Health).FirstOrDefault();if(ally!=null){int heal=Math.Min(st.Attack,ally.Spec.Stats.Health-ally.Health);ally.Health+=heal;f.Cooldown=st.Cooldown;f.LastAttack=Tick;Events.Add(new(Tick,f.Id,ally.Id,"heal",heal,ally.Position.X,ally.Position.Y));continue;}}
            var delta=target.Position-f.Position;float dist=delta.Length();
            bool ranged=f.Spec.Role is Role.Archer or Role.Mage or Role.Spitter;
            if(dist>st.Range||!LineOfSight(f.Position,target.Position)){Move(f,delta,st.Speed/Hz*(f.Spec.Role==Role.Charger&&dist>50?1.35f:1));}
            else if(ranged&&dist<20&&f.Health<st.Health/2){Move(f,-delta,st.Speed/Hz*.7f);}
            if(dist<=st.Range&&f.Cooldown==0&&LineOfSight(f.Position,target.Position)){f.Cooldown=st.Cooldown;f.LastAttack=Tick;Events.Add(new(Tick,f.Id,target.Id,ranged?"projectile":"attack",0,target.Position.X,target.Position.Y));if(ranged)Projectiles.Add(new(f.Id,f.Position,Vector2.Normalize(delta)*5));else Resolve(f,target);}
        }
        // Separação simétrica: ordem de ids estável, sem RNG dependente do framerate.
        for(int pass=0;pass<2;pass++)for(int i=0;i<Fighters.Length;i++)for(int j=i+1;j<Fighters.Length;j++){
            var a=Fighters[i];var b=Fighters[j];if(!a.Alive||!b.Alive||a.Retreated||b.Retreated)continue;var d=b.Position-a.Position;float length=d.Length();if(length<11){var axis=length<.001f?new Vector2(0,i%2==0?1:-1):d/length;float push=(11-length)/2;var ap=Clamp(a.Position-axis*push);var bp=Clamp(b.Position+axis*push);if(!Blocked(ap,4))a.Position=ap;if(!Blocked(bp,4))b.Position=bp;}}
        foreach(var unit in Fighters)if(unit.Alive&&!unit.Retreated){if(unit.Moving||unit.CastRemaining>0||unit.LastAttack==Tick||unit.LastSpecial==Tick||unit.Statuses.ContainsKey(EffectKind.Root)||unit.Statuses.ContainsKey(EffectKind.Stun))unit.IdleTicks=0;else unit.IdleTicks++;unit.MaxIdleTicks=Math.Max(unit.MaxIdleTicks,unit.IdleTicks);}
        int team=-1;bool mixed=false;foreach(var f in Fighters.Where(f=>f.Alive&&!f.Retreated)){if(team<0)team=f.Spec.Team;else if(team!=f.Spec.Team)mixed=true;}
        if(!mixed||Tick>=Spec.MaxTicks){Finished=true;Winner=mixed?-1:team;Events.Add(new(Tick,-1,-1,"end",Winner,0,0));}
    }
    static Vector2 Clamp(Vector2 p)=>Vector2.Clamp(p,new(20,54),new(Width-20,Height-8));
    void Move(Fighter f,Vector2 direction,float distance){if(direction.LengthSquared()<.0001f||f.Statuses.ContainsKey(EffectKind.Root)||f.Statuses.ContainsKey(EffectKind.Stun))return;if(f.Statuses.TryGetValue(EffectKind.Slow,out var slow))distance*=Math.Max(.2f,1-slow.Power/100f);var axis=Vector2.Normalize(direction);int steps=Math.Max(1,(int)MathF.Ceiling(distance));for(int step=0;step<steps;step++){var next=Clamp(f.Position+axis*(distance/steps));if(Blocked(next,5)){var side=new Vector2(-axis.Y,axis.X);next=Clamp(f.Position+side*(distance/steps));if(Blocked(next,5))next=Clamp(f.Position-side*(distance/steps));}if(!Blocked(next,5)&&next!=f.Position){f.Position=next;f.Moving=true;}}}
    void Resolve(Fighter a,Fighter b)
    {
        if(!b.Alive)return;
        Hit(a,b,a.Spec.Stats.Attack);
        if(a.Spec.Role==Role.Mage)foreach(var other in Fighters)if(other!=b&&other.Alive&&other.Spec.Team!=a.Spec.Team&&Vector2.DistanceSquared(other.Position,b.Position)<400)Hit(a,other,a.Spec.Stats.Attack/2);
    }
    void Hit(Fighter a,Fighter b,int power,bool special=false)
    {
        if(!b.Alive||b.Retreated||(!special&&b.Invulnerable>0))return;
        if(b.Statuses.TryGetValue(EffectKind.Dodge,out var dodge)&&((Spec.Seed+(ulong)Tick*31+(ulong)b.Id*7)%100)<(ulong)dodge.Power)return;
        if(a.Statuses.TryGetValue(EffectKind.Buff,out var buff))power=power*(100+buff.Power)/100;if(a.Statuses.TryGetValue(EffectKind.Weaken,out var weak))power=power*Math.Max(10,100-weak.Power)/100;
        int incoming=Math.Max(1,power-b.Spec.Stats.Defense);int absorbed=Math.Min(b.Shield,incoming);b.Shield-=absorbed;incoming-=absorbed;if(incoming<=0)return;
        if(b.CastRemaining>0&&b.Casting is {} cast&&(Spec.Seed+(ulong)Tick+(ulong)b.Id*13)%100<(ulong)cast.InterruptPercent){b.CastRemaining=0;b.Casting=null;b.LastSpecial=-100;Events.Add(new(Tick,a.Id,b.Id,"interrupt",0,b.Position.X,b.Position.Y));}
        int damage=Math.Min(incoming,b.Health);b.Health-=damage;a.Damage+=damage;b.Invulnerable=4;b.LastHit=Tick;
        var d=b.Position-a.Position;if(d.LengthSquared()>0)Move(b,d,2);
        Events.Add(new(Tick,a.Id,b.Id,"damage",damage,b.Position.X,b.Position.Y));if(!b.Alive){b.DeathTick=Tick;b.Statuses.Clear();b.CastRemaining=0;a.Kills++;Events.Add(new(Tick,a.Id,b.Id,"death",0,b.Position.X,b.Position.Y));}
    }
    public void Run(){while(!Finished)Step();}
}
