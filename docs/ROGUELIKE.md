# VERDANT — roguelike dos cinco reinos

**Estado desta entrega: R0, fundação.** A arena/oficina existente continua jogável. Ainda não há Nova run, mapa, save de run, recompensas nem conquista de reinos. O plano de economia nacional, guerra por província e extração foi substituído por esta direção. Consulte [decisões](DECISOES.md) e [portabilidade](PORTABILIDADE.md).

## Regras adotadas para R1–R5

Uma run é curta, independente e identificada por semente. Escolha três heróis iniciais; o grupo pode crescer até seis. Conquistar os cinco reinos vence; perder o grupo inteiro encerra a run. A ordem dos reinos é livre, e a dificuldade depende de quantos já foram conquistados e da distância percorrida, usando uma tabela central.

Antes de cada reino, percorra terras selvagens em mapa ramificado: combate, elite, acampamento, recrutamento, relíquia, evento de encruzilhada, loja opcional e entrada do reino. Cada nó deve ser alcançável e existir pelo menos um caminho à entrada. Trilhas arriscadas oferecem recompensas maiores. Dentro do reino, duas a quatro batalhas de tropas, acampamento opcional e boss, em sequência linear. Conquistar oferece uma recompensa maior e libera a escolha do próximo reino restante.

Vida persiste entre combates. Um herói derrotado volta com um ponto de vida após uma vitória; se todos caírem, a run termina. Acampamentos, relíquias e conquistas podem curar. Ouro é o único recurso e serve para a loja. Após combate, escolha uma entre três recompensas: ouro, relíquia, cura, melhoria de habilidade ou recrutamento. Grupo cheio exige escolher uma substituição.

Cada herói mantém uma assinatura de habilidade. Até dois níveis de melhoria serão descritos em tabelas por habilidade, sem código por personagem. Quinze a vinte relíquias passivas terão efeitos e ícones próprios. O combate reutiliza a simulação atual, com vida inicial da run, controle de especiais por toque, pausa, 1×/2×/4× e automático.

## Contratos da implementação futura

- `run/Sim`: estado, comandos, mapas, seletores, tabelas, recompensas, save e bot, sem Godot. `core/Verdant.Core.csproj` será a biblioteca compartilhada com apresentação, CLI e web.
- Fluxos distintos e estáveis por semente para mapa, encontros, recompensas, eventos e combate. Nenhuma decisão usa relógio ou RNG global.
- JSON versionado, escrita temporária e renomeação, backup anterior, validação e migração explícita. Salvar escolhas pendentes, ao concluir nó e perder foco. Continuação no meio de combate exigirá especificação + comandos + tick e reconstrução determinística, ou snapshot completo validado; o replay atual, sozinho, ainda não é esse save.
- UI Godot separada da oficina desktop: viewport lógico 480×270, paisagem, escala inteira nearest e margens seguras. Alvos de toque ≥48 dp medidos após escala/DPI, nunca apenas 48 pixels físicos. Fontes ≥8 pixels lógicos, dicas por toque longo, arrastar mapa, formas além de cores. Não mudar a escala das cenas desktop antes de adaptar seus layouts.
- Opções de velocidade, 30/60 FPS, som, fonte/interface e daltonismo. Autosave e restauração de foco não podem consumir uma decisão duas vezes.
- Metas de balanceamento para R4: bot simples vence o primeiro reino em 70–90% e a run em 5–25%, medido em milhares de runs. Não há taxa de run medida em R0.

## Marcos

| Marco | Conteúdo | Estado |
|---|---|---|
| R0 | Auditoria, biblioteca sem Godot, hashes, estudo/prova web, presets Android | Entrega atual |
| R1 | Estado da run, RNG por fluxo, mapa, cinco reinos em dados, encontros, save, bot básico | Pendente |
| R2 | Integração de combate, fases, escala, relíquias, recompensas, recrutamento e vida | Pendente |
| R3 | Telas de toque, opções, retomada, emblemas, ícones e biomas | Pendente |
| R4 | Milhares de runs, balanceamento, eventos, loja e polimento | Pendente |
| R5 | Builds finais Windows/Linux, tentativa APK, evidências finais | Pendente |

## Fase 2 — pontos de extensão

R1 deve prever `HeroStatus` (`Ready`, `Down`, `Dead`) e `PermadeathMode` (`Off` por padrão). Modo ligado será previsto/testado nas regras, sem UI nesta fase. Tabelas devem permitir desbloqueios de heróis/relíquias, ascensões, semente diária, outros reinos e eventos longos. Nenhum desses sistemas está implementado em R0. Fortalecimento permanente de heróis, relações e traços continuam fora de escopo. Sem anúncios, microtransações, rede ou conta.
