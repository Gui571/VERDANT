using Godot;
using Verdant.Battle;
using Verdant.Forge;
namespace Verdant.Terrain;

public partial class TerrainWorkshop : Control
{
    public static TerrainRecipe Current {get;set;}=new();
    SubViewport view=null!;TerrainStage stage=null!;TextureRect preview=null!;SpinBox seed=null!;OptionButton biome=null!;Label status=null!;
    readonly Dictionary<string,Button> buttons=new();readonly List<HSlider> sliders=new();
    readonly List<(Sprite3D sprite,SpriteCache cache)> actors=new();double time;int frame=-1;Vector2? drag;
    public override void _Ready()
    {
        TextureFilter=TextureFilterEnum.Nearest;
        var root=new VBoxContainer{Position=new(36,28),Size=new(1368,902)};AddChild(root);
        var title=new Label{Text="VERDANT / ATELIÊ DE TERRENOS"};title.AddThemeFontSizeOverride("font_size",28);root.AddChild(title);
        root.AddChild(new Label{Text="Ilhas, patamares e florestas • geração por semente • 2.5D"});
        var nav=new HBoxContainer();root.AddChild(nav);Button(nav,"Elenco",()=>GetTree().ChangeSceneToFile("res://pixel_demo/PixelDemo.tscn"));Button(nav,"Oficina de monstros",()=>GetTree().ChangeSceneToFile("res://workshop/Workshop.tscn"));Button(nav,"Carregar receita",Load);
        var row=new HBoxContainer{SizeFlagsVertical=SizeFlags.ExpandFill};root.AddChild(row);
        var side=new VBoxContainer{CustomMinimumSize=new(340,0)};row.AddChild(side);
        side.AddChild(new Label{Text="SEMENTE"});seed=new SpinBox{MinValue=1,MaxValue=999999,Value=Current.Seed,CustomMinimumSize=new(0,48)};side.AddChild(seed);
        side.AddChild(new Label{Text="BIOMA"});biome=new OptionButton{CustomMinimumSize=new(0,48)};foreach(var name in new[]{"Bosque de Verdália","Pântano das Brumas","Cinzas de Emberhold","Gelo de Frostmar"})biome.AddItem(name);biome.Selected=(int)Current.Biome;side.AddChild(biome);
        Slider(side,"Relevo",Current.Relief);Slider(side,"Água / lava",Current.Water);Slider(side,"Vegetação",Current.Forest);
        Button(side,"Gerar terreno",Generate);Button(side,"Próxima semente",()=>{seed.Value=seed.Value%999999+1;Generate();});
        Button(side,"Usar na arena",()=>{Current=Recipe();GetTree().ChangeSceneToFile("res://battle/Arena.tscn");});
        Button(side,"Salvar receita + PNG",Save);
        side.AddChild(new Label{Text="Na arena, uma clareira plana mantém\no espaço do combate livre.\n\nArraste para girar.\nUse + / − para aproximar.\n\nBase: Cartoon Terrain Studio\n© idlerunner00 · MIT",AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new(300,180)});
        var panel=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddChild(panel);
        var center=new CenterContainer{SizeFlagsVertical=SizeFlags.ExpandFill};panel.AddChild(center);
        view=new SubViewport{Size=new(480,270),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always,TransparentBg=false};AddChild(view);
        stage=new TerrainStage();view.AddChild(stage);
        preview=new TextureRect{Texture=view.GetTexture(),CustomMinimumSize=new(960,540),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale,MouseFilter=MouseFilterEnum.Stop};center.AddChild(preview);preview.GuiInput+=PreviewInput;
        var camera=new HBoxContainer();panel.AddChild(camera);Button(camera,"Girar ←",()=>stage.Turn(-15));Button(camera,"Girar →",()=>stage.Turn(15));Button(camera,"+",()=>stage.Zoom(-.1f));Button(camera,"−",()=>stage.Zoom(.1f));
        status=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new(960,66)};panel.AddChild(status);Generate();
        var args=OS.GetCmdlineUserArgs();if(args.Contains("--terrain-ui-test"))Test();if(args.Contains("--terrain-capture"))Capture();
    }
    void Button(Container parent,string name,Action action){var b=new Button{Text=name,CustomMinimumSize=new(0,48)};b.Pressed+=()=>{try{action();}catch(Exception e){status.Text=e.Message;}};parent.AddChild(b);buttons[name]=b;}
    void Slider(Container parent,string name,int value){var label=new Label{Text=name+": "+value};parent.AddChild(label);var s=new HSlider{MinValue=0,MaxValue=5,Step=1,Value=value,CustomMinimumSize=new(0,36)};s.ValueChanged+=v=>label.Text=name+": "+v;parent.AddChild(s);sliders.Add(s);}
    TerrainRecipe Recipe()=>new((ulong)seed.Value,(TerrainBiome)biome.Selected,(int)sliders[0].Value,(int)sliders[1].Value,(int)sliders[2].Value);
    void Generate()
    {
        foreach(var (sprite,cache) in actors){sprite.Free();cache.Dispose();}actors.Clear();
        Current=Recipe();stage.Build(Current);time=0;frame=-1;
        for(int i=0;i<3;i++){
            int x=18+i*5;var road=Enumerable.Range(0,TerrainMap.Depth).Where(z=>stage.Map[x,z].Kind is GroundKind.Path or GroundKind.Bridge).ToArray();int z=road[road.Length/2];var cell=stage.Map[x,z];var cache=new SpriteCache(ActorRecipe.FromHero(i));
            var sprite=new Sprite3D{Texture=cache.Get(MonsterMotion.Idle,0),PixelSize=.10f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,TextureFilter=BaseMaterial3D.TextureFilterEnum.Nearest,Shaded=false,Position=new(x-24,cell.Level*TerrainMap.StepHeight+.03f,z-16),Offset=new(0,13),AlphaCut=SpriteBase3D.AlphaCutMode.Discard};stage.AddChild(sprite);actors.Add((sprite,cache));
        }
        status.Text=$"Semente {Current.Seed} · {48} × {32} células · {stage.TriangleCount:N0} triângulos\nRelevo em camadas, margens e vegetação são regenerados juntos. A semente reproduz o terreno.";
    }
    void Save(){string dir=ProjectSettings.GlobalizePath("user://terrain");Directory.CreateDirectory(dir);string path=Path.Combine(dir,"recipe.json");File.WriteAllText(path+".tmp",TerrainSave.Serialize(Current));File.Move(path+".tmp",path,true);using var img=view.GetTexture().GetImage();img.SavePng(Path.Combine(dir,"terrain.png"));status.Text="Salvo em "+dir;}
    void Load(){var recipe=TerrainSave.Deserialize(File.ReadAllText(ProjectSettings.GlobalizePath("user://terrain/recipe.json")));if(recipe.Seed<1||recipe.Seed>999999)throw new ArgumentException("A interface aceita sementes de 1 a 999999.");seed.Value=recipe.Seed;biome.Selected=(int)recipe.Biome;sliders[0].Value=recipe.Relief;sliders[1].Value=recipe.Water;sliders[2].Value=recipe.Forest;Generate();}
    void PreviewInput(InputEvent e){if(e is InputEventMouseButton b&&b.ButtonIndex==MouseButton.Left)drag=b.Pressed?b.Position:null;else if(e is InputEventMouseMotion m&&drag.HasValue){stage.Turn(m.Relative.X*.2f);drag=m.Position;}else if(e is InputEventScreenDrag touch)stage.Turn(touch.Relative.X*.2f);}
    public override void _Process(double dt){time+=dt;int now=(int)(time*8);if(now==frame)return;frame=now;foreach(var (sprite,cache) in actors)sprite.Texture=cache.Get(MonsterMotion.Idle,time);}
    async void Test(){try{await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);int n=0;void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}var a=stage.Map.FingerprintBytes();buttons["Gerar terreno"].EmitSignal(Godot.Button.SignalName.Pressed);Check(a.SequenceEqual(stage.Map.FingerprintBytes()),"UI determinism");buttons["Próxima semente"].EmitSignal(Godot.Button.SignalName.Pressed);Check(!a.SequenceEqual(stage.Map.FingerprintBytes()),"new seed");foreach(var b in Enum.GetValues<TerrainBiome>()){biome.Selected=(int)b;buttons["Gerar terreno"].EmitSignal(Godot.Button.SignalName.Pressed);Check(stage.Map.Recipe.Biome==b,"biome");}var pos=stage.Camera.Position;preview.EmitSignal(Control.SignalName.GuiInput,new InputEventScreenDrag{Relative=new(30,0)});Check(pos!=stage.Camera.Position,"touch orbit");foreach(var button in buttons.Values)Check(GetViewportRect().Encloses(button.GetGlobalRect()),"button bounds "+button.Text);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);Save();Check(TerrainSave.Deserialize(File.ReadAllText(ProjectSettings.GlobalizePath("user://terrain/recipe.json")))==Current,"save");var saved=Current;seed.Value=91;Generate();buttons["Carregar receita"].EmitSignal(Godot.Button.SignalName.Pressed);Check(Current==saved,"load UI");GD.Print($"TERRAIN_UI_PASS checks={n} triangles={stage.TriangleCount}");GetTree().Quit();}catch(Exception e){GD.PrintErr(e);GetTree().Quit(1);}}
    async void Capture(){string dir=ProjectSettings.GlobalizePath("user://terrain-capture");Directory.CreateDirectory(dir);foreach(var b in Enum.GetValues<TerrainBiome>()){biome.Selected=(int)b;Generate();await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var img=GetViewport().GetTexture().GetImage();img.SavePng(Path.Combine(dir,b+".png"));}GD.Print("TERRAIN_CAPTURE "+dir);GetTree().Quit();}
    public override void _ExitTree(){foreach(var (_,cache) in actors)cache.Dispose();actors.Clear();}
}
