using Godot;
using Verdant.Battle;
using Verdant.Forge;
using Verdant.Forge.StyleV2;
using Verdant.Forge.Compact;
namespace Verdant;

// Primeira página do futuro estúdio: só materiais/luz M1. Texturas criadas nas escolhas.
public partial class MaterialStudio : Control
{
    readonly List<ImageTexture> textures=new();readonly List<SpriteCache> teams=new();
    OptionButton hero=null!;CheckButton enabled=null!;Button play=null!;
    readonly TextureRect[] pipeline=new TextureRect[3];readonly TextureRect[] swatches=new TextureRect[6];
    Label detail=null!;bool paused;double time;int tick=-1;
    readonly List<Button> buttons=new();
    public override void _Ready()
    {
        TextureFilter=TextureFilterEnum.Nearest;
        var root=new VBoxContainer{Position=new(42,26),Size=new(1356,908)};AddChild(root);
        var title=new Label{Text="VERDANT / ELENCO COMPACTO"};title.AddThemeFontSizeOverride("font_size",28);root.AddChild(title);
        root.AddChild(new Label{Text="Cabeças grandes · corpos curtos · equipamentos próprios · movimento em poucos pixels"});
        var controls=new HBoxContainer();root.AddChild(controls);
        Button(controls,"Elenco",()=>GetTree().ChangeSceneToFile("res://pixel_demo/PixelDemo.tscn"));
        Button(controls,"Arena",()=>{Arena.UseStyleV2=enabled.ButtonPressed;GetTree().ChangeSceneToFile("res://battle/Arena.tscn");});
        hero=new OptionButton{CustomMinimumSize=new(220,48)};foreach(var h in ForgeRoster.All)hero.AddItem(h.Name+" / "+h.Class);controls.AddChild(hero);hero.ItemSelected+=_=>Rebuild();
        enabled=new CheckButton{Text="Arte compacta",ButtonPressed=true,CustomMinimumSize=new(160,48)};controls.AddChild(enabled);enabled.Toggled+=_=>Rebuild();
        play=Button(controls,"Pausar",()=>{paused=!paused;play.Text=paused?"Animar":"Pausar";});
        var row=new HBoxContainer{Alignment=BoxContainer.AlignmentMode.Center};root.AddChild(row);
        for(int i=0;i<3;i++){
            var card=new VBoxContainer{CustomMinimumSize=new(340,320)};row.AddChild(card);
            pipeline[i]=Preview(card,256);card.AddChild(new Label{Text=new[]{"SILHUETA","MATERIAIS","LUZ E TOM"}[i],HorizontalAlignment=HorizontalAlignment.Center});
        }
        root.AddChild(new Label{Text="MESMO DESENHO / SEIS EQUIPES · apenas o tecido muda"});
        var colors=new HBoxContainer{Alignment=BoxContainer.AlignmentMode.Center};root.AddChild(colors);
        for(int i=0;i<6;i++){var card=new VBoxContainer{CustomMinimumSize=new(212,210)};colors.AddChild(card);swatches[i]=Preview(card,160);card.AddChild(new Label{Text=new[]{"Rosa","Brasa","Lima","Jade","Azul","Violeta"}[i],HorizontalAlignment=HorizontalAlignment.Center});}
        detail=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new(0,88)};root.AddChild(detail);Rebuild();
        var args=OS.GetCmdlineUserArgs();if(args.Contains("--material-ui-test"))Test();if(args.Contains("--material-capture"))Capture();
    }
    Button Button(Container c,string text,Action action){var b=new Button{Text=text,CustomMinimumSize=new(120,48)};b.Pressed+=action;c.AddChild(b);buttons.Add(b);return b;}
    TextureRect Preview(Container c,int size){var center=new CenterContainer();c.AddChild(center);var r=new TextureRect{CustomMinimumSize=new(size,size),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale};center.AddChild(r);return r;}
    void Rebuild(){
        foreach(var cache in teams)cache.Dispose();teams.Clear();foreach(var texture in textures)texture.Dispose();textures.Clear();
        var recipe=ForgeRoster.All[hero.Selected];
        for(int i=0;i<3;i++){
            var renderer=new ChibiRenderer(recipe){GroundShadow=false,Style=ChibiRenderer.Look(recipe,3) with{Pass=new[]{ProofPass.Silhouette,ProofPass.Materials,ProofPass.Lit}[i]}};
            using var image=renderer.Render(0).Image();var texture=ImageTexture.CreateFromImage(image);textures.Add(texture);pipeline[i].Texture=texture;
        }
        for(int i=0;i<6;i++)teams.Add(new SpriteCache(ActorRecipe.FromHero(hero.Selected),styleV2:enabled.ButtonPressed,team:i,compact:enabled.ButtonPressed));
        time=0;tick=-1;UpdateFrames();
        detail.Text=enabled.ButtonPressed?"Novo desenho: cabeça dominante, corpo curto, rosto simples e arma destacada.\nEscolha um herói e compare as seis cores de equipe. As orelhas, capas e poses acompanham a animação.":"Comparação: as seis prévias mostram o desenho anterior.\nAtive Arte compacta para voltar ao elenco redesenhado. As três imagens superiores mostram o novo desenho.";
    }
    void UpdateFrames(){for(int i=0;i<teams.Count;i++)swatches[i].Texture=teams[i].Get(MonsterMotion.Idle,time);}
    public override void _Process(double delta){if(paused)return;time+=delta;int f=(int)(time*8);if(f!=tick){tick=f;UpdateFrames();}}
    public override void _Draw(){DrawRect(GetViewportRect(),new Color("151d2a"));}
    async void Test(){try{int checks=0;void Check(bool b,string why){checks++;if(!b)throw new Exception(why);}await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(enabled.ButtonPressed,"default new drawing");
        // Exercita eventos de toque reais na árvore de controles, sem converter para mouse.
        var p=enabled.GetGlobalRect().GetCenter();Input.ParseInputEvent(new InputEventScreenTouch{Index=0,Position=p,Pressed=true});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Input.ParseInputEvent(new InputEventScreenTouch{Index=0,Position=p,Pressed=false});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Check(!enabled.ButtonPressed,"touch switches to legacy");
        using var a=teams[0].Get(MonsterMotion.Idle,0).GetImage();using var original=new CompactAdventurer(1,(AdventurerKind)hero.Selected).Render(0).Image();Check(a.GetData().SequenceEqual(original.GetData()),"legacy pixels");enabled.ButtonPressed=true;
        for(int i=0;i<7;i++){hero.Selected=i;hero.EmitSignal(OptionButton.SignalName.ItemSelected,(long)i);using var actual=teams[4].Get(MonsterMotion.Idle,0).GetImage();using var expected=new ChibiRenderer(ForgeRoster.All[i]){Style=ChibiRenderer.Look(ForgeRoster.All[i],4)}.Render(0).Image();Check(actual.GetData().SequenceEqual(expected.GetData()),"v2 cache "+i);}
        foreach(var button in buttons.Append<Button>(enabled)){Check(button.Size.Y>=48,"touch height");Check(GetViewportRect().Encloses(button.GetGlobalRect()),"button bounds");}
        GD.Print($"MATERIAL_UI_PASS checks={checks}");GetTree().Quit();
    }catch(Exception e){GD.PrintErr(e);GetTree().Quit(1);}}
    async void Capture(){hero.Selected=1;enabled.ButtonPressed=true;Rebuild();await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var img=GetViewport().GetTexture().GetImage();img.SavePng("user://material-studio.png");GD.Print("MATERIAL_CAPTURE user://material-studio.png");GetTree().Quit();}
    public override void _ExitTree(){foreach(var cache in teams)cache.Dispose();foreach(var texture in textures)texture.Dispose();}
}
