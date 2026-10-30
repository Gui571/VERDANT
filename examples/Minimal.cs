using Godot;
using Verdant;
public partial class Minimal : Node2D
{
    public override void _Ready() { var actor = GD.Load<PackedScene>("res://addons/procedural_creatures/CreatureActor.tscn").Instantiate<CreatureActor>(); AddChild(actor); actor.Position = new Vector2(500, 400); actor.Scale = new Vector2(4, 4); actor.SetGenome(Genome.Generate(27031, "dragon")); actor.SetState(MotionState.Walk); }
}
