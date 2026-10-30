# Framework contracts

`addons/procedural_creatures` is independent of the workshop. Copy this directory into another Godot 4.5.2 .NET project, reference the `Verdant` namespace, and instantiate `CreatureActor.tscn`. `examples/Minimal.tscn` is an executable integration scene (F6), with no workshop references.

Pipeline: versioned `Genome` → validated `BodyPlan` → `CreatureRenderer` (immutable genome, palette and body-local markings) → `Pose` → CPU RGBA `Raster` → Godot ImageTexture. The live view, actors, exports and tests call the same renderer. No animal sprites, external models, image generators or runtime services are involved.

## Public API

```csharp
var actor = GD.Load<PackedScene>("res://addons/procedural_creatures/CreatureActor.tscn")
    .Instantiate<Verdant.CreatureActor>();
AddChild(actor);
actor.SetGenome(Verdant.Genome.Generate(1042, "dragon"));
actor.SetState(Verdant.MotionState.Walk);
actor.SetDirection(0); // east, west, north, south
actor.Pose.Speed = 0.5f;
actor.Paused = true;
actor.Step();
```

Simulation uses a 60 Hz accumulator with a 250 ms hitch cap; the main preview uploads textures at 30 Hz. `Pose.At` supports exact export timestamps. Actor textures are updated, not replaced every frame, and disposed on removal. No frame cache exists. GenerationQueue retains at most one running request, one pending request, and one completed CPU result. Cancellation invalidates results rather than interrupting a partially rasterized frame. It never publishes an older revision; texture creation stays on the main thread.

## Genome v1

`Version=1`, `Generator="verdant-1"`, `Seed` (uint32), and `Family` are required JSON identity fields. Unknown versions/families are rejected. Morphology (`Mass`, `Length`, `Legs`, `Tail`, `Head`) and `Tempo` range from 0.55 to 1.5. `Hue`, `Pattern`, `Horns` range from zero to one. All fields must be finite. Missing optional trait fields use record defaults. Future migrations must explicitly translate a source version before validation; there is no silent migration.

The PRNG avalanches the seed using unsigned 32-bit multiplication/xor mixing, then uses xorshift32. Anatomy, palette, markings, movement, mutation and inheritance use different stream identifiers. Genomes store resolved parameters: palette editing never rerolls anatomy. Pattern coordinates are generated once in body space. Crossbreeding requires matching families, then chooses parental trait values independently. Mutation bounds traits and preserves body-plan identity.

## Extension points and current boundaries

Register a `BodyPlan` in `Plans.All` to combine existing locomotion and morphology modules. `plant` demonstrates composition of upright anatomy and leaf-crown geometry. New geometry needs a renderer module; the renderer is currently a shared implementation, **not yet a plug-in registry**. Palette roles and raster primitives are centralized. Custom motion modules and fully independent anatomy/rig contracts remain work toward the broader production specification.

The CPU rasterizer performs binary pixel coverage, palette shading, capsule strokes and polygon fills at 192×160. The creature silhouette occupies roughly 64–128 logical pixels; the surrounding margin accommodates tails/wings and export origins. No finished sprite is rotated. Directional projection changes geometry, but north/south anatomy and occlusion still need dedicated art refinement.

`user://` holds saves and exports. Source-relative resources use `res://`. Linux shell launchers can override `DOTNET_ROOT`, `GODOT_BIN`, and XDG paths; Windows uses Godot/.NET from local installations.
