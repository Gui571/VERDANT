using Godot;
namespace Verdant.Forge;

// ---------------------------------------------------------------------------------------------
//  VERDANT Forge — núcleo
//  Todos os personagens (elenco fixo e gerados por semente) são montados em uma camada de "tons"
//  (papéis de cor) 32×32 e só no fim convertidos em cores reais. Isso garante:
//   • contorno externo contínuo de 1 px, igual para todos;
//   • paleta limitada (≤16 cores) e trocável sem redesenhar;
//   • sombreamento coerente (luz no canto superior esquerdo).
// ---------------------------------------------------------------------------------------------

/// <summary>Papel de cor. O desenho nunca usa cores diretas: usa o papel e a paleta decide.</summary>
public enum Tone : byte
{
    None, Ink,
    FurD, FurM, FurL,           // pele/pelo: sombra, base, luz
    ClothD, ClothM, ClothL,     // tecido principal (lenço, túnica, chapéu)
    AccD, AccM,                 // acento (bico, joias, forro)
    LeatherD, LeatherM,         // couro, madeira
    MetalD, MetalM, MetalL,     // aço: sombra, base, brilho
    White, Pink
}

/// <summary>Paleta de um personagem: 16 cores no máximo (1 contorno + 15 papéis).</summary>
public sealed record ForgePalette(
    Color Ink, Color FurD, Color FurM, Color FurL, Color ClothD, Color ClothM, Color ClothL,
    Color AccD, Color AccM, Color LeatherD, Color LeatherM, Color MetalD, Color MetalM, Color MetalL,
    Color White, Color Pink)
{
    public static readonly Color InkColor = new("151829");
    public Color Resolve(Tone t) => t switch
    {
        Tone.Ink => Ink, Tone.FurD => FurD, Tone.FurM => FurM, Tone.FurL => FurL,
        Tone.ClothD => ClothD, Tone.ClothM => ClothM, Tone.ClothL => ClothL,
        Tone.AccD => AccD, Tone.AccM => AccM, Tone.LeatherD => LeatherD, Tone.LeatherM => LeatherM,
        Tone.MetalD => MetalD, Tone.MetalM => MetalM, Tone.MetalL => MetalL,
        Tone.White => White, Tone.Pink => Pink, _ => new Color(0, 0, 0, 0)
    };
    /// <summary>Cores do metal/branco/rosa são compartilhadas por todo o elenco (família visual).</summary>
    public static ForgePalette Make(Ramp fur, Ramp cloth, Ramp acc, Ramp leather) => new(
        InkColor, fur.D, fur.M, fur.L, cloth.D, cloth.M, cloth.L, acc.D, acc.M, leather.D, leather.M,
        new Color("5f7b94"), new Color("829fb9"), new Color("c1dceb"), new Color("f0f0e5"), new Color("e991aa"));
}

/// <summary>Rampa de três tons: sombra (desloca para o azul), base e luz (desloca para o quente).</summary>
public readonly record struct Ramp(Color D, Color M, Color L)
{
    public static Ramp Of(string dark, string mid, string light) => new(new Color(dark), new Color(mid), new Color(light));
    /// <summary>Gera a rampa a partir de uma cor base, no estilo do elenco (sombras frias, luzes quentes).</summary>
    public static Ramp From(Color m, float dark = .76f, float light = .30f)
    {
        var (h, s, v) = Hsv.From(m);
        var d = Hsv.To((h + .965f) % 1f, Math.Min(1, s * 1.08f + .04f), v * dark);
        var l = Hsv.To((h + .018f) % 1f, s * .72f, Math.Min(1, v + (1 - v) * light + .04f));
        return new Ramp(d, m, l);
    }
}

public static class Hsv
{
    public static (float h, float s, float v) From(Color c)
    {
        float max = Math.Max(c.R, Math.Max(c.G, c.B)), min = Math.Min(c.R, Math.Min(c.G, c.B)), d = max - min, h = 0;
        if (d > 1e-5f)
        {
            if (max == c.R) h = ((c.G - c.B) / d + 6) % 6; else if (max == c.G) h = (c.B - c.R) / d + 2; else h = (c.R - c.G) / d + 4;
            h /= 6;
        }
        return (h, max <= 0 ? 0 : d / max, max);
    }
    public static Color To(float h, float s, float v)
    {
        h = (h % 1f + 1f) % 1f; float c = v * s, x = c * (1 - MathF.Abs(h * 6 % 2 - 1)), m = v - c; int i = (int)(h * 6);
        (float r, float g, float b) = i switch { 0 => (c, x, 0f), 1 => (x, c, 0f), 2 => (0f, c, x), 3 => (0f, x, c), 4 => (x, 0f, c), _ => (c, 0f, x) };
        // Quantiza em 8 bits para a paleta ser exata e reproduzível.
        float Q(float f) => MathF.Round(Math.Clamp(f + m, 0, 1) * 255f) / 255f;
        return new Color(Q(r), Q(g), Q(b));
    }
}

/// <summary>Gerador pseudoaleatório pequeno e determinístico (SplitMix64) — mesma semente, mesmo personagem.</summary>
public sealed class Rng
{
    ulong s;
    public Rng(ulong seed) { s = seed + 0x9E3779B97F4A7C15UL; Next(); }
    public ulong Next() { s += 0x9E3779B97F4A7C15UL; ulong z = s; z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL; z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL; return z ^ (z >> 31); }
    public float Float() => (Next() >> 40) / (float)(1UL << 24);
    public int Int(int min, int maxExclusive) => min + (int)(Next() % (ulong)(maxExclusive - min));
    public bool Chance(float p) => Float() < p;
    public T Pick<T>(IReadOnlyList<T> items) => items[Int(0, items.Count)];
}

/// <summary>Camada 32×32 de tons com primitivas de pixel art (linhas perfeitas, polígonos, mapas de texto).</summary>
public sealed class Layer
{
    public const int Size = 32;
    public int Dimension { get; }
    public readonly Tone[] Px;
    public Layer(int dimension = Size) { Dimension = dimension; Px = new Tone[dimension * dimension]; }
    public int Clipped { get; private set; }
    public void Clear() { Array.Clear(Px); Clipped = 0; }
    public Tone Get(int x, int y) => x < 0 || y < 0 || x >= Dimension || y >= Dimension ? Tone.None : Px[y * Dimension + x];
    public void Set(int x, int y, Tone t)
    {
        if (t == Tone.None) return;
        if (x < 0 || y < 0 || x >= Dimension || y >= Dimension) { Clipped++; return; }
        Px[y * Dimension + x] = t;
    }
    public void Rect(int x, int y, int w, int h, Tone t) { for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) Set(x + i, y + j, t); }
    public void HLine(int x0, int x1, int y, Tone t) { for (int x = x0; x <= x1; x++) Set(x, y, t); }

    /// <summary>Linha de 1 px (Bresenham). Com <paramref name="outline"/> contorna com tinta sem pisar nos pixels da própria linha.</summary>
    public void Line(int x0, int y0, int x1, int y1, Tone t)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            Set(x0, y0, t); if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err; if (e2 >= dy) { err += dy; x0 += sx; } if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
    public static IEnumerable<(int x, int y)> LinePoints(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            yield return (x0, y0); if (x0 == x1 && y0 == y1) yield break;
            int e2 = 2 * err; if (e2 >= dy) { err += dy; x0 += sx; } if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>Preenche polígono testando o centro de cada pixel (regra par-ímpar).</summary>
    public void Poly(Tone t, params (float x, float y)[] p)
    {
        int y0 = (int)MathF.Floor(p.Min(q => q.y)), y1 = (int)MathF.Ceiling(p.Max(q => q.y)), x0 = (int)MathF.Floor(p.Min(q => q.x)), x1 = (int)MathF.Ceiling(p.Max(q => q.x));
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
        {
            float cx = x + .5f, cy = y + .5f; bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > cy) != (p[j].y > cy) && cx < (p[j].x - p[i].x) * (cy - p[i].y) / (p[j].y - p[i].y) + p[i].x) inside = !inside;
            if (inside) Set(x, y, t);
        }
    }

    /// <summary>Contorna (com tinta) a vizinhança de 4 pixels de todos os pixels dos tons dados, sem apagar o preenchimento deles.</summary>
    public void Halo(IReadOnlyCollection<(int x, int y)> filled, Tone ink = Tone.Ink)
    {
        var set = new HashSet<(int, int)>(filled);
        foreach (var (x, y) in filled) foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) }) if (!set.Contains((nx, ny)) && nx >= 0 && ny >= 0 && nx < Dimension && ny < Dimension) Set(nx, ny, ink); // contorno que cairia fora da célula é descartado
    }

    /// <summary>Cola <paramref name="src"/> sobre esta camada. Com <paramref name="outline"/>, antes separa a peça do que está atrás
    /// com um contorno de tinta de 1 px (vizinhança de 4), como se faz à mão em pixel art.</summary>
    public void Composite(Layer src, bool outline = true, int dx = 0, int dy = 0)
    {
        var filled = new List<(int, int)>();
        for (int y = 0; y < Dimension; y++) for (int x = 0; x < Dimension; x++) if (src.Px[y * Dimension + x] != Tone.None) filled.Add((x + dx, y + dy));
        if (outline) Halo(filled);
        for (int y = 0; y < Dimension; y++) for (int x = 0; x < Dimension; x++) { var t = src.Px[y * Dimension + x]; if (t != Tone.None) Set(x + dx, y + dy, t); }
    }
    public bool Any() { foreach (var t in Px) if (t != Tone.None) return true; return false; }

    public static Tone Legend(char c) => c switch
    {
        'o' => Tone.Ink,
        'F' => Tone.FurD, 'f' => Tone.FurM, 'g' => Tone.FurL,
        'C' => Tone.ClothD, 'c' => Tone.ClothM, 'k' => Tone.ClothL,
        'A' => Tone.AccD, 'a' => Tone.AccM,
        'L' => Tone.LeatherD, 'l' => Tone.LeatherM,
        'M' => Tone.MetalD, 'm' => Tone.MetalM, 'i' => Tone.MetalL,
        'w' => Tone.White, 'p' => Tone.Pink,
        _ => Tone.None
    };

    /// <summary>Carimba um mapa de texto. Legenda: ver <see cref="Legend"/>; '.' e ' ' são transparentes.
    /// Com <paramref name="outline"/> separa a peça das de trás com uma linha de tinta de 1 px.</summary>
    public void Stamp(string[] rows, int ox, int oy, bool outline = false, bool flip = false, Func<Tone, Tone>? remap = null)
    {
        int w = rows.Max(r => r.Length);
        var filled = new List<(int, int)>();
        for (int j = 0; j < rows.Length; j++) for (int i = 0; i < rows[j].Length; i++)
            if (Legend(rows[j][i]) != Tone.None) filled.Add((ox + (flip ? w - 1 - i : i), oy + j));
        if (outline) Halo(filled);
        for (int j = 0; j < rows.Length; j++) for (int i = 0; i < rows[j].Length; i++)
        {
            var t = Legend(rows[j][i]); if (t == Tone.None) continue;
            Set(ox + (flip ? w - 1 - i : i), oy + j, remap == null ? t : remap(t));
        }
    }
}
