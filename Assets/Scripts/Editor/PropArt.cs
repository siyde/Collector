using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// Odaya konan nesnelerin kodla çizilen sprite'ları (alt-orta pivot). Işık soldan: sol yüzler açık,
    /// sağ yan yüz koyu, gölge sağa düşer. Yapay zekâ çizimiyle aynı ada/ölçüye koyarak değiştirilebilir.
    /// </summary>
    public static class PropArt
    {
        public const int BoxW = 48, BoxH = 44;
        static Color32 C(string hex) => PixelCanvas.C(hex);
        static readonly Color32 Ink = C("#2B1E2F");

        // Ortak ölçüler: ön yüz x 4..33, y 3..22; yan yüz 6 px derinlik; üst yüz y 23..28
        const int FrontL = 4, FrontR = 33, Bottom = 3, FrontTop = 22, Depth = 6;

        public static PixelCanvas CardboardBoxClosed()
        {
            var p = new PixelCanvas(BoxW, BoxH);
            DrawBody(p);

            var top = C("#C4955A"); var topLight = C("#D2A566");
            for (int y = FrontTop + 1; y <= FrontTop + Depth; y++)             // üst yüz (paralelkenar)
            {
                int shift = y - FrontTop;
                p.HLine(FrontL + shift, y, FrontR - FrontL + 1, top);
            }
            p.HLine(FrontL + 1, FrontTop + 1, 10, topLight);
            var seam = C("#A57B45");
            for (int k = 0; k < Depth; k++) p.Set(19 + k, FrontTop + 1 + k, seam);   // kapak birleşimi

            var tape = C("#E6CFA0"); var tapeShade = C("#CBB183");                  // koli bandı
            for (int k = 0; k < Depth; k++) p.HLine(17 + k, FrontTop + 1 + k, 4, tape);
            p.Rect(17, 14, 4, FrontTop - 14 + 1, tape);
            p.VLine(20, 14, FrontTop - 14 + 1, tapeShade);
            p.Set(17, 14, C("#B07E45")); p.Set(20, 14, C("#B07E45"));              // yırtık uç

            p.OutlineOuter(Ink);
            DrawShadow(p);
            return p;
        }

        public static PixelCanvas CardboardBoxOpen()
        {
            var p = new PixelCanvas(BoxW, BoxH);
            var flap = C("#C4965C"); var flapLight = C("#D6AA6E"); var flapDark = C("#A57B45");

            for (int y = FrontTop + Depth; y <= FrontTop + Depth + 8; y++)       // arka kapak (dik)
            {
                int k = y - (FrontTop + Depth);
                p.HLine(FrontL + Depth + 1 + k / 3, y, FrontR - FrontL - 1, flapDark);
            }
            for (int y = FrontTop + 2; y <= FrontTop + 10; y++)                  // sol kapak (dışa açık)
            {
                int k = y - (FrontTop + 2);
                p.HLine(FrontL - k, y, 6, flapLight);
            }
            for (int y = FrontTop + 4; y <= FrontTop + 12; y++)                  // sağ kapak (dışa açık)
            {
                int k = y - (FrontTop + 4);
                p.HLine(FrontR + 2 + k / 2, y, 5, flap);
            }

            DrawBody(p);

            var inside = C("#3A2618"); var insideWall = C("#6E4E2E");            // iç kısım
            for (int y = FrontTop + 1; y <= FrontTop + Depth; y++)
            {
                int shift = y - FrontTop;
                p.HLine(FrontL + shift, y, FrontR - FrontL + 1, inside);
            }
            p.HLine(FrontL + Depth, FrontTop + Depth, FrontR - FrontL + 1, insideWall);
            for (int k = 1; k < Depth; k++) p.Set(FrontL + k, FrontTop + k, insideWall);

            p.Rect(FrontL, FrontTop - 4, FrontR - FrontL + 1, 4, flap);           // öne sarkan kapak
            p.HLine(FrontL, FrontTop, FrontR - FrontL + 1, flapLight);
            p.Rect(17, FrontTop - 4, 4, 3, C("#E6CFA0"));                         // kopmuş bant parçası

            p.OutlineOuter(Ink);
            DrawShadow(p);
            return p;
        }

        // Ön yüz, sağ yan yüz, üzerine keçeli kalemle "OYUNCAK" ve birkaç leke
        static void DrawBody(PixelCanvas p)
        {
            var front = C("#B07E45"); var frontLight = C("#C08E52"); var frontDark = C("#8E6436");
            var side = C("#7E5A30"); var sideDark = C("#664826");

            p.Rect(FrontL, Bottom, FrontR - FrontL + 1, FrontTop - Bottom + 1, front);
            p.Rect(FrontL, Bottom, 2, FrontTop - Bottom + 1, frontLight);
            p.HLine(FrontL, Bottom, FrontR - FrontL + 1, frontDark);
            for (int x = FrontR + 1; x <= FrontR + Depth; x++)                  // sağ yan yüz
            {
                int shift = x - FrontR;
                p.VLine(x, Bottom + shift, FrontTop - Bottom + 1, x == FrontR + Depth ? sideDark : side);
            }

            p.Text(FrontL + 2, 11, "OYUNCAK", C("#3A2A20"));                     // keçeli kalem yazısı
            p.HLine(FrontL + 3, 5, 12, C("#A57B45"));                            // eski etiket izi
            p.HLine(FrontL + 3, 4, 8, C("#A57B45"));
            foreach (var (x, y) in new[] { (26, 17), (27, 17), (27, 16), (9, 19), (30, 6) })
                p.Set(x, y, frontDark);
        }

        // Sağa düşen yumuşak gölge (yalnızca boş piksellere, yarı saydam)
        static void DrawShadow(PixelCanvas p)
        {
            var shadow = new Color32(20, 10, 12, 90);
            for (int y = 0; y <= 3; y++)
                for (int x = FrontL - 1; x <= FrontR + Depth + 5; x++)
                {
                    float dx = (x - (FrontL + FrontR + Depth + 6) * 0.5f) / 22f, dy = (y - 1f) / 2.5f;
                    if (dx * dx + dy * dy <= 1f && p.Get(x, y).a == 0) p.Set(x, y, shadow);
                }
        }
    }
}
