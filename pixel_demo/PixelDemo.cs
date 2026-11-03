using Godot;
using System.Security.Cryptography;
using Verdant.Forge;
namespace Verdant;
public partial class PixelDemo : Control
{
    public static AdventurerKind InitialCharacter {get;set;}=AdventurerKind.Duck;
    CompactAdventurer sprite = new();
    ImageTexture? texture, terrain;
    double time, accumulator;
    bool paused, back, landscape=true, flip, shadow=true;
    SpinBox seedBox=null!;
    AdventurerMotion motion;
    double motionStarted;
    int zoom=4;
    Label status=null!;
    public override void _Ready()
    {
        var args=OS.GetCmdlineUserArgs();
        if(args.Contains("--forge-hashes")){string path=ProjectSettings.GlobalizePath("user://forge-hashes.json");ForgeHashes.Write(path);GD.Print("FORGE_HASHES "+path);GetTree().Quit();return;}
        if(args.Contains("--self-test")||args.Contains("--ui-test")||args.Contains("--gallery-capture")) { GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile,"res://workshop/Workshop.tscn"); return; }
        if(args.Contains("--pixel-test")){GetTree().Quit(ValidateSprite());return;}
        sprite=new CompactAdventurer(1,InitialCharacter);
        TextureFilter=TextureFilterEnum.Nearest;BuildTerrain();

        var root=new VBoxContainer{Position=new Vector2(60,32),Size=new Vector2(1320,896)};AddChild(root);
        var title=new Label{Text="VERDANT  /  PEQUENOS VIAJANTES"};title.AddThemeColorOverride("font_color",new Color("ede5cb"));title.AddThemeFontSizeOverride("font_size",26);var titleRow=new HBoxContainer();root.AddChild(titleRow);title.SizeFlagsHorizontal=SizeFlags.ExpandFill;titleRow.AddChild(title);AddButton(titleRow,"Guia do elenco",()=>GetTree().ChangeSceneToFile("res://pixel_demo/RosterBoard.tscn"));
        var desc=new Label{Text="Patrulha do vale     •     32 × 32 pixels     •     animação em camadas"};desc.AddThemeColorOverride("font_color",new Color("a2b3cc"));root.AddChild(desc);
        var spacer=new Control{SizeFlagsVertical=SizeFlags.ExpandFill};root.AddChild(spacer);
        var actions=new HFlowContainer{};actions.AddThemeConstantOverride("h_separation",12);actions.AddThemeConstantOverride("v_separation",8);root.AddChild(actions);
        var choose=new OptionButton{FitToLongestItem=false,CustomMinimumSize=new Vector2(170,0)};choose.AddThemeFontSizeOverride("font_size",14);foreach(var entry in RosterCatalog.All)choose.AddItem(entry.Name+" · "+entry.Class,(int)entry.Kind);choose.Select(choose.GetItemIndex((int)InitialCharacter));choose.ItemSelected+=i=>{InitialCharacter=(AdventurerKind)choose.GetItemId((int)i);sprite=new CompactAdventurer(1,InitialCharacter);Use(new CompactAdventurer(1,InitialCharacter));};actions.AddChild(choose);
        AddButton(actions,"Pausar / animar",()=>paused=!paused);
        AddButton(actions,"Parado / caminhar",()=>PlayMotion(motion==AdventurerMotion.Walk?AdventurerMotion.Idle:AdventurerMotion.Walk));
        AddButton(actions,"Frente / costas",()=>{back=!back;Refresh();});
        AddButton(actions,"Ampliar",()=>{zoom=zoom==8?2:zoom*2;QueueRedraw();});
        AddButton(actions,"Cenário / branco",()=>{landscape=!landscape;QueueRedraw();});
        AddButton(actions,"Exportar PNG",Export);
        seedBox=new SpinBox{MinValue=1,MaxValue=999999,Value=1,Step=1,CustomMinimumSize=new Vector2(110,0),TooltipText="Semente do personagem gerado"};actions.AddChild(seedBox);
        AddButton(actions,"Gerar",()=>Use(new CompactAdventurer(ForgeGenerator.Generate((ulong)seedBox.Value))));
        AddButton(actions,"Sortear",()=>{seedBox.Value=(GD.Randi()%999999)+1;Use(new CompactAdventurer(ForgeGenerator.Generate((ulong)seedBox.Value)));});
        AddButton(actions,"Espelhar",()=>{flip=!flip;QueueRedraw();});
        AddButton(actions,"Sombra",()=>{shadow=!shadow;sprite.GroundShadow=shadow;Refresh();});
        var extra=new MenuButton{Text="Ações"};foreach(string label in new[]{"Correr","Atacar","Receber dano","Especial","Conjurar"})extra.GetPopup().AddItem(label);extra.GetPopup().IdPressed+=i=>PlayMotion(i==0?AdventurerMotion.Run:i==1?AdventurerMotion.Attack:i==2?AdventurerMotion.Hurt:i==3?AdventurerMotion.Special:AdventurerMotion.Cast);actions.AddChild(extra);
        AddButton(actions,"Arena",()=>GetTree().ChangeSceneToFile("res://battle/Arena.tscn"));
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
            seedBox.Value=42;buttons.Single(x=>x.Text=="Gerar").EmitSignal(Button.SignalName.Pressed);Check(sprite.Spec.Id=="g42","seed generation");
            using var gen42=sprite.Render(0).Image();buttons.Single(x=>x.Text=="Gerar").EmitSignal(Button.SignalName.Pressed);using var again=sprite.Render(0).Image();Check(gen42.GetData().SequenceEqual(again.GetData()),"seed determinism in UI");
            buttons.Single(x=>x.Text=="Sombra").EmitSignal(Button.SignalName.Pressed);Check(!sprite.GroundShadow,"shadow toggle");
            buttons.Single(x=>x.Text=="Sortear").EmitSignal(Button.SignalName.Pressed);Check(sprite.Spec.Id=="g"+(ulong)seedBox.Value,"random generation");
            actions.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,0L);Check(motion==AdventurerMotion.Run,"run menu");
            buttons.Single(x=>x.Text=="Parado / caminhar").EmitSignal(Button.SignalName.Pressed);Check(motion==AdventurerMotion.Walk,"walk toggle");
            buttons.Single(x=>x.Text=="Espelhar").EmitSignal(Button.SignalName.Pressed);Check(flip,"mirror toggle");
            int oldZoom=zoom;buttons.Single(x=>x.Text=="Ampliar").EmitSignal(Button.SignalName.Pressed);Check(zoom!=oldZoom,"zoom");
            paused=true;buttons.Single(x=>x.Text=="Exportar PNG").EmitSignal(Button.SignalName.Pressed);
            using var saved=Image.LoadFromFile(ProjectSettings.GlobalizePath("user://compact-"+sprite.Spec.Id+".png"));
            using var expected=sprite.Render(PoseTime,motion,back).Image();expected.FlipX();Check(saved.GetData().SequenceEqual(expected.GetData()),"export matches mirrored pose");
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(var button in buttons.Where(b=>b.IsVisibleInTree()))Check(GetViewportRect().Encloses(button.GetGlobalRect()),"button outside viewport: "+button.Text);
            GD.Print("PIXEL_UI_PASS checks="+checks);GetTree().Quit();
        }catch(Exception e){GD.PrintErr(e);GetTree().Quit(1);}
    }
    void BuildTerrain()
    {
        // Native density matches the character at the default 4x world scale. The ground shadow is part of the sprite now.
        var r=new Raster(320,160);r.Rect(0,0,320,160,new Color("1c2031"));r.Rect(0,52,320,108,new Color("839d59"));r.Rect(0,52,320,1,new Color("bab98b"));
        r.Ellipse(175,118,110,38,new Color("6d7047"));r.Ellipse(175,113,108,35,new Color("937b50"));r.Ellipse(182,108,90,30,new Color("9c8254"));r.Ellipse(30,78,55,20,new Color("6d7047"));r.Ellipse(30,75,55,20,new Color("937b50"));
        var random=new RandomStream(717);
        for(int i=0;i<330;i++){int x=(int)random.Range(1,316),y=(int)random.Range(56,157);float d=(x-175)*(x-175)/12100f+(y-113)*(y-113)/1225f;bool dirt=d<1||(x<80&&y<86);var c=new Color(dirt?"b39b68":i%2==0?"699052":"a8b56a");r.Rect(x,y,2,1,c);if(!dirt&&i%3==0){r.Rect(x+1,y-1,1,2,c);r.Rect(x+2,y,2,1,c);}}
        using var img=r.Image();terrain=ImageTexture.CreateFromImage(img);
    }
    void AddButton(Container parent,string text,Action action){var b=new Button{Text=text};b.AddThemeStyleboxOverride("normal",new StyleBoxFlat{BgColor=new Color("28354b"),ContentMarginLeft=10,ContentMarginRight=10,ContentMarginTop=14,ContentMarginBottom=14,CornerRadiusTopLeft=6,CornerRadiusTopRight=6,CornerRadiusBottomLeft=6,CornerRadiusBottomRight=6});b.Pressed+=action;parent.AddChild(b);}
    void Use(CompactAdventurer next){sprite=next;sprite.GroundShadow=shadow;status.Text=sprite.Name+" — "+sprite.Description;Refresh();}
    void PlayMotion(AdventurerMotion next){motion=next;motionStarted=time;Refresh();}
    double PoseTime=>motion is AdventurerMotion.Attack or AdventurerMotion.Hurt or AdventurerMotion.Special or AdventurerMotion.Cast?time-motionStarted:time;
    void Refresh(){using var img=sprite.Render(PoseTime,motion,back).Image();if(texture==null)texture=ImageTexture.CreateFromImage(img);else texture.Update(img);QueueRedraw();}
    public override void _Process(double delta){if(texture==null||paused)return;time+=delta;if((motion==AdventurerMotion.Attack&&PoseTime>=.75)||(motion==AdventurerMotion.Hurt&&PoseTime>=.5)||((motion is AdventurerMotion.Special or AdventurerMotion.Cast)&&PoseTime>=1))motion=AdventurerMotion.Idle;accumulator+=delta;if(accumulator>=.125){accumulator%=.125;Refresh();}}
    public override void _Draw(){DrawRect(new Rect2(Vector2.Zero,Size),new Color("1c2031"));if(texture==null)return;var rect=new Rect2(new Vector2(80,135),new Vector2(1280,640));DrawStyleBox(new StyleBoxFlat{BgColor=Colors.White,CornerRadiusTopLeft=16,CornerRadiusTopRight=16,CornerRadiusBottomLeft=16,CornerRadiusBottomRight=16},rect);if(landscape&&terrain!=null){if(zoom>=4)DrawTextureRectRegion(terrain,rect,new Rect2(160-640/zoom,120-480/zoom,1280/zoom,640/zoom));else DrawTextureRect(terrain,new Rect2(rect.Position+new Vector2(640-160*zoom,480-120*zoom),new Vector2(320,160)*zoom),false);}var size=new Vector2(32,32)*zoom;var origin=landscape?new Vector2(rect.GetCenter().X-16*zoom,rect.Position.Y+480-29*zoom):rect.GetCenter()-size/2;DrawTextureRect(texture,flip?new Rect2(origin+new Vector2(size.X,0),new Vector2(-size.X,size.Y)):new Rect2(origin,size),false);}
    void Export(){string path=ProjectSettings.GlobalizePath("user://compact-"+sprite.Spec.Id+".png");using var img=sprite.Render(PoseTime,motion,back).Image();if(flip)img.FlipX();var error=img.SavePng(path);status.Text=error==Error.Ok?"PNG transparente salvo em: "+path:"Falha ao exportar: "+error;}
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
        ExportGallery();
        for(int i=0;i<32;i++)
        {
            using var party=Image.CreateEmpty(160,112,false,Image.Format.Rgba8);party.Fill(new Color("1c2031"));
            for(int a=0;a<cast.Length;a++){using var actor=cast[a].Render(i/8.0).Image();party.BlendRect(actor,new Rect2I(0,0,32,32),new Vector2I(10+(a%4)*38,8+(a/4)*48));}
            party.Resize(960,672,Image.Interpolation.Nearest);party.SavePng(Path.Combine(dir,$"frame-{i:D2}.png"));
        }
    }
    /// <summary>Galeria de 24 personagens gerados (sementes 1–24) + catálogo, para revisão rápida do gerador.</summary>
    void ExportGallery()
    {
        const int cols=6,rows=4;using var sheet=Image.CreateEmpty(cols*40,rows*40,false,Image.Format.Rgba8);sheet.Fill(new Color("1c2031"));var entries=new List<object>();
        for(int i=0;i<cols*rows;i++){var spec=ForgeGenerator.Generate((ulong)(i+1));using var cell=new CompactAdventurer(spec).Render(0).Image();sheet.BlendRect(cell,new Rect2I(0,0,32,32),new Vector2I(i%cols*40+4,i/cols*40+4));entries.Add(new{seed=i+1,id=spec.Id,name=spec.Name,@class=spec.Class,species=spec.Species.ToString(),headwear=spec.Headwear.ToString(),weapon=spec.Weapon.ToString(),description=spec.Description});}
        sheet.Resize(cols*40*4,rows*40*4,Image.Interpolation.Nearest);sheet.SavePng("user://generated-gallery.png");
        File.WriteAllText(ProjectSettings.GlobalizePath("user://generated-catalog.json"),System.Text.Json.JsonSerializer.Serialize(new{version=1,generator="VERDANT Forge",characters=entries},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }
    /// <summary>Regras objetivas do estilo (limites, paleta, determinismo, loops, silhuetas, pixels órfãos) para o elenco e 200 sementes.
    /// A mesma lógica roda fora do Godot em tools/PixelPreview (comando "check").</summary>
    int ValidateSprite(){var heroes=ForgeValidation.Run();var monsters=MonsterValidation.Run();var icons=ForgeEffects.ValidateIcons(Verdant.Battle.Abilities.All.Keys);var result=new ForgeValidation.Result(heroes.Checks+monsters.Checks+icons.Checks,heroes.Failures.Concat(monsters.Failures).Concat(icons.Failures).ToArray());foreach(var failure in result.Failures)GD.PrintErr("PIXEL_FAIL "+failure);if(result.Failures.Count==0){GD.Print("PIXEL_TEST_PASS assertions="+result.Checks);return 0;}return 1;}
    public override void _ExitTree(){texture?.Dispose();terrain?.Dispose();}
}
