# Revisão visual orientada pelas quatro gravações

## Leitura das referências antes da implementação

Foram inspecionados quadros das quatro gravações, mais uma sequência de oito quadros do guerreiro azul para comparar movimento. Os vídeos são referências visuais; nenhum quadro foi importado como sprite do jogo.

As referências mostram cabeças largas, grandes áreas contínuas de cor, troncos curtos, mãos de poucos pixels e pés quase escondidos. O guerreiro azul tem bico, capacete com pontas separadas, machado e escudo. O coelho tem cabeça arredondada e orelhas finas com interior rosa. Os pequenos humanos usam acessórios grandes, mas pouco detalhe facial. A animação desloca partes em passos pequenos; o equipamento não fica rigidamente colado ao tronco.

A versão anterior tinha contornos internos demais, pequenos detalhes espalhados, roupa segmentada e uma silhueta mais alta. A última solicitação priorizou explicitamente simplificação e fidelidade, em vez de mais ornamentos.

## Padrão aplicado aos três personagens

| Elemento | Padrão novo |
| --- | --- |
| Grade e origem | 32×32, origem comum (16,29); margem para orelhas e equipamento |
| Cabeça | núcleo de cerca de 14 pixels de largura, contorno em degraus; acessórios ampliam a silhueta |
| Tronco | cinco pixels de altura, com grandes grupos de tecido/armadura |
| Mãos e pés | mãos de dois pixels úteis; botas de dois pixels de altura |
| Contorno | azul muito escuro, predominantemente um pixel externo |
| Luz | planos superiores/esquerdos claros; sombras inferiores/direitas |
| Materiais | cores chapadas, normalmente base e sombra; realce claro compartilhado nos metais |
| Paleta | até 16 cores visíveis por personagem e quadro; alfa estritamente 0 ou 255 |
| Escala no cenário | 4× por padrão para terreno e personagem; zoom inteiro 2×/4×/8× |

32×32 foi mantido porque comporta os volumes e acessórios observados sem aumentar artificialmente a resolução. A gravação não fornece a resolução nativa do jogo original; esta grade é uma decisão artística derivada da observação, não uma medição exata do asset de referência.

## Reconstrução e identidades

- **Bram:** capacete azul reconstruído com três pontas legíveis e uma faixa metálica simples; bico preservado; placas brancas contínuas; arma e escudo separados e reduzidos. Removidos rebites e símbolos internos que prejudicavam a leitura.
- **Nix:** cabeça branca mais arredondada, orelhas harmonizadas, faixa azul, lenço cobre reduzido a uma faixa e ponta curta. Olhos simples, espada curta e bolsa pequena preservam a identidade sem carregar o rosto.
- **Rowan:** capuz verde mais baixo, abertura delimitada por poucos blocos, uma pequena franja, aljava e arco. A roupa verde/marrom usa uma tira diagonal simples, sem manchas de detalhe.

## Arquivos e sistemas alterados

- `pixel_demo/CompactAdventurer.cs`: reconstrução das rotinas de desenho; proporções compartilhadas, contornos, materiais e poses. Continua gerando pixels por código, sem PNG de origem. A API anterior `Render(time, walking, back)` foi preservada.
- `pixel_demo/PixelDemo.cs`: seleção dos personagens, zoom inteiro com origem fixa, escala nativa do terreno, menu de animações, exportação e verificações. A oficina anterior continua acessível pelo mesmo botão.
- `docs/pixel/refactor-*`: novos PNGs, folhas de animação, metadados e capturas reais. O antes/depois usa a captura anterior preservada e a nova saída do mesmo pipeline.

Nenhum sistema de combate, colisão ou regra de jogo foi criado ou substituído.

## Animações e exportação

Idle mantém respiração e acompanhamento discretos. Walk e Run alternam os pés e o peso do tronco, com equipamento em fase oposta. Attack usa arco de machado, golpe de espada ou puxada/soltura de arco, conforme o personagem. Hurt é um recuo curto com olhos semicerrados. As ações temporárias retornam automaticamente ao idle no visualizador.

| Estado | Quadros exportados | FPS | Repetição |
| --- | ---: | ---: | --- |
| Idle | 32 | 8 | sim |
| Walk | 8 | 8 | sim |
| Run | 8 | 12 | sim |
| Attack | 6 | 8 | não |
| Hurt | 4 | 8 | não |

As folhas usam células de 32×32 e origem (16,29). O JSON informa quadro, estado, duração e repetição. Ações são demonstrações visuais de animação; não causam dano de jogo. A vista de costas mantém o mecanismo simplificado que já existia.

## Verificações executadas

- Build Godot 4.5.2 .NET: sem erros ou avisos C#.
- Teste de pixels: três personagens, variações de cabeça, determinismo, paleta, alfa binário, pés fixos no idle, fechamento dos ciclos e ausência de recorte nas cinco animações e nas duas vistas existentes.
- Teste gráfico de interface: seleção de personagens, pausa, mudança de vista com pausa, acionamento e recuperação de ataque e dano; nove verificações aprovadas.
- Captura real do visualizador via Godot Compatibility / Mesa e inspeção dos sprites lado a lado.

O teste de leitura de textura da interface requer display gráfico: o renderizador dummy de `--headless` não permite verificar a imagem enviada à GPU. O runner detecta esse caso e informa o requisito. O teste de pixels continua funcionando sem display.

```bash
./scripts/run.sh --headless -- --pixel-test
DISPLAY=:99 ./scripts/run.sh --audio-driver Dummy -- --pixel-ui-test
./scripts/run.sh --headless -- --pixel-frames
DISPLAY=:99 ./scripts/run.sh --audio-driver Dummy -- --pixel-capture
```

Os testes verificam funcionamento e consistência. A comparação visual é a evidência para avaliar a aproximação artística às referências.
