using Godot;
using System.Security.Cryptography;
namespace Verdant;
public partial class PixelDemo : Control
{
    public static AdventurerKind InitialCharacter {get;set;}=AdventurerKind.Duck;
    CompactAdventurer sprite = new();
    ImageTexture? texture, terrain;
    double time, accumulator;
    bool paused, back, landscape=true;
    AdventurerMotion motion;
    double motionStarted;
    int zoom=4;
    Label status=null!;
    public override void _Ready()
    {
        var args=OS.GetCmdlineUserArgs();
        if(args.Contains("--self-test")||args.Contains("--ui-test")||args.Contains("--gallery-capture")) { GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile,"res://workshop/Workshop.tscn"); return; }
        if(args.Contains("--pixel-test")){GetTree().Quit(ValidateSprite());return;}
        sprite=new CompactAdventurer(1,InitialCharacter);
        TextureFilter=TextureFilterEnum.Nearest;BuildTerrain();

        var root=new VBoxContainer{Position=new Vector2(60,32),Size=new Vector2(1320,896)};AddChild(root);
        var title=new Label{Text="VERDANT  /  PEQUENOS VIAJANTES"};title.AddThemeColorOverride("font_color",new Color("ede5cb"));title.AddThemeFontSizeOverride("font_size",26);var titleRow=new HBoxContainer();root.AddChild(titleRow);title.SizeFlagsHorizontal=SizeFlags.ExpandFill;titleRow.AddChild(title);AddButton(titleRow,"Guia do elenco",()=>GetTree().ChangeSceneToFile("res://pixel_demo/RosterBoard.tscn"));
        var desc=new Label{Text="Patrulha do vale     •     32 × 32 pixels     •     animação em camadas"};desc.AddThemeColorOverride("font_color",new Color("a2b3cc"));root.AddChild(desc);
        var spacer=new Control{SizeFlagsVertical=SizeFlags.ExpandFill};root.AddChild(spacer);
        var actions=new HBoxContainer{Alignment=BoxContainer.AlignmentMode.Center};actions.AddThemeConstantOverride("separation",12);root.AddChild(actions);
        var choose=new OptionButton{FitToLongestItem=false,CustomMinimumSize=new Vector2(170,0)};choose.AddThemeFontSizeOverride("font_size",14);foreach(var entry in RosterCatalog.All)choose.AddItem(entry.Name+" · "+entry.Class,(int)entry.Kind);choose.Select(choose.GetItemIndex((int)InitialCharacter));choose.ItemSelected+=i=>{InitialCharacter=(AdventurerKind)choose.GetItemId((int)i);sprite=new CompactAdventurer(1,InitialCharacter);status.Text=sprite.Description;Refresh();};actions.AddChild(choose);
        AddButton(actions,"Pausar / animar",()=>paused=!paused);
        AddButton(actions,"Parado / caminhar",()=>PlayMotion(motion==AdventurerMotion.Walk?AdventurerMotion.Idle:AdventurerMotion.Walk));
        AddButton(actions,"Frente / costas",()=>{back=!back;Refresh();});
        AddButton(actions,"Ampliar",()=>{zoom=zoom==8?2:zoom*2;QueueRedraw();});
        AddButton(actions,"Cenário / branco",()=>{landscape=!landscape;QueueRedraw();});
        AddButton(actions,"Exportar PNG",Export);
        var extra=new MenuButton{Text="Ações"};foreach(string label in new[]{"Correr","Atacar","Receber dano"})extra.GetPopup().AddItem(label);extra.GetPopup().IdPressed+=i=>PlayMotion(i==0?AdventurerMotion.Run:i==1?AdventurerMotion.Attack:AdventurerMotion.Hurt);actions.AddChild(extra);
        AddButton(actions,"Oficina procedural",()=>GetTree().ChangeSceneToFile("res://workshop/Workshop.tscn"));
        status=new Label{Text=sprite.Description,HorizontalAlignment=HorizontalAlignment.Center};status.AddThemeColorOverride("font_color",new Color("a2b3cc"));root.AddChild(status);
        Refresh();
        if(args.Contains("--pixel-ui-test")){if(DisplayServer.GetName()=="headless"){GD.PrintErr("Pixel UI texture tests require an actual display; use --pixel-test for headless validation.");GetTree().Quit(2);return;}RunPixelUiTests();}
        if(args.Contains("--pixel-capture"))Capture();
        if(args.Contains("--pixel-frames")){ExportFrames();GetTree().Quit();}
    }
    async void RunPixelUiTests()
    {
        int checks=0;void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
        IEnumerable<Node> Descendants(Node node){foreach(var child in node.GetChildren()){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
        try{
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var selector=Descendants(this).OfType<OptionButton>().Single();
            for(int i=0;i<RosterCatalog.All.Count;i++){selector.Select(i);selector.EmitSignal(OptionButton.SignalName.ItemSelected,(long)i);Check((int)sprite.Kind==i,"character selector");}
            var buttons=Descendants(this).OfType<Button>().ToList();
            buttons.Single(x=>x.Text=="Pausar / animar").EmitSignal(Button.SignalName.Pressed);Check(paused,"pause");
            using var before=texture!.GetImage();buttons.Single(x=>x.Text=="Frente / costas").EmitSignal(Button.SignalName.Pressed);using var after=texture.GetImage();Check(!before.GetData().SequenceEqual(after.GetData()),"facing updates while paused");
            var actions=Descendants(this).OfType<MenuButton>().Single();actions.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,1L);Check(motion==AdventurerMotion.Attack,"attack menu");
            buttons.Single(x=>x.Text=="Pausar / animar").EmitSignal(Button.SignalName.Pressed);await ToSignal(GetTree().CreateTimer(.9),SceneTreeTimer.SignalName.Timeout);Check(motion==AdventurerMotion.Idle,"attack recovery");
            actions.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,2L);Check(motion==AdventurerMotion.Hurt,"hurt menu");await ToSignal(GetTree().CreateTimer(.6),SceneTreeTimer.SignalName.Timeout);Check(motion==AdventurerMotion.Idle,"hurt recovery");
            GD.Print("PIXEL_UI_PASS checks="+checks);GetTree().Quit();
        }catch(Exception e){GD.PrintErr(e);GetTree().Quit(1);}
    }
    void BuildTerrain()
    {
        // Native density matches the character at the default 4x world scale.
        var r=new Raster(320,160);r.Rect(0,0,320,160,new Color("1c2031"));r.Rect(0,52,320,108,new Color("839d59"));r.Rect(0,52,320,1,new Color("bab98b"));
        r.Ellipse(175,118,110,38,new Color("6d7047"));r.Ellipse(175,113,108,35,new Color("937b50"));r.Ellipse(182,108,90,30,new Color("9c8254"));r.Ellipse(30,78,55,20,new Color("6d7047"));r.Ellipse(30,75,55,20,new Color("937b50"));
        var random=new RandomStream(717);
        for(int i=0;i<330;i++){int x=(int)random.Range(1,316),y=(int)random.Range(56,157);float d=(x-175)*(x-175)/12100f+(y-113)*(y-113)/1225f;bool dirt=d<1||(x<80&&y<86);var c=new Color(dirt?"b39b68":i%2==0?"699052":"a8b56a");r.Rect(x,y,2,1,c);if(!dirt&&i%3==0){r.Rect(x+1,y-1,1,2,c);r.Rect(x+2,y,2,1,c);}}
        // Fixed world contact remains aligned through every integer zoom.
        r.Ellipse(160,121,11,2,new Color("716343"));
        using var img=r.Image();terrain=ImageTexture.CreateFromImage(img);
    }
    void AddButton(Container parent,string text,Action action){var b=new Button{Text=text};b.AddThemeStyleboxOverride("normal",new StyleBoxFlat{BgColor=new Color("28354b"),ContentMarginLeft=10,ContentMarginRight=10,ContentMarginTop=14,ContentMarginBottom=14,CornerRadiusTopLeft=6,CornerRadiusTopRight=6,CornerRadiusBottomLeft=6,CornerRadiusBottomRight=6});b.Pressed+=action;parent.AddChild(b);}
    void PlayMotion(AdventurerMotion next){motion=next;motionStarted=time;Refresh();}
    double PoseTime=>motion is AdventurerMotion.Attack or AdventurerMotion.Hurt?time-motionStarted:time;
    void Refresh(){using var img=sprite.Render(PoseTime,motion,back).Image();if(texture==null)texture=ImageTexture.CreateFromImage(img);else texture.Update(img);QueueRedraw();}
    public override void _Process(double delta){if(texture==null||paused)return;time+=delta;if((motion==AdventurerMotion.Attack&&PoseTime>=.75)||(motion==AdventurerMotion.Hurt&&PoseTime>=.5))motion=AdventurerMotion.Idle;accumulator+=delta;if(accumulator>=.125){accumulator%=.125;Refresh();}}
    public override void _Draw(){DrawRect(new Rect2(Vector2.Zero,Size),new Color("1c2031"));if(texture==null)return;var rect=new Rect2(new Vector2(80,135),new Vector2(1280,640));DrawStyleBox(new StyleBoxFlat{BgColor=Colors.White,CornerRadiusTopLeft=16,CornerRadiusTopRight=16,CornerRadiusBottomLeft=16,CornerRadiusBottomRight=16},rect);if(landscape&&terrain!=null){if(zoom>=4)DrawTextureRectRegion(terrain,rect,new Rect2(160-640/zoom,120-480/zoom,1280/zoom,640/zoom));else DrawTextureRect(terrain,new Rect2(rect.Position+new Vector2(640-160*zoom,480-120*zoom),new Vector2(320,160)*zoom),false);}var size=new Vector2(32,32)*zoom;var origin=landscape?new Vector2(rect.GetCenter().X-16*zoom,rect.Position.Y+480-29*zoom):rect.GetCenter()-size/2;DrawTextureRect(texture,new Rect2(origin,size),false);}
    void Export(){string path=ProjectSettings.GlobalizePath("user://compact-"+sprite.Kind.ToString().ToLowerInvariant()+".png");using var img=sprite.Render(PoseTime,motion,back).Image();var error=img.SavePng(path);status.Text=error==Error.Ok?"PNG transparente salvo em: "+path:"Falha ao exportar: "+error;}
    async void Capture(){await ToSignal(GetTree().CreateTimer(.2),SceneTreeTimer.SignalName.Timeout);using var img=GetViewport().GetTexture().GetImage();img.SavePng("user://pixel-demo.png");GetTree().Quit();}
    void ExportFrames(){string path=ProjectSettings.GlobalizePath("user://pixel-frames");Directory.CreateDirectory(path);for(int i=0;i<32;i++){using var img=sprite.Render(i/8.0).Image();using var frame=Image.CreateEmpty(64,64,false,Image.Format.Rgba8);frame.Fill(Colors.White);frame.BlendRect(img,new Rect2I(0,0,32,32),new Vector2I(16,16));frame.Resize(512,512,Image.Interpolation.Nearest);frame.SavePng(Path.Combine(path,$"frame-{i:D2}.png"));}
        using var sheet=Image.CreateEmpty(32*8,32*4,false,Image.Format.Rgba8);
        for(int i=0;i<32;i++){using var cell=sprite.Render(i/8.0).Image();sheet.BlitRect(cell,new Rect2I(0,0,32,32),new Vector2I(i%8*32,i/8*32));}
        sheet.SavePng("user://guardian-idle-sheet.png");
        File.WriteAllText(ProjectSettings.GlobalizePath("user://guardian-idle-sheet.json"),System.Text.Json.JsonSerializer.Serialize(new{version=1,width=32,height=32,columns=8,frames=32,fps=8,duration=4,loop=true,origin=new[]{16,29},state="idle"}));
        File.WriteAllText(ProjectSettings.GlobalizePath("user://roster-catalog.json"),System.Text.Json.JsonSerializer.Serialize(new{version=1,cell=RosterVisualRules.Cell,origin=new[]{16,29},maxColors=RosterVisualRules.MaxColors,characters=RosterCatalog.All},new System.Text.Json.JsonSerializerOptions{WriteIndented=true,Converters={new System.Text.Json.Serialization.JsonStringEnumConverter()}}));
        ExportParty();Export();}
    void ExportParty()
    {
        string dir=ProjectSettings.GlobalizePath("user://party-frames");Directory.CreateDirectory(dir);
        var cast=Enum.GetValues<AdventurerKind>().Select(k=>new CompactAdventurer(1,k)).ToArray();
        foreach(var actor in cast)
        {
            string key="compact-"+actor.Kind.ToString().ToLowerInvariant();
            using var asset=actor.Render(0).Image();asset.SavePng("user://"+key+".png");
            using var atlas=Image.CreateEmpty(256,128,false,Image.Format.Rgba8);
            for(int frame=0;frame<32;frame++){using var cell=actor.Render(frame/8.0).Image();atlas.BlitRect(cell,new Rect2I(0,0,32,32),new Vector2I(frame%8*32,frame/8*32));}
            atlas.SavePng("user://"+key+"-idle.png");
            foreach(var state in new[]{AdventurerMotion.Walk,AdventurerMotion.Run,AdventurerMotion.Attack,AdventurerMotion.Hurt})
            {
                int frames=state==AdventurerMotion.Attack?6:state==AdventurerMotion.Hurt?4:8;
                int fps=state==AdventurerMotion.Run?12:8;string action=state.ToString().ToLowerInvariant();
                using var sheet=Image.CreateEmpty(256,32,false,Image.Format.Rgba8);
                for(int f=0;f<frames;f++){using var cell=actor.Render(f/(double)fps,state).Image();sheet.BlitRect(cell,new Rect2I(0,0,32,32),new Vector2I(f*32,0));}
                sheet.SavePng("user://"+key+"-"+action+".png");
                File.WriteAllText(ProjectSettings.GlobalizePath("user://"+key+"-"+action+".json"),System.Text.Json.JsonSerializer.Serialize(new{version=1,character=actor.Name,width=32,height=32,columns=8,frames,fps,duration=frames/(double)fps,origin=new[]{16,29},state=action,loop=state is AdventurerMotion.Walk or AdventurerMotion.Run}));
            }

            File.WriteAllText(ProjectSettings.GlobalizePath("user://"+key+"-idle.json"),System.Text.Json.JsonSerializer.Serialize(new{version=1,character=actor.Name,width=32,height=32,columns=8,frames=32,fps=8,duration=4,origin=new[]{16,29},state="idle",loop=true}));
        }
        for(int i=0;i<32;i++)
        {
            using var party=Image.CreateEmpty(160,112,false,Image.Format.Rgba8);party.Fill(new Color("1c2031"));
            for(int a=0;a<cast.Length;a++){using var actor=cast[a].Render(i/8.0).Image();party.BlendRect(actor,new Rect2I(0,0,32,32),new Vector2I(10+(a%4)*38,8+(a/4)*48));}
            party.Resize(960,672,Image.Interpolation.Nearest);party.SavePng(Path.Combine(dir,$"frame-{i:D2}.png"));
        }
    }
    int ValidateSprite(){int checks=0;void Assert(bool ok,string name){checks++;if(!ok)throw new Exception(name);}try{foreach(var kind in Enum.GetValues<AdventurerKind>())foreach(float head in new[]{.85f,1f,1.15f}){var r=new CompactAdventurer(head,kind);for(int i=0;i<32;i++){var a=r.Render(i/8.0);Assert(a.Clipped==0,"sprite cropped");var pixels=(byte[])a.Pixels.Clone();Assert(pixels.SequenceEqual(new CompactAdventurer(head,kind).Render(i/8.0).Pixels),"determinism");var colors=new HashSet<uint>();for(int p=0;p<pixels.Length;p+=4){Assert(pixels[p+3]==0||pixels[p+3]==255,"anti-aliasing");if(pixels[p+3]!=0)colors.Add(BitConverter.ToUInt32(pixels,p));}Assert(colors.Count<=16,"palette budget");for(int y=28;y<29;y++)for(int x=10;x<22;x++)Assert(pixels.AsSpan((y*32+x)*4,4).SequenceEqual(r.Render(0).Pixels.AsSpan((y*32+x)*4,4)),"idle foot movement");}}foreach(var kind in Enum.GetValues<AdventurerKind>()){var actor=new CompactAdventurer(1,kind);var loopStart=(byte[])actor.Render(0).Pixels.Clone();Assert(loopStart.SequenceEqual(actor.Render(4).Pixels),"character loop closure");var variants=new HashSet<string>();for(int t=0;t<32;t++)variants.Add(Convert.ToHexString(SHA256.HashData(actor.Render(t/8.0).Pixels)));Assert(variants.Count>=4,"layered idle has distinct poses");foreach(var state in Enum.GetValues<AdventurerMotion>())foreach(bool rearView in new[]{false,true})for(int f=0;f<32;f++){actor.Render(f/16.0,state,rearView);Assert(actor.Buffer.Clipped==0,$"bounds: {kind}/{state}/{f}");}foreach(bool rear in new[]{false,true})foreach(bool walk in new[]{false,true})for(int i=0;i<32;i++){actor.Render(i/8.0,walk,rear);Assert(actor.Buffer.Clipped==0,"direction/action bounds");}}var silhouettes=new HashSet<string>();foreach(var kind in Enum.GetValues<AdventurerKind>()){var pixels=new CompactAdventurer(1,kind).Render(0).Pixels;var alpha=new byte[1024];for(int i=0;i<1024;i++)alpha[i]=pixels[i*4+3];Assert(silhouettes.Add(Convert.ToHexString(SHA256.HashData(alpha))),"unique class silhouette");}
        foreach(var pair in new[]{(AdventurerKind.Duck,"bram"),(AdventurerKind.Rabbit,"nix"),(AdventurerKind.Knight,"rowan")}){using var baseline=Image.LoadFromFile(ProjectSettings.GlobalizePath("res://docs/pixel/refactor-"+pair.Item2+".png"));Assert(baseline.GetData().SequenceEqual(new CompactAdventurer(1,pair.Item1).Render(0).Pixels),"approved sprite unchanged");}
        var first=(byte[])sprite.Render(0).Pixels.Clone();Assert(!first.SequenceEqual(sprite.Render(.875).Pixels),"idle actually animates");Assert(first.SequenceEqual(sprite.Render(4).Pixels),"seamless 4s loop");GD.Print("PIXEL_TEST_PASS assertions="+checks);return 0;}catch(Exception e){GD.PrintErr(e);return 1;}}
    public override void _ExitTree(){texture?.Dispose();terrain?.Dispose();}
}
