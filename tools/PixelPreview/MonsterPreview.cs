using Verdant;
using Verdant.Forge;
namespace PixelPreview;
public static class MonsterPreview
{
    public static int Strip(string path,MonsterFamily family,MonsterMotion motion,int zoom=5) {var s=MonsterGenerator.Generate(100+(ulong)family,family) with{Rank=Rank.Minion};var r=new MonsterRenderer(s);int n=MonsterRenderer.Frames(motion);var c=new Canvas(32*n*zoom,32*zoom,0x1c2031);for(int f=0;f<n;f++)c.Blit(r.Render(f/(double)MonsterRenderer.Fps(motion),motion),32*f*zoom,0,zoom);c.Save(path);return 0;}
    public static int Export(string dir)
    {
        Directory.CreateDirectory(dir);var specs=new List<MonsterSpec>();var gallery=new Canvas(3*48*5,3*48*5,0x1c2031);
        foreach(var fam in Enum.GetValues<MonsterFamily>()){
            var s=MonsterGenerator.Generate(100+(ulong)fam,fam) with{Rank=Rank.Minion};specs.Add(s);var r=new MonsterRenderer(s);string id=fam.ToString().ToLowerInvariant();
            gallery.Blit(r.Render(0),(int)fam%3*240+40,(int)fam/3*240+40,5);
            foreach(var m in Enum.GetValues<MonsterMotion>()){
                int n=MonsterRenderer.Frames(m),size=s.Size;var atlas=new Canvas(size*n,size,0);Array.Clear(atlas.Data);var review=new Canvas(size*n*5,size*5,0x1c2031);
                for(int f=0;f<n;f++){var px=r.Render(f/(double)MonsterRenderer.Fps(m),m);atlas.Blit(px,size*f,0,1);review.Blit(px,size*f*5,0,5);}
                string path=Path.Combine(dir,id+"-"+m.ToString().ToLowerInvariant());atlas.Save(path+".png");review.Save(path+"-review.png");
                File.WriteAllText(path+".json",System.Text.Json.JsonSerializer.Serialize(new{version=1,recipe=s,width=size,height=size,frames=n,columns=n,fps=MonsterRenderer.Fps(m),origin=new[]{size/2,size-3},state=m.ToString(),loop=m<=MonsterMotion.Run}));
            }
        }
        gallery.Save(Path.Combine(dir,"families.png"));var gen=new Canvas(6*48*5,4*48*5,0x1c2031);for(int i=0;i<24;i++){var s=MonsterGenerator.Generate((ulong)i+1);gen.Blit(new MonsterRenderer(s).Render(0),i%6*240,i/6*240,5);}gen.Save(Path.Combine(dir,"generated.png"));
        File.WriteAllText(Path.Combine(dir,"catalog.json"),System.Text.Json.JsonSerializer.Serialize(specs));return 0;
    }
}
