# Forge — motor de pixel art do elenco

O Forge desenha **todos** os personagens de `pixel_demo` (os sete fixos e qualquer um gerado por semente) a partir de *receitas*. Em vez de um desenho único por personagem, cada um é a combinação de peças compartilhadas, então o acabamento é idêntico para todos.

## O estilo, em regras (extraídas das referências)

| Regra | Como o Forge aplica |
| --- | --- |
| Cabeça grande e arredondada, corpo curto | Máscara de cabeça fixa de 14×11 px; tronco de 5 px; pés de 2–3 px |
| Contorno azul-noite contínuo de 1 px | Passe final `OutlinePass` sobre a silhueta inteira + contorno de separação (`Composite`) para armas e chapéus |
| Sombra de contato escura sob os pés | Duas linhas de tinta (y=29 e 30) desenhadas só onde não há sprite; desligável (`GroundShadow`) |
| Faixa/lenço azul como motivo da "equipe" | `Headwear.Headband`, `Scarf`, `BackGear.Cape` usam a rampa azul em ~40% dos sorteios |
| Sombras frias, luzes quentes | `Ramp.From(cor)` desloca o matiz para o azul nas sombras e para o quente nas luzes |
| Poucas cores por personagem | Papéis de cor (`Tone`): 1 tinta + 15 tons; teste exige ≤16 cores por quadro |
| Armas com realce branco-gelo | Lâminas: base `MetalM`, brilho `MetalL`, ponta `White` |
| Luz no canto superior esquerdo | Cabeça/tronco: realce no alto-esquerda, sombra à direita e embaixo |

## Como uma receita vira pixels

1. **Papéis, não cores.** Peças escrevem `Tone` (ex.: `FurM`, `ClothD`, `MetalL`) numa camada 32×32. Só no fim a `ForgePalette` converte para cores reais — trocar a paleta não exige redesenhar.
2. **Ordem de camadas:** costas (cauda, carapaça, aljava, capa) → item da mão de trás → pernas → tronco → cabeça + rosto + barba + chapéu → item da mão da frente → mãos → rastro/faíscas → contorno e sombra.
3. **Armas por ângulo.** `Gear.DrawWeapon(…, graus)` constrói a arma com linhas perfeitas e polígonos pequenos; qualquer pose de ataque sai limpa. Se a ponta sairia da célula, a arma é encurtada ao longo do próprio eixo (`Reach`).
4. **Animação por poses** (`ForgeRenderer.Compute`): idle de 32 quadros (respiração com cabeça atrasada, piscada, balanço de capa/orelha), caminhada e corrida de 8 quadros (pés alternados, quicada), ataque de 6 quadros (antecipação → preparação → golpe → impacto → continuação → recuperação, com rastro e faíscas) e dano de 4 quadros (flash branco, recuo).

## Gerador

`ForgeGenerator.Generate(semente)` sorteia uma classe (equipamento coerente), uma espécie compatível com os chapéus da classe (espécies de orelha alta só usam faixa/coroa/nada), cauda natural, barba e uma paleta derivada de rampas curadas. **Mesma semente = mesmo personagem**, sempre (`ForgeValidation` confere). Espécies: pato, coelho, humano, tartaruga, raposa, coruja, rato, gato, sapo, elfo, urso.

## Como estender

- **Nova espécie:** acrescentar em `Species`, uma rampa em `ForgeLook.Fur`, orelhas/adornos em `Parts.Ears`, rosto em `Parts.Face`, e (se tiver cauda) `Parts.Tail`.
- **Novo chapéu:** `Parts.Hat` (mapas de texto com legenda `o f F g c C k a A l L m M i w p` ou polígonos), e permissões por espécie em `ForgeGenerator.Allowed`.
- **Nova arma:** `Gear.RestAngle`, `Gear.Attack` (6 ângulos), `Gear.DrawWeapon` e os pontos extremos em `Gear.Extremes`.
- **Novo personagem fixo:** uma `ForgeSpec` em `ForgeRoster` + uma frase de silhueta em `RosterCatalog`.

## Validação

`ForgeValidation.Run()` (usada por `--pixel-test` e por `tools/PixelPreview check`) cobre, para o elenco e 200 sementes, todos os movimentos, frente e costas: sem recorte, alfa só 0/255, ≤16 cores, determinismo, nenhum pixel órfão no idle, loops de idle/caminhada/corrida sem salto, pés fixos no idle, silhuetas únicas no elenco e variedade entre sementes. Essas verificações são técnicas; a aprovação artística continua sendo visual (veja `DIRECAO-ELENCO.md`).

## Revisão validada no Godot

- Controles agora quebram em linhas: Gerar/Sortear/Espelhar/Sombra/Ações cabem na janela 1440×960. PNG exportado respeita o espelhamento.
- Machado: cabo com realce separado da lâmina. Martelo: topo claro e face lateral escura para comunicar volume, inclusive no arco de ataque. Capuzes de costas têm dobra central e luz lateral.
- Nova espécie Urso e boina de explorador, pelas mesmas peças e paletas do Forge. Batedores e guardiões sorteiam mais machados, reduzindo a concentração de lanças. As receitas antigas continuam válidas; a distribuição de sementes mudou nesta versão.
- Nenhuma verificação do ForgeValidation foi removida ou relaxada. A geometria e o tempo dos ataques existentes foram preservados; a melhoria nessa etapa é de acabamento do equipamento.

### Evidências

[Prancha capturada no Godot](roster/board.png) · [Demo capturada no Godot](roster/demo-godot.png) · [Elenco 7×](roster/roster-review.png) · [Sementes 5×](roster/generated.png)

Prévias 6×: [idle](roster/boro-idle-review.png), [walk](roster/boro-walk-review.png), [run](roster/boro-run-review.png), [attack](roster/boro-attack-review.png), [hurt](roster/boro-hurt-review.png). Há também as cinco tiras `bram-*-review.png` e atlases regenerados.

Executados: `dotnet build`, `PixelPreview check 200` (110252 checks, 0 falhas), Godot `--pixel-test` (110252), `--pixel-ui-test` (36), `--roster-capture` e `--pixel-capture`. Interface exercitada com Xorg dummy/Mesa llvmpipe; há aviso de VSync indisponível. Logs reais em `docs/roster/*validation.log`.

Build Linux exportada: cena principal iniciou e passou os mesmos 110252 testes e 36 verificações de UI, incluindo Gerar/Sortear e comparação dos bytes do PNG espelhado. A primeira exportação Linux falhou (`Failed to build project`) por falta do runtime de publicação; restaurar `linux-x64` com `SelfContained=true` via NuGet resolveu, e a exportação foi repetida sem erros. O executável incompleto foi substituído.

Windows x86_64: exportação e integridade do ZIP verificadas, runtime incluído. **O EXE não foi executado neste ambiente Linux.** Aprovação artística final e teste em máquina Windows permanecem pendentes.

### Reexportar

Instale os templates oficiais 4.5.2 mono; ajuste os caminhos `custom_template` em `export_presets.cfg` conforme a instalação. Restaure os runtimes antes do export (o NuGet.Config normal usa apenas pacotes locais):

```sh
dotnet restore -r win-x64 -p:SelfContained=true --source https://api.nuget.org/v3/index.json --source ./vendor/nuget
dotnet restore -r linux-x64 -p:SelfContained=true --source https://api.nuget.org/v3/index.json --source ./vendor/nuget
./scripts/run.sh --headless --export-release "Windows Desktop" /caminho/Verdant.exe
./scripts/run.sh --headless --export-release Linux /caminho/Verdant.x86_64
```

Compacte a pasta inteira, com PCK e `data_Verdant_*`. `downloads/` e `tools/` são excluídos dos recursos exportados.

## Monstros e arena

`ForgeFinish` compartilha o passe de contorno e sombra entre elenco e monstros. `Layer` mantém 32 como dimensão padrão e aceita dimensão explícita de 48 para chefes; os 42 PNGs/atlases do elenco permaneceram byte a byte iguais. `Monsters/` adiciona receitas, geração, poses e validação das nove famílias. Veja [MONSTROS.md](MONSTROS.md) e [BATALHA.md](BATALHA.md). A oficina anterior está isolada em `LegacyWorkshop.tscn`.

## Atualização: marco 1 de especiais

Consulte [ESPECIAIS.md](ESPECIAIS.md) para as regras atuais, movimentos Special/Cast, edição na oficina, quatro times na arena, obstáculos e validação. As métricas acima descrevem a entrega anterior; as métricas atuais e logs ficam no novo relatório.

## Estilo v2 — entrega M1: materiais e luz

O novo pipeline é **opcional**, em `StyleV2/`. Não substitui anatomia, receitas, RNG ou animações do elenco. `ForgeRenderer.Style == null` e `MonsterRenderer.Style == null` seguem o caminho anterior; o manifesto de regressão é `docs/arte/estilo-v2/antes/atlas-sha256.json`. A opção começa desligada. Abra **Materiais v2** no elenco/oficina para comparar; na arena, **Estilo v2** reconstrói só os atlases, preservando o tick e os eventos da luta.

`Tone` e `Layer` continuam como entrada. `MaterialLighting.Identify` traduz os tons existentes para Fur, TEAM, acento, couro, metal, marfim e rosa. A luz superior/esquerda é calculada com vizinhança de material e índices inteiros; não depende da cor da equipe. O resultado mantém exatamente a máscara atual. A geometria ainda agrupa madeira/couro e pele/pelo; a separação dessas peças fica para M2/M6. As tabelas já incluem madeira, pedra, folha, pano, ouro e osso para esses consumidores futuros; isso não significa que construções/terrenos já foram convertidos.

Rampas de cinco tons são geradas **offline em OKLCH**, por `scripts/generate-style-v2-palettes.py`, com redução de croma para o gamut sRGB e arredondamento explícito para bytes. `MaterialRamps.g.cs` contém somente constantes RGB. Sombras aproximam-se do matiz frio; realces aproximam-se do quente. Pelagem clara tem luminosidade própria para não transformar coelhos/corujas em cinza. A rasterização escreve os bytes diretamente; não converte OKLCH durante a apresentação.

Seis rampas TEAM: rosa, brasa, lima, jade, azul e violeta. Alterar TEAM conserva todos os outros materiais e a máscara. Na arena os quatro times usam azul/rosa/jade/violeta. Cores de indicadores e efeitos de especiais permanecem as existentes. Limite v2: **32 cores opacas por quadro**, até cinco por rampa (máximo observado nesta entrega: 31). O legado conserva o limite de 16. As mesmas dimensões e quantidades de quadros mantêm o tamanho dos atlases.

O estúdio de materiais é a primeira página de prova, **não o estúdio completo M8**. Ele usa `SpriteCache`; animação em `_Process` apenas troca a região do atlas. As três provas superiores são estáticas; as seis equipes usam os clipes atuais. Não há quatro direções novas ou morte nova de herói nesta entrega.

### Exportação M1

`style-v2 <pasta>` exporta provas, atlas PNG e JSON de cada clipe frontal atual. JSON v1 contém `style`, `recipe`, `atlas`, `cell`, `columns`, `pivot`, `clip`, `direction`, `loop`, `frames` (índice, x/y, duração em ms), `palettes` e `tags`. `catalog.json` registra limites úteis reais por herói; `palettes.json` registra as rampas RGB. IDs de material são expostos pelo renderer para validar isolamento; não integram a receita de batalha. Mapas binários, profundidade por pixel e o consumidor genérico de todas as categorias ficam para M7/M8.

As caixas úteis medidas estão no catálogo. A margem transparente completa ainda não existe em alguns quadros antigos (3.035 amostras da matriz M1 tocam uma borda). M1 não altera máscaras para esconder isso; M2 deve redesenhar equipamento/poses antes de exigir margem para os sprites novos. Os testes anteriores de recorte não foram removidos ou relaxados.

[Direção e auditoria](DIRECAO-ESTILO-V2.md) · [Folha cor/luz](arte/estilo-v2/m1-cor-luz.png) · [Antes/depois](arte/estilo-v2/m1-antes-depois.png) · [Relatório M1](arte/estilo-v2/RELATORIO-M1.md).

## Redesenho compacto do elenco

A revisão após M1 altera a **geometria**, em `Compact/ChibiRenderer.cs`: cabeça 13×10, tronco-base 8×5, pés de dois pixels, equipamentos em camadas e posições inteiras. O contorno final respeita as peças já contornadas para não duplicar a espessura. Bram tem pelagem clara; os heróis mantêm azul, verde, violeta, bronze, vinho e jade como identidades próprias. Uma cor de equipe explícita pode substituir o tecido na arena/estudo de materiais.

A nova entrada é `studio/CompactGallery.tscn`: sete heróis, quatro vistas, ações e geração por semente. As quatro vistas têm rosto frontal/lateral e costas com capa/capuz; oeste espelha a geometria. Célula 32×32 e origem (16,29) preservadas. A animação tem respiração, piscar, orelha/capa atrasadas, passada curta, antecipação/golpe/recuperação e queda lateral na morte. As durações seguem os clipes atuais; não é a entrega completa de animação/direções M3.

`SpriteCache` aceita a variante `compact`; galeria, arena e prévia usam atlas e não rasterizam a cada quadro. O caminho antigo continua acessível por **Arte nova** na galeria e **Arte compacta** na arena. `CompactAdventurer.Compact` começa false no núcleo para preservar exportadores/hash legados; as cenas de apresentação escolhem explicitamente a arte nova. As nove famílias de monstros não foram redesenhadas nesta revisão.

```sh
dotnet run --project tools/PixelPreview -- chibi docs/arte/compactos/atlas
dotnet run --project tools/PixelPreview -- chibi-check
python3 scripts/chibi-review.py
./scripts/run.sh --headless -- --chibi-test
DISPLAY=:99 ./scripts/run.sh --audio-driver Dummy -- --compact-ui-test
DISPLAY=:99 ./scripts/run.sh --audio-driver Dummy -- --compact-capture
```

[Prancha nova](arte/compactos/elenco.png) · [Comparação](arte/compactos/comparacao.png) · [Galeria real](arte/compactos/galeria.gif) · [Relatório](arte/compactos/RELATORIO.md).


## Revisão visual de 2026-11-08

A variante compacta atual substitui a construção descrita na seção anterior: cabeça-base de nove linhas, perfis diferentes de mandíbula para raposa/coruja/tartaruga, plano lateral de sombra e olhos deslocados em três quartos. O corpo é uma massa poligonal larga, com pé dianteiro dominante e pé distante parcialmente coberto. Não há cinto horizontal comum a todos. As golas e alças dependem do equipamento; elmos/capuzes, escudos e a lâmina do machado foram reconstruídos. No ataque, o corpo avança dois pixels no impacto e a capa acompanha depois; o arco tem corda tensionada e desaparecimento da flecha na liberação. Célula/origem e duração dos clipes continuam 32×32/(16,29) e os valores anteriores.

`ChibiRenderer.Look` usa equipe azul por padrão, com `MaterialRamps.Cornflower`, produzida pelo mesmo gerador OKLCH offline. Trocas de equipe explícitas continuam alterando apenas TEAM. Na galeria, **Equipe azul / Cores do herói** seleciona a apresentação; 1×/2×/3× compartilham a textura já cacheada. Desligar **Arte nova** restaura o renderer legado. A validação da variante compacta exige **≤16 cores**, com máximo observado de 15 nesta revisão. O limite geral do pipeline M1 não muda.

```sh
dotnet run --project tools/PixelPreview -- chibi docs/arte/revisao-referencia/atlas
python3 scripts/reference-review.py
dotnet run --project tools/PixelPreview -- chibi-check
DISPLAY=:99 ./scripts/run.sh --audio-driver Dummy -- --compact-ui-test
```

O script de revisão apenas compõe PNGs exportados pela Forge em pranchas/GIFs, sem retocar os sprites. [Prancha em fundos claro/escuro](arte/revisao-referencia/elenco.png) · [Antes/depois](arte/revisao-referencia/comparacao.png) · [Relatório atual](arte/revisao-referencia/RELATORIO.md).
