# Auditoria visual dos especiais (antes → depois)

Imagens "antes": `docs/arte/antes/` (anel/linha tracejada + losango + números de texto). "Depois": `docs/arte/depois/`, GIFs em `docs/arte/gifs/`, comparação em `docs/arte/antes-depois-especiais.png`.
Gerar: `dotnet PixelPreview.dll fx-before <dir>` / `fx <dir> [frames]` (ver `tools/PixelPreview/README.md`).

## Problemas do "antes" (todos os 16)
- Sem telégrafo legível: só um anel pontilhado fino; formas (cone, linha, círculo) mal distinguíveis.
- Impacto igual para todos: dano era só número e flash; sem resíduo nem identidade por elemento.
- Estados (atordoar, raiz, veneno…) sem ícone próprio nem camada no corpo; só cor.
- Ícones 16×16 toscos, com a paleta do primeiro personagem.
- Projéteis e acertos básicos sem papel (flecha vs. magia vs. cuspe).

## Checklist por especial (✔ feito, ~ moderado)
| Especial | Telégrafo | Forma/alcance | Impacto | Resíduo | Fim limpo | Observação |
|---|---|---|---|---|---|---|
| ice-wall | ✔ círculo | ✔ | ✔ pilares de gelo + bolha | ✔ | ✔ | forte |
| lightning-cut | ✔ linha | ✔ | ✔ raio + faíscas | ✔ | ✔ | raio fino em 2× |
| arrow-rain | ✔ círculo + sombras | ✔ | ✔ | ✔ | ✔ | bom |
| nova | ✔ | ✔ | ✔ | ✔ | ✔ | bom |
| quake | ✔ | ✔ | ✔ rachaduras | ✔ | ✔ | bom |
| dagger-dance | ✔ | ✔ | ~ arcos | ✔ | ✔ | **fraco**: sem sprite-fantasma (alfa 0/255) |
| blessing | ✔ | ✔ | ✔ coluna de luz | ✔ | ✔ | bom |
| acid | ✔ linha | ✔ | ✔ | ✔ poça | ✔ | moderado |
| roar | ✔ cone | ✔ | ~ ondas | ✔ | ✔ | moderado |
| breath | ✔ cone | ✔ | ✔ fogo | ✔ | ✔ | forte |
| web | ✔ | ✔ | ~ teia | ✔ | ✔ | **fraco/moderado** |
| dive | ✔ linha | ✔ | ✔ | ✔ | ✔ | bom |
| constrict | ✔ | ✔ | ~ espiral de planta | ✔ | ✔ | moderado |
| water-jet | ✔ linha | ✔ | ✔ | ✔ | ✔ | bom |
| roots | ✔ | ✔ | ✔ | ✔ | ✔ | bom |
| rally | ✔ | ✔ | ✔ | ✔ | ✔ | sem imagem de "antes" |

Legibilidade em 2×/3× conferida apenas nas folhas 4× recortadas; **não** foi testada na tela real (sem Godot aqui).
