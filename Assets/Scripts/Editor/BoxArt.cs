using System.Collections.Generic;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// Yapay zekâ ile üretilmiş kutu görselinden (Reference_Box.png) odaya uygun kapalı/açık sprite'lar üretir:
    /// arka planı siler, kırpar, küçültür, sağa düşen gölge ekler. Açık hal için üst yüz karanlık iç kısım
    /// olarak boyanır ve kenarlarına açılmış kapaklar çizilir.
    /// </summary>
    public static class BoxArt
    {
        public const int BoxWidth = 50;                        // odadaki kutu genişliği (piksel)
        const int MarginX = 9, MarginBottom = 3, MarginTop = 12;

        // Reference_Box.png (1254x1254) üzerindeki üst yüz köşeleri (piksel, sol-ÜST orijin):
        // ön-sol, arka-sol, arka-sağ, ön-sağ
        static readonly Vector2[] TopFaceSrc =
        {
            new Vector2(175, 394), new Vector2(512, 219), new Vector2(1075, 302), new Vector2(802, 525)
        };

        public readonly struct Result
        {
            public readonly PixelCanvas Closed, Open;
            /// <summary>Kutu gövdesinin sprite içindeki yeri (piksel, sol-alt orijin).</summary>
            public readonly RectInt Body;
            /// <summary>Kutunun ağzının (üst yüz merkezi) pivot'a (alt-orta) göre konumu, piksel.</summary>
            public readonly Vector2 MouthFromPivot;

            public Result(PixelCanvas closed, PixelCanvas open, RectInt body, Vector2 mouth)
            {
                Closed = closed; Open = open; Body = body; MouthFromPivot = mouth;
            }
        }

        public static Result FromReference(PixelCanvas reference, Color32[] palette)
        {
            RemoveBackground(reference, 60);
            var bounds = OpaqueBounds(reference);

            var cropped = new PixelCanvas(bounds.width, bounds.height);
            for (int y = 0; y < bounds.height; y++)
                for (int x = 0; x < bounds.width; x++)
                    cropped.Set(x, y, reference.Get(bounds.x + x, bounds.y + y));

            float scale = BoxWidth / (float)bounds.width;
            int boxHeight = Mathf.RoundToInt(bounds.height * scale);
            var small = cropped.Downscaled(BoxWidth, boxHeight, snapToOwnPalette: false);
            if (palette != null && palette.Length > 0) AIArtPixelator.MapToPalette(small.Pixels, palette);

            int w = BoxWidth + MarginX * 2, h = boxHeight + MarginBottom + MarginTop;
            var closed = new PixelCanvas(w, h);
            DrawShadow(closed, MarginX, BoxWidth);
            for (int y = 0; y < boxHeight; y++)
                for (int x = 0; x < BoxWidth; x++)
                {
                    var c = small.Get(x, y);
                    if (c.a > 0) closed.Set(MarginX + x, MarginBottom + y, c);
                }

            // Üst yüz köşelerini sprite koordinatına taşı
            var top = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                float yBottomUp = reference.H - 1 - TopFaceSrc[i].y;
                top[i] = new Vector2(MarginX + (TopFaceSrc[i].x - bounds.x) * scale,
                                     MarginBottom + (yBottomUp - bounds.y) * scale);
            }

            var open = DrawOpen(closed, top);
            var center = (top[0] + top[1] + top[2] + top[3]) * 0.25f;
            var body = new RectInt(MarginX, MarginBottom, BoxWidth, boxHeight);
            return new Result(closed, open, body, new Vector2(center.x - w * 0.5f, center.y));
        }

        static PixelCanvas DrawOpen(PixelCanvas closed, Vector2[] top)
        {
            var open = new PixelCanvas(closed.W, closed.H);
            System.Array.Copy(closed.Pixels, open.Pixels, closed.Pixels.Length);

            var lid = AverageInside(closed, top);
            var light = Color32.Lerp(lid, new Color32(255, 240, 210, 255), 0.18f);
            var mid = Color32.Lerp(lid, new Color32(0, 0, 0, 255), 0.15f);
            var dark = Color32.Lerp(lid, new Color32(0, 0, 0, 255), 0.35f);
            var ink = PixelCanvas.C("#2B1E2F");
            Vector2 fl = top[0], bl = top[1], br = top[2], fr = top[3];

            // Arkadaki kapaklar: arka (dik), sol (sola açık), sağ (sağa açık)
            Flap(open, bl, br, new Vector2(1, 10), dark, ink);
            Flap(open, fl, bl, new Vector2(-7, 5), light, ink);
            Flap(open, br, fr, new Vector2(6, 5), mid, ink);

            // İç kısım: karanlık taban, arka duvarlarda biraz ışık
            open.FillPolygon(top, PixelCanvas.C("#2E1E14"));
            var wall = Color32.Lerp(lid, new Color32(0, 0, 0, 255), 0.5f);
            open.Line(bl + new Vector2(0, -1), br + new Vector2(-1, -1), wall);
            open.Line(bl + new Vector2(1, -1), fl + new Vector2(1, 0), wall);
            for (int i = 0; i < 4; i++) open.Line(top[i], top[(i + 1) % 4], ink);

            // Öne sarkan kapak
            Flap(open, fl, fr, new Vector2(0, -6), light, ink);
            return open;
        }

        static void Flap(PixelCanvas p, Vector2 a, Vector2 b, Vector2 offset, Color32 fill, Color32 ink)
        {
            var quad = new[] { a, b, b + offset, a + offset };
            p.FillPolygon(quad, fill);
            for (int i = 0; i < 4; i++) p.Line(quad[i], quad[(i + 1) % 4], ink);
        }

        static Color32 AverageInside(PixelCanvas p, Vector2[] poly)
        {
            var probe = new PixelCanvas(p.W, p.H);
            probe.FillPolygon(poly, new Color32(255, 255, 255, 255));
            long r = 0, g = 0, b = 0; int n = 0;
            for (int i = 0; i < p.Pixels.Length; i++)
                if (probe.Pixels[i].a > 0 && p.Pixels[i].a > 0)
                {
                    r += p.Pixels[i].r; g += p.Pixels[i].g; b += p.Pixels[i].b; n++;
                }
            return n == 0 ? PixelCanvas.C("#C4955A") : new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        /// <summary>Kenarlardan başlayarak arka plan rengine yakın pikselleri şeffaf yapar (taşma dolgusu).</summary>
        static void RemoveBackground(PixelCanvas p, int threshold)
        {
            var bg = p.Get(2, p.H - 3);
            var visited = new bool[p.W * p.H];
            var stack = new Stack<int>();
            for (int x = 0; x < p.W; x++) { stack.Push(x); stack.Push((p.H - 1) * p.W + x); }
            for (int y = 0; y < p.H; y++) { stack.Push(y * p.W); stack.Push(y * p.W + p.W - 1); }

            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (visited[i]) continue;
                visited[i] = true;
                var c = p.Pixels[i];
                if (Mathf.Abs(c.r - bg.r) + Mathf.Abs(c.g - bg.g) + Mathf.Abs(c.b - bg.b) > threshold) continue;
                p.Pixels[i] = default;
                int x = i % p.W, y = i / p.W;
                if (x > 0) stack.Push(i - 1);
                if (x < p.W - 1) stack.Push(i + 1);
                if (y > 0) stack.Push(i - p.W);
                if (y < p.H - 1) stack.Push(i + p.W);
            }
        }

        static RectInt OpaqueBounds(PixelCanvas p)
        {
            int minX = p.W, minY = p.H, maxX = -1, maxY = -1;
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                    if (p.Get(x, y).a > 0)
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // Kutunun altından sağa doğru uzanan yarı saydam gölge
        static void DrawShadow(PixelCanvas p, int boxLeft, int boxWidth)
        {
            var shadow = new Color32(20, 10, 12, 90);
            float cx = boxLeft + boxWidth * 0.58f, rx = boxWidth * 0.62f;
            for (int y = 0; y <= 4; y++)
                for (int x = 0; x < p.W; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - 2f) / 3f;
                    if (dx * dx + dy * dy <= 1f) p.Set(x, y, shadow);
                }
        }
    }
}
