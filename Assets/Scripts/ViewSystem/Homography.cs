using UnityEngine;

namespace Collector.ViewSystem
{
    /// <summary>
    /// Birim kareyi herhangi bir dörtgene eşleyen projektif dönüşüm (homografi).
    /// (u, v) = (0,0) Sol-Üst, (1,0) Sağ-Üst, (1,1) Sağ-Alt, (0,1) Sol-Alt köşeye gider.
    /// Perspektifli bir yüzeyi (ör. açılı çizilmiş raf) düzleştirmek ve geri eğmek için kullanılır;
    /// iki yönde de aynı dönüşüm kullanıldığı için geçiş efekti oda görseliyle birebir örtüşür.
    /// </summary>
    public readonly struct Homography
    {
        readonly double a, b, c, d, e, f, g, h, i;

        Homography(double a, double b, double c, double d, double e, double f, double g, double h, double i)
        {
            this.a = a; this.b = b; this.c = c;
            this.d = d; this.e = e; this.f = f;
            this.g = g; this.h = h; this.i = i;
        }

        public static Homography SquareToQuad(Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
        {
            double x0 = topLeft.x, y0 = topLeft.y, x1 = topRight.x, y1 = topRight.y;
            double x2 = bottomRight.x, y2 = bottomRight.y, x3 = bottomLeft.x, y3 = bottomLeft.y;
            double sx = x0 - x1 + x2 - x3, sy = y0 - y1 + y2 - y3;

            if (System.Math.Abs(sx) < 1e-9 && System.Math.Abs(sy) < 1e-9)
                return new Homography(x1 - x0, x3 - x0, x0, y1 - y0, y3 - y0, y0, 0, 0, 1); // paralelkenar

            double dx1 = x1 - x2, dx2 = x3 - x2, dy1 = y1 - y2, dy2 = y3 - y2;
            double det = dx1 * dy2 - dx2 * dy1;
            double g = (sx * dy2 - dx2 * sy) / det;
            double h = (dx1 * sy - sx * dy1) / det;
            return new Homography(
                x1 - x0 + g * x1, x3 - x0 + h * x3, x0,
                y1 - y0 + g * y1, y3 - y0 + h * y3, y0,
                g, h, 1);
        }

        public Vector2 Map(float u, float v)
        {
            double w = g * u + h * v + i;
            return new Vector2((float)((a * u + b * v + c) / w), (float)((d * u + e * v + f) / w));
        }

        public Vector2 Map(Vector2 uv) => Map(uv.x, uv.y);

        public Homography Inverse()
        {
            // 3x3 matrisin eki (adjugate); ölçek homografide önemsiz olduğu için determinanta bölmeye gerek yok
            return new Homography(
                e * i - f * h, c * h - b * i, b * f - c * e,
                f * g - d * i, a * i - c * g, c * d - a * f,
                d * h - e * g, b * g - a * h, a * e - b * d);
        }
    }
}
