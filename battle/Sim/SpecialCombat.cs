using System.Numerics;
namespace Verdant.Battle;
public sealed class Projectile
{
    public int Source,Life=45; public Vector2 Position,Velocity;
    public Projectile(int source,Vector2 pos,Vector2 velocity){Source=source;Position=pos;Velocity=velocity;}
}
public sealed partial class BattleSimulation
{
    public List<Projectile> Projectiles {get;}=new();
    readonly Dictionary<int,Vector2> orders=new();
    readonly HashSet<int> manualTeams=new();
    public void SetManual(int team,bool manual=true){RecordInput("manual",team,manual?1:0,0);if(manual)manualTeams.Add(team);else manualTeams.Remove(team);}
    public bool MoveOrder(int id,Vector2 point){if(id<0||id>=Fighters.Length||!Fighters[id].Alive||Blocked(Clamp(point),5))return false;orders[id]=Clamp(point);RecordInput("move",id,point.X,point.Y);return true;}
    public void Retreat(int id){if(id<0||id>=Fighters.Length)return;Fighters[id].Retreating=true;orders[id]=new(Fighters[id].Spec.Team%2==0?22:298,125);RecordInput("retreat",id,0,0);}
    public bool CommandSpecial(int id){bool accepted=id>=0&&id<Fighters.Length&&BeginCast(Fighters[id],true);if(accepted)RecordInput("special",id,0,0);return accepted;}
    public bool Place(int id,Vector2 point){if(Tick!=0||id<0||id>=Fighters.Length||Blocked(point,5))return false;Fighters[id].Position=Clamp(point);RecordInput("place",id,point.X,point.Y);return true;}
    Vector2 SafePosition(Vector2 p){p=Clamp(p);if(!Blocked(p,5))return p;for(int r=6;r<=96;r+=6)for(int k=0;k<8;k++){float a=k*MathF.Tau/8;var candidate=Clamp(p+new Vector2(MathF.Cos(a),MathF.Sin(a))*r);if(!Blocked(candidate,5))return candidate;}throw new ArgumentException("Sem espaço livre para uma unidade");}
    bool Blocked(Vector2 point,float pad=0){var obstacles=Spec.Obstacles;if(obstacles!=null)for(int i=0;i<obstacles.Count;i++)if(obstacles[i].Contains(point,pad))return true;return false;}
    static bool ClipAxis(float start,float delta,float min,float max,ref float enter,ref float leave){if(Math.Abs(delta)<.00001f)return start>=min&&start<=max;float a=(min-start)/delta,b=(max-start)/delta;if(a>b)(a,b)=(b,a);enter=Math.Max(enter,a);leave=Math.Min(leave,b);return enter<=leave;}
    public bool LineOfSight(Vector2 a,Vector2 b){var obstacles=Spec.Obstacles;if(obstacles==null)return true;var delta=b-a;for(int i=0;i<obstacles.Count;i++){var o=obstacles[i];float enter=0,leave=1;if(ClipAxis(a.X,delta.X,o.X,o.X+o.Width,ref enter,ref leave)&&ClipAxis(a.Y,delta.Y,o.Y,o.Y+o.Height,ref enter,ref leave))return false;}return true;}
    public AbilitySpec Ability(Fighter f)=>f.Phase>0&&f.PhaseAbility!=null?f.PhaseAbility:f.PrimaryAbility;
    bool BeginCast(Fighter f,bool command)
    {
        if(!Spec.Specials||!f.Alive||f.Retreated||f.Retreating||f.SpecialCooldown>0||f.CastRemaining>0||f.Statuses.ContainsKey(EffectKind.Stun))return false;
        var a=Ability(f);Fighter? target=null;float best=float.MaxValue;int enemies=0;
        foreach(var t in Fighters)if(t.Alive&&!t.Retreated&&t.Spec.Team!=f.Spec.Team){enemies++;float distance=Vector2.DistanceSquared(t.Position,f.Position);if(distance<best){best=distance;target=t;}}
        if(target==null)return false;
        if(Vector2.Distance(f.Position,target.Position)>(a.Shape is TargetShape.Circle or TargetShape.Allies?80:a.Radius))return false;
        if(!LineOfSight(f.Position,target.Position)&&a.Shape!=TargetShape.Allies)return false;
        if(!command&&a.Trigger=="wounded"){bool wounded=false;foreach(var t in Fighters)if(t.Alive&&t.Spec.Team==f.Spec.Team&&t.Health<t.Spec.Stats.Health*.4f)wounded=true;if(!wounded)return false;}
        if(!command&&a.Shape==TargetShape.Circle&&enemies>=3){int nearby=0;foreach(var t in Fighters)if(t.Alive&&!t.Retreated&&t.Spec.Team!=f.Spec.Team&&Vector2.DistanceSquared(t.Position,target.Position)<a.Radius*a.Radius)nearby++;if(nearby<3)return false;}
        f.CastTarget=target.Id;f.CastRemaining=a.CastTicks;f.SpecialCooldown=a.Cooldown;f.LastSpecial=Tick;f.Casting=a;
        Events.Add(new(Tick,f.Id,target.Id,"cast:"+a.Id,0,target.Position.X,target.Position.Y));return true;
    }
    void TickSpecials()
    {
        foreach(var f in Fighters){if(!f.Alive||f.Retreated)continue;if(f.SpecialCooldown>0)f.SpecialCooldown--;
            if(f.Spec.SecondAbility!=null&&f.Phase==0&&f.Health*2<f.Spec.Stats.Health){f.Phase=1;f.SpecialCooldown=0;Events.Add(new(Tick,f.Id,f.Id,"phase",1,f.Position.X,f.Position.Y));}
            foreach(var pair in f.Statuses.ToArray()){
                if(!f.Alive)break;var s=pair.Value;if(s.Until<=Tick){f.Statuses.Remove(pair.Key);Events.Add(new(Tick,s.Source,f.Id,"expire:"+s.Kind,0,f.Position.X,f.Position.Y));continue;}
                if(Tick%30==0&&s.Source<Fighters.Length){if(s.Kind is EffectKind.Poison or EffectKind.Burn or EffectKind.Bleed)Hit(Fighters[s.Source],f,s.Power*s.Stacks+f.Spec.Stats.Defense,true);if(s.Kind==EffectKind.Regen)Heal(f,f,s.Power);}
            }
            if(f.ShieldUntil<=Tick)f.Shield=0;
            if(f.CastRemaining>0){f.CastRemaining--;if(f.CastRemaining==0&&f.Casting is {} a){ResolveAbility(f,a);f.Casting=null;}}
        }
        for(int i=Projectiles.Count-1;i>=0;i--){var p=Projectiles[i];bool done=false;for(int sub=0;sub<4&&!done;sub++){p.Position+=p.Velocity/4;if(Blocked(p.Position)){done=true;break;}foreach(var t in Fighters)if(t.Alive&&!t.Retreated&&t.Spec.Team!=Fighters[p.Source].Spec.Team&&Vector2.DistanceSquared(t.Position,p.Position)<36){Resolve(Fighters[p.Source],t);done=true;break;}}if(done||--p.Life<=0)Projectiles.RemoveAt(i);}
    }
    void ResolveAbility(Fighter caster,AbilitySpec a)
    {
        if(!caster.Alive)return;int killsBefore=caster.Kills;var target=Fighters[Math.Clamp(caster.CastTarget,0,Fighters.Length-1)];var center=a.Shape is TargetShape.Self or TargetShape.Allies?caster.Position:target.Position;var direction=target.Position-caster.Position;if(direction.LengthSquared()<.01f)direction=Vector2.UnitX;direction=Vector2.Normalize(direction);
        Events.Add(new(Tick,caster.Id,target.Id,"effect:"+a.Id,0,center.X,center.Y));
        foreach(var fx in a.Effects){
            if(fx.Kind==EffectKind.Dash){Move(caster,target.Position-caster.Position,fx.Power);continue;}
            if(fx.Kind==EffectKind.Summon){if(caster.Summons>=2||Fighters.Length>=256)continue;var stats=caster.Spec.Stats with{Health=40,Attack=8,Defense=0};var summon=new Fighter(Fighters.Length,caster.Spec with{Key=caster.Spec.Key+"-summon",Stats=stats,AbilityId="acid",SecondAbility=null},SafePosition(caster.Position+new Vector2(10,0))){ParentId=caster.Id,SpawnTick=Tick,SpecialCooldown=99999};Fighters=Fighters.Append(summon).ToArray();caster.Summons++;Events.Add(new(Tick,caster.Id,summon.Id,"summon",1,summon.Position.X,summon.Position.Y));continue;}
            bool beneficial=fx.Kind is EffectKind.Heal or EffectKind.Shield;
            bool self=fx.Kind is EffectKind.Taunt or EffectKind.Regen or EffectKind.Buff or EffectKind.Dodge;
            foreach(var t in Fighters){if(!t.Alive||t.Retreated)continue;if(self&&t!=caster)continue;if(!self&&(beneficial?t.Spec.Team!=caster.Spec.Team:t.Spec.Team==caster.Spec.Team))continue;
                var d=t.Position-caster.Position;float along=Vector2.Dot(d,direction);float cross=Math.Abs(d.X*direction.Y-d.Y*direction.X);
                bool inside=self||a.Shape switch{TargetShape.Single=>t.Id==target.Id,TargetShape.Line=>along>=0&&along<=a.Radius&&cross<10,TargetShape.Cone=>along>=0&&along<=a.Radius&&cross<along*.7f+4,_=>Vector2.DistanceSquared(t.Position,center)<=a.Radius*a.Radius};
                if(!inside||!LineOfSight(caster.Position,t.Position))continue;
                switch(fx.Kind){case EffectKind.Damage:Hit(caster,t,fx.Power,true);break;case EffectKind.Drain:int before=t.Health;Hit(caster,t,fx.Power,true);Heal(caster,caster,(before-t.Health)/2);break;case EffectKind.Heal:Heal(caster,t,fx.Power);break;case EffectKind.Shield:t.Shield=Math.Min(60,t.Shield+fx.Power);t.ShieldUntil=Tick+fx.Duration;break;case EffectKind.Push:Move(t,t.Position-caster.Position,fx.Power);break;default:if(fx.Kind==EffectKind.Stun&&t.CastRemaining>0){t.CastRemaining=0;t.Casting=null;t.LastSpecial=-100;Events.Add(new(Tick,caster.Id,t.Id,"interrupt",0,t.Position.X,t.Position.Y));}var old=t.Statuses.GetValueOrDefault(fx.Kind);int stacks=fx.Kind is EffectKind.Poison or EffectKind.Burn or EffectKind.Bleed?Math.Min(3,(old?.Stacks??0)+1):1;t.Statuses[fx.Kind]=new(fx.Kind,Math.Max(fx.Power,old?.Power??0),Tick+fx.Duration,caster.Id,stacks);Events.Add(new(Tick,caster.Id,t.Id,"status:"+fx.Kind,fx.Duration,t.Position.X,t.Position.Y));break;}
            }
        }
        if(caster.Kills>killsBefore&&a.ResetOnKill)caster.SpecialCooldown=0;
    }
    void Heal(Fighter source,Fighter target,int amount){if(!target.Alive)return;int healed=Math.Min(amount,target.Spec.Stats.Health-target.Health);if(healed<=0)return;target.Health+=healed;Events.Add(new(Tick,source.Id,target.Id,"heal",healed,target.Position.X,target.Position.Y));}
    bool Controlled(Fighter f)
    {
        if(f.Retreated)return true;if(f.CastRemaining>0||f.Statuses.ContainsKey(EffectKind.Stun))return true;
        if(orders.TryGetValue(f.Id,out var p)){if(Vector2.DistanceSquared(p,f.Position)<25){orders.Remove(f.Id);if(f.Retreating){f.Retreated=true;Events.Add(new(Tick,f.Id,f.Id,"retreat",0,p.X,p.Y));}return f.Retreating;}Move(f,p-f.Position,f.Spec.Stats.Speed/Hz);return true;}return false;
    }
}
