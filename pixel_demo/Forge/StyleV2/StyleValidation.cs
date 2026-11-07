using System.Security.Cryptography;
namespace Verdant.Forge.StyleV2;
public static class StyleValidation
{
    public static ForgeValidation.Result Run()
    {
        int checks=0,maxColors=0,borderFrames=0;var fails=new List<string>();
        void Check(bool ok,string msg){checks++;if(!ok&&fails.Count<80)fails.Add(msg);}
        foreach(var field in typeof(MaterialRamps).GetFields().Where(f=>f.FieldType==typeof(Ramp5))){
            var ramp=(Ramp5)field.GetValue(null)!;int last=-1;
            for(int k=0;k<5;k++){uint c=ramp[k];int luminance=54*(int)(c>>16)+183*(int)((c>>8)&255)+19*(int)(c&255);Check(luminance>last,"ramp order "+field.Name);last=luminance;}
        }
        int minDistance=int.MaxValue;
        foreach(var species in Enum.GetValues<Species>())foreach(var team in MaterialRamps.Teams){uint a=MaterialStyle.For(species).Fur.Base,b=team.Base;int d=0;foreach(int shift in new[]{0,8,16}){int diff=(int)((a>>shift)&255)-(int)((b>>shift)&255);d+=diff*diff;}minDistance=Math.Min(minDistance,d);Check(d>=625,"fur/team base contrast "+species);}
        Console.WriteLine($"STYLE_CONTRAST metric=rgb_squared minimum={minDistance} required=625 pairs=66");
        var variants=new HashSet<string>();
        foreach(var spec in ForgeRoster.All.Concat(Enumerable.Range(1,24).Select(i=>ForgeGenerator.Generate((ulong)i)))){
            var legacy=new ForgeRenderer(spec){GroundShadow=false};var renderer=new ForgeRenderer(spec){Style=MaterialStyle.For(spec),GroundShadow=false};
            foreach(var motion in Enum.GetValues<AdventurerMotion>())foreach(bool back in new[]{false,true})for(int frame=0;frame<8;frame++){
                double time=frame/8.0;var a=renderer.Render(time,motion,back);var b=legacy.Render(time,motion,back);
                Check(MaskEqual(a,b),"v1/v2 shape "+spec.Id);Inspect(a,spec.Id);
                Check(a.Pixels.SequenceEqual(new ForgeRenderer(spec){Style=renderer.Style,GroundShadow=false}.Render(time,motion,back).Pixels),"pixels deterministic "+spec.Id);
            }
            renderer.Style=MaterialStyle.For(spec,0);var original=(byte[])renderer.Render(0).Pixels.Clone();var mask=(byte[])renderer.MaterialIds!.Clone();
            for(int team=1;team<6;team++){
                renderer.Style=renderer.Style with{Team=team};var px=renderer.Render(0).Pixels;int changed=0;bool onlyTeam=true;
                for(int i=0;i<1024;i++){if(!px.AsSpan(i*4,4).SequenceEqual(original.AsSpan(i*4,4))){changed++;onlyTeam&=mask[i]==(byte)Material.Team;}}
                Check(onlyTeam&&changed>0,"TEAM isolation "+spec.Id+"/"+team);
                Check(mask.SequenceEqual(renderer.MaterialIds!),"material independence "+spec.Id);
                if(spec.Id=="nix")variants.Add(Convert.ToHexString(SHA256.HashData(px)));
            }
        }
        foreach(var family in Enum.GetValues<MonsterFamily>())foreach(var element in Enum.GetValues<Element>())foreach(var rank in Enum.GetValues<Rank>()){
            var spec=MonsterGenerator.Generate(100+(ulong)family,family) with{Element=element,Rank=rank};
            var r=new MonsterRenderer(spec){Style=MaterialStyle.For(element)};var v1=new MonsterRenderer(spec);
            foreach(var motion in Enum.GetValues<MonsterMotion>())for(int f=0;f<MonsterRenderer.Frames(motion);f++){
                var px=r.Render(f/(double)MonsterRenderer.Fps(motion),motion);Inspect(px,spec.Id);
                Check(MaskEqual(px,v1.Render(f/(double)MonsterRenderer.Fps(motion),motion)),"monster geometry "+spec.Id);
            }
        }
        Check(variants.Count==5,"distinct teams");
        Console.WriteLine($"STYLE_V2 checks={checks} failures={fails.Count} max_colors={maxColors} teams=6 inherited_border_frames={borderFrames}");return new(checks,fails);
        void Inspect(Raster px,string name){var colors=new HashSet<uint>();bool alpha=true,border=true;
            for(int i=0;i<px.Pixels.Length;i+=4){alpha&=px.Pixels[i+3] is 0 or 255;if(px.Pixels[i+3]>0)colors.Add(BitConverter.ToUInt32(px.Pixels,i));}
            for(int x=0;x<px.Width;x++)border&=px.Pixels[x*4+3]==0&&px.Pixels[((px.Height-1)*px.Width+x)*4+3]==0;
            for(int y=0;y<px.Height;y++)border&=px.Pixels[(y*px.Width)*4+3]==0&&px.Pixels[(y*px.Width+px.Width-1)*4+3]==0;
            maxColors=Math.Max(maxColors,colors.Count);Check(alpha&&colors.Count<=MaterialStyle.MaxColors,"colors "+colors.Count+" "+name);if(!border)borderFrames++;Check(px.Clipped==0,"bounds "+name);
        }
        static bool MaskEqual(Raster a,Raster b){for(int i=3;i<a.Pixels.Length;i+=4)if(a.Pixels[i]!=b.Pixels[i])return false;return true;}
    }
}
