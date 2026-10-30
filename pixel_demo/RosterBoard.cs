using Godot;
namespace Verdant;

/// <summary>A real Godot art-direction board; all thumbnails come from the
/// same live renderer as the game-scale preview and exported atlases.</summary>
public partial class RosterBoard:Control
{
    readonly List<ImageTexture> owned=new();
    static Color Ink=new("1c2031"),Text=new("ede5cb"),Muted=new("a2b3cc");
    Label LabelAt(string text,Vector2 position,int size,Color color,Node parent)
    {var label=new Label{Text=text,Position=position};label.AddThemeFontSizeOverride("font_size",size);label.AddThemeColorOverride("font_color",color);parent.AddChild(label);return label;}
    public override void _Ready()
    {
        TextureFilter=TextureFilterEnum.Nearest;
        var bg=new ColorRect{Color=Ink,Size=new Vector2(1440,960)};AddChild(bg);
        LabelAt("VERDANT  /  SISTEMA VISUAL DO ELENCO",new(40,26),28,Text,this);
        LabelAt("7 identidades · mesma grade, luz, contorno e escala",new(40,67),16,Muted,this);
        var back=new Button{Text="Voltar à demo",Position=new(1240,35),Size=new(160,40)};back.Pressed+=()=>GetTree().ChangeSceneToFile("res://pixel_demo/PixelDemo.tscn");AddChild(back);
        int i=0;
        foreach(var entry in RosterCatalog.All)
        {
            var panel=new Panel{Position=new Vector2(40+(i%4)*340,116+(i/4)*366),Size=new Vector2(320,348)};
            panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat{BgColor=new Color("242b3e"),CornerRadiusTopLeft=8,CornerRadiusTopRight=8,CornerRadiusBottomLeft=8,CornerRadiusBottomRight=8});AddChild(panel);
            LabelAt(entry.Name,new(18,14),24,Text,panel);LabelAt(entry.Class.ToUpperInvariant(),new(18,46),13,new Color(entry.Colors[0]),panel);
            using var image=new CompactAdventurer(1,entry.Kind).Render(0).Image();var texture=ImageTexture.CreateFromImage(image);owned.Add(texture);
            panel.AddChild(new TextureRect{Texture=texture,Position=new(14,70),Size=new(128,128),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale,TextureFilter=TextureFilterEnum.Nearest});
            LabelAt("SILHUETA",new(195,76),11,Muted,panel);
            var pixels=image.GetData();for(int p=0;p<pixels.Length;p+=4)if(pixels[p+3]!=0){pixels[p]=237;pixels[p+1]=229;pixels[p+2]=203;}
            using var silhouette=Image.CreateFromData(32,32,false,Image.Format.Rgba8,pixels);var solid=ImageTexture.CreateFromImage(silhouette);owned.Add(solid);
            panel.AddChild(new TextureRect{Texture=solid,Position=new(200,92),Size=new(64,64),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,TextureFilter=TextureFilterEnum.Nearest});
            panel.AddChild(new TextureRect{Texture=texture,Position=new(216,166),TextureFilter=TextureFilterEnum.Nearest});LabelAt("1×",new(258,173),12,Muted,panel);
            for(int c=0;c<entry.Colors.Length;c++)panel.AddChild(new ColorRect{Color=new Color(entry.Colors[c]),Position=new(18+c*24,218),Size=new(18,12)});
            var desc=LabelAt(entry.Silhouette,new(18,245),13,Text,panel);desc.Size=new Vector2(284,40);desc.AutowrapMode=TextServer.AutowrapMode.WordSmart;
            var pick=new Button{Text="Selecionar",Position=new(18,297),Size=new(284,34)};
            pick.Pressed+=()=>{PixelDemo.InitialCharacter=entry.Kind;GetTree().ChangeSceneToFile("res://pixel_demo/PixelDemo.tscn");};panel.AddChild(pick);i++;
        }
        var legend=new VBoxContainer{Position=new(1080,500),Size=new(300,300)};AddChild(legend);
        foreach(var line in new[]{"REGRAS COMPARTILHADAS","32 × 32 / origem 16,29","Cabeça ~14 px / tronco 5 px","Contorno escuro de 1 px","Luz superior esquerda","Até 16 cores por personagem","Identidade antes do ornamento"}){var l=new Label{Text=line};l.AddThemeColorOverride("font_color",Muted);legend.AddChild(l);}
        LabelAt("Os três personagens aprovados são a referência de proporção e acabamento para novas classes.",new(40,878),15,Muted,this);
        if(OS.GetCmdlineUserArgs().Contains("--roster-capture"))Capture();
    }
    async void Capture(){await ToSignal(GetTree().CreateTimer(.2),SceneTreeTimer.SignalName.Timeout);using var img=GetViewport().GetTexture().GetImage();img.SavePng("user://roster-board.png");GetTree().Quit();}
    public override void _ExitTree(){foreach(var texture in owned)texture.Dispose();}
}
