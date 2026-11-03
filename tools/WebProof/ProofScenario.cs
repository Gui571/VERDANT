using System.Text.Json;
using Verdant;
using Verdant.Battle;
using Verdant.Forge;
namespace WebProof;

// Cenário compartilhado com o CLI: a apresentação web não reimplementa regras.
public sealed class ProofScenario
{
    public BattleSimulation Battle { get; }
    public ProofScenario()
    {
        var actors = new[]{ActorRecipe.FromHero(0),ActorRecipe.FromHero(3),ActorRecipe.FromHero(6),
            ActorRecipe.FromMonster(11,MonsterFamily.Beast),ActorRecipe.FromMonster(12,MonsterFamily.Dragon),ActorRecipe.FromMonster(13,MonsterFamily.Golem)};
        Battle = new(new BattleSpec(42,actors.Select((a,i)=>a.Fighter(i<3?0:1)).ToArray(),1800));
        Sprites = actors.Select(a=>{
            var frames = new List<byte[]>();
            for(int f=0;f<8;f++) frames.Add((byte[])(a.Hero is {} hero
                ? new ForgeRenderer(hero).Render(f/8.0,AdventurerMotion.Walk)
                : new MonsterRenderer(a.Monster!).Render(f/8.0,MonsterMotion.Walk)).Pixels.Clone());
            return new Sprite(a.Monster?.Size??32,frames.ToArray());
        }).ToArray();
    }
    public sealed record Sprite(int Size,byte[][] Frames);
    public Sprite[] Sprites {get;}
    public void Advance(int ticks){for(int i=0;i<Math.Clamp(ticks,0,1800)&&!Battle.Finished;i++)Battle.Step();}
    public string Trace()=>JsonSerializer.Serialize(Battle.Events);
    public object Snapshot()=>new {tick=Battle.Tick,finished=Battle.Finished,winner=Battle.Winner,events=Battle.Events.Count,
        units=Battle.Fighters.Select(f=>new {id=f.Id,parent=f.ParentId,team=f.Spec.Team,x=f.Position.X,y=f.Position.Y,hp=f.Health,max=f.Spec.Stats.Health,cooldown=f.SpecialCooldown}).ToArray()};
}
