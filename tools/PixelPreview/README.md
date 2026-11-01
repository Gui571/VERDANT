# PixelPreview

Compila o **mesmo código dos sprites** (`pixel_demo/Forge`, `CompactAdventurer`, `RosterCatalog`, `Raster`) sem o Godot, usando `GodotShim.cs` (apenas `Color`/`Vector2`). Serve para revisar arte e rodar a validação em CI ou numa máquina sem o motor.

```bash
cd tools/PixelPreview
dotnet build            # global.json local aceita qualquer SDK 8.0.x
dotnet bin/Debug/net8.0/PixelPreview.dll roster saida.png 7 idle      # elenco, zoom 7
dotnet bin/Debug/net8.0/PixelPreview.dll strip saida.png bram attack 6 # 8 quadros de um movimento
dotnet bin/Debug/net8.0/PixelPreview.dll motions saida.png 3           # walk/attack/hurt de todos
dotnet bin/Debug/net8.0/PixelPreview.dll gen saida.png 1 5             # 24 sementes a partir da 1
dotnet bin/Debug/net8.0/PixelPreview.dll check 200                     # validação (elenco + 200 sementes)
dotnet bin/Debug/net8.0/PixelPreview.dll export ../../docs/roster      # regenera PNGs/atlases/JSON/catálogo
dotnet bin/Debug/net8.0/PixelPreview.dll pose iria run                 # parâmetros de pose por quadro
```

Sem acesso ao NuGet: `dotnet build -p:RestoreSources=<pasta vazia> -p:UseAppHost=false`.
`Verdant.csproj` exclui `tools/**` da compilação do jogo.
