using System.Security.Cryptography;
using Fluitown.Domain;
namespace Verdant.Terrain;
public static class TerrainValidation
{
    public static (int Checks,List<string> Failures) Run()
    {
        int checks=0;var failures=new List<string>();void Check(bool ok,string message){checks++;if(!ok&&failures.Count<30)failures.Add(message);}
        // Valores obtidos compilando Elevation e a camada JS originais no commit documentado.
        foreach(var (seed,x,z,expected) in new[]{(42d,3.5,-2d,.5729450432820987),(4294967295d,-17.2,99d,.17070761852844718),(0d,0d,0d,.30476046703421533),(123d,48d,32d,.41658033764584845)})
            Check(Elevation.fractalValueNoise(seed,x,z,19,3,2,.4)==expected,"upstream noise parity");
        var hashes=new HashSet<string>();
        for(ulong seed=1;seed<=100;seed++)foreach(var biome in Enum.GetValues<TerrainBiome>()){
            var recipe=new TerrainRecipe(seed,biome,(int)(seed%6),(int)((seed/3)%6),(int)((seed/7)%6));
            var a=TerrainMap.Generate(recipe);var bytes=a.FingerprintBytes();Check(bytes.SequenceEqual(TerrainMap.Generate(recipe).FingerprintBytes()),"determinism");
            Check(TerrainSave.Deserialize(TerrainSave.Serialize(recipe))==recipe,"save roundtrip");
            Check(bytes.SequenceEqual(TerrainMap.Generate(TerrainSave.Deserialize(TerrainSave.Serialize(recipe))).FingerprintBytes()),"saved generation");
            for(int z=0;z<TerrainMap.Depth;z++)for(int x=0;x<TerrainMap.Width;x++){
                var c=a[x,z];Check(c.Level<=6&&Enum.IsDefined(c.Kind)&&c.Tree<=3,"valid cell");
                if(recipe.Water==0)Check(c.Kind is not(GroundKind.Water or GroundKind.Bridge),"water zero");
                if(recipe.Forest==0)Check(c.Tree==0,"forest zero");
                if(recipe.Relief==0)Check(c.Level<=1,"relief zero");
            }
            Check(PathExists(a),"continuous road and bridges");
            var arena=TerrainMap.Generate(recipe with{Arena=true});
            for(int z=7;z<=24;z++)for(int x=7;x<=40;x++)Check(arena[x,z]==new TerrainCell(1,GroundKind.Grass,0),"arena unobstructed");
            if(biome==TerrainBiome.Woodland)hashes.Add(Convert.ToHexString(SHA256.HashData(bytes)));
        }
        Check(hashes.Count>=95,"seed diversity");
        foreach(var invalid in new[]{new TerrainRecipe(Relief:-1),new TerrainRecipe(Water:6),new TerrainRecipe(Biome:(TerrainBiome)99)}){bool rejected=false;try{TerrainMap.Generate(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid recipe");}
        bool version=false;try{TerrainSave.Deserialize("{\"Version\":2}");}catch(ArgumentException){version=true;}Check(version,"unknown save version");
        Console.WriteLine($"TERRAIN cases=400 unique_seeds={hashes.Count}/100 upstream_vectors=4");return(checks,failures);
    }
    static bool PathExists(TerrainMap map)
    {
        var seen=new HashSet<(int,int)>();var queue=new Queue<(int,int)>();
        for(int z=0;z<TerrainMap.Depth;z++)if(Walk(0,z)){queue.Enqueue((0,z));seen.Add((0,z));}
        while(queue.TryDequeue(out var p)){if(p.Item1==TerrainMap.Width-1)return true;foreach(var (dx,dz) in new[]{(1,0),(-1,0),(0,1),(0,-1)}){int x=p.Item1+dx,z=p.Item2+dz;if(x>=0&&z>=0&&x<TerrainMap.Width&&z<TerrainMap.Depth&&Walk(x,z)&&seen.Add((x,z)))queue.Enqueue((x,z));}}
        return false;
        bool Walk(int x,int z)=>map[x,z].Kind is GroundKind.Path or GroundKind.Bridge;
    }
}
