namespace Verdant.Battle;
public static class BattleValidation
{
    public static (int checks,List<string> failures) Run(Func<ulong,int,FighterSpec[]> generate,int count=240)
    {
        int checks=0;var failures=new List<string>();var wins=new Dictionary<string,(int wins,int total)>();
        void Check(bool ok,string m){checks++;if(!ok&&failures.Count<40)failures.Add(m);}
        for(ulong seed=1;seed<=(ulong)count;seed++){
            var spec=new BattleSpec(seed,generate(seed,seed%4==0?20:6),1800);var a=new BattleSimulation(spec);var b=new BattleSimulation(spec);a.Run();b.Run();
            Check(a.Winner==b.Winner&&a.Events.SequenceEqual(b.Events),"determinism "+seed);Check(a.Tick<=1800,"timeout");Check(a.Events.Any(e=>e.Kind=="damage"),"stalled "+seed);
            Check(a.Events.All(e=>e.Kind!="damage"||e.Amount>=0)&&a.Fighters.All(f=>f.Health>=0&&f.Health<=f.Spec.Stats.Health),"health");
            foreach(var f in a.Fighters){var v=wins.GetValueOrDefault(f.Spec.Group);wins[f.Spec.Group]=(v.wins+(a.Winner==f.Spec.Team?1:0),v.total+1);}
        }
        foreach(var (key,value) in wins.OrderBy(x=>x.Key)){Console.WriteLine($"BALANCE {key}: {value.wins}/{value.total} wins ({100.0*value.wins/value.total:F1}%)");Check(value.wins/(double)value.total>=.2&&value.wins/(double)value.total<=.8,"balance "+key);}
        return(checks,failures);
    }
}
