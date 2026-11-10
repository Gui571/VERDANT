using System.Security.Cryptography;
namespace Verdant.Forge.Compact;
public static class ChibiValidation
{
    public static ForgeValidation.Result Run(int seeds=120){
        int checks=0,maxColors=0;var errors=new List<string>();
        void Check(bool yes,string message){checks++;if(!yes&&errors.Count<60)errors.Add(message);}
        var specs=ForgeRoster.All.Concat(Enumerable.Range(1,seeds).Select(i=>ForgeGenerator.Generate((ulong)i)));
        foreach(var spec in specs){var a=new ChibiRenderer(spec);var b=new ChibiRenderer(spec);
            foreach(var motion in Enum.GetValues<AdventurerMotion>())foreach(var facing in Enum.GetValues<Facing>()){
                int count=ChibiRenderer.Frames(motion);int fps=ChibiRenderer.Fps(motion);
                for(int f=0;f<count;f++){
                    var px=a.Render(f/(double)fps,motion,facing);var color=new HashSet<uint>();bool alpha=true,margin=true;
                    for(int i=0;i<1024;i++){alpha&=px.Pixels[i*4+3] is 0 or 255;if(px.Pixels[i*4+3]!=0){color.Add(BitConverter.ToUInt32(px.Pixels,i*4));if(i%32==0||i%32==31||i/32==0||i/32==31)margin=false;}}
                    maxColors=Math.Max(maxColors,color.Count);string tag=$"{spec.Id}/{motion}/{facing}/{f}";
                    Check(alpha&&color.Count is >0 and <=16,"palette "+tag);Check(margin&&px.Clipped==0,"margin "+tag+" clipped="+px.Clipped+" edge="+!margin);Check(px.Pixels.SequenceEqual(b.Render(f/(double)fps,motion,facing).Pixels),"determinism "+tag);
                    if(motion==AdventurerMotion.Idle)Check(ForgeValidation.Orphans(px)==0,"orphan "+tag);
                }
            }
            foreach(var motion in new[]{AdventurerMotion.Idle,AdventurerMotion.Walk,AdventurerMotion.Run}){var px=(byte[])a.Render(0,motion).Pixels.Clone();Check(px.SequenceEqual(a.Render(motion==AdventurerMotion.Idle?4:motion==AdventurerMotion.Run?8/12.0:1,motion).Pixels),"loop "+spec.Id);}
            for(int f=0;f<6;f++){var death=a.RenderDeath(f/8.0);Check(death.Clipped==0,"death bounds "+spec.Id);Check(f==5?death.Pixels.All(b=>b==0):death.Pixels.Any(b=>b!=0),"death pose "+spec.Id+"/"+f);}
            var original=(byte[])a.Render(0).Pixels.Clone();int different=0;for(int f=1;f<32;f++)if(!original.SequenceEqual(a.Render(f/8.0).Pixels))different++;
            Check(different>=4,"idle movement "+spec.Id);
        }
        var masks=new HashSet<string>();foreach(var spec in ForgeRoster.All){var renderer=new ChibiRenderer(spec){GroundShadow=false};var px=renderer.Render(0);Check(masks.Add(Convert.ToHexString(SHA256.HashData(px.Pixels.Where((_,i)=>i%4==3).ToArray()))),"roster silhouette "+spec.Id);
            var east=(byte[])px.Pixels.Clone();var west=renderer.Render(0,AdventurerMotion.Idle,Facing.West).Pixels;bool mirrored=true;
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)mirrored&=east.AsSpan((y*32+x)*4,4).SequenceEqual(west.AsSpan((y*32+31-x)*4,4));
            Check(mirrored,"west mirrors east "+spec.Id);
        }
        var owl=new ChibiRenderer(ForgeRoster.All[6]);owl.Render(0);
        Check(Enumerable.Range(12,9).Sum(x=>Enumerable.Range(18,2).Count(y=>owl.Parts.Get(x,y)==Tone.Ink))==4,"owl has two two-pixel eyes, no generic eye underneath");
        var speciesMasks=new HashSet<string>();foreach(var species in Enum.GetValues<Species>()){
            var recipe=ForgeRoster.All[1] with{Species=species,Headwear=Headwear.Headband,Back=BackGear.None,Scarf=false};
            var px=new ChibiRenderer(recipe){GroundShadow=false}.Render(0);Check(speciesMasks.Add(Convert.ToHexString(SHA256.HashData(px.Pixels.Where((_,i)=>i%4==3).ToArray()))),"species silhouette "+species);
        }
        var nix=ForgeRoster.All[1];var first=new ChibiRenderer(nix){Style=ChibiRenderer.Look(nix,0)};var initial=(byte[])first.Render(0).Pixels.Clone();var ids=(byte[])first.MaterialIds.Clone();
        for(int team=1;team<6;team++){var next=new ChibiRenderer(nix){Style=ChibiRenderer.Look(nix,team)}.Render(0).Pixels;int delta=0;bool isolated=true;for(int i=0;i<1024;i++)if(!initial.AsSpan(i*4,4).SequenceEqual(next.AsSpan(i*4,4))){delta++;isolated&=ids[i]==(byte)StyleV2.Material.Team;}Check(delta>0&&isolated,"team isolation "+team);}
        Console.WriteLine($"CHIBI checks={checks} failures={errors.Count} max_colors={maxColors} seeds={seeds} heroes=7 directions=4");return new(checks,errors);
    }
}
