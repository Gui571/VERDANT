using System.IO.Compression;
namespace PixelPreview;

/// <summary>Gravador de PNG RGBA 8-bit sem dependências.</summary>
public static class Png
{
    static readonly uint[] Table = Enumerable.Range(0, 256).Select(n => { uint c = (uint)n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; return c; }).ToArray();
    static uint Crc(byte[] type, byte[] data) { uint c = 0xFFFFFFFF; foreach (var b in type) c = Table[(c ^ b) & 255] ^ (c >> 8); foreach (var b in data) c = Table[(c ^ b) & 255] ^ (c >> 8); return c ^ 0xFFFFFFFF; }
    static void Chunk(Stream s, string name, byte[] data)
    {
        var type = System.Text.Encoding.ASCII.GetBytes(name); var len = BitConverter.GetBytes((uint)data.Length); var crc = BitConverter.GetBytes(Crc(type, data));
        if (BitConverter.IsLittleEndian) { Array.Reverse(len); Array.Reverse(crc); }
        s.Write(len); s.Write(type); s.Write(data); s.Write(crc);
    }
    public static void Save(string path, int w, int h, byte[] rgba)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var fs = File.Create(path); fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13]; void BE(int o, int v) { ihdr[o] = (byte)(v >> 24); ihdr[o + 1] = (byte)(v >> 16); ihdr[o + 2] = (byte)(v >> 8); ihdr[o + 3] = (byte)v; }
        BE(0, w); BE(4, h); ihdr[8] = 8; ihdr[9] = 6; Chunk(fs, "IHDR", ihdr);
        using var raw = new MemoryStream();
        using (var z = new ZLibStream(raw, CompressionLevel.Optimal, true)) { for (int y = 0; y < h; y++) { z.WriteByte(0); z.Write(rgba, y * w * 4, w * 4); } }
        Chunk(fs, "IDAT", raw.ToArray()); Chunk(fs, "IEND", Array.Empty<byte>());
    }
}

/// <summary>Tela RGBA simples para montar folhas de contato com ampliação inteira (vizinho mais próximo).</summary>
public sealed class Canvas
{
    public readonly int W, H; public readonly byte[] Data;
    public Canvas(int w, int h, uint bg) { W = w; H = h; Data = new byte[w * h * 4]; Fill(bg); }
    public void Fill(uint rgb) { for (int i = 0; i < W * H; i++) { Data[i * 4] = (byte)(rgb >> 16); Data[i * 4 + 1] = (byte)(rgb >> 8); Data[i * 4 + 2] = (byte)rgb; Data[i * 4 + 3] = 255; } }
    public void Rect(int x, int y, int w, int h, uint rgb) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) if (i >= 0 && j >= 0 && i < W && j < H) { int o = (j * W + i) * 4; Data[o] = (byte)(rgb >> 16); Data[o + 1] = (byte)(rgb >> 8); Data[o + 2] = (byte)rgb; Data[o + 3] = 255; } }
    public void Blit(Verdant.Raster r, int ox, int oy, int zoom, bool flip = false)
    {
        for (int y = 0; y < r.Height; y++) for (int x = 0; x < r.Width; x++)
        {
            int sx = flip ? r.Width - 1 - x : x; int i = (y * r.Width + sx) * 4; if (r.Pixels[i + 3] == 0) continue;
            for (int j = 0; j < zoom; j++) for (int k = 0; k < zoom; k++) { int px = ox + x * zoom + k, py = oy + y * zoom + j; if (px < 0 || py < 0 || px >= W || py >= H) continue; int o = (py * W + px) * 4; Data[o] = r.Pixels[i]; Data[o + 1] = r.Pixels[i + 1]; Data[o + 2] = r.Pixels[i + 2]; Data[o + 3] = 255; }
        }
    }
    public void Save(string path) => Png.Save(path, W, H, Data);
}
