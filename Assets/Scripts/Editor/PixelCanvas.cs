using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>Basit piksel tuvali. (0,0) sol-alt köşedir (Unity texture düzeni).</summary>
    public class PixelCanvas
    {
        public readonly int W, H;
        readonly Color32[] px;

        public PixelCanvas(int w, int h) { W = w; H = h; px = new Color32[w * h]; }

        public static Color32 C(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        public void Set(int x, int y, Color32 c) { if (In(x, y)) px[y * W + x] = c; }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Set(i, j, c);
        }

        public void HLine(int x, int y, int len, Color32 c) { for (int i = 0; i < len; i++) Set(x + i, y, c); }
        public void VLine(int x, int y, int len, Color32 c) { for (int j = 0; j < len; j++) Set(x, y + j, c); }

        public void Border(int x, int y, int w, int h, Color32 c)
        {
            HLine(x, y, w, c); HLine(x, y + h - 1, w, c);
            VLine(x, y, h, c); VLine(x + w - 1, y, h, c);
        }

        public Color32 Get(int x, int y) => In(x, y) ? px[y * W + x] : default;

        /// <summary>Ham piksel dizisi (satırlar alttan üste).</summary>
        public Color32[] Pixels => px;

        /// <summary>Çokgeni doldurur (köşeler piksel koordinatında, sol-alt orijin; piksel merkezleri test edilir).</summary>
        public void FillPolygon(Vector2[] points, Color32 c)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            for (int y = (int)minY; y <= Mathf.CeilToInt(maxY); y++)
                for (int x = (int)minX; x <= Mathf.CeilToInt(maxX); x++)
                    if (Inside(points, x + 0.5f, y + 0.5f)) Set(x, y, c);
        }

        static bool Inside(Vector2[] poly, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        /// <summary>İki nokta arasında 1 piksel kalınlığında çizgi (Bresenham).</summary>
        public void Line(Vector2 a, Vector2 b, Color32 c)
        {
            int x0 = Mathf.RoundToInt(a.x), y0 = Mathf.RoundToInt(a.y), x1 = Mathf.RoundToInt(b.x), y1 = Mathf.RoundToInt(b.y);
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void Ellipse(int cx, int cy, int rx, int ry, Color32 c)
        {
            for (int y = -ry; y <= ry; y++)
                for (int x = -rx; x <= rx; x++)
                    if ((x * x) / (float)(rx * rx) + (y * y) / (float)(ry * ry) <= 1.05f)
                        Set(cx + x, cy + y, c);
        }

        public void Clear(int x, int y) => Set(x, y, default);

        /// <summary>Şeklin dışına 1 px kontur çizer (opak piksele komşu şeffaf pikseller boyanır).</summary>
        public void OutlineOuter(Color32 ink)
        {
            var copy = (Color32[])px.Clone();
            bool Solid(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && copy[y * W + x].a > 127;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (!Solid(x, y) && (Solid(x - 1, y) || Solid(x + 1, y) || Solid(x, y - 1) || Solid(x, y + 1)))
                        px[y * W + x] = ink;
        }

        /// <summary>Şeklin kenarındaki opak pikselleri kontur rengine boyar (boyut değişmez).</summary>
        public void OutlineInner(Color32 ink)
        {
            var copy = (Color32[])px.Clone();
            bool Solid(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && copy[y * W + x].a > 127;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (Solid(x, y) && (!Solid(x - 1, y) || !Solid(x + 1, y) || !Solid(x, y - 1) || !Solid(x, y + 1)))
                        px[y * W + x] = ink;
        }

        /// <summary>
        /// Alan ağırlıklı küçültme (oran tam sayı olmak zorunda değil). Sonuç renkleri kaynağın kendi
        /// paletine oturtulur ve yarıdan az dolu pikseller şeffaf olur; böylece küçük hali de keskin kalır.
        /// </summary>
        public PixelCanvas Downscaled(int width, int height, bool snapToOwnPalette = true)
        {
            var palette = new List<Color32>();
            var seen = new HashSet<int>();
            if (snapToOwnPalette)
                foreach (var c in px)
                    if (c.a > 127 && seen.Add(c.r << 16 | c.g << 8 | c.b)) palette.Add(c);

            var result = new PixelCanvas(width, height);
            float sx = W / (float)width, sy = H / (float)height;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float x0 = x * sx, x1 = x0 + sx, y0 = y * sy, y1 = y0 + sy;
                    float r = 0, g = 0, b = 0, a = 0, area = 0;
                    for (int j = (int)y0; j < Mathf.CeilToInt(y1); j++)
                        for (int i = (int)x0; i < Mathf.CeilToInt(x1); i++)
                        {
                            float w = (Mathf.Min(i + 1, x1) - Mathf.Max(i, x0)) * (Mathf.Min(j + 1, y1) - Mathf.Max(j, y0));
                            var c = Get(i, j);
                            float alpha = c.a / 255f * w;
                            r += c.r * alpha; g += c.g * alpha; b += c.b * alpha; a += alpha; area += w;
                        }
                    if (a / area < 0.5f) continue;
                    var avg = new Color32((byte)(r / a), (byte)(g / a), (byte)(b / a), 255);
                    result.Set(x, y, snapToOwnPalette ? Nearest(palette, avg) : avg);
                }
            return result;
        }

        static Color32 Nearest(List<Color32> palette, Color32 c)
        {
            Color32 best = c;
            int bestD = int.MaxValue;
            foreach (var p in palette)
            {
                int dr = p.r - c.r, dg = p.g - c.g, db = p.b - c.b;
                int d = dr * dr * 3 + dg * dg * 4 + db * db * 2;
                if (d < bestD) { bestD = d; best = p; }
            }
            return new Color32(best.r, best.g, best.b, 255);
        }

        public void Diamond(int cx, int cy, int r, Color32 c)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= r) Set(cx + x, cy + y, c);
        }

        /// <summary>Opak pikseli c rengine doğru t oranında karıştırır (şeffaf piksellere dokunmaz).</summary>
        public void Blend(int x, int y, Color32 c, float t)
        {
            if (!In(x, y)) return;
            var o = px[y * W + x];
            if (o.a == 0) return;
            var r = Color32.Lerp(o, c, t);
            r.a = o.a;
            px[y * W + x] = r;
        }

        public void BlendRect(int x, int y, int w, int h, Color32 c, float t)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Blend(i, j, c, t);
        }

        /// <summary>(cx,cy) merkezli yumuşak ışık lekesi.</summary>
        public void Glow(int cx, int cy, int radius, Color32 c, float strength)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radius;
                    if (d < 1f) Blend(x, y, c, (1f - d) * (1f - d) * strength);
                }
        }

        static readonly float[] Bayer4 =
        {
            0f / 16, 8f / 16, 2f / 16, 10f / 16,
            12f / 16, 4f / 16, 14f / 16, 6f / 16,
            3f / 16, 11f / 16, 1f / 16, 9f / 16,
            15f / 16, 7f / 16, 13f / 16, 5f / 16
        };

        /// <summary>
        /// <see cref="Glow"/> gibi, ama yumuşak geçiş yerine <paramref name="levels"/> kademe ve 4x4 Bayer
        /// deseniyle: renk indirgemesinde halka halka bant oluşmaz, pixel art'taki desenli ışık gibi görünür.
        /// </summary>
        public void DitherGlow(int cx, int cy, int radius, Color32 c, float strength, int levels = 4)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radius;
                    if (d >= 1f) continue;
                    float scaled = (1f - d) * (1f - d) * levels;
                    int step = (int)scaled;
                    if (scaled - step > Bayer4[(y & 3) * 4 + (x & 3)]) step++;
                    if (step > 0) Blend(x, y, c, strength * step / levels);
                }
        }

        // ------------------------------------------------------------ 3x5 piksel yazı

        // Her karakter 5 satır x 3 sütun, üst satırdan başlayarak
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            { 'A', "010101111101101" }, { 'B', "110101110101110" }, { 'C', "011100100100011" },
            { 'E', "111100110100111" }, { 'G', "011100101101011" }, { 'I', "111010010010111" },
            { 'K', "101101110101101" }, { 'L', "100100100100111" }, { 'M', "101111111101101" },
            { 'N', "110101101101101" }, { 'O', "010101101101010" }, { 'R', "110101110101101" },
            { 'S', "011100010001110" }, { 'T', "111010010010010" }, { 'U', "101101101101111" },
            { 'Y', "101101010010010" },
            { '0', "111101101101111" }, { '1', "010110010010111" }, { '2', "111001111100111" },
            { '3', "111001111001111" }, { '4', "101101111001001" }, { '5', "111100111001111" },
            { '6', "111100111101111" }, { '7', "111001001010010" }, { '8', "111101111101111" },
            { '9', "111101111001111" }, { ':', "000010000010000" }, { ' ', "000000000000000" },
        };

        public static int TextWidth(string text, int scale = 1) => (text.Length * 4 - 1) * scale;

        /// <summary>Metni yazar; <paramref name="yTop"/> harflerin en üst satırıdır.</summary>
        public void Text(int x, int yTop, string text, Color32 c, int scale = 1)
        {
            foreach (var ch in text)
            {
                if (Glyphs.TryGetValue(char.ToUpperInvariant(ch), out var glyph))
                    for (int row = 0; row < 5; row++)
                        for (int col = 0; col < 3; col++)
                            if (glyph[row * 3 + col] == '1')
                                Rect(x + col * scale, yTop - (row + 1) * scale + 1, scale, scale, c);
                x += 4 * scale;
            }
        }

        // ------------------------------------------------------------ çıktı

        public void Quantize(int colors) => AIArtPixelator.QuantizeInPlace(px, colors);

        public void SavePng(string assetPath) => AIArtPixelator.SavePng(px, W, H, assetPath);
    }
}
