# Validação do terreno 2.5D

Executado no Godot 4.5.2 .NET oficial, .NET 8, Linux, renderer Compatibility/OpenGL, Mesa llvmpipe (CPU), display Xorg virtual 1440×960. Logs nesta pasta são saídas reais dos comandos documentados em [TERRENOS.md](../TERRENOS.md).

| Verificação | Resultado |
|---|---|
| Geração pura, 100 sementes × 4 temas | **1.186.441 verificações, 0 falhas** |
| Variedade | **100/100** sementes com mapas diferentes |
| Paridade com o ruído original do Studio | **4/4** doubles idênticos |
| Regressão de pixels Forge | **5.328 hashes, 0 diferenças** |
| Eventos de batalha contra a referência anterior | **240 sementes, 65.548 eventos, 0 diferenças** |
| UI do ateliê no Godot | **20 verificações**, inclui arrasto por evento de toque, geração, temas, limites dos botões e save/load |
| UI da arena no Godot | **16 verificações**, inclui projeção e projeção inversa |
| UI do elenco no Godot | **38 verificações** |
| Benchmark da arena, 42 combatentes | Mediana **16,07 ms**, p95 **23,28 ms** por quadro |

O teste de geração cobre continuidade da trilha/ponte por busca no grafo, clareira plana da arena, limites das células, repetibilidade, receita JSON, rejeição de versão/parâmetros inválidos e parâmetros em zero. Os eventos foram comparados com `docs/arte/logs/events-depois.json`, usando os hashes por semente emitidos por `battle-trace` (incluem todos os eventos). `battle/Sim` e as receitas de combate não foram alterados.

O benchmark é uma medição desta máquina, sem comparação controlada antes/depois nesta tarefa. Não comprova 60 FPS estáveis: o p95 supera 16,67 ms. O driver não suporta alternar V-Sync; o aviso está preservado no log. Não foi medido desempenho de GPU dedicada nem celular.

## Revisão visual

Capturas reais em [Woodland.png](Woodland.png), [Marsh.png](Marsh.png), [Ember.png](Ember.png), [Frost.png](Frost.png), [arena.png](arena.png) e [arena.gif](arena.gif). O GIF reúne 48 quadros da arena, separados por oito passos de simulação (30 Hz); é uma amostragem, não uma medição da fluidez da apresentação.

Foram inspecionados os quatro temas e a arena. Após a primeira revisão, o relevo ganhou uma formação montanhosa por semente, os patamares aumentaram e a trilha ficou mais larga para os sprites não desaparecerem atrás da borda frontal. Na revisão final, os três heróis aparecem inteiros sobre a trilha; água/ponte e penhascos são distinguíveis nos quatro temas. Na arena, sprites e indicadores acompanham a projeção do terreno e os obstáculos existentes aparecem como blocos.

Limites visuais: os quatro temas compartilham geometria e espécies de árvore; diferem pela paleta. As faces dos penhascos são simples, com estratos estilizados. Sprites próximos continuam se sobrepondo no combate. A arena usa relevo periférico e clareira plana; a movimentação não percorre alturas.

## Não executado nesta tarefa

- Android, navegador, GPU de celular, dispositivo de toque físico e controle.
- Exportação de novos executáveis Windows/Linux. Os ZIPs anteriores em `downloads/` não incluem esta mudança.
- Editor completo, projeto original Godot 4.7.2 e renderer Forward+ do Cartoon Terrain Studio. Foram executados separadamente os módulos matemáticos originais para obter os vetores de referência.
- Novas regras de navegação em altura, colisão com rios/penhascos e campanha; esses sistemas não foram acrescentados.
