using Godot;
namespace Verdant;
public partial class CreatureActor : Node2D
{
    public Genome Genome { get; private set; } = new();
    public Pose Pose { get; } = new();
    CreatureRenderer renderer = null!;
    ImageTexture? texture;
    double accumulator;
    public bool Paused { get; set; }
    public bool ShowRig { get; set; }
    public override void _Ready() { TextureFilter = TextureFilterEnum.Nearest; SetGenome(Genome); }
    public void SetGenome(Genome g) { g.Validate(); Genome = g; renderer = new(g); Refresh(); }
    public void SetState(MotionState state) => Pose.SetState(state);
    public void SetDirection(int direction) => Pose.Direction = Math.Clamp(direction, 0, 3);
    public void Step() { Pose.Tick(1.0 / 60); Refresh(); }
    public override void _Process(double delta) { if (Paused) return; accumulator += Math.Min(delta, .25); while (accumulator >= 1.0 / 60) { Pose.Tick(1.0 / 60); accumulator -= 1.0 / 60; } Refresh(); }
    void Refresh() { using var img = renderer.Render(Pose, ShowRig).Image(); if (texture == null) texture = ImageTexture.CreateFromImage(img); else texture.Update(img); QueueRedraw(); }
    public override void _Draw() { if (texture != null) DrawTexture(texture, new Vector2(-96, -120)); }
    public override void _ExitTree() { texture?.Dispose(); }
}
