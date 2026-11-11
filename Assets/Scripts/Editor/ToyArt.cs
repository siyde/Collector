using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// İlk 5 placeholder oyuncağın kodla çizilen 48x48 raf sprite'ları (alt-orta pivot, 1 px kenar payı).
    /// Işık odadaki gibi soldan gelir: sol kenarlar açık, sağ kenarlar koyu. Hepsi özgün tasarımdır.
    /// </summary>
    public static class ToyArt
    {
        public const int Size = 48;
        static Color32 C(string hex) => PixelCanvas.C(hex);
        static readonly Color32 Ink = C("#2B1E2F");

        public static PixelCanvas RetroRobot()
        {
            var p = new PixelCanvas(Size, Size);
            var metal = C("#9AA7B8"); var light = C("#C3CEDB"); var shade = C("#7D8BA0"); var dark = C("#4A5568");
            var red = C("#E9785A"); var yellow = C("#F2C14E");

            p.Rect(15, 1, 7, 3, dark); p.Rect(26, 1, 7, 3, dark);                 // ayaklar
            p.Rect(17, 4, 4, 8, shade); p.Rect(27, 4, 4, 8, shade);               // bacaklar
            p.VLine(17, 4, 8, metal); p.VLine(27, 4, 8, metal);

            p.Rect(13, 12, 22, 16, metal);                                        // gövde
            p.Rect(13, 12, 2, 16, light); p.Rect(32, 12, 3, 16, shade);
            p.Rect(17, 15, 14, 9, C("#2E3A6B"));                                  // göğüs paneli
            p.HLine(19, 17, 10, C("#5A6B8C")); p.HLine(19, 16, 10, C("#5A6B8C"));
            p.Rect(19, 20, 2, 2, red); p.Rect(23, 20, 2, 2, yellow); p.Rect(27, 20, 2, 2, C("#7BC47F"));
            foreach (var (x, y) in new[] { (15, 26), (32, 26), (15, 13), (32, 13) }) p.Set(x, y, dark); // perçinler

            p.Rect(9, 15, 4, 11, shade); p.VLine(9, 15, 11, metal);                // kollar
            p.Rect(35, 15, 4, 11, shade);
            p.Rect(8, 12, 5, 3, dark); p.Rect(35, 12, 5, 3, dark);                 // kıskaçlar
            p.Clear(10, 12); p.Clear(37, 12);

            p.Rect(21, 28, 6, 2, dark);                                           // boyun
            p.Rect(15, 30, 18, 12, metal);                                        // kafa
            p.HLine(15, 41, 18, light); p.VLine(15, 30, 12, light); p.Rect(31, 30, 2, 12, shade);
            p.Rect(18, 34, 4, 4, yellow); p.Rect(26, 34, 4, 4, yellow);           // gözler
            p.Set(19, 35, Ink); p.Set(27, 35, Ink); p.Set(18, 37, C("#FFF4C8")); p.Set(26, 37, C("#FFF4C8"));
            p.HLine(20, 31, 8, dark);                                             // ağız ızgarası
            for (int x = 21; x < 28; x += 2) p.Set(x, 32, dark);
            p.Rect(13, 34, 2, 4, red); p.Rect(33, 34, 2, 4, red);                 // kulak cıvataları
            p.VLine(24, 42, 3, dark); p.Rect(23, 45, 3, 2, red);                  // anten

            p.OutlineOuter(Ink);
            return p;
        }

        public static PixelCanvas RedToyCar()
        {
            var p = new PixelCanvas(Size, Size);
            var red = C("#C93C3C"); var redLight = C("#E8645A"); var redDark = C("#8E2A2A");
            var glass = C("#9FD3D8"); var chrome = C("#D8D8D8");

            for (int y = 14; y <= 21; y++)                                        // kabin (eğimli ön cam)
            {
                int left = 12 + (y - 14) / 2, right = 33 - (y - 14);
                p.HLine(left, y, right - left + 1, red);
            }
            for (int y = 15; y <= 20; y++)
            {
                int left = 14 + (y - 14) / 2, right = 31 - (y - 14);
                if (right > left) p.HLine(left, y, right - left + 1, glass);
            }
            p.VLine(22, 15, 6, red);                                              // kapı direği
            p.Set(15, 20, C("#E6F6F8")); p.Set(16, 19, C("#E6F6F8"));              // cam parlaması

            p.Rect(3, 6, 42, 8, red);                                             // gövde
            p.HLine(4, 13, 40, redLight); p.HLine(3, 6, 42, redDark);
            p.HLine(5, 9, 38, C("#F2E8D0"));                                      // yarış şeridi
            p.Rect(2, 7, 2, 4, chrome); p.Rect(44, 7, 2, 4, chrome);              // tamponlar
            p.Rect(43, 10, 2, 2, C("#F2E8B0")); p.Rect(3, 10, 2, 2, C("#F2C14E")); // farlar
            p.VLine(22, 7, 6, redDark);                                           // kapı çizgisi
            p.Set(26, 11, redDark); p.Set(27, 11, redDark);                       // kapı kolu

            foreach (int cx in new[] { 12, 36 })                                  // tekerlekler
            {
                p.Ellipse(cx, 6, 5, 5, C("#1E1E24"));
                p.Ellipse(cx, 6, 2, 2, C("#B8C0C8"));
                p.Set(cx - 1, 7, C("#E8EEF2"));
            }

            p.OutlineOuter(Ink);
            return p;
        }

        public static PixelCanvas OldTaso()
        {
            var p = new PixelCanvas(Size, Size);
            var wood = C("#8C5433");

            p.Rect(18, 1, 12, 3, wood); p.HLine(18, 3, 12, C("#A86B40"));          // ahşap stand
            p.Rect(21, 4, 6, 2, C("#6E4024"));

            p.Ellipse(24, 20, 14, 14, C("#E9A15A"));                              // kenar halkası
            p.Ellipse(24, 20, 12, 12, C("#F2E8D0"));
            p.Ellipse(24, 20, 9, 9, C("#3E8C86"));                                // orta desen
            p.Ellipse(24, 20, 6, 6, C("#2E6B66"));
            p.Diamond(24, 20, 6, C("#F2C14E"));                                   // yıldız
            p.Diamond(24, 20, 3, C("#FFF4C8"));
            p.HLine(17, 20, 15, C("#F2C14E")); p.VLine(24, 13, 15, C("#F2C14E"));
            for (int i = 0; i < 8; i++)                                           // halkadaki noktalar
            {
                float a = i * Mathf.PI / 4f;
                p.Set(24 + Mathf.RoundToInt(Mathf.Cos(a) * 13), 20 + Mathf.RoundToInt(Mathf.Sin(a) * 13), C("#C93C3C"));
            }
            p.Set(15, 29, C("#FFFFFF")); p.Set(16, 30, C("#FFFFFF")); p.Set(14, 27, C("#FFF4C8")); // parlama
            p.Set(33, 12, C("#B88A5A")); p.Set(34, 14, C("#B88A5A")); p.Set(30, 8, C("#B88A5A"));   // yıpranma

            p.OutlineOuter(Ink);
            return p;
        }

        public static PixelCanvas MiniFigure()
        {
            var p = new PixelCanvas(Size, Size);
            var suit = C("#E9785A"); var suitDark = C("#B9553F"); var skin = C("#F2C29B"); var cape = C("#C93C3C");

            p.Ellipse(24, 3, 9, 2, C("#2E3A6B"));                                 // taban
            p.HLine(17, 4, 14, C("#4A5A8A"));
            p.Rect(17, 12, 14, 14, cape);                                         // pelerin (arkada)
            p.Rect(20, 5, 3, 9, C("#3B4A7A")); p.Rect(25, 5, 3, 9, C("#3B4A7A")); // bacaklar
            p.Rect(19, 5, 4, 2, Ink); p.Rect(25, 5, 4, 2, Ink);                   // botlar
            p.Rect(18, 14, 12, 11, suit);                                         // gövde
            p.VLine(18, 14, 11, C("#F29A7C")); p.VLine(29, 14, 11, suitDark);
            p.HLine(18, 15, 12, C("#F2C14E"));                                    // kemer
            p.Diamond(24, 20, 2, C("#F2E8D0"));                                   // amblem
            p.Rect(15, 15, 3, 9, suit); p.Rect(15, 13, 3, 2, skin);               // sol kol
            p.Rect(30, 22, 3, 9, suit); p.Rect(30, 31, 3, 3, skin);               // kalkık sağ kol
            p.Rect(19, 25, 10, 9, skin);                                          // kafa
            p.Rect(19, 28, 10, 2, C("#2E3A6B"));                                  // maske
            p.Set(21, 28, C("#FFFFFF")); p.Set(26, 28, C("#FFFFFF"));
            p.Rect(18, 32, 12, 4, C("#4A2E1F"));                                  // saç
            p.Set(19, 36, C("#4A2E1F")); p.Set(23, 36, C("#4A2E1F")); p.Set(27, 36, C("#4A2E1F"));
            p.HLine(22, 26, 4, C("#C98A6B"));                                     // ağız

            p.OutlineOuter(Ink);
            return p;
        }

        public static PixelCanvas HandheldToy()
        {
            var p = new PixelCanvas(Size, Size);
            var body = C("#6FB7A8"); var light = C("#9AD8CB"); var shade = C("#4E8F83"); var dark = C("#2B2430");

            p.Rect(11, 2, 26, 38, body);                                          // gövde
            p.VLine(12, 4, 34, light); p.VLine(35, 3, 36, shade); p.HLine(12, 2, 24, shade);
            p.Clear(11, 2); p.Clear(36, 39); p.Clear(11, 39);
            p.Clear(36, 2); p.Clear(35, 2); p.Clear(36, 3);                        // sağ alt köşe daha yuvarlak

            p.Rect(14, 22, 20, 15, C("#3A3F52"));                                 // ekran çerçevesi
            p.Rect(16, 24, 16, 11, C("#A8C66C"));                                 // LCD
            var lcd = C("#4E6B2E");
            p.HLine(16, 25, 16, lcd);                                             // zemin
            p.Rect(18, 26, 2, 3, lcd); p.Set(18, 29, lcd);                        // küçük karakter
            p.Rect(26, 26, 3, 3, lcd); p.Rect(29, 30, 2, 1, lcd);                 // engel ve kuş
            p.Set(15, 35, C("#E9785A"));                                          // güç ışığı

            p.Rect(15, 13, 7, 3, dark); p.Rect(17, 11, 3, 7, dark);               // yön tuşu
            p.Ellipse(29, 14, 2, 2, C("#C93C3C")); p.Ellipse(33, 17, 2, 2, C("#C93C3C")); // A/B
            p.Set(28, 15, C("#F28A7A")); p.Set(32, 18, C("#F28A7A"));
            p.HLine(20, 7, 4, C("#3A3F52")); p.HLine(26, 7, 4, C("#3A3F52"));     // start/select
            for (int i = 0; i < 3; i++)                                           // hoparlör
                for (int k = 0; k < 3; k++) p.Set(28 + i * 2 + k, 3 + k, shade);

            p.OutlineOuter(Ink);
            return p;
        }
    }
}
