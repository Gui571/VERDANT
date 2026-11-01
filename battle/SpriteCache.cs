using Godot;
using Verdant.Forge;
namespace Verdant.Battle;
// Todas as texturas são criadas antes da simulação, nunca por quadro desenhado.
public sealed class SpriteCache : IDisposable
{
    readonly Dictionary<(MonsterMotion,int),ImageTexture> frames=new();
    public int Size {get;}
    public SpriteCache(ActorRecipe recipe,bool back=false)
    {
        Size=recipe.Monster?.Size??32;var monster=recipe.Monster==null?null:new MonsterRenderer(recipe.Monster);var hero=recipe.Hero==null?null:new CompactAdventurer(recipe.Hero);
        foreach(var m in Enum.GetValues<MonsterMotion>())for(int f=0;f<MonsterRenderer.Frames(m);f++){
            Raster px;
            if(monster!=null)px=monster.Render(f/(double)MonsterRenderer.Fps(m),m,back);
            else if(m!=MonsterMotion.Death)px=hero!.Render(f/(double)MonsterRenderer.Fps(m),(AdventurerMotion)m,back);
            else {var original=hero!.Render(0,AdventurerMotion.Hurt,back);px=new Raster(32,32);if(f<5)for(int y=0;y<32;y++)for(int x=0;x<32;x++){int k=(y*32+x)*4;if(original.Pixels[k+3]!=0)px.Pixel(x,29-(29-y)*(5-f)/5,new Color(original.Pixels[k]/255f,original.Pixels[k+1]/255f,original.Pixels[k+2]/255f,1));}}
            using var image=px.Image();frames[(m,f)]=ImageTexture.CreateFromImage(image);
        }
    }
    public ImageTexture Get(MonsterMotion m,double seconds){int f=(int)(Math.Max(0,seconds)*MonsterRenderer.Fps(m));f=m<=MonsterMotion.Run?f%MonsterRenderer.Frames(m):Math.Min(f,MonsterRenderer.Frames(m)-1);return frames[(m,f)];}
    public void Dispose(){foreach(var t in frames.Values)t.Dispose();frames.Clear();}
}
