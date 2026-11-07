namespace Verdant.Forge;

/// <summary>Poses e quadros-chave por movimento. Mantém o contrato do projeto: idle de 32 quadros a 8 fps (loop de 4 s),
/// caminhada/corrida de 8 quadros, ataque de 6 quadros (0,75 s) e dano de 4 quadros (0,5 s).</summary>
public struct ForgePose
{
    public int Dx, Dy, HeadDx, HeadDy, LiftL, LiftR, StepL, StepR, NearDx, NearDy, FarDx, FarDy, Flutter, Draw, Atk = -1;
    public float NearAngle, FarAngle;
    public bool Blink, Flash, Hurt, Run;
    public int FlashStage;
    public ForgePose() { }
}

public sealed class ForgeRenderer
{
    public const int Size = 32;
    public readonly Raster Buffer = new(Size, Size);
    public ForgeSpec Spec { get; }
    public StyleV2.MaterialStyle? Style { get; set; }
    public byte[]? MaterialIds { get; private set; }
    /// <summary>Sombra de contato opaca (mesma tinta do contorno), como nas referências.</summary>
    public bool GroundShadow { get; set; } = true;
    readonly Layer L = new(), T = new();
    readonly int phaseOffset;
    public ForgeRenderer(ForgeSpec spec)
    {
        Spec = spec;
        int h = 17; foreach (char c in spec.Id) h = h * 31 + c; phaseOffset = Math.Abs(h) % 32;
    }

    static readonly int[] StepWalk = { 2, 1, 0, -1, -2, -1, 0, 1 };
    static readonly int[] StepRun = { 3, 2, 0, -2, -3, -2, 0, 2 };
    static readonly int[] BobWalk = { 0, 0, -1, -1, 0, 0, -1, -1 };
    static readonly int[] BobRun = { 0, -1, -2, -1, 0, -1, -2, -1 };

    public ForgePose Compute(double seconds, AdventurerMotion motion)
    {
        var p = new ForgePose();
        seconds = Math.Max(0, seconds);
        bool far = Spec.WeaponFar;
        float rest = Gear.RestAngle(Spec.Weapon);
        p.NearAngle = far ? 0 : rest; p.FarAngle = far ? -rest : 0;
        if (Spec.Weapon == Weapon.Daggers) { p.NearAngle = rest; p.FarAngle = -rest; }
        switch (motion)
        {
            case AdventurerMotion.Idle:
            {
                int f = (int)(seconds * 8) % 32, personal = (f + phaseOffset) % 32, c = personal % 16;
                p.Dy = c >= 6 && c < 10 ? -1 : 0; p.HeadDy = c >= 7 && c < 11 ? -1 : 0;
                p.NearDy = c >= 8 && c < 12 ? -1 : 0; p.FarDy = c >= 9 && c < 13 ? -1 : 0;
                p.Flutter = c >= 10 && c < 14 ? 1 : 0; p.Blink = personal == 29;
                break;
            }
            case AdventurerMotion.Walk:
            case AdventurerMotion.Run:
            {
                bool run = motion == AdventurerMotion.Run; p.Run = run;
                int g = (int)(seconds * (run ? 12 : 8)) % 8, gp = (g + 7) % 8;
                var step = run ? StepRun : StepWalk; var bob = run ? BobRun : BobWalk;
                p.StepR = step[g]; p.StepL = -step[g];
                p.LiftR = run ? (g is 5 or 6 or 7 ? 1 : 0) + (g == 6 ? 1 : 0) : (g is 5 or 6 ? 1 : 0);
                p.LiftL = run ? (g is 1 or 2 or 3 ? 1 : 0) + (g == 2 ? 1 : 0) : (g is 1 or 2 ? 1 : 0);
                p.Dy = bob[g]; p.HeadDy = bob[gp]; p.Dx = 0; p.HeadDx = run ? 1 : 0;
                p.NearDx = p.StepL <= -2 ? -1 : 0; p.FarDx = p.StepL >= 2 ? -1 : 0;   // as mãos só recuam: a frente da célula é reservada às armas
                p.NearDy = run ? -(p.Dy < -1 ? 1 : 0) : 0; p.FarDy = p.NearDy;
                p.Flutter = run ? 2 : (g % 4 >= 2 ? 1 : 0);
                p.Blink = false; p.NearAngle += run ? 6 : 0; if (!far) p.NearAngle += p.NearDx * 4; else p.FarAngle += p.FarDx * 4;
                break;
            }
            case AdventurerMotion.Attack:
            {
                int a = Math.Min(5, (int)(Math.Clamp(seconds, 0, .749) * 8)); p.Atk = a;
                var (ang, smear, hx, hy) = Gear.Attack(Spec.Weapon, a);
                p.Dx = new[] { 0, -1, 1, 2, 1, 0 }[a]; p.HeadDx = 0; p.HeadDy = a == 0 ? 1 : a == 1 ? 0 : a == 3 ? 1 : 0;
                p.Dy = a == 0 ? 0 : 0; p.Flutter = a is 2 or 3 ? 2 : 1;
                if (a is 2 or 3) { p.StepR = 2; p.StepL = -2; }
                if (a == 1) { p.StepR = -1; p.StepL = 1; }
                if (far) { if (a is >= 1 and <= 4) { p.NearAngle = ang; p.NearDx = hx; p.NearDy = hy; } else p.FarAngle = -Gear.RestAngle(Spec.Weapon); } else { p.NearAngle = ang; p.NearDx = hx; p.NearDy = hy; }
                if (Spec.Weapon == Weapon.Daggers) { p.FarAngle = a == 0 ? -rest : ang; p.FarDy = hy; }
                if (Spec.Offhand != Offhand.None) { if (far) { p.FarDy = a is 1 or 2 ? -1 : 0; } else p.FarDy = a is 1 or 2 or 3 ? -1 : 0; }
                p.Draw = Spec.Weapon == Weapon.Bow ? new[] { 0, 2, 3, 0, 0, 0 }[a] : 0;
                if (Spec.Weapon == Weapon.Bow) { p.Dx = new[] { 0, -1, -1, 0, 0, 0 }[a]; p.NearDx = 0; }
                break;
            }
            case AdventurerMotion.Hurt:
            {
                int h = Math.Min(3, (int)(Math.Clamp(seconds, 0, .499) * 8));
                p.Dx = new[] { -2, -2, -1, 0 }[h]; p.Dy = h == 0 ? -1 : 0; p.HeadDy = h == 0 ? -1 : h == 1 ? 0 : 0; p.HeadDx = h < 2 ? -1 : 0;
                p.NearDy = h < 3 ? 1 : 0; p.FarDy = p.NearDy; p.Hurt = h < 3; p.Flash = h < 2; p.FlashStage = h; p.Flutter = 2;
                p.NearAngle -= h < 3 ? 14 : 0; p.FarAngle += h < 3 ? 14 : 0;
                break;
            }
        }
        return p;
    }

    public Raster Render(double seconds, AdventurerMotion motion = AdventurerMotion.Idle, bool back = false)
    {
        if(motion is AdventurerMotion.Special or AdventurerMotion.Cast){int frame=Math.Clamp((int)(seconds*8),0,7);Render(Math.Min(5,Math.Max(0,frame-1))/8.0,AdventurerMotion.Attack,back);ForgeEffects.Special(Buffer,Spec.Palette,frame,motion==AdventurerMotion.Cast);return Buffer;}
        var p = Compute(seconds, motion);
        L.Clear(); Buffer.Clear();
        var s = Spec; int bx = p.Dx, by = p.Dy;
        bool far = s.WeaponFar;
        int hx = 9 + bx + p.HeadDx, hy = 10 + p.HeadDy;

        // 1 — equipamento de costas (cauda, casco de tartaruga, aljava, capa)
        if (s.Back != BackGear.None && !(back && s.Back == BackGear.Cape))
            L.Composite(Parts.Back(s.Back, s.Species, p.Flutter, by, s.Scarf && s.Back == BackGear.Cape), true, bx, 0);

        // 2 — item da mão de trás (esquerda)
        int fxN = 21 + bx + p.NearDx, fyN = 24 + by + p.NearDy, fxF = 9 + bx + p.FarDx, fyF = 24 + by + p.FarDy;
        void Hold(bool farHand)
        {
            T.Clear();
            int gxi = farHand ? fxF : fxN, gyi = farHand ? fyF : fyN; float gx = gxi + 1f, gy = gyi + 1f;
            bool weaponHere = farHand == (far && !(p.Atk is >= 1 and <= 4)) && s.Weapon != Weapon.None;
            if (weaponHere)
            {
                if (s.Weapon == Weapon.Bow) Gear.DrawBow(T, gxi + 1, gyi + 1, p.Draw, p.Atk);
                else Gear.DrawWeapon(T, s.Weapon, gx, gy, farHand ? p.FarAngle : p.NearAngle, farHand ? -1 : 1, p.Atk);
            }
            else if (s.Weapon == Weapon.Daggers) Gear.DrawWeapon(T, Weapon.Daggers, gx, gy, farHand ? p.FarAngle : p.NearAngle, farHand ? -1 : 1, p.Atk);
            else if (s.Offhand != Offhand.None) Gear.DrawOffhand(T, s.Offhand, gxi, gyi, farHand ? -1 : 1);
            if (T.Any()) L.Composite(T, true);
        }
        Hold(true);

        // 3 — pernas
        Parts.Leg(L, 12 + bx * 0 + p.StepL, p.LiftL, 0);
        Parts.Leg(L, 17 + p.StepR, p.LiftR, 1);

        // 4 — tronco, mangas e capa vista de costas
        Parts.Torso(L, s.Outfit, s.Scarf, 20, bx, by, false);
        L.Set(10 + bx, 23 + by, Tone.ClothM); L.Set(21 + bx, 23 + by, Tone.ClothM);
        if (back && s.Back == BackGear.Cape)
        {
            for (int y = 21; y <= 27; y++) for (int x = 10; x <= 21; x++) L.Set(x + bx, y + by, x >= 19 || y >= 26 ? Tone.ClothD : x <= 11 ? Tone.ClothL : Tone.ClothM);
            if (s.Scarf) L.HLine(11 + bx, 20 + bx, 21 + by, Tone.AccM);
        }

        // 5 — cabeça, rosto, barba, chapéu
        Parts.Head(L, hx, hy, s.Species, back);
        if (s.Species == Species.Rabbit && p.Flutter > 0) { /* orelhas balançam */ }
        Parts.Face(L, hx, hy, s.Species, p.Blink, p.Hurt, back);
        if (s.Beard && !back) Parts.Beard(L, hx, hy);
        Parts.Hat(L, hx, hy, s.Headwear, p.Flutter, s.Species, back);

        // 6 — mãos e item da frente (direita)
        Hold(false);
        Fist(fxF, fyF); Fist(fxN, fyN);

        // 7 — rastro do golpe
        if (p.Atk is 2 or 3 && s.Weapon is Weapon.Sword or Weapon.Axe or Weapon.Hammer or Weapon.Daggers) Smear(p, far);

        if (p.Atk is 2 or 3 && s.Weapon is Weapon.Staff or Weapon.Crook) Sparks(p);

        // 8 — contorno externo contínuo (1 px) e sombra de contato
        ForgeFinish.Outline(L);
        if (GroundShadow) ForgeFinish.Shadow(L,16,29,7);

        if (Style != null) { MaterialIds ??= new byte[Size*Size]; StyleV2.MaterialLighting.Paint(L, Buffer, Style, p.Flash, MaterialIds); for(int i=0;i<L.Clipped;i++)Buffer.Pixel(-1,-1,default); return Buffer; }
        var pal = s.Palette;
        for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
        {
            var t = L.Px[y * Size + x]; if (t == Tone.None) continue;
            if (p.Flash && t != Tone.Ink) t = p.FlashStage == 0 ? Tone.White : Tone.MetalL;
            Buffer.Pixel(x, y, pal.Resolve(t));
        }
        for (int i = 0; i < L.Clipped; i++) Buffer.Pixel(-1, -1, default);
        return Buffer;
    }

    // Faíscas de magia: cruzes de 1 px (centro + 4 pontas) ao redor da ponta do cajado/báculo.
    void Sparks(ForgePose p)
    {
        float gx = 21 + p.Dx + p.NearDx + 1f, gy = 24 + p.Dy + p.NearDy + 1f; float rad = p.NearAngle * MathF.PI / 180f;
        float tx = gx + MathF.Sin(rad) * 18f * .9f, ty = gy - MathF.Cos(rad) * 18f * .9f;
        var pts = p.Atk == 2 ? new[] { (-4, -3), (4, -1), (-1, 4) } : new[] { (-5, 0), (3, -4), (5, 3), (-2, -5) };
        int i = 0;
        foreach (var (ox, oy) in pts)
        {
            int x = (int)MathF.Round(tx) + ox, y = (int)MathF.Round(ty) + oy; if (x < 2 || y < 2 || x > 29 || y > 29) continue;
            bool big = i++ % 2 == 0; var tone = Spec.Weapon == Weapon.Crook ? Tone.Pink : Tone.White;
            if (L.Get(x, y) != Tone.None) continue; L.Set(x, y, Tone.White);
            if (big) foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) if (L.Get(x + dx, y + dy) == Tone.None && x + dx > 0 && y + dy > 0 && x + dx < 31 && y + dy < 31) L.Set(x + dx, y + dy, tone);
        }
    }

    void Fist(int x, int y) => L.Stamp(new[] { "ff", "fF" }, x, y, true);

    void Smear(ForgePose p, bool far)
    {
        float ang = far ? p.FarAngle : p.NearAngle; float gx = (far ? 9 + p.Dx + p.FarDx : 21 + p.Dx + p.NearDx) + 1f, gy = (far ? 24 + p.Dy + p.FarDy : 24 + p.Dy + p.NearDy) + 1f;
        float r0 = Spec.Weapon switch { Weapon.Hammer => 10f, Weapon.Axe => 8f, Weapon.Daggers => 6f, _ => 9f }, r1 = Spec.Weapon switch { Weapon.Hammer => 14f, Weapon.Axe => 11f, Weapon.Daggers => 9f, _ => 13f };
        float from = ang - (p.Atk == 2 ? 60 : 55), to = ang - 8;
        for (float a = from; a <= to; a += 5)
            for (float r = r0; r <= r1; r += 1f)
            {
                float rad = a * MathF.PI / 180f; int x = (int)MathF.Round(gx + MathF.Sin(rad) * r), y = (int)MathF.Round(gy - MathF.Cos(rad) * r);
                if (x >= 1 && y >= 1 && x < Size - 1 && y < Size - 1 && L.Get(x, y) == Tone.None && (r == r0 || r == r1 || a > to - 12)) L.Set(x, y, a > to - 14 ? Tone.White : Tone.MetalL);
            }
    }

    void OutlinePass()
    {
        var filled = new List<(int, int)>();
        for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) if (L.Px[y * Size + x] != Tone.None) filled.Add((x, y));
        L.Halo(filled);
    }
}
