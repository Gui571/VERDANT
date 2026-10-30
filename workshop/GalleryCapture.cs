using Godot;
namespace Verdant;
public partial class GalleryCapture : Control
{
    readonly List<ImageTexture> textures = new();
    public override async void _Ready()
    {
        GetWindow().ContentScaleSize = new Vector2I(1440, 1600); GetWindow().Size = new Vector2I(1440, 1600);
        var background = new ColorRect { Color = new Color("10201d"), Size = new Vector2(1440, 1600) }; AddChild(background);
        var title = new Label { Text = "VERDANT  /  UNCURATED FIELD GUIDE  /  seeds 600–605", Position = new Vector2(24, 12) }; title.AddThemeFontSizeOverride("font_size", 24); AddChild(title);
        for (int row = 0; row < Plans.All.Count; row++) { var label = new Label { Text = Plans.All[row].Label, Position = new Vector2(24, 90 + row * 170) }; AddChild(label); for (int col = 0; col < 6; col++) { var g = Genome.Generate((uint)(600 + col), Plans.All[row].Id); using var image = new CreatureRenderer(g).Render(Pose.At(.3, MotionState.Walk)).Image(); var texture = ImageTexture.CreateFromImage(image); textures.Add(texture); var display = new TextureRect { Texture = texture, TextureFilter = TextureFilterEnum.Nearest, Position = new Vector2(190 + col * 200, 40 + row * 170) }; AddChild(display); AddChild(new Label { Text = (600 + col).ToString(), Position = new Vector2(270 + col * 200, 184 + row * 170) }); } }
        await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout); using var capture = GetViewport().GetTexture().GetImage(); capture.SavePng("user://field-guide.png"); GetTree().Quit();
    }
    public override void _ExitTree() { foreach (var texture in textures) texture.Dispose(); }
}
