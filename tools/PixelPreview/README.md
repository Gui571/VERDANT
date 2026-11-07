# PixelPreview

Compila o **mesmo código dos sprites** (`pixel_demo/Forge`, `CompactAdventurer`, `RosterCatalog`, `Raster`) sem o Godot, referenciando `core/Verdant.Core.csproj`, sem shim ou pacote Godot. Serve para revisar arte e rodar a validação em CI ou numa máquina sem o motor.

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

Monstros e combate (mesmas fontes do jogo):

```sh
dotnet run --project tools/PixelPreview -- monsters docs/monsters
dotnet run --project tools/PixelPreview -- strip /tmp/slime-death.png monster:slime death 5
dotnet run --project tools/PixelPreview -- monster-check
dotnet run --project tools/PixelPreview -- battle
```

`check 200` agora inclui MonsterValidation. `battle` executa 240 confrontos duas vezes, compara eventos e imprime as taxas por classe/família. `battle/Sim` não depende do Godot.

Portabilidade R0:

```sh
dotnet run --project tools/PixelPreview -- pixel-verify docs/portability/atlas-sha256.json
dotnet run --project tools/PixelPreview -- web-fixture /tmp/verdant-web-trace.json
```

`pixel-verify` compara SHA-256 de 5.058 conjuntos completos de quadros com a linha de base do Godot anterior à separação; também verifica que a biblioteca não referencia Godot. `web-fixture` executa no CLI o mesmo cenário C# usado pela prova web.

Estilo v2 (M1, formas atuais):

```sh
dotnet run --project tools/PixelPreview -- style-v2 docs/arte/estilo-v2/m1
python3 scripts/style-v2-review.py # Pillow; apenas compõe PNGs gerados em C#
dotnet run --project tools/PixelPreview -- style-v2-check
dotnet run --project tools/PixelPreview -- pixel-verify docs/arte/estilo-v2/antes/atlas-sha256.json
python3 scripts/generate-style-v2-palettes.py # regeneração offline OKLCH, biblioteca padrão Python
./scripts/run.sh --headless -- --style-v2-test
DISPLAY=:99 ./scripts/run.sh --audio-driver Dummy res://studio/MaterialStudio.tscn -- --material-ui-test
DISPLAY=:99 ./scripts/run.sh --audio-driver Dummy res://battle/Arena.tscn -- --style-v2 --battle-benchmark
```

Os comandos são executados da raiz do repositório. O limite novo é 32 cores, sem alterar o limite de 16 dos testes do estilo antigo. `style-v2-check` mede máscara, repetição, alfa, paleta, recorte, contraste-base de 66 pares espécie/time e isolamento de TEAM. Não testa independência de peças (M2), novas direções (M3) ou geração de mundo (M7). [Esquema de exportação e limites](../../docs/FORGE.md).
