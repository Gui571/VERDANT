using System.Security.Cryptography;
using System.Text.Json;
using Verdant.Battle;
namespace PixelPreview;

// Mesmas receitas e sementes de BattleValidation; registra TODOS os campos dos eventos.
public static class BattleTrace
{
    public static void Write(string path)
    {
        var hashes=new SortedDictionary<ulong,string>();int events=0;
        for(ulong seed=1;seed<=240;seed++){
            int count=seed%4==0?20:6;
            var fighters=Enumerable.Range(0,count).Select(i=>(i%3==0?ActorRecipe.FromHero((int)((seed+(ulong)i)%7)):i%3==1?ActorRecipe.Generated(seed*31+(ulong)i):ActorRecipe.FromMonster(seed*31+(ulong)i)).Fighter(i%2)).ToArray();
            var sim=new BattleSimulation(new(seed,fighters,1800));sim.Run();events+=sim.Events.Count;
            hashes.Add(seed,Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(sim.Events))));
        }
        File.WriteAllText(path,JsonSerializer.Serialize(hashes,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"BATTLE_TRACE seeds={hashes.Count} events={events}");
    }
}
