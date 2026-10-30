# VERDANT — Pixel character demo & procedural workshop

**[Baixar demo para Windows 64 bits](https://github.com/Gui571/Teste-game/raw/refs/heads/verdant-demo/downloads/Verdant-Windows.zip)** — extraia todo o ZIP e abra `Verdant.exe`. Godot e .NET já estão incluídos no pacote. A execução no Windows ainda não foi testada.

**Current roster:** seven visually distinct adventurers built on the approved Bram / Nix / Rowan style. Those three native sprites and all 15 of their animation atlases remain pixel-identical to the approved version.

![Expanded roster](docs/roster/lineup.gif)

[Visual direction guide (Portuguese)](docs/DIRECAO-ELENCO.md) · [Roster board](docs/roster/board.png) · [Versioned catalog](docs/roster/catalog.json)

New characters: **Iria** (violet astral mage, pointed hat, crystal and tome), **Boro** (bronze guardian, shell, tower shield and hammer), **Suri** (orange fox duelist, wine scarf, tail and twin daggers), and **Mira** (jade/ivory owl healer, round hood and circular crook).

Shared rules: 32×32 cells, origin (16,29), a roughly 14px head, 5px torso, 2px feet, dark one-pixel outline, upper-left light, at most 16 visible colors and integer nearest-neighbor scaling. Each class has a different silhouette and equipment grammar; these are separate constructions, not recolors.

F5 launches the demo. Select any character from the dropdown or open **Guia do elenco**, inspect the 4× rendering / silhouette / native sprite, then press **Selecionar**. Existing pause, walking, front/back, zoom, backdrop, PNG export and workshop navigation remain. **Ações** offers run, class action and hurt; these are visual demonstrations, without new combat rules.

`docs/roster/` contains seven native PNGs and 35 atlases with JSON timing, origin and loop metadata. `RosterCatalog.cs` defines class identities and shared constraints; `ExpandedAdventurer.cs` implements the four additions. The approved renderer is retained in `CompactAdventurer.cs`.

```bash
./scripts/run.sh --headless -- --pixel-test
./scripts/run.sh --headless -- --pixel-frames
# A real/virtual display is required for UI image readback and captures:
./scripts/run.sh --audio-driver Dummy -- --pixel-ui-test
./scripts/run.sh --audio-driver Dummy res://pixel_demo/RosterBoard.tscn -- --roster-capture
```


A working **Godot 4.5.2 .NET / C# .NET 8 demo** of procedural pixel creatures, genetics and animation. This is an original implementation based on the supplied written art brief. The linked reference sites returned HTTP 403 in the development environment; their contents were not inspected or copied.

**Status:** executable and tested demo; the full production/AAA acceptance specification is **incomplete**. See [quality report](docs/QUALITY.md) for explicit remaining work and measured limitations.

![Actual Godot workshop capture](docs/workshop.png)

## Launch

Windows: install **Godot 4.5.2 .NET** and **.NET SDK 8.0.408** (not just the runtime). Open `project.godot`, build the C# project, then F5. F6 works on `workshop/Workshop.tscn` or `examples/Minimal.tscn`. Alternatively:

```powershell
.\scripts\run.ps1 -Godot 'C:\Godot\Godot_v4.5.2-stable_mono_win64.exe'
.\scripts\run.ps1 -Godot 'C:\Godot\Godot_v4.5.2-stable_mono_win64.exe' -Test
```

NuGet packages are bundled under `vendor/nuget`; package restoration uses that local directory exclusively. After installing the SDK and editor, import/build/run requires no Internet. Windows execution has not been tested in this Linux environment.

Linux cloud environment:

```bash
./scripts/setup.sh                          # install prerequisites if absent, build/import
./scripts/run.sh                            # graphical desktop/display required
./scripts/run.sh --headless -- --self-test   # deterministic CPU tests + artifacts
./scripts/run.sh --headless -- --ui-test     # UI control/state/lifecycle checks
./scripts/run.sh --path . res://examples/Minimal.tscn
```

`setup.sh` defaults to `/workspace/toolchains` and verifies the Godot archive with the official SHA512 manifest. Set `CREATURE_TOOLS` to change the installation directory. For subsequent runs set `DOTNET_ROOT` and `GODOT_BIN` accordingly. Set `XDG_DATA_HOME`, `XDG_CONFIG_HOME`, `XDG_CACHE_HOME`, `DOTNET_CLI_HOME`, `NUGET_PACKAGES` to writable paths outside `/workspace` on another Linux machine. The cloud scripts use `/workspace` for these directories. On a machine with no audio device, add `--audio-driver Dummy`.

## Using the workshop

- Enter a uint32 seed and Generate, or choose New creature. Nine family selectors change anatomy; the field guide cycles through six fixed specimens per family.
- Sliders edit anatomy, palette, markings and tempo live. Anatomy/color/marking locks apply to regeneration. Undo/redo covers slider dragging and genome actions; Reset restores the last saved or loaded genome.
- Parent A and four animated children share the renderer. Adjust mutation strength with the lineage slider and adopt a child. Capture parent B, then cross it with a compatible current parent A.
- Select idle/walk/run/action/hit/sleep, facing, slow motion, pause, and fixed-step advance. Zoom cycles integer pixel scales. Landscape switches to a neutral grid; Rig displays a basic body axis.
- Field test: use WASD or arrow keys to walk beside four animated neighbors. This is a bounded movement sandbox, without combat, collision or terrain interaction.
- Save/load uses `user://genome.json`. PNG and 16-frame spritesheet exports write to `user://`; the status bar shows locations. Spritesheet JSON contains genome, state, direction, time, frame bounds and origin. Files of the same output name are replaced intentionally.

On this cloud machine the user-data directory is `/workspace/.local/share/godot/app_userdata/Verdant — Procedural Creature Workshop/`. On Windows use `%APPDATA%\Godot\app_userdata\Verdant — Procedural Creature Workshop\`.

## Repository

- `addons/procedural_creatures/`: reusable genetics, bounded generation queue, motion, rasterizer, actor and exporter.
- `workshop/`: interactive UI and procedural landscape.
- `samples/`: 54 versioned genomes (six per family), independent of test execution.
- `tests/`: custom `--self-test` runner; nonzero exit on failed assertion.
- `examples/`: minimal actor integration.
- `docs/`: architecture, actual captures, reproducible benchmark report and unfinished acceptance work.

See [architecture and extension guide](docs/ARCHITECTURE.md) and [quality/checkpoint report](docs/QUALITY.md).
