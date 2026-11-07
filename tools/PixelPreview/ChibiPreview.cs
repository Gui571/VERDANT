using Verdant;
using Verdant.Forge;
using Verdant.Forge.Compact;
namespace PixelPreview;
public static class ChibiPreview
{
    public static int Export(string dir){
        Directory.CreateDirectory(dir);var all=new List<ForgeSpec>(ForgeRoster.All);
        foreach(var s in Enum.GetValues<Species>())all.Add(ForgeRoster.All[1] with{Id="species-"+s,Species=s,Headwear=Headwear.Headband,Back=BackGear.None,Weapon=Weapon.Sword,Scarf=false});
        for(ulong seed=1;seed<=24;seed++)all.Add(ForgeGenerator.Generate(seed));
        foreach(var spec in all){var renderer=new ChibiRenderer(spec){GroundShadow=false};
            var px=renderer.Render(0);Png.Save(Path.Combine(dir,spec.Id+".png"),32,32,px.Pixels);
            if(!ForgeRoster.All.Contains(spec))continue;
            foreach(var motion in Enum.GetValues<AdventurerMotion>()){
                int frames=motion switch{AdventurerMotion.Idle=>32,AdventurerMotion.Attack=>6,AdventurerMotion.Hurt=>4,_=>8};int fps=motion==AdventurerMotion.Run?12:8;
                var c=new Canvas(32*frames,32,0);Array.Clear(c.Data);for(int f=0;f<frames;f++)c.Blit(renderer.Render(f/(double)fps,motion),f*32,0,1);c.Save(Path.Combine(dir,spec.Id+"-"+motion+".png"));
                File.WriteAllText(Path.Combine(dir,spec.Id+"-"+motion+".json"),System.Text.Json.JsonSerializer.Serialize(new{version=1,style="compact",recipe=spec.Id,species=spec.Species.ToString(),cell=new[]{32,32},origin=new[]{16,29},direction="East",clip=motion.ToString(),loop=motion<=AdventurerMotion.Run,frames=Enumerable.Range(0,frames).Select(i=>new{index=i,x=i*32,y=0,durationMs=1000.0/fps})}));
            }
            var death=new Canvas(32*6,32,0);Array.Clear(death.Data);for(int f=0;f<6;f++)death.Blit(renderer.RenderDeath(f/8.0),f*32,0,1);death.Save(Path.Combine(dir,spec.Id+"-Death.png"));
            foreach(var facing in Enum.GetValues<Facing>()){var frame=renderer.Render(0,AdventurerMotion.Idle,facing);Png.Save(Path.Combine(dir,spec.Id+"-"+facing+".png"),32,32,frame.Pixels);}
        }
        return 0;
    }
}
