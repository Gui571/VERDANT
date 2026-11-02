using Verdant;
using Verdant.Forge;
using Verdant.Battle;
namespace PixelPreview;
public static class SpecialPreview
{
    public static int Export(string dir)
    {
        Directory.CreateDirectory(dir);var recipes=Enumerable.Range(0,7).Select(ActorRecipe.FromHero).Concat(Enum.GetValues<MonsterFamily>().Select(f=>ActorRecipe.FromMonster(100+(ulong)f,f) with{Monster=MonsterGenerator.Generate(100+(ulong)f,f) with{Rank=Rank.Minion}})).ToArray();
        foreach(var recipe in recipes)foreach(bool cast in new[]{false,true}){
            var c=new Canvas(32*8*5,32*5,0x1c2031);var atlas=new Canvas(32*8,32,0);Array.Clear(atlas.Data);var hero=recipe.Hero==null?null:new CompactAdventurer(recipe.Hero);var mon=recipe.Monster==null?null:new MonsterRenderer(recipe.Monster);
            for(int f=0;f<8;f++){var pixels=hero?.Render(f/8.0,cast?AdventurerMotion.Cast:AdventurerMotion.Special)??mon!.Render(f/8.0,cast?MonsterMotion.Cast:MonsterMotion.Special);c.Blit(pixels,f*32*5,0,5);atlas.Blit(pixels,f*32,0,1);}
            string id=recipe.Hero?.Id??recipe.Monster!.Family.ToString().ToLowerInvariant();string state=cast?"cast":"special";c.Save(Path.Combine(dir,id+"-"+state+"-review.png"));atlas.Save(Path.Combine(dir,id+"-"+state+".png"));File.WriteAllText(Path.Combine(dir,id+"-"+state+".json"),System.Text.Json.JsonSerializer.Serialize(new{version=1,width=32,height=32,origin=new[]{16,29},frames=8,columns=8,fps=8,loop=false,state}));
        }
        var gallery=new Canvas(16*5*8,16*5*2,0x1c2031);int i=0;
        foreach(var a in Abilities.All.Values){var c=new Canvas(16,16,0);Array.Clear(c.Data);var icon=ForgeEffects.Icon(a.Id);c.Blit(icon,0,0,1);c.Save(Path.Combine(dir,a.Id+"-icon.png"));gallery.Blit(icon,i%8*80,i/8*80,5);i++;}
        gallery.Save(Path.Combine(dir,"icons-review.png"));File.WriteAllText(Path.Combine(dir,"catalog.json"),System.Text.Json.JsonSerializer.Serialize(Abilities.All,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));return 0;
    }
}
