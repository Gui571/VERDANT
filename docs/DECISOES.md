# Decisões — roguelike dos reinos

## Revisão pela referência em vídeo (2026-11-08)

A versão compacta anterior também foi rejeitada pelo autor. A comparação com o vídeo identificou pose frontal rígida, rosto quadrado repetido, roupa dividida em faixas e equipamento sem volume. Esta revisão corrige a construção: cabeça-base de nove linhas com perfis por espécie, rosto deslocado em três quartos, tronco mais largo e baixo, pés assimétricos, elmo com coroa/aba, capuzes envolventes, escudos em perspectiva e machado em cunha. A roupa passa a ter alça diagonal no arqueiro, painel lateral na maga e gola em V na curandeira. Ataque desloca mais o peso e atrasa a capa; arco mostra puxada e liberação da corda.

A apresentação nova usa **equipe azul** como padrão, com rampa `Cornflower` gerada offline pelo pipeline OKLCH. **Cores do herói** permanece uma opção da galeria. A identidade vem também de espécie, arma e adereço, e não apenas de cores individuais. A nova rampa não substitui as tabelas anteriores. Célula 32×32, origem (16,29), receitas e tempos de clipe permanecem. `ChibiValidation` agora exige no máximo 16 cores por quadro; a matriz executada observou 15.

Tudo continua gerado por C# com `Layer`/`Tone` e cacheado antes da animação. O caminho legado conserva 5.328 hashes idênticos; a simulação e os efeitos de especiais não foram alterados. O escopo é o elenco existente de sete heróis e onze espécies. Quatro vistas continuam sendo estudo: norte simples, oeste espelhado e passada lateral compartilhada. Monstros, terreno, expansão de classes/espécies e demais marcos M2–M8 não estão concluídos por esta revisão.

[Comparação e evidências](arte/revisao-referencia/RELATORIO.md). As imagens documentam o resultado; não há declaração de equivalência exata ao vídeo nem de aprovação do autor.


## Correção artística após M1 — elenco compacto (2026-11-07)

O autor rejeitou a entrega apenas de cor e pediu maior proximidade com a referência. O foco desta revisão é mudar desenho/proporções dos sete heróis e das onze espécies atuais, sem anunciar a expansão inteira de M2–M8. Novo renderer de anatomia em C# reutiliza receitas, Layer/Tone e rampas. Cabeça 13×10, tronco 8×5, olhos mínimos, formas/armas separadas e cores próprias por herói. Sem extrair sprites da referência.

Após comparar as pranchas, a nova arte passa a ser a apresentação inicial da galeria, prévia e arena; há botão de comparação com o legado. A nova cena principal mostra o elenco imediatamente. O núcleo/exportadores antigos continuam com flag desligada e hashes preservados. Argumentos antigos de teste são encaminhados à cena anterior. Não alteramos battle/Sim, stats, especiais ou geração de encontros.

Quatro vistas para estudo/galeria; a arena conserva seleção lateral de direção existente. Oeste espelha o desenho, norte retira o rosto e constrói costas/capa. Não é a conclusão completa do marco M3. Peças ainda usam a receita atual e o RNG existente; fluxos independentes, 45 espécies/20 classes, bestas e mundo continuam no plano seguinte.


## M1 — materiais/luz opcionais (2026-11-07)

`estilo-v2-m1-cor-luz` parte de M0. A flag continua desligada por padrão; nova página Materiais v2 e alternância na arena. O cache incorpora estilo e equipe à chave visual. Trocar o estilo durante a luta só troca atlas, sem reiniciar simulação. Nenhum arquivo de `battle/Sim` ou dado de habilidades foi alterado.

Reutilização de Layer/Tone, sem redesenhar o elenco nesta etapa. Os tons legados viram identificadores de material, depois cinco níveis de luz. As rampas OKLCH são geradas offline em tabelas sRGB; cor escrita em bytes, com luz por vizinhança inteira. Materiais futuros têm tabelas, mas terreno, construções e bestas não foram convertidos nesta entrega. Efeitos existentes de Special/Cast são preservados.

Limite de 32 cores no v2 (31 observadas). Comparação de cores-base Fur/TEAM usa distância RGB ao quadrado mínima de 625, complementada por contorno e revisão visual; não é uma alegação WCAG. As rampas de equipe usam base um pouco mais escura para separar verde do sapo/tartaruga e equipe lima. A máscara antiga toca as bordas em algumas poses; M1 conserva isso e M2 terá de redesenhar para garantir margens.

M0/M1 são a primeira entrega desta sequência. M2–M8 continuam pendentes; não há anúncio de 45 espécies, 20 classes, bestas, heráldica ou mundo completo. Priorizar em seguida as 11 espécies com fluxos independentes e margem antes de ampliar o catálogo.


## Estilo unificado v2 — M0 (2026-11-07)

Base real `terreno-generativo-25d` em `e2e96e3`; auditoria em [DIRECAO-ESTILO-V2.md](DIRECAO-ESTILO-V2.md). Referência autorizada apenas como inspiração; nenhuma imagem extraída é distribuída como asset. O arquivo recebido tem 1024×576 e os tempos dos slides diferem do índice textual.

Entregar por marcos/PRs em ordem. M0 documenta; M1 introduz material/cor/luz em opção desligada por padrão; redesenho/fluxos por peça pertencem a M2. Cinco tons em tabelas OKLCH calculadas offline; 32 cores opacas no v2, 16 no legado. Células 32×32, origem (16,29), sem mudar a simulação. Terreno 3D anterior preservado, alternativa pintada por chunks fica em M7.


## Terreno generativo 2.5D (2026-11-03)

Branch `terreno-generativo-25d`, base `arte-monstros`. A tarefa de terreno usa o núcleo de ruído/elevação MIT do Cartoon Terrain Studio, fixado em `7aa76ad5`, com renderer novo Compatibility para manter Godot 4.5.2. Não se migra o projeto para 4.7.2 nem se importa todo o editor Forward+.

Mapa finito 48×32, patamares em blocos 2×2, quatro paletas, rio e trilha com pontes. Cenário de arena mantém clareira plana para não criar obstáculos visuais no espaço caminhável. A câmera muda a apresentação: projeção/inversa são compartilhadas por unidades, efeitos, seleção e ordens. Sem alterações na simulação ou nos dados de combate. Terreno decorativo não habilita navegação vertical, água transitável ou combate em alturas diferentes.

O RNG da batalha não consome valores da geração do terreno. O movimento da água é só shader e não faz parte do estado determinístico. Receitas JSON versionadas são locais; não são saves de run ou mapas completos do Studio. [Escopo, atribuição e resultados](TERRENOS.md).


## Revisão de arte de monstros (2026-11-03)

Base real: `roguelike-reinos` em `0b0ed469`; branch de trabalho `arte-monstros`. O branch contém R0 e os especiais; não contém uma run completa. A tarefa atual se limita à arte de monstros e aos caminhos de atlas/apresentação. Nenhum arquivo de `battle/Sim`, `BattleRecipes`, `ForgeEffects` ou do elenco foi alterado.

- A auditoria e as imagens anteriores estão em [ARTE-MONSTROS-AUDITORIA.md](ARTE-MONSTROS-AUDITORIA.md). A revisão preserva células/origens e receitas, com `Entry` acrescentado ao fim do enum de movimentos.
- Mortes passam a oito quadros a 12 FPS para caber no tempo de apresentação existente. O cache preserva seis quadros a 8 FPS para a morte dos heróis.
- Segunda fase é exclusivamente visual e usa o `Phase` existente. Cache inclui dois atlases somente para chefes; não muda o limiar, atributos ou habilidades.
- Adornos elementais são camadas aderidas ao corpo dentro do atlas. Sem partículas novas que possam ser confundidas com os efeitos dos especiais, que estão fora do escopo.
- `MonsterSpec` mantém o formato; nenhum campo de gameplay novo. A exportação da oficina registra a fase visual em metadados, fora da receita.
- Manifesto de portabilidade é atualizado intencionalmente para os monstros; os 2.898 hashes de heróis permanecem iguais. Os resultados históricos de R0 abaixo continuam descrevendo aquela entrega.
- Não se altera IA/movimento para evitar sobreposição de sprites na arena. A captura registra essa limitação; o teste de silhuetas isoladas não mede legibilidade em aglomeração.


## R0: auditoria do estado real

Auditado `extracao-e-guerras` em `2939397`, antes de criar `roguelike-reinos`. Leitura de ESPECIAIS, BATALHA, MONSTROS, FORGE, ARCHITECTURE, decisões, fontes de batalha/Forge/oficina e PixelPreview.

| Área | Encontrado no código |
|---|---|
| Batalha | C# puro, 30 Hz, N times, 16 habilidades, recarga 210 ticks, cast 12 ticks, estados, áreas, obstáculos retangulares e projéteis com interceptação |
| Comandos | Movimento, colocação, especial, retirada e modo manual; replay JSON de comandos, sem save completo de partida |
| Chefes | Receita 48×48; segunda habilidade ao passar de 50% da vida; não há bosses únicos de reino ou terceira fase |
| Arte | Forge procedural para sete heróis/gerados/nove famílias; Special/Cast reaproveitam poses de ataque; cache de atlas por receita |
| Oficina e arena | Cenas desktop 1440×960, quatro times na UI, teclas/hover/botão direito em partes da interação; ainda não cumprem a UI mobile solicitada |
| Legado | Oficina 192×160 e save de genoma isolados; não são sistema de campanha |
| Campanha | Nenhum sistema de nações, economia, províncias, extração, save de campanha ou run encontrado; nada dessa natureza foi removido |
| Portabilidade anterior | PixelPreview usava shim Godot; Forge/Raster ainda referenciavam Color/Image/Vector2 do motor |

BATALHA/MONSTROS e partes de ARCHITECTURE descrevem versões antigas. O apêndice de especiais atualiza a arena. A arquitetura documentada do addon legado não deve ser confundida com a simulação de combate.

## Nova direção aprovada

O plano anterior de nações/economia/extração/guerra territorial está **substituído**. Não implementá-lo nos próximos marcos. Preservar o combate, a oficina, os especiais e a ideia de identidade por reino (Cloth + emblema).

Runs independentes, cinco reinos em ordem livre e dificuldade por número conquistado/distância. Três heróis iniciais, máximo seis; vida persistente; derrotado volta com 1 HP após vitória; derrota total encerra. Ouro é o único recurso. As regras e extensões de fase 2 estão em [ROGUELIKE.md](ROGUELIKE.md); conteúdo de reinos em [REINOS.md](REINOS.md). Nenhuma economia de nação ou província permanece no rumo ativo.

## R0 implementado e escolhas técnicas

- Uma biblioteca `Verdant.Core` .NET 8 reúne as fontes existentes por links de compilação. Godot, CLI e prova WASM usam a mesma implementação. Nada de shim com namespace Godot no núcleo.
- `Rgba` conserva floats e quantização anterior; `PixelPoint` guarda origem inteira. Image/Color/Vector2 do motor ficam no adaptador `GodotRaster`. Raster usa System.Numerics. Hashes de 5.058 conjuntos de quadros são iguais nos quatro caminhos antes/depois.
- Núcleo de batalha continua com estado mutável isolado e eventos/entradas imutáveis, como já funcionava. R1 adicionará transições de run por comandos imutáveis; não reescrever o combate para fazer parecer que já era inteiramente imutável.
- Browser recomendado: núcleo C# em .NET WebAssembly com apresentação canvas separada. Prova mínima executada; não adotar fork experimental nem mudar todo o projeto para .NET 9 agora.
- Presets Android APK debug/AAB release arm64, sem segredos. Tentativa de export registrada como falha de ambiente, não como build entregue.
- UI futura da run em 480×270, integer/nearest, toque e safe areas. Preservar cenas desktop em sua escala até a implementação de R3; mudar só project.godot agora quebraria o que é jogável.
- Água ainda não existe no enum de elementos: acrescentar ao final em R2 e testar que receitas antigas não mudam. Naufraga não receberá gelo como substituto silencioso de água.
- Terceira fase opcional liga-se ao quinto reino conquistado; ordem livre continua válida. Campos de morte permanente/desbloqueios são contratos de R1/fase 2, não uma funcionalidade de R0.

Entrega desta etapa: **R0**. R1–R5 permanecem pendentes, um marco jogável/testado por vez. Não chamar o benchmark desktop de validação de desempenho mobile, nem taxas de batalhas avulsas de taxa de vitória da run.

---

O histórico abaixo é mantido para rastreabilidade; seu plano futuro não deve ser executado.

# Histórico: extração e guerras (superado)

## Entrega incremental

Branch `extracao-e-guerras`, base e destino do PR `monstros-e-arena`. Este PR entrega o **marco 1: especiais, estados, N times, obstáculos, oficina e arena**. Os demais marcos ainda não estão implementados. Evoluímos o Forge e a simulação existentes, preservando os PNGs anteriores do elenco.

## Decisões implementadas

- Recarga fixa por habilidade, em vez de energia: custo previsível para comandar até seis unidades, menos estado oculto e comparação de balanceamento mais simples. Valores e associações ficam em `Abilities.cs`.
- Simulação a 30 Hz; apresentação desacoplada. Decisões de interrupção são derivadas deterministicamente da semente/tick/unidade; nenhum relógio participa da lógica.
- Chefes têm uma segunda assinatura ao atravessar 50% da vida, com no máximo duas invocações por chefe e teto de 256 combatentes.
- Obstáculos retangulares e deslizamento simples bastam para a arena aberta deste marco. Navegação de zonas maiores será tratada e validada com a extração.
- Logs JSON de comandos aceitos permitem repetir uma batalha. Save versionado da campanha será implementado junto ao estado do mundo.
- Special/Cast compartilham a construção procedural de poses do Forge e usam oito quadros; atlas por receita evita rasterização por frame.

## Plano anterior cancelado para trabalho futuro

**Marco 2 — nações e mundo:** dez províncias em grafo e quatro nações iniciais (jogador + três IA), variáveis dentro dos intervalos solicitados. Cada dia: planejamento → incursão opcional → guerra → produção/manutenção/eventos. Ouro, ferro, madeira, cristais e comida. Quartel, Forja, Muralha, Torre, Oficina, Hospital, Celeiro e Mercado com custos explícitos. Estado imutável e JSON versionado, comandos e eventos separados da UI, fluxos de RNG por sistema. Cor da nação na rampa Cloth e emblema procedural 16×16 de três cores. IA agressiva/defensiva/mercantil usa os mesmos recursos visíveis; sem recursos extras ocultos.

**Marco 3 — extração:** zonas curtas por semente, quatro biomas, capacidade de carga, ameaça crescente e canalização no ponto de saída. Derrota deixa feridos por padrão, morte permanente no hardcore; carga só entra no estoque após extração. Caminho início→extração validado. Comandos de movimento/especial/recuo compartilham a simulação da arena. Névoa simples e facções rivais podem gerar mais de dois times.

**Marco 4 — guerra:** formação, defesa e ataque, estruturas, capital e defensor final; relatório de feridos/baixas/saque/território. Mesma simulação na apresentação e autoresolução. Derrota quando cai a capital; vitória por conquistar capitais, sobreviver ao prazo ou tratado. O benchmark de 40 unidades totais do marco 1 não substitui a validação futura de 40 por lado.

**Marco 5 — integração:** ciclo planejar→extrair→defender→crescer, balanceamento, testes ponta a ponta e save→load→continuar, documentação NACOES/EXTRACAO/GUERRA e builds finais. Sem rede, conta, microtransações ou progressão infinita. Áudio opcional com licença conhecida.

Números de economia e duração de campanha serão definidos no marco correspondente, com simulações completas e registro da justificativa. Esta lista registra a direção aprovada, não funcionalidades disponíveis nesta build.
