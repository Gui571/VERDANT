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
          .Concat(Enumerable.Range(1,count).Select(i=>MonsterGenerator.Generate((ulong)i)));
        foreach(var s in specs){var a=new MonsterRenderer(s);var b=new MonsterRenderer(s);
            foreach(var m in Enum.GetValues<MonsterMotion>())foreach(bool back in new[]{false,true})for(int f=0;f<MonsterRenderer.Frames(m);f++){
                var p=a.Render(f/(double)MonsterRenderer.Fps(m),m,back);Check(Enumerable.Range(0,s.Size).All(i=>p.Pixels[i*4+3]==0&&p.Pixels[((s.Size-1)*s.Size+i)*4+3]==0&&p.Pixels[(i*s.Size)*4+3]==0&&p.Pixels[(i*s.Size+s.Size-1)*4+3]==0),"border "+s.Id+"/"+m);Check(p.Clipped==0,$"clip {s.Id}/{m}/{f}");var colors=new HashSet<uint>();bool binary=true;for(int i=0;i<p.Pixels.Length;i+=4){binary&=p.Pixels[i+3] is 0 or 255;if(p.Pixels[i+3]>0)colors.Add(BitConverter.ToUInt32(p.Pixels,i));}
                Check(binary&&colors.Count<=16,$"palette {s.Id}/{m}");Check(p.Pixels.SequenceEqual(b.Render(f/(double)MonsterRenderer.Fps(m),m,back).Pixels),"determinism "+s.Id);
                if(m==MonsterMotion.Idle){bool orphan=false;for(int y=1;y<s.Size-1;y++)for(int x=1;x<s.Size-1;x++)if(p.Pixels[(y*s.Size+x)*4+3]>0){bool neighbor=false;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if((dx!=0||dy!=0)&&p.Pixels[((y+dy)*s.Size+x+dx)*4+3]>0)neighbor=true;orphan|=!neighbor;}Check(!orphan,"orphan "+s.Id);}
            }
            foreach(var m in new[]{MonsterMotion.Idle,MonsterMotion.Walk,MonsterMotion.Run}){var px=(byte[])a.Render(0,m).Pixels.Clone();Check(px.SequenceEqual(a.Render(MonsterRenderer.Frames(m)/(double)MonsterRenderer.Fps(m),m).Pixels),"loop "+s.Id);}
            var ground=a.Render(0).Pixels.AsSpan(((s.Size-3)*s.Size+s.Size/2-4)*4,32).ToArray();for(int f=0;f<16;f++)Check(ground.AsSpan().SequenceEqual(a.Render(f/8.0).Pixels.AsSpan(((s.Size-3)*s.Size+s.Size/2-4)*4,32)),"base "+s.Id);
            Check(a.Render(1,MonsterMotion.Death).Pixels.All(p=>p==0),"death removal "+s.Id);
        }
        for(ulong i=1;i<=120;i++){var s=MonsterGenerator.Generate(i);Check(s==MonsterGenerator.Generate(i),"recipe");hashes.Add(Hash(new MonsterRenderer(s).Render(0).Pixels));}
        Check(hashes.Count>=110,$"variety {hashes.Count}");
        foreach(var family in Enum.GetValues<MonsterFamily>()){var px=new MonsterRenderer(new(1,family,Element.Plant,Rank.Minion,1,1,0,0,0)).Render(0).Pixels;Check(silhouettes.Add(Hash(px.Where((_,i)=>i%4==3).ToArray())),"silhouette "+family);}
        return new(checks,failures);
    }
}
