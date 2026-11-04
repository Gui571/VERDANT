# Decisões de extração e guerras

## Entrega incremental

Branch `extracao-e-guerras`, base e destino do PR `monstros-e-arena`. Este PR entrega o **marco 1: especiais, estados, N times, obstáculos, oficina e arena**. Os demais marcos ainda não estão implementados. Evoluímos o Forge e a simulação existentes, preservando os PNGs anteriores do elenco.

## Decisões implementadas

- Recarga fixa por habilidade, em vez de energia: custo previsível para comandar até seis unidades, menos estado oculto e comparação de balanceamento mais simples. Valores e associações ficam em `Abilities.cs`.
- Simulação a 30 Hz; apresentação desacoplada. Decisões de interrupção são derivadas deterministicamente da semente/tick/unidade; nenhum relógio participa da lógica.
- Chefes têm uma segunda assinatura ao atravessar 50% da vida, com no máximo duas invocações por chefe e teto de 256 combatentes.
- Obstáculos retangulares e deslizamento simples bastam para a arena aberta deste marco. Navegação de zonas maiores será tratada e validada com a extração.
- Logs JSON de comandos aceitos permitem repetir uma batalha. Save versionado da campanha será implementado junto ao estado do mundo.
- Special/Cast compartilham a construção procedural de poses do Forge e usam oito quadros; atlas por receita evita rasterização por frame.

## Direção adotada para os próximos marcos (planejada)

**Marco 2 — nações e mundo:** dez províncias em grafo e quatro nações iniciais (jogador + três IA), variáveis dentro dos intervalos solicitados. Cada dia: planejamento → incursão opcional → guerra → produção/manutenção/eventos. Ouro, ferro, madeira, cristais e comida. Quartel, Forja, Muralha, Torre, Oficina, Hospital, Celeiro e Mercado com custos explícitos. Estado imutável e JSON versionado, comandos e eventos separados da UI, fluxos de RNG por sistema. Cor da nação na rampa Cloth e emblema procedural 16×16 de três cores. IA agressiva/defensiva/mercantil usa os mesmos recursos visíveis; sem recursos extras ocultos.

**Marco 3 — extração:** zonas curtas por semente, quatro biomas, capacidade de carga, ameaça crescente e canalização no ponto de saída. Derrota deixa feridos por padrão, morte permanente no hardcore; carga só entra no estoque após extração. Caminho início→extração validado. Comandos de movimento/especial/recuo compartilham a simulação da arena. Névoa simples e facções rivais podem gerar mais de dois times.

**Marco 4 — guerra:** formação, defesa e ataque, estruturas, capital e defensor final; relatório de feridos/baixas/saque/território. Mesma simulação na apresentação e autoresolução. Derrota quando cai a capital; vitória por conquistar capitais, sobreviver ao prazo ou tratado. O benchmark de 40 unidades totais do marco 1 não substitui a validação futura de 40 por lado.

**Marco 5 — integração:** ciclo planejar→extrair→defender→crescer, balanceamento, testes ponta a ponta e save→load→continuar, documentação NACOES/EXTRACAO/GUERRA e builds finais. Sem rede, conta, microtransações ou progressão infinita. Áudio opcional com licença conhecida.

Números de economia e duração de campanha serão definidos no marco correspondente, com simulações completas e registro da justificativa. Esta lista registra a direção aprovada, não funcionalidades disponíveis nesta build.

## Efeitos especiais (arte-efeitos-especiais)
- Efeitos ficam fora de `pixel_demo/Forge` (atlases do elenco/monstros byte-idênticos, verificado por diff do `export`/`monsters`); só os ícones 16×16 foram redesenhados (limite de cores 3→5).
- Atlas único + três camadas, sem rasterizar por quadro; RNG por evento (semente da simulação + índice) para determinismo.
- Hit-stop e tremor são só da arena, nunca da simulação; tremor tem botão de desligar.
- Sem sprite-fantasma translúcido (alfa só 0/255): o rastro do Nix usa eco de faíscas.
