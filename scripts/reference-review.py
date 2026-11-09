"""Compõe provas a partir dos PNGs da Forge; não desenha nem retoca sprites.

Executar na raiz do repositório depois de `PixelPreview chibi`.
As ampliações usam fatores inteiros e nearest-neighbor. GIFs usam os tempos
do JSON exportado junto do clipe e uma paleta exata, sem dithering.
"""
from pathlib import Path
import json

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1] / "docs/arte/revisao-referencia"
ATLAS = ROOT / "atlas"
BEFORE = ROOT / "antes"
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
HEROES = ["bram", "nix", "rowan", "iria", "boro", "suri", "mira"]
SPECIES = ["Duck", "Rabbit", "Human", "Turtle", "Fox", "Owl", "Mouse", "Cat", "Frog", "Elf", "Bear"]
DARK, LIGHT = "#151e2a", "#e5dfce"
TEXT, MUTED, GOLD = "#f0eade", "#aebaca", "#c8ac72"


def font(size):
    return ImageFont.truetype(FONT, size)


def page(title, subtitle):
    canvas = Image.new("RGB", (1280, 720), DARK)
    draw = ImageDraw.Draw(canvas)
    draw.text((32, 22), "VERDANT / REVISÃO DO ELENCO", font=font(14), fill=GOLD)
    draw.text((32, 50), title, font=font(30), fill=TEXT)
    draw.text((32, 94), subtitle, font=font(16), fill=MUTED)
    return canvas, draw


def sprite(canvas, source, x, y, zoom):
    with Image.open(source) as raw:
        image = raw.convert("RGBA")
    image = image.resize((image.width * zoom, image.height * zoom), Image.Resampling.NEAREST)
    canvas.paste(image, (x, y), image)


def centered(draw, text, x, y, width, size=17, color=GOLD):
    draw.text((x + width / 2, y), text, font=font(size), fill=color, anchor="mt")


def roster():
    canvas, draw = page("Elenco / revisão de formas e poses", "Sete receitas geradas pela Forge · ampliação inteira · pixels nativos abaixo")
    for i, name in enumerate(HEROES):
        x = 32 + i * 174
        sprite(canvas, ATLAS / f"{name}.png", x + 22, 137, 4)
        centered(draw, name.capitalize(), x, 278, 174)
    # Em cada coluna, 1× e 2× à esquerda, 3× à direita; mesmos pivôs e recortes.
    for top, background, ink in [(330, DARK, MUTED), (520, LIGHT, "#39424b")]:
        draw.rectangle((24, top, 1256, top + 172), fill=background)
        draw.text((36, top + 8), "FUNDO ESCURO" if background == DARK else "FUNDO CLARO", font=font(12), fill=ink)
        for i, name in enumerate(HEROES):
            x = 32 + i * 174
            sprite(canvas, ATLAS / f"{name}.png", x + 16, top + 28, 1)
            sprite(canvas, ATLAS / f"{name}.png", x, top + 70, 2)
            sprite(canvas, ATLAS / f"{name}.png", x + 72, top + 38, 3)
            draw.text((x + 48, top + 37), "1×", font=font(11), fill=ink)
            centered(draw, "2×", x, top + 143, 64, 12, ink)
            centered(draw, "3×", x + 72, top + 143, 96, 12, ink)
    canvas.save(ROOT / "elenco.png")


def comparison():
    canvas, draw = page("Comparação / mesma escala", "Em cima: versão anterior · embaixo: revisão atual · todos em 4×")
    draw.text((34, 136), "ANTERIOR", font=font(13), fill=MUTED)
    draw.line((32, 385, 1248, 385), fill="#354352")
    draw.text((34, 409), "REVISÃO", font=font(13), fill=MUTED)
    for i, name in enumerate(HEROES):
        x = 32 + i * 174
        sprite(canvas, BEFORE / f"{name}.png", x + 22, 187, 4)
        sprite(canvas, ATLAS / f"{name}.png", x + 22, 456, 4)
        centered(draw, name.capitalize(), x, 617, 174)
    canvas.save(ROOT / "comparacao.png")


def species():
    canvas, draw = page("Espécies / formas e materiais", "Onze espécies existentes · equipamento comum para comparar a anatomia · 4×")
    for i, name in enumerate(SPECIES):
        col, row = i % 6, i // 6
        x, y = 44 + col * 204, 172 + row * 250
        sprite(canvas, ATLAS / f"species-{name}.png", x + 20, y, 4)
        centered(draw, name, x, y + 149, 168)
    canvas.save(ROOT / "especies.png")


def exact_gif(frames, target, durations):
    colors = sorted({pixel for frame in frames for pixel in frame.getdata()})
    if len(colors) > 256:
        raise ValueError(f"{target}: {len(colors)} cores; não quantizar a arte silenciosamente")
    palette = [component for color in colors for component in color]
    palette += [0] * (768 - len(palette))
    lookup = {color: index for index, color in enumerate(colors)}
    indexed = []
    for frame in frames:
        result = Image.new("P", frame.size)
        result.putpalette(palette)
        result.putdata([lookup[pixel] for pixel in frame.getdata()])
        indexed.append(result)
    # GIF mede centésimos; distribuir o arredondamento preserva a duração do clipe.
    elapsed, written, gif_durations = 0, 0, []
    for duration in durations:
        elapsed += duration
        boundary = int(elapsed / 10 + .5) * 10
        gif_durations.append(boundary - written)
        written = boundary
    indexed[0].save(target, save_all=True, append_images=indexed[1:], duration=gif_durations,
                    loop=0, disposal=2, optimize=False)


def animations():
    for name in HEROES:
        canvas = Image.new("RGB", (256, 256), DARK)
        sprite(canvas, ATLAS / f"{name}.png", 0, 0, 8)
        canvas.save(ROOT / f"{name}-8x.png")
        for clip, suffix in [("Idle", "idle"), ("Attack", "ataque")]:
            with Image.open(ATLAS / f"{name}-{clip}.png") as raw:
                sheet = raw.convert("RGBA")
            metadata = json.loads((ATLAS / f"{name}-{clip}.json").read_text())
            cell_width, cell_height = metadata["cell"]
            frames = []
            durations = []
            for frame in metadata["frames"]:
                x, y = frame["x"], frame["y"]
                source = sheet.crop((x, y, x + cell_width, y + cell_height))
                source = source.resize((cell_width * 8, cell_height * 8), Image.Resampling.NEAREST)
                canvas = Image.new("RGB", source.size, DARK)
                canvas.paste(source, (0, 0), source)
                frames.append(canvas)
                durations.append(round(frame["durationMs"]))
            exact_gif(frames, ROOT / f"{name}-{suffix}.gif", durations)
            if clip == "Attack":
                strip = Image.new("RGB", (len(frames) * cell_width * 8, cell_height * 8), DARK)
                for i, frame in enumerate(frames):
                    strip.paste(frame, (i * cell_width * 8, 0))
                strip.save(ROOT / f"{name}-ataque-8x.png")


if __name__ == "__main__":
    roster()
    comparison()
    species()
    animations()
    print("REVIEW boards=3 portraits=7 gifs=14 attack_strips=7 source=Forge nearest=True")
