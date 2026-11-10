# VERDANT — Análise do vídeo de referência + Prompts C, D e E para o Codex

O vídeo (2min40, 1280×720, 15 "estudos" em slides) é uma vitrine de um gerador procedural de pixel art: personagens, bestas, brasões, construções, flora, biomas, vilas e um mapa-múndi, tudo gerado por código a partir de sementes. Este documento tem duas partes:

1. **Análise** (o que o vídeo mostra e quais regras de estilo dá para extrair)
2. **Prompts** (C = personagens/animação/bestas, D = brasões/construções/props, E = flora/biomas/vilas/mapa), prontos para colar no Codex, junto com o vídeo anexado.

> Aviso honesto: tudo abaixo vem do que se **vê** no vídeo. Não há acesso ao código do projeto do seu amigo, então técnicas internas (como a geração em OKLCH) são as que o próprio vídeo cita, e tamanhos de sprite são **estimativas** por contagem de pixels na tela. O Codex deve tratar isto como direção de arte, não como especificação exata. Inspiração total, cópia não: espécies, nomes, logotipo e sprites devem ser originais do VERDANT.

---

# PARTE 1 — ANÁLISE DO VÍDEO

## 1.1 Linguagem visual geral (o que dá a "cara" do estilo)

| Regra | Como aparece no vídeo |
|---|---|
| **Forma primeiro, cor depois** (slide 02) | Mostra a silhueta chapada, depois os materiais, depois luz + tom. A silhueta já identifica espécie/classe sozinha. |
| **Material + tom → rampas de 5 tons em OKLCH → luz compartilhada** | Paleta exibida: FUR (pelagem), TEAM (cor do time), METAL, cada uma com 5 passos do escuro ao claro. |
| **Sombras frias, realces quentes** | Legenda literal: "Shared outlines. Cool shadows. Warm highlights." A rampa muda de matiz, não só de brilho. |
| **Contorno compartilhado** | Mesmo tratamento de contorno em todas as espécies/estruturas ("shared light and outlines"). Contorno escuro colorido, não preto puro; em coisas claras vira tom médio. |
| **Pixel perfeito** | Sem blur, sem meio-pixel; tudo em grade inteira, ampliado em múltiplos inteiros (cerca de 8× nos slides). |
| **Mesmo time, matizes diferentes** | Seis variações de cor de time sobre a mesma forma (faixa de cabeça e detalhes mudam; corpo permanece). |
| **Proporção chibi** | Cabeça grande (~metade da altura), corpo curto, arma grande, orelhas/chifres/acessórios que quebram a silhueta. Estimativa: personagem ≈ 16×20 pixels de arte. |
| **Fundo e UI** | Azul-marinho quase preto, tipografia limpa, rótulos mono em âmbar. (Só da apresentação; não precisa copiar.) |

## 1.2 Seção por seção (com marcações de tempo)

**00:00 Abertura — "A world. Built from rules."**
Grade de 45 espécies × classes numa semente (2042) e um herói grande. "Replay / same pixels": mesma semente ⇒ mesmos pixels. Meta declarada: 45 espécies, 20 classes, um estilo.

**00:08 Estudo 01 — "Change one part. Keep the character."**
Oito painéis: HEAD, HAIR, HEADGEAR, OUTFIT, WEAPON, OFFHAND, BACK, COLORS. Cada painel troca **um** fluxo de semente independente (cada um com seu número) e preserva o resto. Lição: o gerador é por **slots**, cada slot com seu próprio RNG derivado da semente-base.

**00:16 Estudo 02 — "Shape first. Colour second."**
Silhueta → materiais → luz/tom. Rampas 5 tons por material. Seis matizes de time.

**00:24 Estudo 03 — "45 species. One visual language."**
Mosaico 9×5: humanos e povos fantásticos (human, elf, dwarf, gnome, orc, goblin, lizardfolk, fishfolk, demon), mortos-vivos (skeleton, zombie), animais antropomorfos (cat, dog, wolf, fox, bear, panda, boar, pig, rabbit, mouse, owl, duck, bull, goat, deer, horse, monkey, raccoon, frog, beetle, turtle) e "levemente incomuns" (pumpkin, mushroom, eyeball, slime, flame, candle, crystal, robot, golem, ghost, flower, fishbowl, moon). Todos com a mesma estrutura: cabeça grande, corpo curto, arma na mão, **uma coisa reconhecível por espécie** (orelha, chifre, bico, casco, tampa de cogumelo).

**00:32 Estudo 04 — "A silhouette for every role."**
20 classes, 5 papéis de combate, 4 patentes. Classes vistas: Knight, Paladin, Guardian, Swordsman, Barbarian, Halberdier, Rogue, Monk, Militia, Archer, Crossbowman, Alchemist, Mage, Witch, Necromancer, Shaman, Cleric, Druid, Bard, Bannerman. Cada uma muda **arma + roupa + acessório de cabeça** mantendo o corpo da espécie. Patentes **Recruit → Veteran → Elite → Hero**: mesmo personagem com equipamento cada vez mais elaborado (mais metal, plumas, capa); HP e dano progridem junto.

**00:40 Estudo 05 — "The motion is generated, too."**
Clipes: idle, walk, attack, hit, death; **4 direções** (south, east, north, west). Detalhes que dão fluidez, e que você gostou:
- **Ataque em fases**: antecipação (recuo/wind-up) → contato → recuperação, com **durações de quadro diferentes por pose** (não é tudo igual).
- **Arco de golpe** desenhado (meia-lua clara) no quadro de contato.
- **Hit**: personagem se encolhe + padrão **xadrez (dither) branco** sobre o corpo (piscada sem alfa parcial).
- **Death**: tomba/derrete no chão com a arma largada e um brilho (gema) que fica.
- Movimento **secundário**: orelhas, capa, plumas e arma balançam com atraso.
- Cada direção é um desenho coerente (norte mostra costas, capacete/cabelo traseiro), não só espelho; oeste parece espelho do leste (**incerto**, vale testar espelhar e comparar).

**00:52 Estudo 06 — "Ten beasts. Six body plans."**
Lobo, cão, aranha, elefante, coruja, escorpião, macaco, urso, gato, javali. Seis planos de corpo: quadrúpede, aranha (8 patas), escorpião, ave, símio, paquiderme. **Barding** (armadura de montaria) em 4 estilos sobre o mesmo bicho: Knight (placas), Raider (peles/espetos), Tribal (pintura/ossos), Lamellar (escamas). Cada besta carrega um **estandarte** na sela.

**01:00 Estudo 07 — "One identity. At every scale."**
8 famílias heráldicas × 24 sementes: Heraldic, Order, War, Noble, Tribal, Guild, Arcane, Flag. Campo + ordinária (faixa, cruz, diagonal) + carga (leão, chave, estrela, lua…) + cores + pano, **fluxos independentes**. Mesma identidade em 4 usos: **bandeira com pano ondulando**, **estandarte pendurado**, **escudo**, **marcador de mapa**; "the same arms adapt to the available pixels" (versão simplificada em tamanho pequeno).

**01:08–01:28 Estudos 08 e 09 — Kit de lugares e "small things"**
- 11 famílias estruturais: palisade, tent, hut, tower, campfire, prop, plant, fort, bridge, mill, barricade.
- 6 tipos de tenda (A-tent, round, command, lean-to, tipi, ridge); 5 fortificações (keep, gatehouse, round tower, square tower, motte).
- Casas por uso: house, hall, smithy, tavern, store, chapel, stable, church, townhouse, barn, shed, mill.
- Cercas/muros: log, wattle, fortified, rail, picket, hedge, dry stone, curtain wall.
- **32 props**: caixote, barril, saco, poço, palheiro, pilha de lenha, alvo, espantalho, estandarte, carroça, estante de armas, manequim, varal, colmeias, placa, mesa, bloco de corte, sepulturas, barraca de feira, totem, rack de secar, fonte, santuário, banco, cocho, esterco, barco, mastro de maio, lampião.
- Todos "pintados com as mesmas regras de material"; camadas **animadas independentes** (velas do moinho, pano, fumaça, chama de fogueira).

**01:28 Estudo 10 — "Built to change state."**
Ponte inteira → **com vão** → reparo; portão aberto → **fechado → destruição**; abatis, carroças, estacas, toras em estados. "State changes redraw the affected geometry" (redesenha a geometria afetada, não só troca sprite).

**01:36 Estudo 11 — "From canopy to resource node."**
18 espécies selvagens (carvalho, bétula, choupo, salgueiro, árvore morta, abeto, pinheiro, arbusto, arbusto de frutas, mato seco, pedra, laje, veio de ferro, ametista, cogumelo, taboa, madeira de deriva, pilha de pedras) + 11 plantadas. **Recursos com estágios Full → Damaged → Depleted** (exatamente o que o jogo de extração precisa).

**01:44 Estudo 12 — "Four biomes. Shared rules."**
Pradaria, floresta de outono, estepe, pântano: **mesma semente, quatro conjuntos de regras**. Relevo, rio, penhascos, estrada, escadas. Mostra também as **camadas de dados reais**: material do solo e **walkability** (verde = andável, vermelho = bloqueado). Lição: a arte e a navegação saem do mesmo mapa de dados.

**02:00 Estudo 13 — "Parts become places."**
14 formas de assentamento (farmstead, cluster, round, brook, motte, keep, bandit, stockade…) montadas em ordem: **terra → estradas e água → lotes → prédios → jardins → detalhes** (12 passos de montagem). Cada forma tem lógica própria (vila de riacho, castelo com fosso, acampamento bandido na floresta, paliçada).

**02:20 Estudo 14 — "And places become a world."**
Renderer do mundo (detalhe de rua, castelo com fosso e portão, casas de telhado vermelho, campos) + **mapa inteiro 1024×1024** células, uma semente, rotas ligando lugares, visão estratégica.

**02:32 Estudo 15 — "Pixels with the data to use them."**
Atlas de sprites, tempos, paletas, **profundidade por pixel** ("a sprite is only the visible part"), navegação e receitas exportadas ao lado da arte (PNG + JSON + dados binários do mapa).

## 1.3 O que fez o vídeo parecer "fluido" (síntese)
1. Todo o resto compartilha a mesma regra de paleta e contorno ⇒ coesão.
2. A animação tem **timing por pose** e movimento secundário, não só ciclo uniforme.
3. Quase tudo tem **estados** (dano, reparo, esgotado) e camadas animadas (fumaça, pano, vela).
4. A identidade (cor de time, brasão) atravessa escalas: sprite, bandeira, escudo, marcador de mapa.
5. Arte e dados jogáveis (navegação, profundidade, receitas) saem da mesma semente.

## 1.4 Como isso encaixa no VERDANT
- Já existe: Forge (personagens/monstros procedurais), elenco de 7, oficina de monstros, arena, efeitos (PR #7), roguelike com 5 reinos, extração e guerras.
- Ganho direto: **brasões dos 5 reinos**, **recursos com estágios** (extração), **construções com dano** (guerra), **biomas por reino**, **patentes** (progressão do roguelike), **bestas com barding** (montarias/monstros).
- Cuidado: o VERDANT já tem identidade (elenco, contorno azul-marinho). O estilo novo deve **evoluir** o Forge, mantendo Bram, Nix, Rowan, Iria, Boro, Suri e Mira reconhecíveis.

---

# PARTE 2 — PROMPTS PARA O CODEX

Sugestão: mande **um prompt por vez**, na ordem C → D → E, cada um numa branch própria e com PR próprio (você já preferiu dividir assim). Anexe o vídeo em todos.

## Regras comuns (copie no começo de cada prompt)

```
CONTEXTO
Projeto: VERDANT (Godot 4.5.2 .NET, C#/.NET 8, pixel art, motor procedural "Forge", batalha determinística a 30 Hz, arena, oficina de monstros, camada de efeitos em battle/Fx, roguelike de 5 reinos). Anexei um vídeo de referência de um gerador procedural de pixel art, que um amigo meu autorizou usar como INSPIRAÇÃO. Use o vídeo como direção de arte. NÃO copie nomes, logotipo, sprites nem espécies iguais; tudo deve ser original do VERDANT. Documentação com a análise do vídeo: ANALISE-E-PROMPTS-ESTILO-VIDEO.md (Parte 1).

Antes de qualquer coisa: leia o repositório (Forge, ForgeRoster, ForgeEffects, MonsterWorkshop, battle/Sim, battle/Fx, tools/PixelPreview, docs/*) e responda com um RESUMO do que já existe e o que será reaproveitado. Parta da branch mais atual (confirme qual; provavelmente a do PR #7 ou a branch que o incorporou).

REGRAS DE ARTE (valem para tudo)
- Pixel art de verdade: alfa só 0 ou 255, grade inteira, sem blur, sem escala fracionária, sem meio-pixel, ampliação só em múltiplos inteiros.
- Forma primeiro, cor depois: cada coisa deve ser identificável pela silhueta chapada. Crie um teste/imagem de silhueta.
- Cor por MATERIAL + TOM: rampas de 5 passos geradas em OKLCH (implemente a conversão em C#), com sombras frias e realces quentes (mudança de matiz na rampa), luz compartilhada vinda de cima-esquerda.
- Contorno compartilhado e escuro colorido (não preto puro); contorno mais claro em materiais claros.
- Limite de cores por quadro/sprite (≤16; documentar o real).
- Determinismo total: mesma semente => mesmos pixels (hash de atlas em teste). RNG por SLOT (cabeça, cabelo, elmo, roupa, arma, mão secundária, costas, cores…), cada slot com fluxo próprio derivado da semente-base, para que trocar UM slot não altere os outros (teste "mude uma parte, mantenha o personagem").
- Nada rasterizado por quadro: atlas gerado uma vez e em cache.

REGRAS DE ENGENHARIA E HONESTIDADE
- NÃO mudar simulação, balanceamento, regras, eventos de batalha. BattleValidation/SpecialValidation/FxValidation devem produzir os mesmos eventos.
- Rode e reporte: check 200, special-check, monster-check, fx-check, battle e as novas validações. Mostre números reais.
- Tudo que você não conseguir compilar/rodar (ex.: partes Godot sem engine na nuvem) deve ser dito claramente. Nunca diga "ótimo" sem olhar as imagens geradas; nunca diga que testou o que não rodou.
- Gere imagens antes/depois e folhas de contato em docs/arte/ e INSPECIONE-AS: liste o que ficou fraco.
- Commits pequenos e temáticos; PR contra a branch-base; resumo final honesto (o que melhorou, o que está fraco, números, o que NÃO foi executado).
- Prioridade é preservar a identidade do elenco atual (Bram, Nix, Rowan, Iria, Boro, Suri, Mira) e o tamanho de tela/legibilidade já adotados. Não reduza o canvas dos sprites só para imitar o vídeo.
```

---

## PROMPT C — Personagens, classes, patentes, animação e bestas

```
[cole as REGRAS COMUNS]

OBJETIVO
Elevar o Forge ao nível do vídeo no que diz respeito a: espécies, classes, patentes, animação com timing e bestas com barding. Trabalhe em branch nova "arte-forge-estilo-video-personagens".

0) AUDITORIA (primeiro entregável)
docs/ARTE-FORGE-AUDITORIA.md: compare os sprites atuais com o vídeo (silhueta, rampas, contorno, proporção, animação). Gere imagens "antes" em docs/arte/antes/. Liste o que o Forge já faz bem para NÃO regredir.

1) PIPELINE DE COR
- Implemente rampas de 5 tons por material (pelagem/pele, tecido, metal, couro, madeira, cor de time) em OKLCH, com deriva de matiz (sombra fria, realce quente) e luz compartilhada.
- Seis matizes de time sobre a mesma forma (só faixa de cabeça/detalhes mudam).
- Gerar imagem "silhueta / materiais / luz+tom" por personagem (como o estudo 02 do vídeo).

2) SLOTS E SEMENTES INDEPENDENTES
- Slots: cabeça, cabelo/pelagem, elmo/chapéu, roupa, arma, mão secundária, costas, cores. Cada um com fluxo de RNG próprio.
- Teste automático: para cada slot, trocar só ele e verificar que os pixels dos outros slots não mudam. Folha "mude uma parte, mantenha o personagem".

3) ESPÉCIES
- Meta: pelo menos 30 espécies (as atuais + novas), organizadas em: povos (humano, elfo, anão, gnomo, orc, goblin, povo-lagarto, povo-peixe…), animais antropomorfos, mortos-vivos e "levemente incomuns" (slime, cogumelo, abóbora, vela, cristal, golem, fantasma, robô…). Nomes e desenhos originais.
- Regra: cabeça grande, corpo curto, UM traço marcante por espécie visível na silhueta.
- Tudo compatível com o sistema de monstros/oficina existente (não quebrar monster-check).

4) CLASSES E PATENTES
- 20 classes mapeadas nos papéis de combate já existentes (Role) — NÃO crie papéis novos na simulação; classe é só visual/dados de recrutamento. Cada classe muda arma + roupa + acessório de cabeça mantendo o corpo da espécie.
- 4 patentes visuais: Recruta, Veterano, Elite, Herói (equipamento cada vez mais elaborado: mais metal, pluma, capa, brilho). Exponha a patente como parâmetro visual; NÃO altere atributos do simulador (apenas deixe um ponto de ligação documentado para o roguelike usar depois).

5) ANIMAÇÃO
- Clipes: idle, walk, attack, hit, death, nas 4 direções (sul, leste, norte, oeste). Teste se oeste = espelho de leste; se ficar bom, espelhe, senão desenhe.
- Ataque com antecipação → contato → recuperação, com DURAÇÃO DE QUADRO POR POSE (tabela em dados, não ciclo uniforme), e arco de golpe de 1 quadro.
- Hit: encolhida + padrão xadrez (dither) branco, sem alfa parcial. Death: tomba e deixa arma/brilho no chão.
- Movimento secundário (orelhas, capa, pluma, arma) com atraso de 1–2 quadros.
- Respeitar o tempo da simulação (30 Hz) e o CastTicks atual dos especiais; apenas apresentação.
- GIFs por clipe e por direção em docs/arte/gifs/.

6) BESTAS
- 10 bestas em 6 planos de corpo (quadrúpede, aranha, escorpião, ave, símio, paquiderme), integradas à oficina de monstros.
- 4 estilos de barding (placas, peles/espetos, tribal/ossos, lamelar/escamas) aplicáveis à mesma besta, e estandarte opcional na sela com a cor/brasão do time.

7) VALIDAÇÕES NOVAS
Alfa 0/255, ≤16 cores, sem recorte nas bordas, determinismo por hash, independência de slots, 5 clipes × 4 direções existentes e sem quadro vazio, contagem de espécies/classes/patentes, silhueta distinta entre classes (limiar documentado), elenco original reconhecível (compare com os atlases antigos e explique diferenças).

8) ENTREGAS
Folha antes/depois, folha de espécies, de classes, de patentes, de bestas; docs/FORGE.md e docs/DECISOES.md atualizados; tools/PixelPreview/README.md com os novos comandos. Resumo final honesto.
```

---

## PROMPT D — Brasões, construções, props e estados

```
[cole as REGRAS COMUNS]

OBJETIVO
Criar identidade visual de reinos/times e um kit de construções e props procedurais com estados de dano. Branch "arte-forge-estilo-video-estruturas". Parta da branch do Prompt C se já estiver integrada.

0) AUDITORIA: o que já existe de estruturas/props/bandeiras no jogo e no roguelike (5 reinos). Imagens "antes".

1) HERÁLDICA
- Brasão = campo + ordinária + carga + cores + pano, cada parte com fluxo de semente independente.
- 8 famílias (ex.: Heráldico, Ordem, Guerra, Nobre, Tribal, Guilda, Arcano, Bandeira), nomes originais. Gere 24 sementes de exemplo.
- Quatro usos da MESMA identidade: bandeira com pano ondulando (animada), estandarte pendurado, escudo e marcador de mapa, com versão simplificada para poucos pixels.
- Cada um dos 5 reinos do roguelike recebe um brasão fixo (semente salva em dados) + cores de time. Integre ao que existir na tela (sem quebrar telas atuais).

2) KIT DE ESTRUTURAS (famílias)
Paliçada, tenda (6 formas), cabana, torre, fogueira, ponte, moinho, barricada, forte (guarita, torre redonda, torre quadrada, castelo de motte, fortaleza), casas por uso (casa, salão, ferraria, taverna, loja, capela, estábulo, celeiro, galpão), muros e cercas (tora, trançado, fortificado, trilho, estacas, sebe, pedra seca, muralha). Meta mínima: 40 variações originais distintas.
- Material e desgaste por semente (madeira nova/velha, pedra, palha, telha), mesmas regras de rampa/contorno.
- Camadas animadas independentes: velas do moinho, pano, fumaça de chaminé, chama de fogueira.

3) PROPS
Pelo menos 32 props pequenos (caixote, barril, sacos, poço, palheiro, lenha, alvo, espantalho, carroça, estante de armas, varal, colmeias, placa, mesa, bloco de corte, sepulturas, barraca de feira, totem, fonte, santuário, banco, cocho, barco, lampião…). Variações por semente.

4) ESTADOS (para extração e guerra)
- Cada estrutura tem estados: Intacta → Danificada → Destruída, com REDESENHO da geometria afetada (não só trocar de sprite); ponte com vão e reparo; portão aberto/fechado/destruído; barricadas e estacas com falhas.
- Defina um enum/dados de estado de apresentação; NÃO altere a lógica do jogo de guerra, apenas deixe um ponto de ligação documentado (ex.: ler integridade 0–100% e mapear para o estado visual).

5) DADOS EXPORTADOS
Para cada estrutura: atlas PNG + JSON (tamanho, pivô, quadros, durações, âncoras de entrada/porta, pegada de colisão/navegação em células). Profundidade por pixel é opcional; se implementar, documente o formato.

6) VALIDAÇÕES: alfa/cores/determinismo, pegada dentro do sprite, âncoras válidas, todos os estados existem e diferem entre si, camadas animadas com ciclo fechado, brasão legível em 2×/3× nos 4 usos, nenhum reino com brasão duplicado.

7) ENTREGAS: folhas de bandeiras (24), de estruturas por família, de props, de estados (3 colunas), GIFs das animações; docs atualizados; resumo honesto.
```

---

## PROMPT E — Flora, recursos, biomas, vilas e mapa

```
[cole as REGRAS COMUNS]

OBJETIVO
Gerar terreno, flora, recursos e assentamentos coerentes com os 5 reinos do roguelike e com o jogo de extração. Branch "arte-forge-estilo-video-mundo". Parta das branches dos Prompts C/D se integradas.

0) AUDITORIA: como o mapa/cenário é feito hoje, dados de navegação existentes, e imagens "antes".

1) FLORA E RECURSOS
- ≥18 espécies selvagens (árvores variadas, arbusto, arbusto de frutas, mato seco, pedra, laje, veio de ferro, cristal, cogumelo, taboa, madeira de deriva, pilha de pedras) + ≥11 plantadas (pomar, lavoura, etc.). Originais.
- Nós de recurso com estágios Cheio → Danificado → Esgotado, redesenhando a forma. Ligue aos recursos que o jogo de extração já usa (apenas apresentação + ponto de ligação documentado).
- Luz e contorno compartilhados, sombra projetada simples e consistente.

2) BIOMAS
- 4 biomas (ex.: pradaria, floresta de outono, estepe, pântano) com a MESMA semente e quatro conjuntos de regras. Mapeie 1:1 ou por afinidade aos 5 reinos (documentar a decisão; um reino pode ter bioma próprio).
- Relevo, rio, penhascos, estradas, escadas, rampas; linguagem de paleta comum entre biomas (cada bioma muda matizes, não regras de contorno).
- Camadas de dados do mesmo gerador: material do solo e WALKABILITY (andável/bloqueado), exportadas e visualizáveis (como no vídeo).

3) ASSENTAMENTOS
- 14 formas (sítio, aglomerado, redondo, riacho, motte, fortaleza, acampamento bandido, paliçada…) usando as estruturas/props do Prompt D (se ele não estiver integrado, use marcadores e documente).
- Montagem em passos ordenados: terra → estradas e água → lotes → prédios → jardins → detalhes. Gere um relatório/imagem por passo para uma forma de exemplo.
- Cada reino tem pelo menos uma capital e vilas; bandidos/monstros fora dos reinos (conforme o design do roguelike).

4) MAPA
- Mapa inteiro de uma semente, com lugares ligados por rotas e visão estratégica. Proponha tamanho adequado ao roguelike (o vídeo usa 1024×1024 células; para o VERDANT escolha e justifique, por exemplo 256×256 ou por reino) e meça tempo/memória de geração.
- O mapa deve alimentar o modo roguelike (percurso entre reinos, encontros com monstros procedurais fora dos reinos, tropas e chefe dentro). Só crie o ponto de ligação; não implemente nova lógica de jogo.

5) DESEMPENHO E PLATAFORMA
- Geração em cache; meça tempo de geração por tela/chunk e consumo de memória. Considerar Android (tamanho de textura, número de draw calls) e documente riscos; não afirme que rodou em Android se não rodou.

6) VALIDAÇÕES: determinismo (hash do mapa por semente), walkability consistente com o desenho (nenhuma estrutura sobre célula "andável" bloqueando rota principal; conectividade entre lugares), cores/alfa, estágios de recurso distintos, todas as formas de assentamento geradas sem sobreposição.

7) ENTREGAS: folhas por bioma, por categoria de flora, estágios de recurso, 14 assentamentos, mapa inteiro, camadas de dados; docs atualizados; resumo final honesto (inclusive o que ficou fraco e o que não foi executado).
```

---

## Observações finais para você
- **Ordem sugerida:** C (maior impacto visual e menor risco) → D (brasões dos 5 reinos) → E (mundo).
- **Cuidados com expectativa:** o vídeo é uma vitrine com um gerador maduro; atingir "o mesmo nível" em todas as áreas é trabalho de várias rodadas. Peça ao Codex sempre a lista do que ficou fraco.
- **Anexos:** o vídeo + este arquivo.
