namespace Verdant.Forge;

/// <summary>Peças do corpo: cabeça por espécie, cabeças-de-chapéu, tronco, pernas e equipamento de costas.
/// Coordenadas da cabeça são locais (hx,hy = canto superior esquerdo do bloco da cabeça, 14×11).</summary>
public static class Parts
{
    // Máscara da cabeça: [início, fim] de cada linha. 14 px de largura no máximo (contrato do elenco).
    static readonly (int a, int b)[] HeadSpan = { (3, 10), (1, 12), (0, 13), (0, 13), (0, 13), (0, 13), (0, 13), (0, 13), (0, 13), (1, 12), (2, 11) };
    public const int HeadRows = 11;

    // ------------------------------------------------------------------ cabeça
    public static void Head(Layer L, int hx, int hy, Species sp, bool back)
    {
        // Orelhas e adornos que ficam ATRÁS do crânio.
        Ears(L, hx, hy, sp, 0);
        for (int y = 0; y < HeadSpan.Length; y++)
            for (int x = HeadSpan[y].a; x <= HeadSpan[y].b; x++)
            {
                var t = Tone.FurM;
                if ((y == 0 && x >= 4 && x <= 7) || (y == 1 && x >= 2 && x <= 5) || (y == 2 && x <= 2)) t = Tone.FurL;
                if ((x == HeadSpan[y].b && y >= 3) || y >= 9) t = Tone.FurD;
                if (back && y >= 6) t = Tone.FurD;
                L.Set(hx + x, hy + y, t);
            }
    }

    public static void Ears(Layer L, int hx, int hy, Species sp, int flutter)
    {
        switch (sp)
        {
            case Species.Rabbit:
                foreach (int ex in new[] { 1, 8 })
                {
                    int lean = ex == 8 ? flutter : -flutter;
                    string[] ear = { ".g.", "gfg", "fpf", "fpf", "fpf", "fpf", "fpf", "fff" };
                    for (int j = 0; j < ear.Length; j++) for (int i = 0; i < 3; i++)
                    { var t = Layer.Legend(ear[j][i]); if (t != Tone.None) L.Set(hx + ex + i + (j < 3 ? lean : 0), hy - 8 + j + (j < 3 && flutter > 0 ? 1 : 0), t); }
                    // base da orelha liga ao crânio
                    L.Rect(hx + ex, hy - 1, 3, 1, Tone.FurM);
                }
                break;
            case Species.Fox:
                {
                    string[] left = { ".F..", ".ff.", "fppf", "fppf", "ffff" };
                    L.Stamp(left, hx + 0, hy - 5, false);
                    L.Stamp(new[] { "..F.", ".ff.", "fppf", "fppf", "ffff" }, hx + 10, hy - 5, false);
                    break;
                }
            case Species.Cat:
                {
                    L.Stamp(new[] { ".f..", ".ff.", "fppf", "ffpf", "ffff" }, hx + 0, hy - 5, false);
                    L.Stamp(new[] { "..f.", ".ff.", "fppf", "fpff", "ffff" }, hx + 10, hy - 5, false);
                    break;
                }
            case Species.Bear:
                L.Stamp(new[] { ".fff.", "fFFff", "fffff", ".fff." }, hx, hy - 3, false);
                L.Stamp(new[] { ".fff.", "ffFFf", "fffff", ".fff." }, hx + 9, hy - 3, false);
                break;
            case Species.Mouse:
                L.Stamp(new[] { ".fff.", "fpppf", "fpppf", ".fff." }, hx - 1, hy - 3, false);
                L.Stamp(new[] { ".fff.", "fpppf", "fpppf", ".fff." }, hx + 10, hy - 3, false);
                break;
            case Species.Owl:
                L.Stamp(new[] { "f.", "ff", "ff" }, hx + 1, hy - 2, false);
                L.Stamp(new[] { ".f", "ff", "ff" }, hx + 11, hy - 2, false);
                break;
            case Species.Elf:
                L.Stamp(new[] { "f...", "ff..", "fff.", ".fff" }, hx - 3, hy + 3, false);
                L.Stamp(new[] { "...f", "..ff", ".fff", "fff." }, hx + 13, hy + 3, false);
                break;
            case Species.Frog:
                L.Stamp(new[] { "gff.", "ffff" }, hx + 0, hy - 1, false);
                L.Stamp(new[] { ".ffg", "ffff" }, hx + 10, hy - 1, false);
                break;
        }
    }

    // ------------------------------------------------------------------ rosto
    public static void Face(Layer L, int hx, int hy, Species sp, bool blink, bool hurt, bool back)
    {
        if (back) return;
        int eh = blink || hurt ? 1 : 2, ey = blink ? hy + 7 : hy + 6;
        void Eyes(int x1, int x2) { L.Rect(hx + x1, ey, 1, eh, Tone.Ink); L.Rect(hx + x2, ey, 1, eh, Tone.Ink); }
        switch (sp)
        {
            case Species.Duck:
                Eyes(7, 11);
                L.HLine(hx + 10, hx + 16, hy + 8, Tone.AccM); L.HLine(hx + 10, hx + 16, hy + 9, Tone.AccD);
                break;
            case Species.Rabbit:
                Eyes(7, 11);
                L.Set(hx + 12, hy + 8, Tone.Pink); L.HLine(hx + 9, hx + 12, hy + 9, Tone.FurL);
                break;
            case Species.Bear:
            case Species.Human:
            case Species.Elf:
                Eyes(7, 11);
                L.Set(hx + 12, hy + 8, Tone.FurL);
                break;
            case Species.Turtle:
                Eyes(7, 11);
                L.Rect(hx + 10, hy + 8, 4, 3, Tone.FurL); L.Set(hx + 13, hy + 9, Tone.FurD);
                break;
            case Species.Fox:
                Eyes(7, 11);
                L.HLine(hx + 8, hx + 13, hy + 8, Tone.FurL); L.HLine(hx + 6, hx + 13, hy + 9, Tone.FurL); L.HLine(hx + 7, hx + 12, hy + 10, Tone.FurL);
                L.Set(hx + 13, hy + 8, Tone.Ink);
                break;
            case Species.Owl:
            {
                // dois discos faciais pálidos com olhos grandes e um bico pequeno entre eles
                L.Stamp(new[] { ".ggg.", "ggggg", "ggggg", "ggggg", ".ggg." }, hx + 3, hy + 4);
                L.Stamp(new[] { ".ggg.", "ggggg", "ggggg", "ggggg", ".ggg." }, hx + 9, hy + 4);
                int oy = blink ? hy + 7 : hy + 6, oh = blink || hurt ? 1 : 2;
                L.Rect(hx + 5, oy, 2, oh, Tone.Ink); L.Rect(hx + 10, oy, 2, oh, Tone.Ink);
                if (!blink && !hurt) { L.Set(hx + 5, oy, Tone.White); L.Set(hx + 10, oy, Tone.White); }
                L.Set(hx + 8, hy + 8, Tone.AccM); L.Set(hx + 8, hy + 9, Tone.AccD);
                break;
            }
            case Species.Mouse:
                Eyes(7, 11);
                L.Set(hx + 13, hy + 8, Tone.Pink); L.HLine(hx + 10, hx + 13, hy + 9, Tone.FurL);
                break;
            case Species.Cat:
                Eyes(7, 11);
                L.Set(hx + 12, hy + 8, Tone.Pink); L.HLine(hx + 8, hx + 13, hy + 9, Tone.FurL); L.HLine(hx + 9, hx + 12, hy + 10, Tone.FurL);
                L.Set(hx + 3, hy + 2, Tone.FurD); L.Set(hx + 5, hy + 1, Tone.FurD); L.Set(hx + 5, hy + 2, Tone.FurD);
                break;
            case Species.Frog:
                Eyes(7, 11);
                L.HLine(hx + 4, hx + 13, hy + 8, Tone.FurD); L.HLine(hx + 5, hx + 12, hy + 10, Tone.FurL);
                break;
        }
    }

    public static void Beard(Layer L, int hx, int hy)
    {
        L.HLine(hx + 6, hx + 13, hy + 8, Tone.White);
        L.HLine(hx + 2, hx + 13, hy + 9, Tone.White); L.HLine(hx + 3, hx + 12, hy + 10, Tone.White);
        L.HLine(hx + 3, hx + 11, hy + 11, Tone.White); L.HLine(hx + 5, hx + 10, hy + 12, Tone.MetalL);
        L.Set(hx + 2, hy + 9, Tone.MetalL); L.Set(hx + 12, hy + 10, Tone.MetalL); L.Set(hx + 11, hy + 11, Tone.MetalL);
    }

    // ------------------------------------------------------------------ chapéus
    public static void Hat(Layer L, int hx, int hy, Headwear hw, int flutter, Species sp, bool back)
    {
        switch (hw)
        {
            case Headwear.Headband:
            {
                L.Rect(hx, hy + 3, 14, 2, Tone.ClothM); L.HLine(hx + 1, hx + 8, hy + 3, Tone.ClothL); L.HLine(hx + 9, hx + 13, hy + 4, Tone.ClothD);
                L.Set(hx, hy + 4, Tone.ClothD);
                // nó e ponta que balança, à esquerda
                L.Stamp(new[] { "cc", "cC" }, hx - 2, hy + 4 + (flutter > 0 ? 1 : 0));
                L.Stamp(new[] { "C" }, hx - 2 - (flutter > 1 ? 1 : 0), hy + 6 + (flutter > 0 ? 1 : 0));
                break;
            }
            case Headwear.IceHelm:
            {
                string[] helm = {
                    "..c...c...c.....",
                    "..cc..cc..cc....",
                    ".kcccckccccccc..",
                    "kccccccccccccCC.",
                    "kcccciiiiiiicCC.",
                    "kcCciiiiiiimmcC.",
                    ".CCmmmmmmmmmmCC.",
                    "..aaaaaaaaaaaa.." };
                L.Stamp(helm, hx - 1, hy - 3, true);
                L.Set(hx + 9, hy + 2, Tone.Pink);
                break;
            }
            case Headwear.Hood:
            case Headwear.Cowl:
            {
                bool cowl = hw == Headwear.Cowl;
                Tone Dm(Tone t) => !cowl ? t : t switch { Tone.ClothM => Tone.AccM, Tone.ClothD => Tone.AccD, Tone.ClothL => Tone.White, _ => t };
                var rows = new (int a, int b)[] { (3, 9), (1, 11), (-1, 13), (-2, 14), (-2, 14) };
                var tmp = new Layer();
                for (int j = 0; j < rows.Length; j++) for (int x = rows[j].a; x <= rows[j].b; x++)
                    tmp.Set(hx + x, hy - 3 + j, Dm(j == 0 || (j == 1 && x < 5) ? Tone.ClothL : x >= rows[j].b - 1 ? Tone.ClothD : Tone.ClothM));
                for (int y = 2; y <= 10; y++) { tmp.HLine(hx - 2, hx + 1, hy + y, Dm(y < 4 ? Tone.ClothM : Tone.ClothD)); tmp.Set(hx + 2, hy + y, Dm(Tone.ClothD)); }
                for (int y = 1; y <= 9; y++) tmp.Set(hx + 13, hy + y, Dm(Tone.ClothD));
                tmp.HLine(hx + 2, hx + 13, hy + 1, Dm(Tone.ClothD)); tmp.HLine(hx + 3, hx + 12, hy + 2, Dm(Tone.ClothD));
                if (cowl) { tmp.HLine(hx + 1, hx + 13, hy + 1, Tone.ClothM); tmp.Set(hx + 6, hy + 1, Tone.White); tmp.Set(hx + 7, hy + 1, Tone.ClothL); }
                else { tmp.Stamp(new[] { "w", "i" }, hx - 2, hy - 5); tmp.Set(hx - 2, hy - 3, Tone.LeatherM); tmp.Rect(hx + 3, hy + 2, 3, 1, Tone.LeatherM); }
                if (back) {
                    for (int y = 2; y <= 10; y++) tmp.HLine(hx + 2, hx + 12, hy + y, Dm(y > 7 ? Tone.ClothD : Tone.ClothM));
                    tmp.Line(hx + 6, hy + 3, hx + 8, hy + 8, Dm(Tone.ClothD));
                    tmp.Line(hx + 3, hy + 3, hx + 3, hy + 6, Dm(Tone.ClothL));
                }
                L.Composite(tmp, !cowl);
                break;
            }
            case Headwear.WizardHat:
            {
                var tmp = new Layer();
                var cone = new (int a, int b)[] { (7, 9), (6, 9), (5, 9), (4, 9), (3, 10), (3, 10), (2, 11), (2, 11), (1, 12) };
                for (int j = 0; j < cone.Length; j++) for (int x = cone[j].a; x <= cone[j].b; x++)
                    tmp.Set(hx + x, hy - 8 + j, x == cone[j].a ? Tone.ClothL : x >= cone[j].b - 1 ? Tone.ClothD : Tone.ClothM);
                tmp.HLine(hx + 1, hx + 12, hy + 1, Tone.AccM); tmp.HLine(hx + 4, hx + 7, hy + 1, Tone.White);
                tmp.HLine(hx - 3, hx + 16, hy + 2, Tone.ClothM); tmp.HLine(hx - 3, hx + 6, hy + 2, Tone.ClothL);
                tmp.HLine(hx - 2, hx + 15, hy + 3, Tone.ClothD);
                L.Composite(tmp, true);
                break;
            }
            case Headwear.WideHat:
            {
                var tmp = new Layer();
                tmp.HLine(hx + 3, hx + 10, hy - 3, Tone.ClothL);
                tmp.HLine(hx + 1, hx + 12, hy - 2, Tone.ClothM); tmp.HLine(hx + 1, hx + 12, hy - 1, Tone.ClothM); tmp.HLine(hx + 1, hx + 12, hy, Tone.ClothM);
                tmp.HLine(hx + 3, hx + 6, hy - 2, Tone.ClothL);
                tmp.HLine(hx + 1, hx + 12, hy + 1, Tone.AccM);
                tmp.HLine(hx - 3, hx + 16, hy + 2, Tone.ClothM); tmp.HLine(hx - 3, hx + 5, hy + 2, Tone.ClothL);
                tmp.HLine(hx - 2, hx + 15, hy + 3, Tone.ClothD);
                L.Composite(tmp, true);
                break;
            }
            case Headwear.Beret:
                L.Stamp(new[] { "...ccccccc...", ".ckkkkkccccc.", "cccccccccccCC", ".CCCCCCCCCCC." }, hx, hy - 3, true);
                L.HLine(hx + 2, hx + 11, hy + 1, Tone.ClothD);
                break;
            case Headwear.Crown:
            {
                L.Stamp(new[] { "a.a.a", "aaaaa", "aiaia" }, hx + 4, hy - 2, true);
                break;
            }
        }
    }

    // ------------------------------------------------------------------ tronco e pernas
    public static void Torso(Layer L, Outfit outfit, bool scarf, int top, int dx, int dy, bool flare)
    {
        var rows = new List<(int y, int a, int b)> { (20, 13, 18), (21, 12, 19), (22, 11, 20), (23, 11, 20), (24, 11, 20), (25, 11, 20), (26, 12, 19) };
        if (outfit == Outfit.Robe) rows = new() { (20, 13, 18), (21, 12, 19), (22, 11, 20), (23, 11, 20), (24, 10, 21), (25, 9, 22), (26, 8, 23), (27, 8, 23) };
        foreach (var (y, a, b) in rows)
            for (int x = a; x <= b; x++)
            {
                var t = Tone.ClothM;
                if (y <= 22 && x > 14) t = Tone.ClothD;                 // sombra projetada pela cabeça
                else if (x == a && y >= 23 && y <= 24) t = Tone.ClothL;  // luz do lado esquerdo
                else if (x == b || y == rows[^1].y) t = Tone.ClothD;
                L.Set(x + dx, y + dy, t);
            }
        if (outfit == Outfit.Plate)
        {
            L.Rect(11 + dx, 22 + dy, 10, 1, Tone.ClothL); L.Rect(12 + dx, 23 + dy, 3, 1, Tone.ClothL);
            L.Set(10 + dx, 22 + dy, Tone.ClothM); L.Set(21 + dx, 22 + dy, Tone.ClothM);
            L.Set(10 + dx, 23 + dy, Tone.ClothD); L.Set(21 + dx, 23 + dy, Tone.ClothD);
            L.HLine(13 + dx, 18 + dx, 24 + dy, Tone.AccD); L.Set(16 + dx, 23 + dy, Tone.AccM);
        }
        else if (outfit == Outfit.Tunic)
        {
            L.HLine(14 + dx, 19 + dx, 23 + dy, Tone.White); L.HLine(13 + dx, 17 + dx, 24 + dy, Tone.MetalL); L.HLine(18 + dx, 19 + dx, 24 + dy, Tone.MetalM);
        }
        else { L.Rect(15 + dx, 23 + dy, 2, 3, Tone.AccM); L.Set(15 + dx, 23 + dy, Tone.White); L.HLine(9 + dx, 22 + dx, 27 + dy, Tone.AccM); }
        if (outfit != Outfit.Robe) { L.HLine(12 + dx, 19 + dx, 25 + dy, Tone.LeatherM); L.Set(16 + dx, 25 + dy, Tone.AccM); L.Set(17 + dx, 25 + dy, Tone.AccM); }
        if (scarf) { L.HLine(12 + dx, 19 + dx, 21 + dy, Tone.AccM); L.HLine(11 + dx, 20 + dx, 22 + dy, Tone.AccD); L.HLine(12 + dx, 14 + dx, 21 + dy, Tone.AccM); }
    }

    public static void Leg(Layer L, int x, int lift, int toe)
    {
        L.HLine(x, x + 2, 26 - lift, Tone.ClothD);
        L.HLine(x, x + 2, 27 - lift, Tone.LeatherM); L.HLine(x, x + 2 + toe, 28 - lift, Tone.LeatherD);
        if (toe > 0) L.Set(x + 3, 27 - lift, Tone.LeatherM);
    }

    // ------------------------------------------------------------------ costas
    public static Layer Back(BackGear g, Species sp, int flutter, int dy, bool scarfTail)
    {
        var tmp = new Layer();
        switch (g)
        {
            case BackGear.Cape:
            {
                int f = flutter;
                var rows = new (int y, int a)[] { (21, 10), (22, 9), (23, 8), (24, 7), (25, 6 - f), (26, 6 - f) };
                foreach (var (y, a) in rows) for (int x = a; x <= 13; x++) tmp.Set(x, y + dy, y >= 25 || x >= 12 ? Tone.ClothD : x <= a + 1 ? Tone.ClothL : Tone.ClothM);
                if (scarfTail) { tmp.HLine(5 - f, 9, 23 + dy, Tone.AccM); tmp.HLine(5 - f, 8, 24 + dy, Tone.AccD); }
                break;
            }
            case BackGear.Shell:
            {
                var rows = new (int y, int a)[] { (14, 6), (15, 4), (16, 3), (17, 2), (18, 2), (19, 2), (20, 2), (21, 2), (22, 2), (23, 2), (24, 3), (25, 4), (26, 5) };
                foreach (var (y, a) in rows) for (int x = a; x <= 12; x++) tmp.Set(x, y + dy, x == a || y == 15 ? Tone.ClothL : x >= 11 || y >= 25 ? Tone.ClothD : Tone.ClothM);
                for (int y = 17; y <= 25; y++) tmp.Set(7, y + dy, Tone.ClothD); tmp.HLine(3, 11, 20 + dy, Tone.ClothD);
                tmp.Rect(4, 17 + dy, 2, 2, Tone.AccM); tmp.Rect(9, 17 + dy, 2, 2, Tone.AccM); tmp.Rect(4, 22 + dy, 2, 2, Tone.AccM);
                break;
            }
            case BackGear.Quiver:
            {
                tmp.Poly(Tone.LeatherM, (8, 19 + dy), (11, 19 + dy), (13, 26 + dy), (10, 27 + dy));
                tmp.Line(9, 20 + dy, 11, 26 + dy, Tone.LeatherD); tmp.Set(10, 19 + dy, Tone.LeatherD);
                tmp.Line(9, 18 + dy, 7, 12 + dy, Tone.LeatherM); tmp.Line(11, 18 + dy, 11, 11 + dy, Tone.LeatherM);
                tmp.Set(7, 11 + dy, Tone.White); tmp.Set(7, 12 + dy, Tone.MetalL); tmp.Set(11, 10 + dy, Tone.White); tmp.Set(11, 11 + dy, Tone.MetalL);
                break;
            }
            case BackGear.Tail:
                Tail(tmp, sp, flutter, dy);
                break;
        }
        return tmp;
    }

    static void Tail(Layer T, Species sp, int fl, int dy)
    {
        switch (sp)
        {
            case Species.Fox:
                T.Poly(Tone.FurM, (11, 24 + dy), (8, 21 - fl + dy), (4, 20 - fl + dy), (2, 23 + dy), (4, 27 + dy), (9, 27 + dy), (11, 26 + dy));
                T.Poly(Tone.FurL, (4, 20 - fl + dy), (2, 23 + dy), (3, 26 + dy), (5, 25 + dy), (5, 22 + dy));
                T.HLine(7, 10, 26 + dy, Tone.FurD);
                break;
            case Species.Cat:
                T.Line(11, 26 + dy, 7, 27 + dy, Tone.FurM); T.Line(7, 27 + dy, 4, 24 + dy, Tone.FurM); T.Line(4, 24 + dy, 4, 19 - fl + dy, Tone.FurM);
                T.Line(10, 26 + dy, 7, 26 + dy, Tone.FurD); T.Line(5, 25 + dy, 5, 20 - fl + dy, Tone.FurD);
                break;
            case Species.Mouse:
                T.Line(11, 26 + dy, 6, 27 + dy, Tone.Pink); T.Line(6, 27 + dy, 3, 25 + dy, Tone.Pink); T.Line(3, 25 + dy, 3, 21 - fl + dy, Tone.Pink);
                break;
            case Species.Rabbit:
                T.Rect(8, 24 + dy, 3, 3, Tone.FurL); T.Set(8, 26 + dy, Tone.FurM);
                break;
            case Species.Duck:
                T.Line(11, 25 + dy, 7, 23 - fl + dy, Tone.FurM); T.Line(11, 26 + dy, 8, 25 + dy, Tone.FurD);
                break;
            case Species.Frog:
            case Species.Turtle:
                T.Line(11, 26 + dy, 8, 27 + dy, Tone.FurD);
                break;
        }
    }
}
