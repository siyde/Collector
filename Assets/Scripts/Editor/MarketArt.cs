using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// Bit pazarı için kodla çizilen geçici görseller: pazar sahnesi (640x360), şehir haritası ve harita simgeleri.
    /// Pazar sahnesi, Assets/Art/Reference/Reference_Market.png eklenince onunla değiştirilir.
    /// Koordinatlar bu dosyada ÜSTTEN ölçülür (TD = top-down); PixelCanvas'a yazarken çevrilir.
    /// </summary>
    public static class MarketArt
    {
        public const int W = 640, H = 360;

        /// <summary>Tezgah merkezleri (x) ve oyuncakların durduğu masa üstü (y, üstten).</summary>
        public static readonly int[] StallCenters = { 120, 320, 520 };
        public const int TableTopY = 252;
        public static readonly int[] ItemOffsets = { -52, 0, 52 };

        static Color32 C(string hex) => PixelCanvas.C(hex);
        static readonly Color32 Ink = C("#2B1E2F");

        // Üstten ölçülen dikdörtgen
        static void RectTD(PixelCanvas p, int x, int yTop, int w, int h, Color32 c) => p.Rect(x, p.H - yTop - h, w, h, c);
        static void SetTD(PixelCanvas p, int x, int y, Color32 c) => p.Set(x, p.H - 1 - y, c);

        public static PixelCanvas Market()
        {
            var p = new PixelCanvas(W, H);

            // Gökyüzü: sabah mavisi, ufka doğru açılır; sağ üstte güneş parıltısı
            for (int y = 0; y < 200; y++)
                RectTD(p, 0, y, W, 1, Color32.Lerp(C("#7FB6E0"), C("#E8EEF0"), y / 200f));
            p.DitherGlow(600, H - 20, 160, C("#FFF4C8"), 0.6f);

            // Uzaktaki binalar
            var rng = new System.Random(5);
            int x = 0;
            while (x < W)
            {
                int bw = rng.Next(40, 80), bh = rng.Next(50, 110), top = 200 - bh;
                var wall = rng.Next(2) == 0 ? C("#A8B4C4") : C("#98A6B8");
                RectTD(p, x, top, bw, bh, wall);
                RectTD(p, x, top, bw, 3, C("#7E8CA0"));
                for (int wy = top + 10; wy < 190; wy += 14)
                    for (int wx = x + 6; wx < x + bw - 6; wx += 12)
                        RectTD(p, wx, wy, 4, 6, C("#D8E0E8"));
                x += bw + rng.Next(0, 6);
            }
            for (int t = 0; t < 9; t++)                                           // ağaçlar
            {
                int tx = 30 + t * 72 + rng.Next(-10, 10);
                p.Ellipse(tx, H - 1 - 188, 16, 13, C("#6E9A5A"));
                p.Ellipse(tx - 5, H - 1 - 184, 9, 7, C("#86B070"));
                RectTD(p, tx - 2, 196, 4, 8, C("#6B4A32"));
            }

            // Arnavut kaldırımı zemin
            RectTD(p, 0, 200, W, 160, C("#A89A88"));
            for (int row = 0; row < 16; row++)
            {
                int y0 = 202 + row * 10, shift = row % 2 == 0 ? 0 : 8;
                for (int sx = -shift; sx < W; sx += 16)
                {
                    RectTD(p, sx + 1, y0, 14, 8, C("#B4A694"));
                    RectTD(p, sx + 1, y0 + 7, 14, 1, C("#8C7E6C"));
                    RectTD(p, sx + 15, y0, 1, 8, C("#8C7E6C"));
                }
            }
            RectTD(p, 0, 200, W, 2, C("#7E7060"));

            // Bayrak dizisi (tezgahların üstünde)
            Color32[] flags = { C("#C93C3C"), C("#F2C14E"), C("#4A7FC0"), C("#7BC47F") };
            for (int fx = 0, i = 0; fx < W; fx += 16, i++)
            {
                int sag = (int)(Mathf.Sin(fx / (float)W * Mathf.PI * 3f) * 6f) + 64;
                RectTD(p, fx, sag, 16, 1, C("#5A5248"));
                for (int k = 0; k < 7; k++) RectTD(p, fx + 4 + k / 2, sag + 1 + k, 8 - k, 1, flags[i % flags.Length]);
            }

            // Tezgahlar
            (Color32 main, Color32 alt, Color32 cloth)[] stalls =
            {
                (C("#C93C3C"), C("#F2E8D0"), C("#B04A3A")),
                (C("#4A7FC0"), C("#F2E8D0"), C("#3E6AA0")),
                (C("#5E9A58"), C("#F2E8D0"), C("#4E8048")),
            };
            Color32[] shirts = { C("#7A5A9A"), C("#C9803C"), C("#3E7A8A") };
            for (int s = 0; s < StallCenters.Length; s++)
                DrawStall(p, StallCenters[s], stalls[s].main, stalls[s].alt, stalls[s].cloth, shirts[s], s);

            p.Quantize(AIArtPixelator.PaletteSize);
            return p;
        }

        static void DrawStall(PixelCanvas p, int c, Color32 main, Color32 alt, Color32 cloth, Color32 shirt, int style)
        {
            var wood = C("#8C5433");
            // Direkler
            RectTD(p, c - 88, 120, 3, 172, wood);
            RectTD(p, c + 85, 120, 3, 172, wood);

            // Satıcı (masanın arkasında)
            p.Ellipse(c, H - 1 - 172, 9, 9, C("#E8B58E"));                        // kafa
            RectTD(p, c - 9, 162, 18, 5, C("#4A2E1F"));                           // saç
            RectTD(p, c - 13, 182, 26, 70, shirt);                                // gövde
            RectTD(p, c - 13, 182, 3, 70, Color32.Lerp(shirt, new Color32(255, 255, 255, 255), 0.2f));
            SetTD(p, c - 3, 172, Ink); SetTD(p, c + 3, 172, Ink);

            // Tente: şeritli, alt kenarı dalgalı
            for (int sx = c - 92, i = 0; sx < c + 92; sx += 12, i++)
                RectTD(p, sx, 120, 12, 26, i % 2 == 0 ? main : alt);
            for (int sx = c - 92, i = 0; sx < c + 92; sx += 12, i++)
                p.Ellipse(sx + 6, H - 1 - 146, 6, 4, i % 2 == 0 ? main : alt);
            RectTD(p, c - 92, 118, 184, 3, Color32.Lerp(main, new Color32(0, 0, 0, 255), 0.35f));

            // İsim tabelası (yazıyı sahne üstündeki piksel yazı koyar)
            RectTD(p, c - 38, 124, 76, 14, C("#F2E8D0"));
            RectTD(p, c - 38, 124, 76, 1, Ink); RectTD(p, c - 38, 137, 76, 1, Ink);
            RectTD(p, c - 38, 124, 1, 14, Ink); RectTD(p, c + 37, 124, 1, 14, Ink);

            // Masa: açık ahşap üst yüzey ve önünde desenli örtü
            RectTD(p, c - 86, TableTopY - 4, 172, 6, C("#B88A5A"));
            RectTD(p, c - 86, TableTopY - 4, 172, 1, C("#D0A470"));
            RectTD(p, c - 88, TableTopY + 2, 176, 38, cloth);
            var dark = Color32.Lerp(cloth, new Color32(0, 0, 0, 255), 0.25f);
            var light = Color32.Lerp(cloth, new Color32(255, 255, 255, 255), 0.3f);
            if (style == 0)                                                       // ekose
                for (int yy = 0; yy < 38; yy += 8)
                    for (int xx = 0; xx < 176; xx += 8)
                        if ((xx / 8 + yy / 8) % 2 == 0) RectTD(p, c - 88 + xx, TableTopY + 2 + yy, 8, 8, dark);
            if (style == 1)                                                       // çizgili
                for (int xx = 0; xx < 176; xx += 10) RectTD(p, c - 88 + xx, TableTopY + 2, 3, 38, light);
            if (style == 2)                                                       // kenar süslü
            {
                RectTD(p, c - 88, TableTopY + 8, 176, 2, light);
                for (int xx = 0; xx < 176; xx += 6) SetTD(p, c - 86 + xx, TableTopY + 36, light);
            }
            RectTD(p, c - 88, TableTopY + 39, 176, 1, dark);

            // Yandaki tahta sandık
            RectTD(p, c - 110, 296, 24, 20, C("#9C6A3E"));
            RectTD(p, c - 110, 296, 24, 2, C("#B8844E"));
            RectTD(p, c - 110, 305, 24, 1, C("#6E4024"));
        }

        // ------------------------------------------------------------ harita

        public const int MapW = 260, MapH = 150;
        /// <summary>Haritadaki yerler (harita pikseli, üstten): ev ve bit pazarı.</summary>
        public static readonly Vector2Int HomeOnMap = new Vector2Int(60, 100), MarketOnMap = new Vector2Int(195, 45);

        public static PixelCanvas Map()
        {
            var p = new PixelCanvas(MapW, MapH);
            var paper = C("#E8D9B5"); var edge = C("#B8A57E"); var street = C("#D8CAA8"); var streetEdge = C("#BCAA84");

            p.Rect(0, 0, MapW, MapH, paper);
            var rng = new System.Random(11);
            for (int i = 0; i < 400; i++) p.Blend(rng.Next(MapW), rng.Next(MapH), C("#D4C39C"), 0.6f);   // kâğıt dokusu

            // Nehir (sol alttan sağ üste)
            for (int x = 0; x < MapW; x++)
            {
                int yc = 110 - x * 70 / MapW;
                RectTD(p, x, yc - 7, 1, 14, C("#7FAFD0"));
                SetTD(p, x, yc - 7, C("#A8CCE4")); SetTD(p, x, yc + 6, C("#5E8FB4"));
            }
            // Park
            RectTD(p, 140, 92, 70, 44, C("#A8C98A"));
            for (int i = 0; i < 14; i++) p.Ellipse(146 + rng.Next(60), MapH - 1 - (98 + rng.Next(34)), 3, 3, C("#6E9A5A"));

            // Sokaklar
            foreach (int y in new[] { 28, 80, 128 }) { RectTD(p, 0, y, MapW, 5, street); RectTD(p, 0, y + 5, MapW, 1, streetEdge); }
            foreach (int x in new[] { 38, 108, 176, 232 }) { RectTD(p, x, 0, 5, MapH, street); RectTD(p, x + 5, 0, 1, MapH, streetEdge); }

            // Ev blokları (kiremit çatılar)
            for (int i = 0; i < 26; i++)
            {
                int bx = rng.Next(4, MapW - 10), by = rng.Next(4, MapH - 10);
                if (bx > 136 && bx < 212 && by > 88 && by < 138) continue;          // park
                RectTD(p, bx, by, 6, 5, C("#C96A4A"));
                RectTD(p, bx, by + 5, 6, 1, C("#8E4A34"));
            }

            // Pusula (sağ üst)
            p.Text(MapW - 16, MapH - 4, "K", C("#5A5248"));
            RectTD(p, MapW - 15, 10, 1, 8, C("#5A5248"));

            p.Border(0, 0, MapW, MapH, edge);
            p.Border(1, 1, MapW - 2, MapH - 2, edge);
            return p;
        }

        public static PixelCanvas HomePin()
        {
            var p = new PixelCanvas(15, 15);
            for (int i = 0; i < 7; i++) RectTD(p, 7 - i, 1 + i, 1 + i * 2, 1, C("#C93C3C"));      // çatı
            RectTD(p, 2, 8, 11, 6, C("#F2E8D0"));                                                   // duvar
            RectTD(p, 6, 10, 3, 4, C("#6E4024"));                                                   // kapı
            RectTD(p, 3, 9, 2, 2, C("#7FAFD0"));                                                    // pencere
            p.OutlineOuter(Ink);
            return p;
        }

        public static PixelCanvas TentPin()
        {
            var p = new PixelCanvas(15, 15);
            RectTD(p, 7, 0, 1, 4, C("#5A5248"));                                                    // bayrak direği
            RectTD(p, 8, 0, 4, 2, C("#F2C14E"));                                                    // bayrak
            for (int i = 0; i < 7; i++)                                                             // çadır
            {
                int half = i;
                for (int x = 7 - half; x <= 7 + half; x++)
                    SetTD(p, x, 4 + i, ((x - (7 - half)) / 2) % 2 == 0 ? C("#C93C3C") : C("#F2E8D0"));
            }
            RectTD(p, 6, 8, 3, 3, C("#3A2618"));                                                    // giriş
            p.OutlineOuter(Ink);
            return p;
        }
    }
}
