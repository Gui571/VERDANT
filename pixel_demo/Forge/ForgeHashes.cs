using System.Security.Cryptography;
using System.Text.Json;
namespace Verdant.Forge;

// Hash do atlas RGBA completo, frente/costas, antes de qualquer codificação PNG.
public static class ForgeHashes
{
    public static SortedDictionary<string,string> Collect()
    {
        var result = new SortedDictionary<string,string>(StringComparer.Ordinal);
        void Add(string key, int count, Func<int,Raster> render)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (int frame=0; frame<count; frame++) hash.AppendData(render(frame).Pixels);
            result.Add(key, Convert.ToHexString(hash.GetHashAndReset()));
        }
        foreach (var spec in ForgeRoster.All.Concat(Enumerable.Range(1,200).Select(i=>ForgeGenerator.Generate((ulong)i))))
        {
            var renderer = new ForgeRenderer(spec);
            foreach (var motion in Enum.GetValues<AdventurerMotion>()) foreach (bool back in new[]{false,true})
            {
                int count = motion switch { AdventurerMotion.Idle=>32, AdventurerMotion.Attack=>6, AdventurerMotion.Hurt=>4, _=>8 };
                Add($"hero/{spec.Id}/{motion}/{back}",count,f=>renderer.Render(f/(motion==AdventurerMotion.Run?12.0:8.0),motion,back));
            }
        }
        foreach (var family in Enum.GetValues<MonsterFamily>()) foreach (var rank in Enum.GetValues<Rank>()) foreach (var element in Enum.GetValues<Element>())
        {
            var spec = MonsterGenerator.Generate(100+(ulong)family,family) with {Rank=rank,Element=element};
            var renderer = new MonsterRenderer(spec);
            foreach (var motion in Enum.GetValues<MonsterMotion>()) foreach (bool back in new[]{false,true})
                Add($"monster/{family}/{rank}/{element}/{motion}/{back}",MonsterRenderer.Frames(motion),f=>renderer.Render(f/(double)MonsterRenderer.Fps(motion),motion,back));
        }
        return result;
    }
    public static void Write(string path) => File.WriteAllText(path,JsonSerializer.Serialize(Collect(),new JsonSerializerOptions {WriteIndented=true}));
}
