using Godot;
using Verdant.Forge;
namespace Verdant.Battle;
// Um atlas por receita reduz trocas de textura; nada é rasterizado durante a batalha.
public sealed class SpriteCache : IDisposable
{
    readonly Dictionary<(MonsterMotion,int),AtlasTexture> frames=new();
    readonly ImageTexture atlas;
    readonly SpriteCache? enraged;
    readonly bool heroRecipe;
    public int Size {get;}
    int Count(MonsterMotion m)=>heroRecipe&&m==MonsterMotion.Entry?0:heroRecipe&&m==MonsterMotion.Idle?32:heroRecipe&&m==MonsterMotion.Death?6:MonsterRenderer.Frames(m);
    int Fps(MonsterMotion m)=>heroRecipe&&m==MonsterMotion.Death?8:MonsterRenderer.Fps(m);
    public SpriteCache(ActorRecipe recipe,bool back=false,int visualPhase=0,bool styleV2=false,int team=4)
    {
        Size=recipe.Monster?.Size??32;heroRecipe=recipe.Hero!=null;
        var monster=recipe.Monster==null?null:new MonsterRenderer(recipe.Monster){VisualPhase=visualPhase};var hero=recipe.Hero==null?null:new CompactAdventurer(recipe.Hero);
        if(styleV2){if(monster!=null)monster.Style=Verdant.Forge.StyleV2.MaterialStyle.For(recipe.Monster!.Element,team);if(hero!=null)hero.Style=Verdant.Forge.StyleV2.MaterialStyle.For(recipe.Hero!,team);}
        int total=Enum.GetValues<MonsterMotion>().Sum(Count),index=0;
        using var sheet=Image.CreateEmpty(Size*8,Size*((total+7)/8),false,Image.Format.Rgba8);
        var regions=new List<(MonsterMotion m,int f,Rect2 region)>();
        foreach(var m in Enum.GetValues<MonsterMotion>())for(int f=0;f<Count(m);f++){
            Raster px;
            if(monster!=null)px=monster.Render(f/(double)Fps(m),m,back);
            else if(m!=MonsterMotion.Death)px=hero!.Render(f/(double)Fps(m),m==MonsterMotion.Special?AdventurerMotion.Special:m==MonsterMotion.Cast?AdventurerMotion.Cast:m==MonsterMotion.Entry?AdventurerMotion.Idle:(AdventurerMotion)m,back);
            else {var original=hero!.Render(0,AdventurerMotion.Hurt,back);px=new Raster(32,32);if(f<5)for(int y=0;y<32;y++)for(int x=0;x<32;x++){int k=(y*32+x)*4;if(original.Pixels[k+3]!=0)px.Pixel(x,29-(29-y)*(5-f)/5,new Color(original.Pixels[k]/255f,original.Pixels[k+1]/255f,original.Pixels[k+2]/255f,1));}}
            using var image=px.Image();var at=new Vector2I(index%8*Size,index/8*Size);sheet.BlitRect(image,new(0,0,Size,Size),at);regions.Add((m,f,new(at.X,at.Y,Size,Size)));index++;
        }
        if(recipe.Monster?.Rank==Rank.Boss&&visualPhase==0)enraged=new(recipe,back,1,styleV2,team);
        atlas=ImageTexture.CreateFromImage(sheet);foreach(var (m,f,region) in regions)frames[(m,f)]=new AtlasTexture{Atlas=atlas,Region=region,FilterClip=true};
    }
    public Texture2D Get(MonsterMotion m,double seconds,int phase=0){if(heroRecipe&&m==MonsterMotion.Entry)m=MonsterMotion.Idle;if(phase>0&&enraged!=null)return enraged.Get(m,seconds);int f=(int)(Math.Max(0,seconds)*Fps(m));f=m<=MonsterMotion.Run?f%Count(m):Math.Min(f,Count(m)-1);return frames[(m,f)];}
    public void Dispose(){enraged?.Dispose();foreach(var t in frames.Values)t.Dispose();frames.Clear();atlas.Dispose();}
}
