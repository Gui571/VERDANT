using System.Numerics;
namespace Verdant.Battle;
public static class SpecialValidation
{
    public static (int checks,List<string> failures) Run()
    {
        var failures=new List<string>();int checks=0;void Check(bool b,string label){checks++;if(!b)failures.Add(label);}
        FighterSpec Unit(int team,string ability="nova")=>new(team,"test","test",Role.Melee,new(1000,5,0,0,18,900),ability);
        BattleSimulation Create(string ability="nova",IReadOnlyList<Obstacle>? obstacles=null){var sim=new BattleSimulation(new(42,new[]{Unit(0,ability),Unit(1),Unit(0),Unit(2)},900,obstacles));for(int i=0;i<3;i++)sim.SetManual(i);sim.Place(0,new(100,90));sim.Place(1,new(120,90));sim.Place(2,new(110,100));sim.Place(3,new(128,95));sim.Fighters[0].SpecialCooldown=0;return sim;}
        var a=Create();Check(a.CommandSpecial(0),"cast accepted");Check(!a.CommandSpecial(0),"cooldown enforced");for(int i=0;i<13;i++)a.Step();Check(a.Fighters[1].Health<1000&&a.Fighters[3].Health<1000,"area reaches hostile teams");Check(a.Fighters[2].Health==1000,"area excludes allies");Check(a.Events.Any(e=>e.Kind=="effect:nova"),"release event");Check(a.Fighters[1].Statuses.ContainsKey(EffectKind.Slow),"slow applied");for(int i=0;i<80;i++)a.Step();Check(!a.Fighters[1].Statuses.ContainsKey(EffectKind.Slow),"status expires");
        var shield=Create("ice-wall");shield.CommandSpecial(0);for(int i=0;i<13;i++)shield.Step();Check(shield.Fighters[2].Shield>0&&shield.Fighters[1].Shield==0,"shield team filtering");
        var poison=Create("acid");poison.CommandSpecial(0);for(int i=0;i<13;i++)poison.Step();int health=poison.Fighters[1].Health;for(int i=0;i<35;i++)poison.Step();Check(poison.Fighters[1].Health<health,"DOT ticks");
        var walls=Create(obstacles:new[]{new Obstacle(108,75,4,30)});Check(!walls.LineOfSight(new(100,90),new(125,90)),"wall blocks LOS");Check(!walls.Place(2,new(110,80)),"wall blocks formation");walls.Projectiles.Add(new(0,new(100,90),new(5,0)));for(int i=0;i<4;i++)walls.Step();Check(walls.Projectiles.Count==0,"wall intercepts projectile");Check(walls.Fighters[1].Health==1000,"wall protects target");
        var shots=Create();shots.Place(3,new(145,90));shots.Projectiles.Add(new(0,new(100,90),new(5,0)));for(int i=0;i<5;i++)shots.Step();Check(shots.Fighters[1].Health<1000&&shots.Fighters[3].Health==1000,"first target interception");
        var root=Create();root.Fighters[0].Statuses[EffectKind.Root]=new(EffectKind.Root,1,60,1);var position=root.Fighters[0].Position;root.MoveOrder(0,new(70,90));for(int i=0;i<10;i++)root.Step();Check(root.Fighters[0].Position==position,"root blocks movement");
        var boss=new BattleSimulation(new(9,new[]{Unit(0) with{SecondAbility="rally"},Unit(1)},300));boss.SetManual(0);boss.SetManual(1);boss.Place(0,new(100,90));boss.Place(1,new(120,90));boss.Fighters[0].Health=400;boss.Step();Check(boss.Fighters[0].Phase==1,"boss phase");Check(boss.CommandSpecial(0),"boss second ability");for(int i=0;i<13;i++)boss.Step();Check(boss.Fighters.Length==3&&boss.Fighters[2].ParentId==0,"deterministic summon");
        var off=new BattleSimulation(Create().Spec with{Specials=false});off.Fighters[0].SpecialCooldown=0;Check(!off.CommandSpecial(0),"disabled specials reject manual casts");
        var interruption=new BattleSimulation(Create().Spec with{Seed=0});for(int i=0;i<3;i++)interruption.SetManual(i);interruption.Place(0,new(100,90));interruption.Place(1,new(115,90));interruption.Fighters[0].SpecialCooldown=0;interruption.Fighters[1].Cooldown=0;interruption.CommandSpecial(0);interruption.Step();Check(interruption.Events.Any(e=>e.Kind=="interrupt"&&e.Target==0),"deterministic interruption");
        for(int i=0;i<120;i++)shield.Step();Check(shield.Fighters[2].Shield==0,"shield expiration");
        var line=Create("acid");line.Place(3,new(145,130));line.CommandSpecial(0);for(int i=0;i<13;i++)line.Step();Check(!line.Events.Any(e=>e.Source==0&&e.Target==3&&e.Kind=="damage"),"line excludes side target");
        var live=Create();for(int i=0;i<70;i++)live.Step();live.CommandSpecial(0);live.Run();var replay=new BattleSimulation(live.Spec);var json=System.Text.Json.JsonSerializer.Serialize(live.Inputs);replay.QueueReplay(System.Text.Json.JsonSerializer.Deserialize<BattleInput[]>(json)!);replay.Run();Check(live.Events.SequenceEqual(replay.Events),"command log replay");
        foreach(var id in Abilities.All.Keys){var first=Create(id);var second=Create(id);first.CommandSpecial(0);second.CommandSpecial(0);first.Run();second.Run();Check(first.Events.SequenceEqual(second.Events),"ability deterministic "+id);}
        return(checks,failures);
    }
}
