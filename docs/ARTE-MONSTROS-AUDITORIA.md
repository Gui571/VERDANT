# Auditoria visual dos monstros

Base: roguelike-reinos. Pranchas anteriores preservadas em `arte/antes`, oito movimentos por família. A leitura ampliada revela repetição de poses, além de problemas que aparecem em 1×/2×.

| Família | Problema observado | Prioridade |
|---|---|---|
| Slime | Cabeça quase separada da base; apêndices parecem chifres. Walk/run só sobem o corpo. Brilho pequeno não sugere gel. | Massa contínua, compressão e poça |
| Besta | Corpo horizontal legível, mas ombros baixos e pernas sem passagem. Boca de humano no golpe. | Ombros, focinho, apoio alternado e tombamento |
| Golem | Cabeça redonda domina; punhos são dois retângulos sem articulação. Morte derrete como slime. | Ombros blocados, punhos com planos, desmoronamento |
| Dragão | Perfil muito próximo da besta; cauda triangular imóvel, pescoço sem curva. | Pescoço elevado, placas, cauda atrasada |
| Artrópode | Pernas parecem uma escada; pinças não antecipam golpe. Abdômen pouco distinto. | Segmentos, pernas angulares e recolhimento |
| Alado | Asas curtas com duas posições; parece levantar os braços. | Membrana e articulação, queda antes do encolhimento |
| Serpente | Base achatada sem espiral; deslocamento copia quadrúpedes. | Espiral com espaço negativo e desenrolar |
| Aquático | Contorno de peixe já legível; nadadeira pequena e cauda rígida. | Nervuras e atraso da cauda; morte murcha |
| Planta | Cabeça redonda e braços finos, sem copa suficiente. | Folhas agrupadas, raízes e secagem |

Em todas: contorno e contraste facial funcionam em 2×, mas adornos genéricos confundem a família em 1×. Idle contém longos trechos repetidos; corrida só acelera caminhada. O flash é legível. Special/Cast cobrem o rosto, porém seus efeitos estão fora do escopo e serão preservados. Não foram vistos pixels isolados no repouso das nove receitas de referência; a validação deve ampliar essa cobertura. Não basta aumentar detalhes: espaços negativos e poses extremas são a primeira correção.

## Revisões realizadas

1. Anatomia e trajetórias por família; renderização de ataque/dano/morte em 8×. A revisão detectou olhos abaixo da nova cabeça do dragão e colunas esticadas na morte do golem. Rosto reposicionado; morte refeita com quatro blocos rígidos, quedas defasadas e planos iluminados.
2. Pernas do artrópode mudaram de travessas paralelas para joelhos diagonais. Elementos receberam cristas animadas e detalhes de superfície; chefes receberam extensões anatômicas por família e fissuras na fase 2. Matrizes dos cinco elementos, três raridades e duas fases foram inspecionadas em 8×.
3. A validação de silhuetas por semente encontrou apenas 2/1/2 máscaras para slime/golem/aquático em 32 sementes com elemento e cor fixos. Cabeça, ombros e posição da cauda agora variam: 4/4/4 máscaras. As outras famílias produzem 4–10; ainda há espaço para mais diversidade anatômica.
4. A primeira entrada deslocava o raster e cortava os pés. Foi substituída por elevação da anatomia antes do contorno, preservando sombra e margem. A checagem de órfãos expôs resíduos nas pernas ao tentar preservar a base do raster; essa abordagem foi removida. A versão final passou e suas tiras foram reinspecionadas em 8×.

As pranchas antes/depois têm as mesmas sementes 100–108. Em `arte/depois/*-8x.png`, as linhas são idle 0–7, idle 8–15, walk, run, attack, hurt, death, special, cast e entry. Os GIFs individuais usam todos os quadros, com os tempos do exportador. As imagens de leitura 1×/2×/3× foram inspecionadas; a captura da arena foi verificada em quadros reais da simulação.

## Checklist final por família

S = satisfatório na prancha isolada; P = parcial, com ressalva artística. Loop/base/cores/órfãos são verificações técnicas, não uma nota de qualidade artística.

| Família | Silhueta 1×/2× | Peso do ataque/movimento | Morte própria | Loop/base/≤16 cores/órfãos | Ressalva observada |
|---|---|---|---|---|---|
| Slime | S | S, compressão visível | S, poça | S | Ainda tem um topo parecido com cabeça; brilho sugere gel, mas não simula transparência |
| Besta | S | P, apoio alternado | P, queda frontal | S | Pernas pequenas e tombamento ainda comprimem muito a massa |
| Golem | S | S, punhos separados | S, blocos | S | Cabeça e ombros variam pouco em relação às outras famílias |
| Dragão | S | P, cabeça e cauda defasadas | P, cauda cai depois | S | Pescoço elevado ainda não é um S muito marcado; morte poderia girar o tronco |
| Artrópode | S | S, joelhos/pinças | P, recolhimento | S | Morte recolhe em vez de mostrar um giro completo de costas |
| Alado | S | P, asas com atraso | S, queda e fechamento | S | Comum ainda tem amplitude curta; chefe lê melhor como asa larga |
| Serpente | S | P, cabeça desliza | P, espiral abre | S | Espaço negativo funciona; falta propagação mais longa de onda no corpo |
| Aquático | S | S, cauda/nadadeira | S, murcha | S | Movimento do corpo ainda pequeno e quatro variantes de máscara |
| Planta | S | S, folhagem secundária | S, curva e seca | S | Secagem final usa um tom escuro e perde detalhe interno |

Em 1×, golem/dragão/artrópode/alado/serpente/aquático têm assinaturas mais claras; slime e planta dependem mais dos apêndices superiores. Em 2× e 3×, olhos, planos dos punhos, espiral e nadadeiras ficam claros. Placas de elite são pequenas, especialmente em gelo; comum/elite não têm separação tão forte quanto comum/chefe. A paleta de planta e veneno é próxima, mas folhas versus bolhas ajudam sem depender só da cor. Não foi feita sessão de avaliação com pessoas com daltonismo.

Os chefes têm extensões de anatomia e segunda fase por fissuras/expressão, mas entrada e morte ainda são compactas; não equivalem a uma sequência cinematográfica exclusiva. Não foram implementadas nove mortes adicionais por elemento, névoa de gelo volumosa ou fumaça com partículas independentes. O impacto visual dos especiais continua igual por escopo.

Na arena, a sobreposição de combatentes e o flash branco simultâneo ainda escondem partes do corpo. O GIF registra isso; esta tarefa não alterou posicionamento, colisões, duração de estados ou efeitos de habilidade para mascarar o problema. A métrica de máscaras isoladas não resolve leitura em aglomeração.

## Evidências

- [Antes × depois, 8×](arte/antes-depois-monstros.png)
- [Pranchas e GIFs por família](arte/depois/)
- [Arena real, 120 quadros](arte/arena-monstros.gif)
- [Leitura 1×](arte/leitura-1x.png), [2×](arte/leitura-2x.png), [3×](arte/leitura-3x.png)
- [Execução, preservação do elenco e benchmark](arte/RELATORIO.md)

A distância mínima entre as nove máscaras de referência é 0,190 (diferença simétrica / união, 36 pares, sem linhas inferiores de sombra). O limite de regressão é 0,10. Isso comprova diferença geométrica, não reconhecimento humano universal. A base fixa é verificada no apoio do idle; caminhada tem apoio alternado e o alado/entrada se afastam intencionalmente da sombra.
