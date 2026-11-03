# Arte de monstros — execução e limites

Base `roguelike-reinos` (`0b0ed469`), branch `arte-monstros`. Ambiente Linux x86_64, .NET 8, Godot **4.5.2 stable mono oficial**, Compatibility, Xorg/Mesa llvmpipe (renderização por software). Logs completos em [logs](logs/).

## Saída dos testes executados

```text
MONSTERS checks=236065 failures=0
MONSTER_MASK minimum_distance=0.190 pairs=36
checks=372861 falhas=0
PIXEL_TEST_PASS assertions=372861
SPECIAL checks=39 failures=0
BATTLE checks=1220 failures=0
GODOT_COMBAT checks=1259 failures=0
BATTLE_UI_PASS checks=13
MONSTER_UI_PASS checks=27
HERO_RGBA hashes=2898 differences=0
HERO_PNG files=105 differences=0
ATLAS_ALL hashes=5328 monster_changes_intentional=true
BATTLE_EVENTS seeds=240 events=65548 differences=0
```

MonsterValidation cobre as nove receitas de referência, 120 sementes e a matriz 9 famílias × 5 elementos × 3 raridades com características máximas, incluindo as duas fases dos chefes, frente/costas e todos os quadros. Verifica limites, margem, alfa, cores, determinismo, período dos loops, apoio do idle, remoção da morte, variedade de receitas e distância entre máscaras. A checagem de pixels órfãos agora inclui todos os movimentos corporais e entrada; Special/Cast preservam partículas existentes e são cobertos por limites/alfa/paleta/determinismo, sem alterar seus efeitos.

Variedade geométrica com elemento/cor/raridade fixos, 32 sementes por família: slime 4, besta 8, golem 4, dragão 8, artrópode 10, alado 4, serpente 8, aquático 4, planta 10 máscaras. A métrica de distinção familiar usa nove receitas de referência, 36 pares, distância simétrica/união mínima 0,190 e limiar 0,10. Não é um teste de reconhecimento humano nem exaustivo de todas as sementes possíveis.

`BattleTrace` usa exatamente as receitas e sementes 1–240 de BattleValidation. Foi executado tanto no código do branch de origem, extraído com `git archive`, quanto neste branch. Os hashes de TODOS os campos dos 65.548 eventos coincidem ([antes](logs/events-antes.json), [depois](logs/events-depois.json)). `battle/Sim`, `BattleRecipes` e `ForgeEffects` têm diff vazio contra a origem.

Os 2.898 hashes RGBA incluem sete heróis do elenco e 200 gerados, seus movimentos e orientações. Os 105 PNGs do diretório do elenco foram comparados por SHA-256 com os blobs do branch de origem ([lista](logs/hero-png-hashes.json)). O manifesto `docs/portability/atlas-sha256.json` foi atualizado apenas para as mudanças intencionais dos monstros e as entradas novas. O núcleo continua sem dependência Godot.

## Benchmark antes/depois

Mesmo `--battle-benchmark` existente, semente padrão 42, 20 receitas por lado; 42 combatentes após invocações, 180 ticks iniciais e 360 intervalos de apresentação. O número impresso conta entradas no elenco, inclusive unidades eventualmente derrotadas; não significa 42 unidades simultaneamente vivas por toda a amostra. Cache é construído antes da medição; tempo de geração do atlas não está incluído.

| Versão | Mediana | Percentil 95 |
|---|---:|---:|
| Origem | 12,36 ms | 25,65 ms |
| Arte final | 13,01 ms | 17,61 ms |

Logs: [antes](logs/benchmark-antes.log), [depois](logs/benchmark-depois.log). Mediana +0,65 ms; p95 -8,04 ms. Uma amostra por versão no renderer por software tem variação de ambiente: não atribuímos a queda do p95 só à mudança de arte. A rodada final ocorreu sem geração de sprites/testes em paralelo. **Não se comprova 60 FPS estáveis:** o p95 ainda ultrapassa 16,67 ms. Não foi feito benchmark Android/web ou de GPU física.

Pixels do atlas por receita comum: 320 KiB; chefe: aproximadamente 1,406 MiB para duas fases. Antes: 256 KiB comum e 576 KiB chefe. O cache guarda as camadas elementais no mesmo atlas. A oficina deixou de criar texturas por quadro e usa as regiões prontas; a arena já usava cache. Essas contas não incluem objetos Godot, cenário ou memória geral do processo.

## Revisão visual e artefatos

Pranchas antes/depois, tiras de todas as famílias/movimentos, quadros extremos, matrizes de elementos/raridades/fases e recortes foram inspecionados em ≥8×. Leitura isolada em 1×, 2× e 3× também foi revista. As falhas encontradas e correções iterativas estão na [auditoria](../ARTE-MONSTROS-AUDITORIA.md).

- [Antes × depois](antes-depois-monstros.png): antes na primeira linha; depois na segunda; ordem Slime, Beast, Golem, Dragon, Arthropod, Winged, Serpent, Aquatic, Plant.
- [Tiras e GIFs](depois/): 81 GIFs individuais, incluindo todos os quadros dos nove movimentos.
- [Arena real](arena-monstros.gif): 120 capturas do Godot, 0,2 s simulados por quadro, nove famílias em dois times e um golem chefe. Os quadros foram inspecionados; o GIF não é uma medição de FPS.
- [Campo em 3×](arena-campo-3x.gif): recorte da mesma captura, nearest, sem redesenhar sprites.
- [Oficina](../monsters/workshop.png): preview 8× usando AtlasTexture.

Continuam fracos: tombamento de dragão/besta/serpente, giro de costas do artrópode, distinção de algumas elites a 1× e leitura durante aglomerações/flash simultâneo. Chefes têm entrada e morte compactas, não uma sequência dramática exclusiva longa. Não há morte alternativa para cada elemento. Essas limitações não foram escondidas por mudanças de simulação ou de efeitos especiais.

## Não executado nesta tarefa

Android, navegador, dispositivos físicos e Windows não foram executados. Nenhum APK, build web ou novo ZIP Windows/Linux foi produzido; os downloads anteriores permanecem identificados como antigos. A revisão é executável pelo Godot e pelo PixelPreview no código deste branch. Não houve teste humano de daltonismo. Não foi implementada ou alterada uma run.
