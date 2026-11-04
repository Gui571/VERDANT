using System.Numerics;
using Verdant.Forge;
namespace Verdant.Battle;

public enum FxLayer : byte { Ground, Over, Top }
/// <summary>Um sprite do atlas a desenhar: canto superior esquerdo em pixels lógicos (320×150), quadro e espelhamento.</summary>
public struct FxDraw { public int Sprite, Frame, X, Y; public bool Flip; }

/// <summary>Camada de apresentação dos efeitos. Lê a simulação (eventos e estado) e produz listas de sprites do atlas; nunca escreve nela.
/// Determinístico: mesma batalha → mesmas listas. Rodar <see cref="Advance"/> uma vez por tick da simulação.
/// Orçamento: <see cref="MaxParticles"/> partículas e <see cref="MaxDots"/> pontos de forma por quadro. Ao encher, uma partícula nova
/// substitui a mais antiga de prioridade menor (decoração &lt; impacto &lt; informação); se não houver, a nova é descartada.</summary>
public sealed partial class FxDirector
{
    public const int MaxParticles = 200, MaxDots = 420;
    public const int PrioDecor = 0, PrioImpact = 1, PrioKey = 2;
    readonly FxAtlas atlas; readonly BattleSimulation sim; readonly Func<int, int> sizeOf;
    int lastTick, evIndex;
    readonly int[] pxIds = new int[16];
    public readonly List<FxDraw> Ground = new(), Over = new(), Top = new();
    public int HitStop { get; private set; }
    public int ShakeLeft { get; private set; }
    public int ShakeAmp { get; private set; }
    public int PeakParticles { get; private set; }
    public int DotsDropped { get; private set; }
    public bool Enabled = true;
    Vector2[] lastPos = Array.Empty<Vector2>(); Vector2[] prevPos = Array.Empty<Vector2>();

    struct P
    {
        public int S1, S2, S3, Swap1, Swap2, Age, Life, Delay, Prio, Speed; public float X, Y, VX, VY, GY, Drag; public FxLayer Layer; public bool Loop, Flip, Blink;
    }
    readonly P[] ps = new P[MaxParticles]; int count;

    sealed class Inst { public string Kind = ""; public int Start, Dur, Owner, Seed, Aux; public float X, Y, X2, Y2, R; public FxTheme Theme, Theme2; public AbilitySpec? Ability; public int Value; public bool Large; }
    readonly List<Inst> insts = new();

    public FxDirector(BattleSimulation sim, Func<int, int>? sizeOf = null, FxAtlas? atlas = null)
    {
        this.sim = sim; this.sizeOf = sizeOf ?? (_ => 32); this.atlas = atlas ?? FxAtlas.Shared;
        foreach (var t in Enum.GetValues<FxTheme>()) pxIds[(int)t] = this.atlas.Id("px", t);
        SyncPositions();
    }
    void SyncPositions() { if (lastPos.Length != sim.Fighters.Length) { Array.Resize(ref lastPos, sim.Fighters.Length); Array.Resize(ref prevPos, sim.Fighters.Length); } }

    // ------------------------------------------------------------------------------------------------------------------
    //  Avanço: um tick de apresentação por tick de simulação
    // ------------------------------------------------------------------------------------------------------------------
    public void Advance()
    {
        while (lastTick < sim.Tick)
        {
            lastTick++; SyncPositions();
            if (HitStop > 0 && lastTick > 0) { }
            ShakeLeft = Math.Max(0, ShakeLeft - 1);
            while (evIndex < sim.Events.Count && sim.Events[evIndex].Tick <= lastTick) { OnEvent(sim.Events[evIndex], evIndex); evIndex++; }
            AgeParticles(); OngoingEmitters();
            for (int i = 0; i < sim.Fighters.Length; i++) { prevPos[i] = lastPos[i]; lastPos[i] = sim.Fighters[i].Position; }
            insts.RemoveAll(i => lastTick - i.Start >= i.Dur);
        }
    }
    /// <summary>Pausa visual curta (em ticks) a ser aplicada pela arena logo após um impacto forte; consome o valor.</summary>
    public int ConsumeHitStop() { int h = HitStop; HitStop = 0; return h; }
    /// <summary>Deslocamento de câmera deste instante (determinístico; zero quando desligado).</summary>
    public (int dx, int dy) ShakeOffset(bool enabled = true)
    {
        if (!enabled || ShakeLeft <= 0) return (0, 0); int a = Math.Max(1, ShakeAmp * ShakeLeft / 6); return ((ShakeLeft % 2 == 0 ? 1 : -1) * a, (ShakeLeft % 3 == 0 ? 1 : -1) * (a > 1 ? 1 : 0));
    }
    public int Active => count + insts.Count;

    // ------------------------------------------------------------------------------------------------------------------
    //  Utilitários de desenho e partículas
    // ------------------------------------------------------------------------------------------------------------------
    int Id(string n, FxTheme t) => atlas.IdNear(n, t);
    List<FxDraw> L(FxLayer l) => l == FxLayer.Ground ? Ground : l == FxLayer.Over ? Over : Top;
    int dots;
    void Spr(FxLayer layer, int sprite, int frame, float x, float y, bool flip = false)
    {
        var s = atlas[sprite]; frame = Math.Clamp(frame, 0, s.Frames - 1);
        L(layer).Add(new FxDraw { Sprite = sprite, Frame = frame, X = (int)MathF.Round(x) - (flip ? s.W - 1 - s.PivotX : s.PivotX), Y = (int)MathF.Round(y) - s.PivotY, Flip = flip });
    }
    void Dot(FxLayer layer, FxTheme theme, int tone, float x, float y)
    {
        if (dots >= MaxDots) { DotsDropped++; return; }
        dots++; L(layer).Add(new FxDraw { Sprite = pxIds[(int)theme], Frame = tone, X = (int)MathF.Round(x), Y = (int)MathF.Round(y) });
    }
    void Dot2(FxLayer layer, FxTheme theme, int tone, float x, float y) { Dot(layer, theme, tone, x, y); Dot(layer, theme, tone, x + 1, y); Dot(layer, theme, tone, x, y + 1); Dot(layer, theme, tone, x + 1, y + 1); }

    /// <summary>Contorno de elipse em pixels, com tracejado (on/off) e rotação do padrão.</summary>
    void EllipseDots(FxLayer layer, FxTheme theme, float cx, float cy, float rx, float ry, int toneA, int toneB, int on = 3, int off = 2, int phase = 0, int thick = 1)
    {
        int n = Math.Max(16, (int)(MathF.Tau * MathF.Max(rx, ry) * 1.1f)); int lastX = int.MinValue, lastY = int.MinValue, k = 0;
        for (int i = 0; i < n; i++)
        {
            float a = i * MathF.Tau / n; int x = (int)MathF.Round(cx + MathF.Cos(a) * rx), y = (int)MathF.Round(cy + MathF.Sin(a) * ry);
            if (x == lastX && y == lastY) continue; lastX = x; lastY = y; k++;
            int period = on + off; if (period > on && ((k + phase) % period + period) % period >= on) continue;
            int tone = ((k + phase) / Math.Max(1, period)) % 2 == 0 ? toneA : toneB;
            Dot(layer, theme, tone, x, y); if (thick > 1) { Dot(layer, theme, toneB, x + (MathF.Cos(a) > 0 ? -1 : 1), y); }
        }
    }
    void LineDots(FxLayer layer, FxTheme theme, float x0, float y0, float x1, float y1, int toneA, int toneB, int on = 1, int off = 0, int phase = 0)
    {
        int k = 0; foreach (var (x, y) in Layer.LinePoints((int)MathF.Round(x0), (int)MathF.Round(y0), (int)MathF.Round(x1), (int)MathF.Round(y1)))
        { k++; int period = on + off; if (off > 0 && ((k + phase) % period + period) % period >= on) continue; Dot(layer, theme, ((k + phase) / Math.Max(1, period)) % 2 == 0 ? toneA : toneB, x, y); }
    }

    bool Spawn(string name, FxTheme theme, float x, float y, float vx, float vy, int life, int delay = 0, FxLayer layer = FxLayer.Over, bool loop = false, float gy = 0, float drag = 0, int prio = PrioImpact, bool flip = false, int speed = 3, string? then = null, int swap = 0, string? then2 = null, int swap2 = 0, bool blink = false)
    {
        var p = new P { S1 = Id(name, theme), S2 = then != null ? Id(then, theme) : -1, S3 = then2 != null ? Id(then2, theme) : -1, Swap1 = swap, Swap2 = swap2, X = x, Y = y, VX = vx, VY = vy, GY = gy, Drag = drag, Life = Math.Max(1, life), Delay = delay, Layer = layer, Loop = loop, Prio = prio, Flip = flip, Speed = speed, Blink = blink };
        if (count < MaxParticles) { ps[count++] = p; PeakParticles = Math.Max(PeakParticles, count); return true; }
        int victim = -1, oldest = -1; for (int i = 0; i < count; i++) if (ps[i].Prio < prio && ps[i].Age > oldest) { oldest = ps[i].Age; victim = i; }
        if (victim < 0) return false; ps[victim] = p; return true;
    }
    void AgeParticles()
    {
        for (int i = count - 1; i >= 0; i--)
        {
            ref var p = ref ps[i]; if (p.Delay > 0) { p.Delay--; continue; }
            p.Age++; p.X += p.VX; p.Y += p.VY; p.VY += p.GY; if (p.Drag != 0) { p.VX *= 1 - p.Drag; p.VY *= 1 - p.Drag; }
            if (p.Age >= p.Life) { ps[i] = ps[--count]; }
        }
    }
    Rng RngFor(int ev, int salt = 0) => new((sim.Spec.Seed * 0x9E3779B97F4A7C15UL) ^ ((ulong)ev * 0xBF58476D1CE4E5B9UL) ^ (ulong)salt);

    // ------------------------------------------------------------------------------------------------------------------
    //  Quadro: monta as listas do instante atual (idempotente — pode ser chamada várias vezes por tick)
    // ------------------------------------------------------------------------------------------------------------------
    public void Collect()
    {
        Ground.Clear(); Over.Clear(); Top.Clear(); dots = 0; if (!Enabled) return;
        foreach (var i in insts) DrawInst(i, lastTick - i.Start);
        for (int i = 0; i < count; i++)
        {
            ref var p = ref ps[i]; if (p.Delay > 0) continue; if (p.Blink && p.Life - p.Age < 8 && p.Age % 2 == 0) continue;
            int sprite = p.S1; if (p.S2 >= 0 && p.Age >= p.Swap1) sprite = p.S2; if (p.S3 >= 0 && p.Age >= p.Swap2) sprite = p.S3;
            var s = atlas[sprite]; int frame = p.Loop ? p.Age / Math.Max(1, p.Speed) % s.Frames : Math.Min(s.Frames - 1, p.Age * s.Frames / p.Life);
            Spr(p.Layer, sprite, frame, p.X, p.Y, p.Flip);
        }
        DrawProjectiles(); DrawUnitOverlays();
    }
}
