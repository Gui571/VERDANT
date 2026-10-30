using Godot;
namespace Verdant;

public sealed class Raster
{
    public readonly int Width, Height;
    public readonly byte[] Pixels;
    public int Clipped { get; private set; }
    public Raster(int w, int h) { Width = w; Height = h; Pixels = new byte[w * h * 4]; }
    public void Clear() { Array.Clear(Pixels); Clipped = 0; }
    public void Pixel(int x, int y, Color c) { if (x < 0 || y < 0 || x >= Width || y >= Height) { Clipped++; return; } int i = (y * Width + x) * 4; Pixels[i] = (byte)(Math.Clamp(c.R, 0, 1) * 255); Pixels[i + 1] = (byte)(Math.Clamp(c.G, 0, 1) * 255); Pixels[i + 2] = (byte)(Math.Clamp(c.B, 0, 1) * 255); Pixels[i + 3] = (byte)(Math.Clamp(c.A, 0, 1) * 255); }
    public void Rect(int x, int y, int w, int h, Color c) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Pixel(i, j, c); }
    public void Ellipse(float x, float y, float rx, float ry, Color c, Color? edge = null)
    {
        for (int j = (int)(y - ry) - 1; j <= y + ry + 1; j++) for (int i = (int)(x - rx) - 1; i <= x + rx + 1; i++)
            {
                float u = (i - x) / rx, v = (j - y) / ry, d = u * u + v * v; if (d > 1) continue;
                var col = edge.HasValue && d > .83f ? edge.Value : c;
                Pixel(i, j, col);
            }
    }
    public void Line(Vector2 a, Vector2 b, float width, Color c) { var delta = b - a; float len = delta.LengthSquared(); for (int y = (int)MathF.Floor(Math.Min(a.Y, b.Y) - width); y <= Math.Max(a.Y, b.Y) + width; y++) for (int x = (int)MathF.Floor(Math.Min(a.X, b.X) - width); x <= Math.Max(a.X, b.X) + width; x++) { float u = len == 0 ? 0 : Math.Clamp(((x - a.X) * delta.X + (y - a.Y) * delta.Y) / len, 0, 1); float dx = x - a.X - u * delta.X, dy = y - a.Y - u * delta.Y; if (dx * dx + dy * dy <= width * width) Pixel(x, y, c); } }
    public void Polygon(Vector2[] points, Color color) { float min = points.Min(p => p.Y), max = points.Max(p => p.Y); var cuts = new List<float>(); for (int y = (int)min; y <= max; y++) { cuts.Clear(); for (int i = 0, j = points.Length - 1; i < points.Length; j = i++) { var a = points[i]; var b = points[j]; if ((a.Y > y) != (b.Y > y)) cuts.Add(a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y)); } cuts.Sort(); for (int i = 0; i + 1 < cuts.Count; i += 2) for (int x = (int)Math.Ceiling(cuts[i]); x <= cuts[i + 1]; x++) Pixel(x, y, color); } }
    public Image Image() => Godot.Image.CreateFromData(Width, Height, false, Godot.Image.Format.Rgba8, Pixels);
}
public enum MotionState { Idle, Walk, Run, Action, Hit, Sleep }
public sealed class Pose
{
    public double Time { get; private set; }
    public MotionState State { get; private set; } = MotionState.Idle;
    public float Activity { get; private set; }
    public int Direction { get; set; }
    public float Speed { get; set; } = 1;
    public void SetState(MotionState state) => State = state;
    public void Tick(double dt) { Time += dt; float target = State == MotionState.Run ? 1.7f : State == MotionState.Walk ? 1 : 0; Activity = Mathf.Lerp(Activity, target, (float)(1 - Math.Exp(-dt * 10))); }
    public static Pose At(double time, MotionState state, int direction = 0, float speed = 1) { var p = new Pose { Time = time, State = state, Direction = direction, Speed = speed }; p.Activity = state == MotionState.Run ? 1.7f : state == MotionState.Walk ? 1 : 0; return p; }
}
public sealed class CreatureRenderer
{
    public const int Width = 192, Height = 160;
    public Raster Buffer { get; } = new(Width, Height);
    readonly Genome g; readonly BodyPlan plan;
    readonly Color ink = new("182c31"), light, basis, shade, deep, accent;
    readonly Vector2[] spots;
    float facing, depth, compress, bodyY, t, activity;
    public CreatureRenderer(Genome genome) { genome.Validate(); g = genome; plan = Plans.Get(g.Family); basis = Color.FromHsv(g.Hue, .53f, .76f); light = Color.FromHsv((g.Hue + .035f) % 1, .34f, .97f); shade = Color.FromHsv((g.Hue + .97f) % 1, .62f, .48f); deep = Color.FromHsv((g.Hue + .94f) % 1, .56f, .3f); accent = new("f4cd7c"); var r = new RandomStream(g.Seed, 2); spots = Enumerable.Range(0, 18).Select(_ => new Vector2(r.Range(-.75f, .65f), r.Range(-.65f, .45f))).ToArray(); }
    Vector2 P(float x, float y, float z = 0) => new(96 + x * facing * compress + z * depth, bodyY + y + z * .32f);
    void Shape(float x, float y, float rx, float ry, float z = 0, bool far = false) { var p = P(x, y, z); Buffer.Ellipse(p.X, p.Y, rx * compress, ry, far ? shade : basis, ink); Buffer.Ellipse(p.X - 1, p.Y - ry * .22f, Math.Max(1, rx * compress - 2), Math.Max(1, ry * .62f), far ? basis : light); Buffer.Ellipse(p.X + 1, p.Y + ry * .2f, Math.Max(1, rx * compress - 2), Math.Max(1, ry * .64f), far ? shade : basis); }
    void Segment(Vector2 a, Vector2 b, float w, bool far = false) { Buffer.Line(a, b, w + 1, ink); Buffer.Line(a, b, w, far ? shade : basis); Buffer.Line(a + new Vector2(0, -1), b + new Vector2(0, -1), Math.Max(.5f, w * .4f), far ? basis : light); }
    void Poly((float x, float y)[] pts, Color c, float z = 0) { var ps = pts.Select(p => P(p.x, p.y, z)).ToArray(); Buffer.Polygon(ps, ink); for (int i = 0; i < ps.Length; i++) Buffer.Line(ps[i], ps[(i + 1) % ps.Length], 1, ink); var center = ps.Aggregate(Vector2.Zero, (s, p) => s + p) / ps.Length; Buffer.Polygon(ps.Select(p => p.Lerp(center, .13f)).ToArray(), c); }
    void Tail(float rootX, float rootY, float length, float width, float phase = 0)
    {
        var points = new Vector2[13]; for (int i = 0; i <= 12; i++) { float f = i / 12f; points[i] = P(rootX - length * f, rootY - MathF.Sin(f * 2.6f + t * 2 + phase) * f * 8 + f * f * 9); }
        for (int layer = 0; layer < 3; layer++) for (int i = 1; i <= 12; i++) { float w = Math.Max(.6f, width * (1 - i / 12f)); var offset = layer == 2 ? new Vector2(0, -w * .4f) : Vector2.Zero; Buffer.Line(points[i - 1] + offset, points[i] + offset, layer == 0 ? w + 1 : layer == 1 ? w : Math.Max(.4f, w * .3f), layer == 0 ? ink : layer == 1 ? basis : light); }
    }
    void Leg(float hipX, float hipY, float z, float phase, float reach, bool far)
    {
        float cycle = (t * g.Tempo * 1.5f * Math.Max(.4f, activity) + phase) % 1; float stride = 11 * activity; float footX = cycle < .62f ? Mathf.Lerp(stride, -stride, cycle / .62f) : Mathf.Lerp(-stride, stride, (cycle - .62f) / .38f); float lift = cycle < .62f ? 0 : MathF.Sin((cycle - .62f) / .38f * MathF.PI) * 9 * activity;
        var hip = P(hipX, hipY, z); var foot = P(hipX + footX, hipY + reach - lift, z); float upper = reach * .61f, lower = reach * .61f; var d = foot - hip; float dist = Math.Clamp(d.Length(), 1, upper + lower - .01f); var axis = d.Normalized(); float along = (upper * upper - lower * lower + dist * dist) / (2 * dist); float bend = MathF.Sqrt(Math.Max(0, upper * upper - along * along)); var knee = hip + axis * along + new Vector2(-axis.Y, axis.X) * bend * (plan.Upright ? -1 : 1);
        Segment(hip, knee, plan.Id == "arthropod" ? 1.5f : 3.7f * g.Mass, far); Segment(knee, foot, plan.Id == "arthropod" ? 1 : 2.5f * g.Mass, far); Buffer.Ellipse(foot.X + facing * 2, foot.Y, 4, 2, far ? shade : accent, ink);
    }
    public Raster Render(Pose pose, bool rig = false)
    {
        Buffer.Clear(); t = (float)pose.Time * pose.Speed; activity = pose.Activity; facing = pose.Direction == 1 ? -1 : 1; depth = pose.Direction == 2 ? -.9f : pose.Direction == 3 ? .9f : .4f; compress = pose.Direction >= 2 ? .67f : 1;
        float bob = MathF.Sin(t * 3) * .7f + MathF.Abs(MathF.Sin(t * 9 * g.Tempo)) * activity; bodyY = 88 + bob;
        if (pose.State == MotionState.Sleep) bodyY += 8;
        if (pose.State == MotionState.Hit) bodyY += MathF.Sin(t * 35) * 2;
        float rx = 22 * g.Length, ry = 14 * g.Mass, reach = 17 * g.Legs;
        if (plan.Upright) { rx = 13 * g.Mass; ry = 22 * g.Length; bodyY -= 8; reach = 22 * g.Legs; }
        if (plan.Wings || plan.Fins) bodyY -= 10 + MathF.Sin(t * 4) * 4;
        if (plan.LongBody) { Tail(23, 0, Math.Min(83 * g.Length, 108), 9 * g.Mass); rx = 11 * g.Head; ry = 9 * g.Head; }
        else if (plan.Soft) { for (int i = 0; i < 6; i++) { float a = i * MathF.PI / 3; Tail(MathF.Cos(a) * 17, 10, MathF.Cos(a) * 19, 5, i); } rx = 24 * g.Mass; ry = (22 + MathF.Sin(t * 4) * 2) * g.Length; }
        else if (!plan.Upright) Tail(-rx + 2, 2, 29 * g.Tail, 5 * g.Mass);
        if (plan.Wings) { float flap = MathF.Sin(t * 8) * 19; for (int s = -1; s <= 1; s += 2) { float z = s * 12; Poly(new[] { (-5f, -7f), (-27f, -30f - flap), (-12f, -43f - flap), (19f, -19f) }, shade, z); for (int j = 0; j < 5; j++) Poly(new[] { (-23f + j * 7, -27f - flap), (-28f + j * 7, -41f - flap), (0f + j * 4, -13f) }, j % 2 == 0 ? light : basis, z); } }
        if (plan.Fins) { Poly(new[] { (-15f, -8f), (-4f, -30f), (13f, -7f) }, light); Poly(new[] { (-rx - 15, 0f), (-rx - 33, -19f), (-rx - 30, 20f) }, accent); }
        for (int i = 0; i < plan.Legs / 2; i++) Leg((plan.Upright ? -rx * .4f : -rx * .65f) + i * (rx * 1.3f / Math.Max(1, plan.Legs / 2 - 1)), ry * .5f, -8, i * .5f + .5f, reach, true);
        Shape(plan.LongBody ? 25 : 0, 0, rx, ry);
        if (g.Family == "dragon") for (int i = 0; i < 5; i++) { float x = -rx + 6 + i * rx * .37f; Poly(new[] { (x - 5, -ry + 3), (x, -ry - 6 - g.Horns * 8), (x + 5, -ry + 3) }, i % 2 == 0 ? accent : shade); }
        if (g.Family == "arthropod") { for (int k = -1; k <= 1; k++) Shape(k * rx * .65f, -1, rx * .45f, ry * .9f, 0, k < 0); for (int side = -1; side <= 1; side += 2) { Segment(P(rx, 3, side * 7), P(rx + 14, 11, side * 15), 3); Poly(new[] { (rx + 10, 9f), (rx + 23, 3f), (rx + 19, 15f), (rx + 27, 19f), (rx + 13, 19f) }, accent, side * 12); } }
        foreach (var sp in spots.Take((int)(g.Pattern * 18))) { var p = P(sp.X * rx, sp.Y * ry); Buffer.Ellipse(p.X, p.Y, 2 + g.Pattern * 2, 1.5f, shade); Buffer.Pixel((int)p.X, (int)p.Y - 1, light); }
        for (int i = 0; i < plan.Legs / 2; i++) Leg((plan.Upright ? rx * .4f : -rx * .65f) + i * (rx * 1.3f / Math.Max(1, plan.Legs / 2 - 1)), ry * .5f, 8, i * .5f, reach, false);
        if (plan.Upright) { Segment(P(-rx + 2, -8), P(-rx - 8, 17 + MathF.Sin(t * 4) * activity * 5), 4); Segment(P(rx - 2, -8), P(rx + 8, 17 - MathF.Sin(t * 4) * activity * 5), 4); }
        float hx = plan.Upright ? 3 : plan.LongBody ? 27 : rx * .8f, hy = plan.Upright ? -ry - 7 : -ry * .55f;
        if (plan.Soft) { hx = 8; hy = -3; }
        if (pose.State == MotionState.Action) hx += MathF.Max(0, MathF.Sin(t * 6)) * 8;
        float hr = 10 * g.Head; if (plan.Upright) Segment(P(3, -ry + 4), P(hx, hy + 4), 4 * g.Mass); Shape(hx, hy, hr, hr * .86f);
        if (!plan.Soft) { Shape(hx + hr * .7f, hy + hr * .35f, hr * .8f, hr * .46f); if (!plan.Fins && !plan.LongBody && g.Family != "arthropod") Poly(new[] { (hx - hr * .7f, hy - hr * .55f), (hx - hr * .9f, hy - hr * 1.8f), (hx + 1, hy - hr * .65f) }, light); }
        if (g.Horns > .35 && !plan.Soft && !plan.Fins && g.Family != "arthropod") { Poly(new[] { (hx - 4, hy - hr + 2), (hx - 8, hy - hr - 14 * g.Horns), (hx + 1, hy - hr) }, accent); }
        if (plan.Leaves) { for (int i = -2; i <= 2; i++) Poly(new[] { (hx - 2, hy - hr + 1), (hx + i * 11, hy - hr - 17 + Math.Abs(i) * 3), (hx + i * 7 + 4, hy - hr - 8) }, i % 2 == 0 ? light : shade); }
        var eye = P(hx + hr * .4f, hy - 2, 3); bool blink = pose.State == MotionState.Sleep || ((t + g.Seed % 11) % 4.7f > 4.52f);
        Buffer.Rect((int)eye.X - 2, (int)eye.Y - 2, 5, blink ? 1 : 5, ink); if (!blink) { Buffer.Rect((int)eye.X - 1, (int)eye.Y - 2, 2, 3, new Color("fff3c9")); Buffer.Pixel((int)eye.X + 2, (int)eye.Y + 2, accent); }
        var nose = P(hx + hr * 1.35f, hy + hr * .25f); Buffer.Rect((int)nose.X - 1, (int)nose.Y, 3, 2, ink);
        if (pose.State == MotionState.Action) { var mouth = P(hx + hr * .8f, hy + hr * .7f); Buffer.Ellipse(mouth.X, mouth.Y, 5, 3, ink); Buffer.Rect((int)mouth.X, (int)mouth.Y - 2, 2, 2, accent); }
        if (plan.Fins) Poly(new[] { (0f, 6f), (10f, 21 + MathF.Sin(t * 5) * 5), (16f, 3f) }, accent);
        if (rig) { Buffer.Line(P(-rx, 0), P(rx, 0), .5f, new Color("ff6f91")); Buffer.Ellipse(96, bodyY, 2, 2, new Color("ffffff")); }
        return Buffer;
    }
}
