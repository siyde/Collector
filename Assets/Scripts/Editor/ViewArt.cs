using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// Kodla çizilen yakın plan görselleri (640x360, 32 PPU).
    /// Bilgisayar görünümü: odadaki bej CRT monitörün önüne yaklaşılmış hali; ekran dikdörtgeni
    /// odadaki cam alanıyla aynı oranda olduğu için yaklaşma efekti monitöre birebir oturur.
    /// </summary>
    public static class ViewArt
    {
        /// <summary>Ekran (cam) alanı, piksel, sol-ALT orijin. Odadaki camın oranı: 123x101.</summary>
        public static readonly RectInt ComputerScreenRect = new RectInt(137, 26, 366, 300);

        static Color32 C(string hex) => PixelCanvas.C(hex);
        static readonly Color32 Ink = C("#2B1E2F");

        public static PixelCanvas ComputerScreen()
        {
            var p = new PixelCanvas(640, 360);
            var s = ComputerScreenRect;

            // 1) Arkadaki loş oda: solda pencereden gün batımı, sağda masa lambası
            p.Rect(0, 0, 640, 360, C("#231B2C"));
            p.DitherGlow(0, 360, 240, C("#E08A5A"), 0.35f);
            p.DitherGlow(640, 230, 260, C("#F2A860"), 0.5f);

            // 2) Monitör kasası (odadaki oranlar: sol 13, sağ 10 px / 123 px cam)
            DrawBezel(p, s, left: 98, right: 532);

            // 3) Masaüstü ve CRT efektleri
            DrawDesktop(p, s);
            DrawCrtEffects(p, s);

            p.Quantize(AIArtPixelator.PaletteSize);
            return p;
        }

        static void DrawBezel(PixelCanvas p, RectInt s, int left, int right)
        {
            var bezel = C("#C7B3A3"); var light = C("#DCC8B6"); var shade = C("#A8927F"); var deep = C("#6E5A50");

            p.Rect(left, 0, right - left + 1, 360, bezel);
            var rng = new System.Random(3);                                  // plastik dokusu
            for (int k = 0; k < 2500; k++)
                p.Blend(rng.Next(left, right), rng.Next(0, 360), rng.Next(2) == 0 ? light : shade, 0.35f);
            for (int i = 0; i < 8; i++) p.BlendRect(left + i, 0, 1, 360, deep, 0.3f * (1f - i / 8f)); // sol kenar gölgede
            p.DitherGlow(right + 30, 230, 280, C("#F2A860"), 0.2f, 3);        // lamba sağ tarafı ısıtır
            p.VLine(left, 0, 360, Ink);
            p.VLine(right, 0, 360, Ink);
            p.VLine(right - 1, 0, 360, light);

            // Camın çevresindeki çukur: üst/sol gölgede, alt/sağ ışıkta
            for (int i = 1; i <= 6; i++)
            {
                p.HLine(s.xMin - i, s.yMax - 1 + i, s.width + 2 * i, shade);
                p.VLine(s.xMin - i, s.yMin - i, s.height + 2 * i, shade);
                p.HLine(s.xMin - i, s.yMin - i, s.width + 2 * i, light);
                p.VLine(s.xMax - 1 + i, s.yMin - i, s.height + 2 * i, light);
            }
            p.Border(s.xMin - 7, s.yMin - 7, s.width + 14, s.height + 14, deep);
            p.Border(s.xMin - 1, s.yMin - 1, s.width + 2, s.height + 2, Ink);

            // Alt çerçeve: markasız plaka, güç düğmesi ve yeşil ışık
            p.Rect(296, 7, 48, 8, C("#BCA796"));
            p.HLine(296, 15, 48, deep); p.HLine(296, 6, 48, light);
            p.Rect(484, 7, 10, 5, shade); p.HLine(484, 11, 10, light);
            p.Glow(472, 9, 8, C("#5CE05C"), 0.4f);
            p.Rect(470, 8, 4, 3, C("#5CE05C"));
        }

        static void DrawDesktop(PixelCanvas p, RectInt s)
        {
            var teal = C("#2A6F6B");
            p.Rect(s.xMin, s.yMin, s.width, s.height, teal);
            p.DitherGlow(s.xMin + s.width / 2, s.yMin + s.height / 2 + 10, 220, C("#3E8C86"), 0.6f);

            // Duvar kâğıdı: soluk büyük yazı
            const string wallpaper = "KOLEKSIYONCU";
            int wx = s.xMin + (s.width - PixelCanvas.TextWidth(wallpaper, 3)) / 2 + 20;
            int wy = s.yMin + s.height / 2 + 16;
            p.Text(wx + 3, wy - 3, wallpaper, C("#17423F"), 3);
            p.Text(wx, wy, wallpaper, C("#6CC2B8"), 3);
            p.Text(wx + PixelCanvas.TextWidth(wallpaper, 3) - PixelCanvas.TextWidth("98", 2), wy - 20, "98", C("#E9A15A"), 2);

            // Görev çubuğu
            int tb = s.yMin;
            p.Rect(s.xMin, tb, s.width, 18, C("#B9B0A2"));
            p.HLine(s.xMin, tb + 17, s.width, C("#EDE6D6"));
            p.HLine(s.xMin, tb + 16, s.width, C("#CFC6B6"));

            Bevel(p, s.xMin + 4, tb + 3, 48, 12, raised: true);               // Başlat
            p.Diamond(s.xMin + 11, tb + 9, 3, C("#E9785A"));
            p.Diamond(s.xMin + 11, tb + 9, 1, C("#F2C14E"));
            p.Text(s.xMin + 18, tb + 11, "BASLAT", Ink);

            const string clock = "19:47";                                     // gün batımı saati
            Bevel(p, s.xMax - 44, tb + 3, 40, 12, raised: false);
            p.Text(s.xMax - 44 + (40 - PixelCanvas.TextWidth(clock)) / 2, tb + 11, clock, Ink);

            // Masaüstü simgeleri (sol sütun)
            string[] labels = { "ILANLAR", "KOLEKSIYON", "BILGISAYAR", "OYUNLAR" };
            System.Action<PixelCanvas, int, int>[] icons = { NewspaperIcon, RobotIcon, ComputerIcon, GamepadIcon };
            int iconX = s.xMin + 14;
            for (int i = 0; i < labels.Length; i++)
            {
                int top = s.yMax - 12 - i * 56;
                icons[i](p, iconX, top - 24);
                int labelX = iconX + 12 - PixelCanvas.TextWidth(labels[i]) / 2;
                p.Text(labelX + 1, top - 29, labels[i], C("#16322F"));         // gölge
                p.Text(labelX, top - 28, labels[i], C("#F2EEE4"));
            }
        }

        static void DrawCrtEffects(PixelCanvas p, RectInt s)
        {
            float cx = s.xMin + s.width * 0.5f, cy = s.yMin + s.height * 0.5f;
            var black = new Color32(0, 0, 0, 255);
            for (int y = s.yMin; y < s.yMax; y++)
                for (int x = s.xMin; x < s.xMax; x++)
                {
                    float dx = (x - cx) / (s.width * 0.5f), dy = (y - cy) / (s.height * 0.5f);
                    p.Blend(x, y, C("#081210"), Mathf.Clamp01((dx * dx + dy * dy) * 0.22f)); // kenar kararması
                    if ((y & 1) == 0) p.Blend(x, y, black, 0.1f);                           // tarama çizgileri
                }
            p.DitherGlow(s.xMin + 70, s.yMax - 50, 120, new Color32(255, 255, 255, 255), 0.12f, 3); // cam parlaması
            p.BlendRect(s.xMin, s.yMax - 3, s.width, 3, black, 0.3f);                         // üst çerçeve gölgesi

            const int r = 7;                                                                   // yuvarlak cam köşeleri
            for (int dy = 0; dy < r; dy++)
                for (int dx = 0; dx < r; dx++)
                {
                    float ox = r - dx - 0.5f, oy = r - dy - 0.5f;
                    if (ox * ox + oy * oy <= r * r) continue;
                    p.Set(s.xMin + dx, s.yMin + dy, Ink);
                    p.Set(s.xMax - 1 - dx, s.yMin + dy, Ink);
                    p.Set(s.xMin + dx, s.yMax - 1 - dy, Ink);
                    p.Set(s.xMax - 1 - dx, s.yMax - 1 - dy, Ink);
                }
        }

        static void Bevel(PixelCanvas p, int x, int y, int w, int h, bool raised)
        {
            var hi = C("#F0E8DA"); var lo = C("#7C7466");
            p.Rect(x, y, w, h, raised ? C("#C9C0B2") : C("#B0A798"));
            p.HLine(x, y + h - 1, w, raised ? hi : lo);
            p.VLine(x, y, h, raised ? hi : lo);
            p.HLine(x, y, w, raised ? lo : hi);
            p.VLine(x + w - 1, y, h, raised ? lo : hi);
        }

        // 24x24 simgeler, (x,y) sol-alt köşe
        static void NewspaperIcon(PixelCanvas p, int x, int y)
        {
            var gray = C("#8A8070");
            p.Rect(x + 4, y + 1, 16, 21, C("#EDE3CC"));
            p.Border(x + 4, y + 1, 16, 21, Ink);
            p.Rect(x + 6, y + 17, 12, 3, C("#B0413E"));
            p.Rect(x + 6, y + 9, 5, 5, C("#6FA3B5"));
            p.HLine(x + 12, y + 13, 6, gray); p.HLine(x + 12, y + 11, 6, gray); p.HLine(x + 12, y + 9, 6, gray);
            p.HLine(x + 6, y + 6, 12, gray); p.HLine(x + 6, y + 4, 9, gray);
        }

        static void RobotIcon(PixelCanvas p, int x, int y)
        {
            var metal = C("#9AA7B8"); var metalDark = C("#6E7C90");
            p.Rect(x + 11, y + 19, 2, 3, metalDark); p.Rect(x + 10, y + 22, 4, 2, C("#E9785A"));
            p.Rect(x + 5, y + 9, 14, 10, metal); p.Border(x + 5, y + 9, 14, 10, Ink);
            p.Rect(x + 8, y + 13, 3, 3, C("#F2C14E")); p.Rect(x + 13, y + 13, 3, 3, C("#F2C14E"));
            p.HLine(x + 8, y + 11, 8, Ink);
            p.Rect(x + 7, y + 2, 10, 7, metalDark); p.Border(x + 7, y + 2, 10, 7, Ink);
            p.Rect(x + 10, y + 4, 4, 2, C("#E9785A"));
        }

        static void ComputerIcon(PixelCanvas p, int x, int y)
        {
            p.Rect(x + 5, y + 2, 14, 2, C("#D8CCB0")); p.Border(x + 5, y + 2, 14, 2, Ink);
            p.Rect(x + 9, y + 4, 6, 4, C("#B8AC90"));
            p.Rect(x + 3, y + 8, 18, 14, C("#D8CCB0")); p.Border(x + 3, y + 8, 18, 14, Ink);
            p.Rect(x + 6, y + 11, 12, 8, C("#1F5552"));
            p.HLine(x + 7, y + 17, 5, C("#7FD1C3")); p.HLine(x + 7, y + 15, 8, C("#7FD1C3"));
        }

        static void GamepadIcon(PixelCanvas p, int x, int y)
        {
            p.VLine(x + 12, y + 17, 5, Ink);
            p.Rect(x + 2, y + 7, 20, 10, C("#5A5F73")); p.Border(x + 2, y + 7, 20, 10, Ink);
            p.Rect(x + 5, y + 11, 5, 1, Ink); p.Rect(x + 7, y + 9, 1, 5, Ink);
            p.Rect(x + 15, y + 12, 2, 2, C("#C93C3C")); p.Rect(x + 18, y + 10, 2, 2, C("#F2C14E"));
        }
    }
}
