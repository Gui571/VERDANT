using Godot;
using Verdant.Forge;
namespace Verdant.Battle;
public partial class SpecialGallery : Control
{
    readonly List<(ActorRecipe recipe,SpriteCache cache,ImageTexture icon)> actors=new();
    double clock;bool capture;
    public override void _Ready()
    {
        TextureFilter=TextureFilterEnum.Nearest;
        foreach(var recipe in Enumerable.Range(0,7).Select(ActorRecipe.FromHero).Concat(Enum.GetValues<MonsterFamily>().Select(f=>ActorRecipe.FromMonster(100+(ulong)f,f) with{Monster=MonsterGenerator.Generate(100+(ulong)f,f) with{Rank=Rank.Minion}}))){var spec=recipe.Fighter(0);var ability=Abilities.Get(spec.AbilityId,spec.Role);using var img=ForgeEffects.Icon(ability.Id).Image();actors.Add((recipe,new(recipe),ImageTexture.CreateFromImage(img)));}
        var button=new Button{Text="Voltar à arena",Position=new(1210,24)};button.Pressed+=()=>GetTree().ChangeSceneToFile("res://battle/Arena.tscn");AddChild(button);
        capture=OS.GetCmdlineUserArgs().Contains("--special-capture");if(capture)Capture();
    }
    public override void _Process(double delta){if(capture)return;clock=(clock+delta)%1;QueueRedraw();}
    public override void _Draw()
    {
        DrawRect(new(0,0,1440,960),new Color("1c2031"));DrawString(ThemeDB.FallbackFont,new(80,48),"VERDANT / ASSINATURAS DE COMBATE",HorizontalAlignment.Left,-1,24,new Color("ede5cb"));
        for(int i=0;i<actors.Count;i++){var (recipe,frames,icon)=actors[i];var p=new Vector2(80+i%4*320,80+i/4*200);DrawRect(new(p,new(304,192)),new Color("252b3e"));DrawTextureRect(frames.Get(MonsterMotion.Special,clock),new(p+new Vector2(80,0),new(160,160)),false);DrawTextureRect(icon,new(p+new Vector2(12,154),new(32,32)),false);var spec=recipe.Fighter(0);var ability=Abilities.Get(spec.AbilityId,spec.Role);DrawString(ThemeDB.FallbackFont,p+new Vector2(50,169),recipe.Hero?.Name??recipe.Monster!.Family.ToString(),HorizontalAlignment.Left,-1,17,new Color("ede5cb"));DrawString(ThemeDB.FallbackFont,p+new Vector2(50,186),ability.Name,HorizontalAlignment.Left,-1,14,new Color("a2b3cc"));}
        DrawString(ThemeDB.FallbackFont,new(80,925),"Sprites 5× · 8 quadros · recarga fixa · na arena: Comandar azul e teclas 1–6",HorizontalAlignment.Left,-1,16,new Color("a2b3cc"));
    }
    async void Capture(){string dir=ProjectSettings.GlobalizePath("user://special-frames");Directory.CreateDirectory(dir);for(int f=0;f<8;f++){clock=f/8.0;QueueRedraw();await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var img=GetViewport().GetTexture().GetImage();img.SavePng(Path.Combine(dir,$"special-{f:D2}.png"));}GD.Print("SPECIAL_CAPTURE "+dir);GetTree().Quit();}
    public override void _ExitTree(){foreach(var actor in actors){actor.cache.Dispose();actor.icon.Dispose();}}
}
