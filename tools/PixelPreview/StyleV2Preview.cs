using System.Diagnostics;
using System.Text.Json;
using Verdant;
using Verdant.Forge;
using Verdant.Forge.StyleV2;
namespace PixelPreview;

public static class StyleV2Preview
{
    public static int Export(string dir)
    {
        Directory.CreateDirectory(dir);var watch=Stopwatch.StartNew();
        var proof=ForgeRoster.All[1];
        void Save(string file,Raster px)=>Png.Save(Path.Combine(dir,file+".png"),px.Width,px.Height,px.Pixels);
        foreach(var pass in Enum.GetValues<ProofPass>())Save("pipeline-"+pass,new ForgeRenderer(proof){Style=MaterialStyle.For(proof,3) with{Pass=pass},GroundShadow=false}.Render(0));
        for(int team=0;team<6;team++)Save("team-"+team,new ForgeRenderer(proof){Style=MaterialStyle.For(proof,team),GroundShadow=false}.Render(0));
        var ramps=typeof(MaterialRamps).GetFields().Where(f=>f.FieldType==typeof(Ramp5)).ToDictionary(f=>f.Name,f=>{var ramp=(Ramp5)f.GetValue(null)!;return Enumerable.Range(0,5).Select(k=>ramp[k].ToString("X6")).ToArray();});
        File.WriteAllText(Path.Combine(dir,"palettes.json"),JsonSerializer.Serialize(ramps,new JsonSerializerOptions{WriteIndented=true}));
        var metadata=new List<object>();
        foreach(var spec in ForgeRoster.All){
            var renderer=new ForgeRenderer(spec){Style=MaterialStyle.For(spec),GroundShadow=false};
            var legacy=new ForgeRenderer(spec){GroundShadow=false};
            var px=renderer.Render(0);Save(spec.Id+"-v2",px);Save(spec.Id+"-v1",legacy.Render(0));
            var occupied=Enumerable.Range(0,1024).Where(i=>px.Pixels[i*4+3]>0).ToArray();
            metadata.Add(new{spec.Id,cell=new[]{32,32},pivot=new[]{16,29},bounds=new[]{occupied.Min(i=>i%32),occupied.Min(i=>i/32),occupied.Max(i=>i%32)+1,occupied.Max(i=>i/32)+1},team=4,species=spec.Species.ToString()});
            // Atlas prova da cor nos clipes atuais; direções novas pertencem a M3.
            foreach(var motion in Enum.GetValues<AdventurerMotion>()){
                int n=Verdant.Forge.Compact.ChibiRenderer.Frames(motion);int fps=Verdant.Forge.Compact.ChibiRenderer.Fps(motion);
                var atlas=new Canvas(32*8,32*((n+7)/8),0);Array.Clear(atlas.Data);
                for(int f=0;f<n;f++)atlas.Blit(renderer.Render(f/(double)fps,motion),f%8*32,f/8*32,1);
                atlas.Save(Path.Combine(dir,$"{spec.Id}-{motion}.png"));
                File.WriteAllText(Path.Combine(dir,$"{spec.Id}-{motion}.json"),JsonSerializer.Serialize(new{version=1,style="material-v2",recipe=spec.Id,atlas=$"{spec.Id}-{motion}.png",cell=new[]{32,32},columns=8,pivot=new[]{16,29},clip=motion.ToString(),direction="front",loop=motion<=AdventurerMotion.Run,frames=Enumerable.Range(0,n).Select(i=>new{index=i,x=i%8*32,y=i/8*32,durationMs=1000.0/fps}),palettes=new{fur=renderer.Style!.Fur,team=MaterialRamps.Teams[4]},tags=new[]{"M1","legacy-geometry"}}));
            }
        }
        foreach(var family in Enum.GetValues<MonsterFamily>()){
            var spec=MonsterGenerator.Generate(100+(ulong)family,family) with{Rank=Rank.Minion};
            Save("monster-"+family+"-v1",new MonsterRenderer(spec){GroundShadow=false}.Render(0));
            Save("monster-"+family+"-v2",new MonsterRenderer(spec){Style=MaterialStyle.For(spec.Element),GroundShadow=false}.Render(0));
        }
        File.WriteAllText(Path.Combine(dir,"catalog.json"),JsonSerializer.Serialize(new{version=1,stage="M1",characters=metadata},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"STYLE_V2_EXPORT heroes=7 monsters=9 teams=6 elapsed_ms={watch.Elapsed.TotalMilliseconds:F1}");return 0;
    }
}
