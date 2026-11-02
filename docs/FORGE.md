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
