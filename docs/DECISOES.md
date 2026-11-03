# Decisões — roguelike dos reinos

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
