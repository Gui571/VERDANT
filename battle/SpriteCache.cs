using Godot;
using Verdant.Forge;
namespace Verdant.Battle;
// Um atlas por receita reduz trocas de textura; nada é rasterizado durante a batalha.
public sealed class SpriteCache : IDisposable
{
    readonly Dictionary<(MonsterMotion,int),AtlasTexture> frames=new();
    readonly ImageTexture atlas;
    readonly bool heroRecipe;
    public int Size {get;}
    int Count(MonsterMotion m)=>heroRecipe&&m==MonsterMotion.Idle?32:MonsterRenderer.Frames(m);
    public SpriteCache(ActorRecipe recipe,bool back=false)
    {
        Size=recipe.Monster?.Size??32;heroRecipe=recipe.Hero!=null;
        var monster=recipe.Monster==null?null:new MonsterRenderer(recipe.Monster);var hero=recipe.Hero==null?null:new CompactAdventurer(recipe.Hero);
        int total=Enum.GetValues<MonsterMotion>().Sum(Count),index=0;
        using var sheet=Image.CreateEmpty(Size*8,Size*((total+7)/8),false,Image.Format.Rgba8);
        var regions=new List<(MonsterMotion m,int f,Rect2 region)>();
        foreach(var m in Enum.GetValues<MonsterMotion>())for(int f=0;f<Count(m);f++){
            Raster px;
            if(monster!=null)px=monster.Render(f/(double)MonsterRenderer.Fps(m),m,back);
            else if(m!=MonsterMotion.Death)px=hero!.Render(f/(double)MonsterRenderer.Fps(m),m==MonsterMotion.Special?AdventurerMotion.Special:m==MonsterMotion.Cast?AdventurerMotion.Cast:(AdventurerMotion)m,back);
            else {var original=hero!.Render(0,AdventurerMotion.Hurt,back);px=new Raster(32,32);if(f<5)for(int y=0;y<32;y++)for(int x=0;x<32;x++){int k=(y*32+x)*4;if(original.Pixels[k+3]!=0)px.Pixel(x,29-(29-y)*(5-f)/5,new Color(original.Pixels[k]/255f,original.Pixels[k+1]/255f,original.Pixels[k+2]/255f,1));}}
            using var image=px.Image();var at=new Vector2I(index%8*Size,index/8*Size);sheet.BlitRect(image,new(0,0,Size,Size),at);regions.Add((m,f,new(at.X,at.Y,Size,Size)));index++;
        }
        atlas=ImageTexture.CreateFromImage(sheet);foreach(var (m,f,region) in regions)frames[(m,f)]=new AtlasTexture{Atlas=atlas,Region=region,FilterClip=true};
    }
    public Texture2D Get(MonsterMotion m,double seconds){int f=(int)(Math.Max(0,seconds)*MonsterRenderer.Fps(m));f=m<=MonsterMotion.Run?f%Count(m):Math.Min(f,Count(m)-1);return frames[(m,f)];}
    public void Dispose(){foreach(var t in frames.Values)t.Dispose();frames.Clear();atlas.Dispose();}
}
