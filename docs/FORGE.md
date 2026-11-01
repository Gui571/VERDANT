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

`ForgeGenerator.Generate(semente)` sorteia uma classe (equipamento coerente), uma espécie compatível com os chapéus da classe (espécies de orelha alta só usam faixa/coroa/nada), cauda natural, barba e uma paleta derivada de rampas curadas. **Mesma semente = mesmo personagem**, sempre (`ForgeValidation` confere). Espécies: pato, coelho, humano, tartaruga, raposa, coruja, rato, gato, sapo, elfo.

## Como estender

- **Nova espécie:** acrescentar em `Species`, uma rampa em `ForgeLook.Fur`, orelhas/adornos em `Parts.Ears`, rosto em `Parts.Face`, e (se tiver cauda) `Parts.Tail`.
- **Novo chapéu:** `Parts.Hat` (mapas de texto com legenda `o f F g c C k a A l L m M i w p` ou polígonos), e permissões por espécie em `ForgeGenerator.Allowed`.
- **Nova arma:** `Gear.RestAngle`, `Gear.Attack` (6 ângulos), `Gear.DrawWeapon` e os pontos extremos em `Gear.Extremes`.
- **Novo personagem fixo:** uma `ForgeSpec` em `ForgeRoster` + uma frase de silhueta em `RosterCatalog`.

## Validação

`ForgeValidation.Run()` (usada por `--pixel-test` e por `tools/PixelPreview check`) cobre, para o elenco e 200 sementes, todos os movimentos, frente e costas: sem recorte, alfa só 0/255, ≤16 cores, determinismo, nenhum pixel órfão no idle, loops de idle/caminhada/corrida sem salto, pés fixos no idle, silhuetas únicas no elenco e variedade entre sementes. Essas verificações são técnicas; a aprovação artística continua sendo visual (veja `DIRECAO-ELENCO.md`).
