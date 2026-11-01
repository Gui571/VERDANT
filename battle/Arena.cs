using Godot;
using Verdant.Forge;
namespace Verdant.Battle;
public partial class Arena : Control
{
    public static MonsterSpec? WorkshopMonster {get;set;}
    readonly List<ActorRecipe>[] teams={new(),new()};
    readonly List<ActorRecipe> cast=new();readonly List<SpriteCache> cache=new();
    readonly Dictionary<string,Button> buttons=new();
    OptionButton left=null!,right=null!;SpinBox seed=null!,quantity=null!;Label status=null!,roster=null!;
    BattleSimulation? sim;BattleSpec? previous;Fighter[] sorted=Array.Empty<Fighter>();ImageTexture? terrain;
    bool paused=true,bars=true,ranges;int speed=1;double accumulator;int shownTick=-1;
    readonly Vector2 origin=new(80,240);
    public override void _Ready()
    {
        TextureFilter=TextureFilterEnum.Nearest;var top=new VBoxContainer{Position=new(40,24),Size=new(1360,210)};AddChild(top);
        top.AddChild(new Label{Text="VERDANT / ARENA DO VALE · combate automático",Theme=null});
        var nav=new HFlowContainer();top.AddChild(nav);Button(nav,"Elenco",()=>GetTree().ChangeSceneToFile("res://pixel_demo/PixelDemo.tscn"));Button(nav,"Oficina",()=>GetTree().ChangeSceneToFile("res://workshop/Workshop.tscn"));
        seed=new SpinBox{MinValue=1,MaxValue=999999,Step=1,Value=42,CustomMinimumSize=new(140,0)};nav.AddChild(new Label{Text="Semente"});nav.AddChild(seed);quantity=new SpinBox{MinValue=1,MaxValue=10,Step=1,Value=3};nav.AddChild(new Label{Text="Quantidade"});nav.AddChild(quantity);
        var setup=new HFlowContainer();top.AddChild(setup);left=Selector(setup);Button(setup,"Adicionar azul",()=>Add(0,left.Selected));right=Selector(setup);right.Selected=8;Button(setup,"Adicionar coral",()=>Add(1,right.Selected));Button(setup,"Limpar times",()=>{teams[0].Clear();teams[1].Clear();Summary();});Button(setup,"Sortear confronto",()=>{seed.Value=GD.Randi()%999999+1;Randomize();});
        var controls=new HFlowContainer();top.AddChild(controls);Button(controls,"Iniciar",Start);Button(controls,"Pausar",()=>paused=!paused);Button(controls,"Velocidade",()=>{speed=speed==4?1:speed*2;buttons["Velocidade"].Text=$"Velocidade {speed}×";});Button(controls,"Passo",()=>{paused=true;sim?.Step();QueueRedraw();});Button(controls,"Barras",()=>{bars=!bars;QueueRedraw();});Button(controls,"Alcances",()=>{ranges=!ranges;QueueRedraw();});Button(controls,"Repetir",Repeat);Button(controls,"Nova batalha",()=>{seed.Value=seed.Value%999999+1;Randomize();Start();});
        roster=new Label();top.AddChild(roster);status=new Label{Position=new(80,860),Size=new(1280,75),AutowrapMode=TextServer.AutowrapMode.WordSmart};AddChild(status);
        BuildTerrain();teams[0].AddRange(new[]{ActorRecipe.FromHero(0),ActorRecipe.FromHero(3),ActorRecipe.FromHero(6)});for(ulong i=1;i<=3;i++)teams[1].Add(ActorRecipe.FromMonster(i));if(WorkshopMonster is {} edited)teams[1][0]=new(edited.Id,null,edited);Summary();status.Text="Monte os times e pressione Iniciar. A semente reproduz o confronto.";
        var args=OS.GetCmdlineUserArgs();if(args.Contains("--battle-ui-test"))Test();if(args.Contains("--battle-capture"))Capture();if(args.Contains("--battle-benchmark"))Benchmark();
    }
    OptionButton Selector(Container p){var s=new OptionButton();foreach(var h in ForgeRoster.All)s.AddItem(h.Name);s.AddItem("Herói por semente");foreach(var f in Enum.GetNames<MonsterFamily>())s.AddItem(f);p.AddChild(s);return s;}
    void Button(Container p,string name,Action a){var b=new Button{Text=name};b.Pressed+=()=>{try{a();}catch(Exception e){status.Text=e.Message;}};p.AddChild(b);buttons[name]=b;}
    void Add(int team,int choice){for(int i=0;i<(int)quantity.Value&&teams[team].Count<20;i++){ulong s=(ulong)seed.Value+(ulong)(teams[team].Count*31+team*100);teams[team].Add(choice<7?ActorRecipe.FromHero(choice):choice==7?ActorRecipe.Generated(s):ActorRecipe.FromMonster(s,(MonsterFamily)(choice-8)));}Summary();}
    void Summary(){roster.Text=$"Azul: {teams[0].Count}/20   ×   Coral: {teams[1].Count}/20 · seleção aplicada ao iniciar · {speed}×";}
    void Randomize(){teams[0].Clear();teams[1].Clear();var r=new Rng((ulong)seed.Value);for(int side=0;side<2;side++)for(int i=0;i<(int)quantity.Value;i++){int pick=r.Int(0,17);ulong s=r.Next();teams[side].Add(pick<7?ActorRecipe.FromHero(pick):pick==7?ActorRecipe.Generated(s):ActorRecipe.FromMonster(s,(MonsterFamily)(pick-8)));}Summary();}
    void Start(){if(teams.Any(t=>t.Count==0))throw new InvalidOperationException("Adicione pelo menos um combatente em cada time.");foreach(var c in cache)c.Dispose();cache.Clear();cast.Clear();cast.AddRange(teams[0]);cast.AddRange(teams[1]);foreach(var r in cast)cache.Add(new(r));previous=new((ulong)seed.Value,cast.Select((r,i)=>r.Fighter(i<teams[0].Count?0:1)).ToArray());Repeat();}
    void Repeat(){if(previous==null)return;sim=new(previous);sorted=sim.Fighters.ToArray();accumulator=0;paused=false;shownTick=-1;QueueRedraw();}
    public override void _Process(double dt)
    {
        if(sim==null)return;if(!paused&&!sim.Finished){accumulator+=Math.Min(dt,.25)*speed;while(accumulator>=1.0/30){sim.Step();accumulator-=1.0/30;}}
        if(shownTick!=sim.Tick){shownTick=sim.Tick;status.Text=sim.Finished?$"{(sim.Winner<0?"Empate por tempo":sim.Winner==0?"Vitória azul":"Vitória coral")} · {sim.Tick/30.0:F1}s · "+string.Join(" | ",sim.Fighters.Select(f=>$"{cast[f.Id].Hero?.Name??cast[f.Id].Monster!.Family.ToString()}: {f.Damage} dano / {f.Kills} abates")):$"Semente {sim.Spec.Seed} · {sim.Tick/30.0:F1}s · {(paused?"pausado":speed+"×")} · Azul {sim.Fighters.Count(f=>f.Alive&&f.Spec.Team==0)} × Coral {sim.Fighters.Count(f=>f.Alive&&f.Spec.Team==1)}";QueueRedraw();}
    }
    void BuildTerrain(){var px=new Raster(320,150);px.Rect(0,0,320,150,new Color("82985a"));px.Rect(8,8,304,136,new Color("92784c"));var r=new Rng(902);for(int i=0;i<700;i++){int x=r.Int(0,320),y=r.Int(0,150);px.Rect(x,y,2,1,new Color(i%3==0?"a58a5a":"7b693f"));}for(int i=0;i<18;i++){int x=r.Int(0,320),y=i%2==0?3:145;px.Rect(x,y,4,2,new Color("555b58"));px.Rect(x,y,3,1,new Color("9da38b"));}using var img=px.Image();terrain=ImageTexture.CreateFromImage(img);}
    Vector2 Screen(System.Numerics.Vector2 p)=>origin+new Vector2(MathF.Round(p.X)*4,MathF.Round(p.Y)*4);
    public override void _Draw()
    {
        if(terrain!=null)DrawTextureRect(terrain,new(origin,new(1280,600)),false);if(sim==null)return;
        Array.Sort(sorted,(a,b)=>a.Position.Y.CompareTo(b.Position.Y));
        foreach(var f in sorted){int elapsed=sim.Tick-f.DeathTick;if(!f.Alive&&elapsed>=23)continue;var p=Screen(f.Position);var c=cache[f.Id];MonsterMotion m;double t;
            if(!f.Alive){m=MonsterMotion.Death;t=elapsed/30.0;}else if(sim.Tick-f.LastHit<12){m=MonsterMotion.Hurt;t=(sim.Tick-f.LastHit)/30.0;}else if(sim.Tick-f.LastAttack<22){m=MonsterMotion.Attack;t=(sim.Tick-f.LastAttack)/30.0;}else{m=f.Moving?MonsterMotion.Walk:MonsterMotion.Idle;t=sim.Tick/30.0;}
            if(ranges&&f.Alive)DrawArc(p,f.Spec.Stats.Range*4,0,Mathf.Tau,32,new Color("d8ce97"),1,false);
            var start=p-new Vector2(c.Size/2*4,(c.Size-3)*4);bool flip=f.Target>=0&&sim.Fighters[f.Target].Position.X<f.Position.X;
            DrawTextureRect(c.Get(m,t),flip?new(start+new Vector2(c.Size*4,0),new(-c.Size*4,c.Size*4)):new(start,new(c.Size*4,c.Size*4)),false);
            if(bars&&f.Alive){var at=p-new Vector2(24,c.Size*4+2);DrawRect(new(at,new(48,8)),new Color("151829"));DrawRect(new(at+new Vector2(2,2),new(44f*f.Health/f.Spec.Stats.Health,4)),new Color(f.Spec.Team==0?"6aa9df":"e58a78"));}
        }
        for(int i=sim.Events.Count-1;i>=0;i--){var e=sim.Events[i];int age=sim.Tick-e.Tick;if(age>25)break;var p=origin+new Vector2(MathF.Round(e.X)*4,MathF.Round(e.Y)*4);
            if(e.Kind=="damage"||e.Kind=="heal"){DrawString(ThemeDB.FallbackFont,p+new Vector2(-8,-50-age),e.Kind=="heal"?"+"+e.Amount:e.Amount.ToString(),HorizontalAlignment.Left,-1,16,new Color(e.Kind=="heal"?"a5d889":"fff0c7"));if(age<4){DrawRect(new(p+new Vector2(-8,-40),new(16,4)),new Color("fff0c7"));DrawRect(new(p+new Vector2(-2,-46),new(4,16)),new Color("fff0c7"));}}
            if(e.Kind=="projectile"&&age<8){var a=Screen(sim.Fighters[e.Source].Position)-new Vector2(0,36);var b=p-new Vector2(0,36);var pos=a.Lerp(b,age/8f);pos=new(MathF.Round(pos.X/4)*4,MathF.Round(pos.Y/4)*4);DrawRect(new(pos,new(12,4)),new Color("ede7c8"));}
        }
    }
    async void Test(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);int checks=0;void Check(bool x,string m){checks++;if(!x)throw new Exception(m);}try{buttons["Limpar times"].EmitSignal(Godot.Button.SignalName.Pressed);quantity.Value=2;left.Selected=3;right.Selected=11;buttons["Adicionar azul"].EmitSignal(Godot.Button.SignalName.Pressed);buttons["Adicionar coral"].EmitSignal(Godot.Button.SignalName.Pressed);Check(teams.All(t=>t.Count==2),"teams");buttons["Iniciar"].EmitSignal(Godot.Button.SignalName.Pressed);Check(sim!=null&&!paused,"start");buttons["Pausar"].EmitSignal(Godot.Button.SignalName.Pressed);Check(paused,"pause");int t=sim!.Tick;buttons["Passo"].EmitSignal(Godot.Button.SignalName.Pressed);Check(sim.Tick==t+1,"step");buttons["Velocidade"].EmitSignal(Godot.Button.SignalName.Pressed);Check(speed==2,"speed2");buttons["Velocidade"].EmitSignal(Godot.Button.SignalName.Pressed);Check(speed==4,"speed4");sim.Run();Check(sim.Finished,"finish");var events=sim.Events.ToArray();buttons["Repetir"].EmitSignal(Godot.Button.SignalName.Pressed);sim!.Run();Check(events.SequenceEqual(sim.Events),"repeat");GD.Print($"BATTLE_UI_PASS checks={checks}");GetTree().Quit();}catch(Exception e){GD.PrintErr(e);GetTree().Quit(1);}}
    async void Capture(){Start();paused=true;string dir=ProjectSettings.GlobalizePath("user://battle-frames");Directory.CreateDirectory(dir);for(int f=0;f<48;f++){for(int i=0;i<8;i++)sim!.Step();QueueRedraw();await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var img=GetViewport().GetTexture().GetImage();img.SavePng(Path.Combine(dir,$"arena-{f:D2}.png"));}GD.Print("BATTLE_CAPTURE "+dir);GetTree().Quit();}
    async void Benchmark(){quantity.Value=10;Randomize();Start();var times=new List<double>();for(int f=0;f<360;f++){var watch=System.Diagnostics.Stopwatch.StartNew();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);times.Add(watch.Elapsed.TotalMilliseconds);}times.Sort();GD.Print($"BATTLE_BENCH fighters={cast.Count} median_ms={times[180]:F2} p95_ms={times[342]:F2}");GetTree().Quit();}
    public override void _ExitTree(){foreach(var c in cache)c.Dispose();terrain?.Dispose();}
}
