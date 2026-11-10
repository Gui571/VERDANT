# Forge — segunda rodada de estilo (inspirada no vídeo de referência)

Inspiração: vitrine de gerador procedural de pixel art de um amigo (autorizou usar como referência de estilo; nada foi copiado: espécies, nomes e sprites são do VERDANT).
Análise do vídeo e prompts: `docs/ANALISE-E-PROMPTS-ESTILO-VIDEO.md`. Imagens: `docs/arte/estilo-video/`.

## O que entrou
| Tema | Antes | Agora |
|---|---|---|
| Espécies | 11 | **25** (+ lobo, cão, panda, javali, porco, bode, cervo, guaxinim, macaco, orc, goblin, lagarto, esqueleto, anão) |
| Classes | só as 7 do elenco | **20 classes visuais** (`ForgeCast.Classes`) com armas/chapéus/costas novos |
| Patentes | — | **Recruta / Veterano / Elite / Herói** (`Grade`): ombreira, faixa, pluma, capa, estrelas piscando. Só visual |
| Slots | sorteio acoplado | cada slot (espécie, classe, chapéu, arma, mão secundária, costas, roupa, nome, cor) tem **fluxo de RNG próprio** |
| Golpe | 6 quadros a 8 fps, poses iguais | **12 quadros a 16 fps** (mesma duração, 0,75 s) com **peso por pose** (`ChibiRenderer.AttackPose`) e **arco de golpe** no contato |
| Hit | silhueta branca chapada | **xadrez (dither)** claro em 2 quadros, sem alfa parcial |
| Morte | inclina e gira | **cambaleia → de joelhos → tomba**, arma larga e brilho no chão, dissolve em xadrez |
| Caminhada | sem pés visíveis | **botas** alternando (pé de trás sobe), passada maior, balanço de braço |

Armas novas: maça, alabarda, besta, varinha, alaúde, frasco. Chapéus: elmo com pluma, chifres, mitra, folhas, pena. Mão secundária: lanterna. Costas: estandarte.

## Como usar
```csharp
var spec = ForgeCast.Generate(seed);                          // espécie × classe × patente sorteadas
var knight = ForgeCast.Generate(7, Grade.Hero, species: Species.Wolf, classId: "knight");
var reroll = ForgeCast.Generate(7, null, rerollSlot: ForgeCast.SlotHat, rerollSeed: 99);   // troca só o chapéu
new ChibiRenderer(spec).Render(seconds, motion, facing);
```
O gerador antigo (`ForgeGenerator.Generate`) continua sorteando as 11 espécies originais: sementes antigas não mudam.
O golpe do herói no `SpriteCache` usa `ChibiRenderer.Frames/Fps(Attack)` (12 quadros a 16 fps) quando o modo compacto está ligado.

## Validação (rodada na nuvem, sem Godot)
- `cast-check`: 63.701 checks, 0 falhas (matriz 25 espécies × 20 classes × 4 patentes, ≤16 cores, alfa 0/255, margem, determinismo, slots independentes, silhuetas de classe distintas em 3 espécies, 4 patentes distintas por classe, tempo do golpe, hit em xadrez, arco de golpe).
- `chibi-check` 0 falhas (140.253), `style-v2-check` 0 falhas, `check` 0 falhas (280.461), `monster-check` 0 falhas, `special-check` 0 falhas, `battle` 0 falhas.

## O que está fraco (honesto)
- **Caminhada**: a diferença entre quadros ainda é sutil em 1×; botas e braço ajudam, mas não é um ciclo de pernas como o do vídeo.
- **Norte/Sul**: sul e leste quase iguais; norte só mostra a nuca. Oeste é espelho do leste (o vídeo parece desenhar mais).
- **Patentes**: diferença pequena em 1× (como no vídeo, mas aqui menos marcada).
- **Espécies parecidas**: javali/urso, orc/goblin/sapo, guaxinim (máscara parece capacete). Pelagem do panda lê como robô em alguns tamanhos.
- **Classes**: monge e alquimista pouco distintos; besta tem forma estranha; folhas do druida finas.
- Só 25 espécies e 20 classes (o vídeo tem 45 espécies). Não há espécies não humanoides (slime, chama, vela).
- Sem sprite-fantasma/translúcido (alfa só 0 ou 255).
- Nada de bestas com armadura, brasões, construções, biomas ou mapa nesta rodada (Prompts D e E).

## O que NÃO foi executado
- `SpriteCache.cs` (Godot) foi alterado e **não compilado nem rodado** aqui. Android/web, benchmark e testes de UI também não.
- Os manifestos de pixels do elenco original mudaram de propósito (botas, passada, golpe de 12 quadros).
