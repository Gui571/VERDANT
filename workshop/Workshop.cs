using Godot;
using Verdant.Forge;
using Verdant.Battle;
using System.Text.Json;
namespace Verdant;
public partial class Workshop : Control
{
    MonsterSpec current=MonsterGenerator.Generate(1042,MonsterFamily.Beast),parent=MonsterGenerator.Generate(1043,MonsterFamily.Beast);
    readonly Stack<MonsterSpec> undo=new(),redo=new();
    SpriteCache? cache;MonsterMotion motion;double clock;bool paused,syncing,back;
    OptionButton special=null!;readonly string[] specialIds=Abilities.All.Keys.OrderBy(x=>x).ToArray();
    OptionButton family=null!,element=null!,rank=null!,animation=null!;SpinBox seed=null!;Label status=null!;TextureRect preview=null!;
    readonly List<(HSlider slider,Func<MonsterSpec,int> get)> sliders=new();
    readonly Dictionary<string,Button> buttons=new();
    public override void _Ready()
    {
        var args=OS.GetCmdlineUserArgs();if(args.Any(a=>a is "--self-test" or "--ui-test" or "--gallery-capture")){GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile,"res://workshop/LegacyWorkshop.tscn");return;}
        TextureFilter=TextureFilterEnum.Nearest;
        var all=new VBoxContainer{Position=new(40,30),Size=new(1360,900)};AddChild(all);all.AddChild(new Label{Text="VERDANT / OFICINA DE MONSTROS"});
        var nav=new HFlowContainer();all.AddChild(nav);Button(nav,"Elenco",()=>GetTree().ChangeSceneToFile("res://pixel_demo/PixelDemo.tscn"));Button(nav,"Arena",()=>{Arena.WorkshopMonster=current;GetTree().ChangeSceneToFile("res://battle/Arena.tscn");});Button(nav,"Modo legado 192×160",()=>GetTree().ChangeSceneToFile("res://workshop/LegacyWorkshop.tscn"));
        var content=new HBoxContainer{SizeFlagsVertical=SizeFlags.ExpandFill};all.AddChild(content);var controls=new VBoxContainer{CustomMinimumSize=new(300,0)};content.AddChild(controls);
        family=Select<MonsterFamily>(controls,"Família",i=>Apply(current with{Family=(MonsterFamily)i}));
        seed=new SpinBox{MinValue=0,MaxValue=999999,Step=1,Value=current.Seed};controls.AddChild(new Label{Text="Semente"});controls.AddChild(seed);
        Button(controls,"Gerar",()=>Apply(MonsterGenerator.Generate((ulong)seed.Value,(MonsterFamily)family.Selected)));
        Button(controls,"Sortear",()=>Apply(MonsterGenerator.Generate(GD.Randi()%999999,(MonsterFamily)family.Selected)));
        Slider(controls,"Massa",s=>s.Mass,(s,v)=>s with{Mass=v});Slider(controls,"Cabeça",s=>s.Head,(s,v)=>s with{Head=v});Slider(controls,"Apêndices",s=>s.Appendages,(s,v)=>s with{Appendages=v});Slider(controls,"Padrão",s=>s.Pattern,(s,v)=>s with{Pattern=v});Slider(controls,"Cor",s=>s.Tint,(s,v)=>s with{Tint=v});
        element=Select<Element>(controls,"Elemento",i=>Apply(current with{Element=(Element)i}));rank=Select<Rank>(controls,"Tamanho / raridade",i=>Apply(current with{Rank=(Rank)i}));
        controls.AddChild(new Label{Text="Especial (família ou escolha livre)"});special=new OptionButton();special.AddItem("Assinatura da família");foreach(var id in specialIds)special.AddItem(Abilities.All[id].Name);special.ItemSelected+=i=>{if(!syncing)Apply(current with{SpecialId=i==0?null:specialIds[(int)i-1]});};controls.AddChild(special);
        Button(controls,"Mutar",()=>Apply(MonsterGenerator.Mutate(current,current.Seed+17)));Button(controls,"Guardar pai B",()=>{parent=current;status.Text="Pai B: "+parent.Id;});Button(controls,"Cruzar A × B",()=>Apply(MonsterGenerator.Cross(current,parent,current.Seed+31)));
        var history=new HBoxContainer();controls.AddChild(history);Button(history,"Desfazer",()=>{if(undo.TryPop(out var s)){redo.Push(current);Apply(s,false);}});Button(history,"Refazer",()=>{if(redo.TryPop(out var s)){undo.Push(current);Apply(s,false);}});
        var main=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};content.AddChild(main);
        preview=new TextureRect{CustomMinimumSize=new(800,580),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepCentered,SizeFlagsVertical=SizeFlags.ExpandFill};main.AddChild(preview);
        animation=Select<MonsterMotion>(main,"Animação",i=>{motion=(MonsterMotion)i;clock=0;});var actions=new HFlowContainer();main.AddChild(actions);Button(actions,"Pausar",()=>paused=!paused);Button(actions,"Passo",()=>{paused=true;clock+=.125;Refresh();});Button(actions,"Frente / costas",()=>{back=!back;Rebuild();});Button(actions,"Exportar PNG + atlas + JSON",Export);
        status=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart};all.AddChild(status);Apply(current,false);
        if(args.Contains("--monster-ui-test"))Test();if(args.Contains("--monster-capture"))Capture();
    }
    OptionButton Select<T>(Container parent,string label,Action<int> action)where T:struct,Enum{parent.AddChild(new Label{Text=label});var o=new OptionButton();foreach(var n in Enum.GetNames<T>())o.AddItem(n);o.ItemSelected+=i=>{if(!syncing)Safe(()=>action((int)i));};parent.AddChild(o);return o;}
    void Button(Container p,string name,Action action){var b=new Button{Text=name};b.Pressed+=()=>Safe(action);p.AddChild(b);buttons[name]=b;}
    void Safe(Action a){try{a();}catch(Exception e){status.Text=e.Message;}}
    void Slider(Container p,string name,Func<MonsterSpec,int> get,Func<MonsterSpec,int,MonsterSpec> set){p.AddChild(new Label{Text=name});var s=new HSlider{MinValue=0,MaxValue=3,Step=1};s.ValueChanged+=v=>{if(!syncing)Apply(set(current,(int)v));};p.AddChild(s);sliders.Add((s,get));}
    void Apply(MonsterSpec s,bool remember=true){s.Validate();if(remember){undo.Push(current);redo.Clear();}current=s;syncing=true;special.Selected=current.SpecialId==null?0:Array.IndexOf(specialIds,current.SpecialId)+1;family.Selected=(int)s.Family;element.Selected=(int)s.Element;rank.Selected=(int)s.Rank;seed.Value=s.Seed;foreach(var (slider,get)in sliders)slider.Value=get(s);syncing=false;Rebuild();status.Text=$"{s.Family} / {s.Element} / {s.Rank} · {s.Size}×{s.Size} · #{s.Seed}";}
    void Rebuild(){cache?.Dispose();cache=new(new(current.Id,null,current),back);clock=0;Refresh();}
    void Refresh(){if(cache==null)return;using var img=cache.Get(motion,clock).GetImage();img.Resize(current.Size*8,current.Size*8,Image.Interpolation.Nearest);var old=preview.Texture;preview.Texture=ImageTexture.CreateFromImage(img);old?.Dispose();}
    public override void _Process(double dt){if(paused||cache==null)return;int before=(int)(clock*8);clock+=dt;if((int)(clock*8)!=before)Refresh();}
    void Export(){string dir=ProjectSettings.GlobalizePath("user://monsters");Directory.CreateDirectory(dir);var r=new MonsterRenderer(current);using(var img=r.Render(clock,motion,back).Image())img.SavePng(Path.Combine(dir,current.Id+".png"));foreach(var m in Enum.GetValues<MonsterMotion>()){int n=MonsterRenderer.Frames(m),s=current.Size;using var sheet=Image.CreateEmpty(s*n,s,false,Image.Format.Rgba8);for(int f=0;f<n;f++){using var img=r.Render(f/(double)MonsterRenderer.Fps(m),m,back).Image();sheet.BlitRect(img,new(0,0,s,s),new(f*s,0));}string path=Path.Combine(dir,current.Id+"-"+m);sheet.SavePng(path+".png");File.WriteAllText(path+".json",JsonSerializer.Serialize(new{version=1,recipe=current,width=s,height=s,origin=new[]{s/2,s-3},frames=n,columns=n,fps=MonsterRenderer.Fps(m),state=m.ToString(),loop=m<=MonsterMotion.Run,back}));}status.Text="Exportado: "+dir;}
    async void Test(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);int n=0;void Check(bool b,string m){n++;if(!b)throw new Exception(m);}try{foreach(var f in Enum.GetValues<MonsterFamily>()){family.Selected=(int)f;family.EmitSignal(OptionButton.SignalName.ItemSelected,(long)f);Check(current.Family==f,"family");}seed.Value=42;buttons["Gerar"].EmitSignal(Godot.Button.SignalName.Pressed);Check(current.Seed==42,"seed");var before=current;buttons["Mutar"].EmitSignal(Godot.Button.SignalName.Pressed);Check(current!=before,"mutate");buttons["Desfazer"].EmitSignal(Godot.Button.SignalName.Pressed);Check(current==before,"undo");buttons["Refazer"].EmitSignal(Godot.Button.SignalName.Pressed);Check(current!=before,"redo");buttons["Guardar pai B"].EmitSignal(Godot.Button.SignalName.Pressed);buttons["Cruzar A × B"].EmitSignal(Godot.Button.SignalName.Pressed);Check(current.Family==parent.Family,"cross");foreach(var m in Enum.GetValues<MonsterMotion>()){animation.EmitSignal(OptionButton.SignalName.ItemSelected,(long)m);Check(motion==m,"animation");}using(var cached=cache!.Get(MonsterMotion.Special,.375).GetImage())using(var expected=new MonsterRenderer(current).Render(.375,MonsterMotion.Special,back).Image())Check(cached.GetData().SequenceEqual(expected.GetData()),"atlas matches monster render");Export();Check(File.Exists(ProjectSettings.GlobalizePath("user://monsters/"+current.Id+".png")),"export");GD.Print($"MONSTER_UI_PASS checks={n}");GetTree().Quit();}catch(Exception e){GD.PrintErr(e);GetTree().Quit(1);}}
    async void Capture(){await ToSignal(GetTree().CreateTimer(.3),SceneTreeTimer.SignalName.Timeout);using var img=GetViewport().GetTexture().GetImage();img.SavePng("user://monster-workshop.png");GetTree().Quit();}
    public override void _ExitTree(){cache?.Dispose();preview?.Texture?.Dispose();}
}
