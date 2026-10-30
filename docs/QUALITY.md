# Quality report and continuation checkpoint

## Delivery status

**A functioning demo is implemented. The full production/AAA specification is incomplete.** Do not treat passing structural tests as approval of art or animation quality. No external art assets or AI-generated source sprites are used. No repository commit, remote push, deployment or publication was performed.

## Verified in this environment

- Godot **4.5.2 stable .NET**, SDK **8.0.408**, Debian 13 x86_64; C# build succeeds with zero warnings/errors.
- Official Godot archive verified against the published SHA512 manifest. Reusable setup script run successfully. Editor import succeeds with writable XDG directories.
- Offline NuGet restore succeeded from an empty `/tmp/verdant-offline-nuget` cache using only bundled packages.
- `./scripts/run.sh --headless -- --self-test`: 10,039 assertions passed, including 100 seeds per family, all six states, four direction projections across the seed set, extreme morphology bounds, JSON roundtrips, reproducible images, sample files, history, inheritance rejection, fixed-time motion, stale generation cancellation, and spritesheet/live-render equality. Tests never rewrite shipped samples.
- `DISPLAY=:99 ./scripts/run.sh --audio-driver Dummy -- --ui-test`: 17 interaction assertions passed; also exercised 20 rapid genome edits and 20 actor creation/removal cycles. This validates control wiring/state and lifecycle execution, not long-duration leak freedom.
- Standalone `examples/Minimal.tscn` started and ran headlessly for 90 frames, without workshop dependencies.
- Real OpenGL Compatibility rendering tested under Xorg dummy display + Mesa llvmpipe, LLVM 19.1.7. `workshop.png` and `field-guide.png` are actual Godot viewport captures. The labeled guide uses fixed seeds 600–605 without curating the outputs. Software rendering gives a harmless unsupported-vsync warning.

## Performance evidence

See `verification-report.json` for the most recent measured CPU-only population run. Machine: virtual Intel Xeon Platinum 8573C, five reported CPUs. Renderer: custom CPU rasterizer, 192×160 RGBA per creature. Benchmark: 30 complete population frames at 30 Hz simulation timestamps, reporting p50/p95/p99, cold constructor time, thread allocations and process working set. Pixel textures are not uploaded during this benchmark.

The final measured run produced p95 **5.78 ms / 47.16 ms / 118.29 ms** for **1 / 25 / 100** creatures respectively. Timings vary in this shared virtual machine. These are CPU timings, **not desktop FPS**. There is no warm frame cache. The live preview uploads at 30 Hz; CreatureActor renders per process frame. Texture upload cost, managed allocation attribution by stage and sustained-memory plateaus remain unmeasured. The 100-creature 60 FPS goal is not met by the current implementation.

## Visual assessment by family

| Family | Present behavior | Remaining quality work |
| --- | --- | --- |
| Mossback | Four articulated limbs, muzzle, ear, variable mass/tail | Far limbs often overlap; foot planting needs world-space validation; profile can feel mechanical |
| Stonekin | Upright torso, connected neck, two separated leg roots, arms | Hands/feet and facial identity remain simple; torso-to-hip transitions need refinement |
| Emberdrake | Long tail, dorsal spikes, horns, quadrupedal gait | Too close to mammal anatomy; needs dedicated reptilian proportions/material treatment |
| Amberclaw | Eight limbs, body segmentation, paired claws | Mandibles and leg ordering need work; residual mammalian head character |
| Sunwing | Feather polygons and wing flapping, airborne bob | Wing construction reads as a narrow fan; downstroke/upstroke mechanics need redesign |
| Reed serpent | Tapered, deforming long body and legless locomotion | Not yet convincing whole-body ground propulsion; head connection and turns need refinement |
| Tidefin | Dorsal/pectoral/caudal fins and swim motion | Needs stronger fish anatomy and traveling axial wave |
| Dewdrop | Mass variation, breathing deformation and soft appendages | Facial relief looks circular; hop/contact behavior not expressive enough |
| Brambleling | Plant extension combines upright body and leaf crown | Demonstrates composition, but botanical movement/material identity is basic |

## Outstanding acceptance work

1. Art quality: replace obvious primitive junctions, develop distinct heads/materials, refine silhouettes and inspect wider random populations at native and enlarged scales. No AAA quality claim is warranted.
2. Animation: activity smooths, but action/hit/sleep are periodic/cosmetic states; full transition blending, anticipation/recovery and expressive per-family actions are missing. Stance/swing and two-bone IK exist, but there is no world-space foot-anchor system or quantified slip test. North/south views use geometric projection; dedicated front/back occlusion is incomplete.
3. Architecture: anatomy and rig construction are still inside the shared renderer. A general registered module API for arbitrary new shapes/materials/motion is not implemented. Plant extension composes existing flags rather than proving a fully extensible plug-in system.
4. Tools: save/load currently uses a fixed named slot. The field guide cycles six samples instead of showing a browsable thumbnail library. Parent B is captured and used for crossover, but no side-by-side second-parent comparison exists. The rig toggle displays an axis rather than a full labeled contact debugger. Mutation controls lack a numeric label. Undo supports slider drags; keyboard-driven slider edits need explicit undo grouping.
5. Stress/performance: implement longer repeated-scene tests, invalid-request stress, memory-plateau measurement, measured texture-upload timings and an allocation reduction pass. Profile the actual Godot 1/25/100-actor scene on desktop hardware before asserting 60 FPS.
6. Portability: Linux build/import/render is verified. Windows F5/F6 and launch scripts are supplied but untested. No native exported Windows executable is bundled; the Godot .NET editor and SDK are required.

## Reproduce and continue

```bash
./scripts/setup.sh
./scripts/run.sh --headless -- --self-test
./scripts/run.sh --headless -- --ui-test
./scripts/run.sh --headless res://examples/Minimal.tscn --quit-after 90
# With a graphical DISPLAY available:
./scripts/run.sh --audio-driver Dummy -- --capture
./scripts/run.sh --audio-driver Dummy -- --gallery-capture
```

Reports and render artifacts go under `user://verification`; UI captures go directly under `user://`. See README for OS paths. A saved environment draft contains install/start instructions; this is distinct from publishing an environment snapshot or verifying a newly restored task. Review/save the draft in environment settings and publish when desired.
