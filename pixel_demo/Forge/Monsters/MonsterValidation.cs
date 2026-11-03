using System.Security.Cryptography;
namespace Verdant.Forge;
public static class MonsterValidation
{
    public static ForgeValidation.Result Run(int count=120)
    {
        var failures=new List<string>();int checks=0;
        void Check(bool ok,string msg){checks++;if(!ok&&failures.Count<100)failures.Add(msg);}
        string Hash(byte[] p)=>Convert.ToHexString(SHA256.HashData(p));
        var hashes=new HashSet<string>();var silhouettes=new HashSet<string>();
        var specs=Enumerable.Range(0,9).Select(i=>new MonsterSpec(100+(ulong)i,(MonsterFamily)i,Element.Plant,Rank.Minion,1,1,1,1,1))
          .Concat(from family in Enum.GetValues<MonsterFamily>() from rank in Enum.GetValues<Rank>() from element in Enum.GetValues<Element>() select new MonsterSpec(300+(ulong)family,family,element,rank,3,3,3,3,3))
          .Concat(Enumerable.Range(1,count).Select(i=>MonsterGenerator.Generate((ulong)i)));
        foreach(var s in specs)foreach(int phase in s.Rank==Rank.Boss?new[]{0,1}:new[]{0}){var a=new MonsterRenderer(s){VisualPhase=phase};var b=new MonsterRenderer(s){VisualPhase=phase};
            foreach(var m in Enum.GetValues<MonsterMotion>())foreach(bool back in new[]{false,true})for(int f=0;f<MonsterRenderer.Frames(m);f++){
                var p=a.Render(f/(double)MonsterRenderer.Fps(m),m,back);Check(Enumerable.Range(0,s.Size).All(i=>p.Pixels[i*4+3]==0&&p.Pixels[((s.Size-1)*s.Size+i)*4+3]==0&&p.Pixels[(i*s.Size)*4+3]==0&&p.Pixels[(i*s.Size+s.Size-1)*4+3]==0),"border "+s.Id+"/"+m);Check(p.Clipped==0,$"clip {s.Id}/{m}/{f}");var colors=new HashSet<uint>();bool binary=true;for(int i=0;i<p.Pixels.Length;i+=4){binary&=p.Pixels[i+3] is 0 or 255;if(p.Pixels[i+3]>0)colors.Add(BitConverter.ToUInt32(p.Pixels,i));}
                Check(binary&&colors.Count<=16,$"palette {s.Id}/{m}");Check(p.Pixels.SequenceEqual(b.Render(f/(double)MonsterRenderer.Fps(m),m,back).Pixels),"determinism "+s.Id);
                if(m is not (MonsterMotion.Special or MonsterMotion.Cast)){bool orphan=false;for(int y=1;y<s.Size-1;y++)for(int x=1;x<s.Size-1;x++)if(p.Pixels[(y*s.Size+x)*4+3]>0){bool neighbor=false;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if((dx!=0||dy!=0)&&p.Pixels[((y+dy)*s.Size+x+dx)*4+3]>0)neighbor=true;orphan|=!neighbor;}Check(!orphan,"orphan "+s.Id);}
            }
            foreach(var m in new[]{MonsterMotion.Idle,MonsterMotion.Walk,MonsterMotion.Run}){var px=(byte[])a.Render(0,m).Pixels.Clone();Check(px.SequenceEqual(a.Render(MonsterRenderer.Frames(m)/(double)MonsterRenderer.Fps(m),m).Pixels),"loop "+s.Id);}
            var ground=a.Render(0).Pixels.AsSpan(((s.Size-3)*s.Size+s.Size/2-4)*4,32).ToArray();for(int f=0;f<16;f++)Check(ground.AsSpan().SequenceEqual(a.Render(f/8.0).Pixels.AsSpan(((s.Size-3)*s.Size+s.Size/2-4)*4,32)),"base "+s.Id);
            Check(a.Render(1,MonsterMotion.Death).Pixels.All(p=>p==0),"death removal "+s.Id);
        }
        for(ulong i=1;i<=120;i++){var s=MonsterGenerator.Generate(i);Check(s==MonsterGenerator.Generate(i),"recipe");hashes.Add(Hash(new MonsterRenderer(s).Render(0).Pixels));}
        Check(hashes.Count>=110,$"variety {hashes.Count}");
        foreach(var family in Enum.GetValues<MonsterFamily>()){var px=new MonsterRenderer(new(1,family,Element.Plant,Rank.Minion,1,1,0,0,0)).Render(0).Pixels;Check(silhouettes.Add(Hash(px.Where((_,i)=>i%4==3).ToArray())),"silhouette "+family);}
        foreach(var family in Enum.GetValues<MonsterFamily>()){
            var shapes=new HashSet<string>();
            for(ulong seed=1;seed<=32;seed++){var spec=MonsterGenerator.Generate(seed,family) with{Element=Element.Plant,Rank=Rank.Minion,Tint=1};shapes.Add(Hash(new MonsterRenderer(spec).Render(0).Pixels.Where((_,i)=>i%4==3).ToArray()));}
            Check(shapes.Count>=4,$"family shape variety {family}: {shapes.Count}/32");
            Console.WriteLine($"MONSTER_VARIETY {family} masks={shapes.Count}/32");
        }
        var masks=Enum.GetValues<MonsterFamily>().Select(f=>new MonsterRenderer(new(1,f,Element.Plant,Rank.Minion,1,1,0,0,0)){GroundShadow=false}.Render(0).Pixels.Where((_,i)=>i%4==3).ToArray()).ToArray();
        double minimum=1;
        for(int i=0;i<masks.Length;i++)for(int j=i+1;j<masks.Length;j++){
            int union=0,different=0;for(int k=0;k<32*28;k++){if(masks[i][k]!=0||masks[j][k]!=0)union++;if(masks[i][k]!=masks[j][k])different++;}
            double distance=different/(double)union;minimum=Math.Min(minimum,distance);Check(distance>=.10,$"silhouette distance {i}/{j}: {distance:F3}");
        }
        foreach(var family in Enum.GetValues<MonsterFamily>()){
            var spec=MonsterGenerator.Generate(91,family) with{Rank=Rank.Boss};var normal=new MonsterRenderer(spec);var phase=new MonsterRenderer(spec){VisualPhase=1};
            Check(!normal.Render(0).Pixels.SequenceEqual(phase.Render(0).Pixels),"boss visual phase "+family);
            Check(normal.Render(1,MonsterMotion.Entry).Pixels.SequenceEqual(normal.Render(0).Pixels),"entry recovery "+family);
        }
        Console.WriteLine($"MONSTER_MASK minimum_distance={minimum:F3} pairs=36");
        return new(checks,failures);
    }
}
