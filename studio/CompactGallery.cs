using Godot;
using Verdant.Battle;
using Verdant.Forge;
using Verdant.Forge.Compact;
namespace Verdant;

// Galeria de arte: todo quadro é rasterizado ao trocar uma receita e guardado no atlas.
public partial class CompactGallery : Control
{
    readonly List<SpriteCache> rosterCaches=new(),selectedCaches=new();
    readonly List<TextureRect> cards=new(),directions=new();readonly List<Button> selectors=new(),buttons=new();
    TextureRect portrait=null!;Label name=null!,description=null!;CheckButton art=null!;SpinBox seed=null!;OptionButton motion=null!;
    ForgeSpec selected=ForgeRoster.All[1];bool paused;double time;int frame=-1;
    static readonly Color Cream=new("f0e9d9"),Muted=new("a9b9c9");
    public override void _Ready(){
        var args=OS.GetCmdlineUserArgs();
        if(args.Any(a=>a is "--chibi-test" or "--pixel-test" or "--style-v2-test" or "--pixel-ui-test" or "--pixel-capture" or "--pixel-frames" or "--forge-hashes" or "--self-test" or "--ui-test" or "--gallery-capture")){GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile,"res://pixel_demo/PixelDemo.tscn");return;}

        TextureFilter=TextureFilterEnum.Nearest;
        Label(new(48,24),"VERDANT / O ELENCO DO VALE",16,new("cfb87e"));
        Label(new(48,60),"Pequenos heróis. Grandes silhuetas.",34,Cream);
        Label(new(48,110),"Escolha um aventureiro • pixel art procedural • toque nos retratos para comparar",18,Muted);
        var nav=new HBoxContainer{Position=new(48,151),Size=new(1332,48)};AddChild(nav);
        Button(nav,"Arena",()=>{Arena.UseStyleV2=art.ButtonPressed;GetTree().ChangeSceneToFile("res://battle/Arena.tscn");});
        Button(nav,"Prévia",()=>GetTree().ChangeSceneToFile("res://pixel_demo/PixelDemo.tscn"));
        Button(nav,"Oficina",()=>GetTree().ChangeSceneToFile("res://workshop/Workshop.tscn"));
        Button(nav,"Terrenos",()=>GetTree().ChangeSceneToFile("res://terrain/UI/TerrainWorkshop.tscn"));
        art=new CheckButton{Text="Arte nova",ButtonPressed=true,CustomMinimumSize=new(140,48)};nav.AddChild(art);art.Toggled+=_=>RebuildAll();
        motion=new OptionButton{CustomMinimumSize=new(160,48)};foreach(var t in new[]{"Respirar","Caminhar","Atacar","Receber dano","Especial"})motion.AddItem(t);nav.AddChild(motion);motion.ItemSelected+=_=>{time=0;UpdateFrames();};
        Button(nav,"Pausar / animar",()=>paused=!paused);
        name=Label(new(64,235),"",26,Cream);description=Label(new(64,283),"",16,Muted);description.AutowrapMode=TextServer.AutowrapMode.WordSmart;description.Size=new(420,88);
        portrait=Picture(new(134,366),256);
        Label(new(64,654),"SEMENTE DO AVENTUREIRO",15,new("cfb87e"));
        var generate=new HBoxContainer{Position=new(64,682),Size=new(420,50)};AddChild(generate);
        seed=new SpinBox{MinValue=1,MaxValue=999999,Value=1,CustomMinimumSize=new(150,48)};generate.AddChild(seed);
        Button(generate,"Gerar",()=>Select(ForgeGenerator.Generate((ulong)seed.Value)));
        Button(generate,"Próxima",()=>{seed.Value=seed.Value%999999+1;Select(ForgeGenerator.Generate((ulong)seed.Value));});
        Label(new(570,222),"SETE PERSONALIDADES",15,new("cfb87e"));
        for(int i=0;i<7;i++){
            int index=i;Vector2 pos=new(548+i%4*203,262+i/4*192);
            var b=new Button{Position=pos,Size=new(182,180),Flat=true};AddChild(b);b.Pressed+=()=>Select(ForgeRoster.All[index]);selectors.Add(b);
            var image=new TextureRect{Position=new(27,0),Size=new(128,128),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale,MouseFilter=MouseFilterEnum.Ignore};b.AddChild(image);cards.Add(image);
            var label=new Label{Text=ForgeRoster.All[i].Name,Position=new(0,134),Size=new(182,26),HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore};label.AddThemeColorOverride("font_color",Cream);b.AddChild(label);
        }
        Label(new(570,654),"O MESMO HERÓI, QUATRO DIREÇÕES",15,new("cfb87e"));
        for(int i=0;i<4;i++){directions.Add(Picture(new(572+i*188,692),96));Label(new(572+i*188,801),new[]{"SUL","LESTE","NORTE","OESTE"}[i],14,Muted);}
        Label(new(48,886),"Cabeça dominante • corpo curto • olhos mínimos • arma legível • movimento discreto",17,Muted);
        RebuildAll();if(args.Contains("--compact-ui-test"))Test();if(args.Contains("--compact-capture"))Capture();
    }
    Label Label(Vector2 at,string text,int size,Color color){var l=new Label{Position=at,Text=text};l.AddThemeFontSizeOverride("font_size",size);l.AddThemeColorOverride("font_color",color);AddChild(l);return l;}
    TextureRect Picture(Vector2 at,int size){var p=new TextureRect{Position=at,Size=new(size,size),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale};AddChild(p);return p;}
    Button Button(Container c,string label,Action click){var b=new Button{Text=label,CustomMinimumSize=new(110,48)};c.AddChild(b);b.Pressed+=click;buttons.Add(b);return b;}
    MonsterMotion Motion=>motion.Selected switch{1=>MonsterMotion.Walk,2=>MonsterMotion.Attack,3=>MonsterMotion.Hurt,4=>MonsterMotion.Special,_=>MonsterMotion.Idle};
    void RebuildAll(){foreach(var c in rosterCaches)c.Dispose();rosterCaches.Clear();foreach(var spec in ForgeRoster.All)rosterCaches.Add(new(new(spec.Id,spec,null),compact:art.ButtonPressed));Select(selected);}
    void Select(ForgeSpec spec){selected=spec;time=0;frame=-1;name.Text=spec.Name+" / "+spec.Class;description.Text=spec.Description;foreach(var c in selectedCaches)c.Dispose();selectedCaches.Clear();foreach(var d in Enum.GetValues<Facing>())selectedCaches.Add(new(new(spec.Id,spec,null),back:d==Facing.North,compact:art.ButtonPressed,direction:d));UpdateFrames();}
    void UpdateFrames(){portrait.Texture=selectedCaches[1].Get(Motion,time);for(int i=0;i<7;i++)cards[i].Texture=rosterCaches[i].Get(Motion,time);for(int i=0;i<4;i++)directions[i].Texture=selectedCaches[i].Get(Motion,time);}
    public override void _Process(double dt){if(paused)return;time+=dt;int f=(int)(time*8);if(f!=frame){frame=f;UpdateFrames();}}
    public override void _Draw(){DrawRect(GetViewportRect(),new Color("151e2a"));DrawRect(new(48,218,450,630),new Color("1a2736"));DrawLine(new(48,862),new(1392,862),new Color("354352"));}
    async void Test(){try{int n=0;void Check(bool ok,string message){n++;if(!ok)throw new Exception(message);}await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(art.ButtonPressed,"new drawing default");for(int i=0;i<7;i++){selectors[i].EmitSignal(Godot.Button.SignalName.Pressed);using var actual=selectedCaches[1].Get(MonsterMotion.Idle,0).GetImage();using var expected=new ChibiRenderer(ForgeRoster.All[i]).Render(0).Image();Check(actual.GetData().SequenceEqual(expected.GetData()),"cached hero "+i);}
        var at=art.GetGlobalRect().GetCenter();Input.ParseInputEvent(new InputEventScreenTouch{Index=0,Position=at,Pressed=true});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Input.ParseInputEvent(new InputEventScreenTouch{Index=0,Position=at,Pressed=false});await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Check(!art.ButtonPressed,"touch comparison");art.ButtonPressed=true;
        seed.Value=43;buttons.Single(b=>b.Text=="Gerar").EmitSignal(Godot.Button.SignalName.Pressed);Check(selected.Id=="g43","seed generation");using var first=selectedCaches[1].Get(MonsterMotion.Idle,0).GetImage();buttons.Single(b=>b.Text=="Gerar").EmitSignal(Godot.Button.SignalName.Pressed);using var second=selectedCaches[1].Get(MonsterMotion.Idle,0).GetImage();Check(first.GetData().SequenceEqual(second.GetData()),"seed repeat");
        foreach(var b in buttons.Concat(selectors).Append(art)){Check(b.Size.Y>=48,"touch size");Check(GetViewportRect().Encloses(b.GetGlobalRect()),"viewport "+b.Text);}GD.Print($"COMPACT_UI_PASS checks={n}");GetTree().Quit();
    }catch(Exception e){GD.PrintErr(e);GetTree().Quit(1);}}
    async void Capture(){Select(ForgeRoster.All[1]);paused=true;string dir=ProjectSettings.GlobalizePath("user://compact-frames");Directory.CreateDirectory(dir);for(int f=0;f<32;f++){time=f/8.0;UpdateFrames();await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();image.SavePng(Path.Combine(dir,$"gallery-{f:D2}.png"));}GD.Print("COMPACT_CAPTURE "+dir);GetTree().Quit();}
    public override void _ExitTree(){foreach(var c in rosterCaches)c.Dispose();foreach(var c in selectedCaches)c.Dispose();}
}
