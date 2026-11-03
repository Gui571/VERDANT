namespace Verdant.Forge;

/// <summary>Armas e itens de mão paramétricos. Cada arma é construída com linhas perfeitas e polígonos pequenos a partir de um
/// ângulo (0° = para cima, 90° = para a frente), então qualquer pose de ataque sai limpa sem desenhar quadro a quadro.</summary>
public static class Gear
{
    // Ângulo de repouso por arma (graus; negativo = inclinada para trás) e se a arma "mira" para o lado de trás quando está na mão de trás.
    public static float RestAngle(Weapon w) => w switch
    {
        Weapon.Sword => 28, Weapon.Axe => 18, Weapon.Staff => 4, Weapon.Hammer => 0, Weapon.Daggers => 58,
        Weapon.Crook => 4, Weapon.Spear => 14, _ => 0
    };

    readonly record struct Frame(float Angle, int Dy);
    // Ataque em 6 quadros: 0 antecipação, 1 preparação, 2 golpe, 3 impacto, 4 continuação, 5 recuperação.
    public static (float angle, bool smear, int hx, int hy) Attack(Weapon w, int f)
    {
        float[] a = w switch
        {
            Weapon.Sword => new float[] { 18, -40, 40, 92, 100, 56 },
            Weapon.Axe => new float[] { 8, -62, 24, 88, 98, 50 },
            Weapon.Hammer => new float[] { 0, -14, 34, 90, 98, 40 },
            Weapon.Daggers => new float[] { 52, -6, 74, 100, 96, 66 },
            Weapon.Spear => new float[] { 14, -16, 70, 88, 84, 40 },
            Weapon.Staff => new float[] { 4, -18, 20, 28, 12, 4 },
            Weapon.Crook => new float[] { 4, -8, 6, 6, 4, 4 },
            _ => new float[] { 0, 0, 0, 0, 0, 0 }
        };
        bool smear = w is Weapon.Sword or Weapon.Axe or Weapon.Hammer or Weapon.Daggers && f is 2 or 3;
        int hy = w switch { Weapon.Staff or Weapon.Crook => f is 2 or 3 ? -2 : f == 1 ? -1 : 0, Weapon.Hammer => f == 1 ? -3 : f == 2 ? -2 : f == 3 ? 1 : 0, _ => 0 };
        int hx = w == Weapon.Spear ? (f == 2 ? 1 : f == 3 ? 3 : f == 4 ? 2 : f == 1 ? -2 : 0) : 0;
        return (a[Math.Clamp(f, 0, 5)], smear, hx, hy);
    }

    // Encurtamento por perspectiva: quando a ponta sairia da célula 32×32, a arma é reduzida ao longo do próprio eixo.
    [ThreadStatic] static float reach = 1;
    static (float x, float y) Pt(float gx, float gy, float deg, float u, float v)
    {
        float r = deg * MathF.PI / 180f, sx = MathF.Sin(r), cy = MathF.Cos(r); u *= reach;
        return (gx + sx * u + cy * v, gy - cy * u + sx * v);
    }
    // Pontos extremos (u = ao longo da arma, v = lateral) que precisam caber na célula; o encurtamento vale para o pior deles.
    static readonly Dictionary<Weapon, (float u, float v)[]> Extremes = new()
    {
        [Weapon.Sword] = new[] { (13f, 0f), (1.6f, 2.1f), (1.6f, -2.1f) },
        [Weapon.Axe] = new[] { (11.8f, 4.6f), (11.8f, -4.6f), (7f, 5.6f), (7f, -5.6f), (-3f, 0f) },
        [Weapon.Hammer] = new[] { (13.6f, 4.6f), (13.6f, -4.6f), (9f, 4.6f), (9f, -4.6f) },
        [Weapon.Staff] = new[] { (21.8f, 0f), (19.4f, 3.2f), (19.4f, -3.2f), (-5f, 0f) },
        [Weapon.Daggers] = new[] { (9.2f, 0f), (1.6f, 1.7f), (1.6f, -1.7f) },
        [Weapon.Crook] = new[] { (21.6f, 0f), (18.2f, 3.6f), (18.2f, -3.6f), (-5f, 0f) },
        [Weapon.Spear] = new[] { (18.8f, 0f), (12.4f, 2.6f), (12.4f, -2.6f), (-5f, 0f) },
    };
    static float Reach(float gx, float gy, float deg, Weapon w)
    {
        float r = deg * MathF.PI / 180f, sx = MathF.Sin(r), cy = MathF.Cos(r), k = 1;
        if (!Extremes.TryGetValue(w, out var pts)) return 1;
        foreach (var (u, v) in pts)
        {
            // posição = g + (sx*u*k + cy*v , -cy*u*k + sx*v); precisa ficar em [1, 30] nos dois eixos.
            float ax = sx * u, bx = cy * v, ay = -cy * u, by = sx * v;
            if (MathF.Abs(ax) > 1e-3f) k = MathF.Min(k, ((ax > 0 ? 30f - gx - bx : gx + bx - 1f)) / MathF.Abs(ax));
            if (MathF.Abs(ay) > 1e-3f) k = MathF.Min(k, ((ay > 0 ? 30f - gy - by : gy + by - 1f)) / MathF.Abs(ay));
        }
        return Math.Clamp(k, .5f, 1f);
    }
    static (int x, int y) Ri((float x, float y) p) => ((int)MathF.Round(p.x), (int)MathF.Round(p.y));
    static void Ln(Layer T, float gx, float gy, float deg, float u0, float u1, float v, Tone t)
    {
        var a = Ri(Pt(gx, gy, deg, u0, v)); var b = Ri(Pt(gx, gy, deg, u1, v)); T.Line(a.x, a.y, b.x, b.y, t);
    }
    static void Pl(Layer T, float gx, float gy, float deg, Tone t, params (float u, float v)[] pts) => T.Poly(t, pts.Select(p => Pt(gx, gy, deg, p.u, p.v)).ToArray());

    /// <summary>Desenha a arma na camada <paramref name="T"/> (sem contorno: quem chama compõe com contorno).</summary>
    public static void DrawWeapon(Layer T, Weapon w, float gx, float gy, float deg, int side, int atkFrame)
    {
        reach = Reach(gx, gy, deg, w);
        switch (w)
        {
            case Weapon.Sword:
                Ln(T, gx, gy, deg, -2, 1, 0, Tone.LeatherM);
                Pl(T, gx, gy, deg, Tone.AccM, (1.6f, -2.1f), (1.6f, 2.1f), (2.6f, 2.1f), (2.6f, -2.1f));
                Pl(T, gx, gy, deg, Tone.MetalM, (2.6f, -1.2f), (2.6f, 1.2f), (10.5f, .9f), (13f, 0f), (10.5f, -.9f));
                Ln(T, gx, gy, deg, 3, 11, -.45f, Tone.MetalL);
                { var tip = Ri(Pt(gx, gy, deg, 12.4f, 0)); T.Set(tip.x, tip.y, Tone.White); }
                break;
            case Weapon.Axe:
                Ln(T, gx, gy, deg, -3, 12, 0, Tone.LeatherM);
                Ln(T, gx, gy, deg, -2, 5, -.7f, Tone.AccM);
                Pl(T, gx, gy, deg, Tone.MetalM, (6f, 0), (5.4f, side * 3f), (7.5f, side * 5.6f), (11.2f, side * 4.6f), (11.8f, side * 1.4f), (10.2f, 0));
                Pl(T, gx, gy, deg, Tone.MetalL, (6.4f, 0), (5.9f, side * 2.8f), (7.7f, side * 4.8f), (9.2f, side * 3.6f), (8.6f, 0));
                { var s2 = Ri(Pt(gx, gy, deg, 7.4f, side * 4.4f)); T.Set(s2.x, s2.y, Tone.White); }
                break;
            case Weapon.Hammer:
                Ln(T, gx, gy, deg, -2, 12, 0, Tone.LeatherM);
                Pl(T, gx, gy, deg, Tone.ClothD, (9f, -4.6f), (9f, 4.6f), (13.6f, 4.6f), (13.6f, -4.6f));
                Pl(T, gx, gy, deg, Tone.ClothM, (9.4f, -4.1f), (9.4f, 3.6f), (13.2f, 3.6f), (13.2f, -4.1f));
                // Top bevel and recessed side give the head three readable planes.
                Pl(T, gx, gy, deg, Tone.MetalL, (12f,-4.1f),(12f,3.6f),(13.2f,3.6f),(13.2f,-4.1f));
                Pl(T, gx, gy, deg, Tone.MetalD, (9.4f,2f),(9.4f,3.6f),(12f,3.6f),(12f,2f));
                Ln(T, gx, gy, deg, 10, 12, -3.6f, Tone.White);
                Ln(T, gx, gy, deg, 10.5f, 12.5f, 0f, Tone.AccM);
                break;
            case Weapon.Staff:
                Ln(T, gx, gy, deg, -5, 17, 0, Tone.LeatherM);
                Pl(T, gx, gy, deg, Tone.AccD, (17.2f, -2.2f), (19.4f, -3.2f), (21.8f, 0), (19.4f, 3.2f), (17.2f, 2.2f));
                Pl(T, gx, gy, deg, Tone.AccM, (17.8f, -1.6f), (19.4f, -2.4f), (21f, 0), (19.4f, 1.0f));
                { var s = Ri(Pt(gx, gy, deg, 19.2f, -1.2f)); T.Set(s.x, s.y, Tone.White); }
                break;
            case Weapon.Daggers:
                Ln(T, gx, gy, deg, -1, 1, 0, Tone.LeatherM);
                Pl(T, gx, gy, deg, Tone.AccM, (1.6f, -1.7f), (1.6f, 1.7f), (2.5f, 1.7f), (2.5f, -1.7f));
                Pl(T, gx, gy, deg, Tone.MetalM, (2.5f, -.9f), (2.5f, .9f), (7.4f, .7f), (9.2f, 0f), (7.4f, -.7f));
                Ln(T, gx, gy, deg, 3, 8, -.35f, Tone.MetalL);
                break;
            case Weapon.Crook:
            {
                Ln(T, gx, gy, deg, -5, 14, 0, Tone.AccM); Ln(T, gx, gy, deg, -5, 14, .9f, Tone.AccD);
                var c = Pt(gx, gy, deg, 18.2f, 0);
                for (int i = 0; i < 24; i++)
                {
                    if (i is 13 or 14 or 15) continue; float a = i * MathF.PI * 2 / 24;
                    foreach (var (r, t) in new[] { (3.4f, Tone.AccD), (2.6f, Tone.AccM) }) { var p = Ri((c.x + MathF.Cos(a) * r, c.y + MathF.Sin(a) * r)); if (r > 3f || T.Get(p.x, p.y) == Tone.None) T.Set(p.x, p.y, t); }
                }
                { var p = Ri((c.x - 2.2f, c.y - 2.2f)); T.Set(p.x, p.y, Tone.White); }
                break;
            }
            case Weapon.Spear:
                Ln(T, gx, gy, deg, -5, 14, 0, Tone.LeatherM);
                Pl(T, gx, gy, deg, Tone.MetalM, (12.4f, -2.6f), (12.4f, 2.6f), (13.4f, 2.6f), (13.4f, -2.6f));
                Pl(T, gx, gy, deg, Tone.MetalM, (13f, -1.7f), (15f, 0f), (18.8f, 0f), (15f, 1.7f), (13f, 1.7f));
                Ln(T, gx, gy, deg, 13.5f, 18f, -.4f, Tone.MetalL);
                break;
        }
    }

    public static void DrawBow(Layer T, int gx, int gy, int draw, int atkFrame)
    {
        var pts = new[] { (1, -8), (3, -7), (4, -4), (4, 0), (4, 4), (3, 7), (1, 8) };
        for (int i = 0; i < pts.Length - 1; i++) T.Line(gx + pts[i].Item1, gy + pts[i].Item2, gx + pts[i + 1].Item1, gy + pts[i + 1].Item2, Tone.LeatherM);
        T.Set(gx + 4, gy - 4, Tone.LeatherD); T.Set(gx + 4, gy + 4, Tone.LeatherD);
        int sx = gx + 1 - draw;                       // a corda recua quando o arco é puxado
        T.Line(gx + 1, gy - 8, sx, gy, Tone.MetalL); T.Line(sx, gy, gx + 1, gy + 8, Tone.MetalL);
        if (atkFrame is >= 0 and <= 2) { T.Line(sx, gy, gx + 7 - draw, gy, Tone.LeatherM); T.Set(gx + 8 - draw, gy, Tone.White); T.Set(gx + 9 - draw, gy, Tone.MetalL); }
        else if (atkFrame == 3) { int e = Math.Min(gx + 10, 29); T.Line(gx + 4, gy, e, gy, Tone.White); T.Set(e + 1, gy, Tone.MetalL); T.Set(e + 2, gy, Tone.MetalM); T.Set(gx + 2, gy - 1, Tone.White); }
        else if (atkFrame < 0) { T.Line(gx - 3, gy, gx + 6, gy, Tone.LeatherM); T.Set(gx + 7, gy, Tone.White); }
    }

    /// <summary>Item da mão secundária. (cx,cy) = centro desejado; <paramref name="dir"/> = lado em que o item sai da mão (-1 esquerda, +1 direita).</summary>
    public static void DrawOffhand(Layer T, Offhand o, int hx, int hy, int dir)
    {
        switch (o)
        {
            case Offhand.RoundShield:
            {
                float cx = hx + dir * 3.0f + .5f, cy = hy + .5f;
                for (int y = (int)cy - 5; y <= (int)cy + 5; y++) for (int x = (int)cx - 5; x <= (int)cx + 5; x++)
                {
                    float d = MathF.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy));
                    if (d > 4.1f) continue;
                    T.Set(x, y, d > 3.1f ? Tone.MetalM : d < 1.5f ? Tone.ClothM : Tone.MetalL);
                }
                T.Set((int)MathF.Round(cx - 1.6f), (int)MathF.Round(cy - 1.8f), Tone.White);
                T.Set((int)MathF.Round(cx), (int)MathF.Round(cy - 1), Tone.ClothL);
                break;
            }
            case Offhand.TowerShield:
            {
                int x0 = dir < 0 ? hx - 5 : hx + 1, y0 = hy - 4;
                T.Rect(x0, y0, 6, 9, Tone.ClothM); T.Rect(x0 + 5, y0, 1, 9, Tone.ClothD); T.Rect(x0, y0 + 8, 6, 1, Tone.ClothD);
                T.HLine(x0, x0 + 5, y0, Tone.ClothL); T.Rect(x0, y0, 1, 8, Tone.ClothL);
                T.Rect(x0 + 2, y0 + 2, 2, 4, Tone.AccM); T.Set(x0 + 2, y0 + 2, Tone.White); T.Rect(x0 + 3, y0 + 5, 1, 1, Tone.AccD);
                T.Set(x0 + 1, y0 + 1, Tone.MetalL); T.Set(x0 + 4, y0 + 7, Tone.MetalL);
                break;
            }
            case Offhand.Tome:
            {
                int x0 = dir < 0 ? hx - 5 : hx + 1, y0 = hy - 1;
                T.Stamp(new[] { "AAAAAA", "AaawwA", "AaawwA", "AAAAAA" }, x0, y0);
                T.Set(x0 + 1, y0 + 1, Tone.White); T.Set(x0 + 3, y0 + 1, Tone.Pink);
                break;
            }
        }
    }
}
