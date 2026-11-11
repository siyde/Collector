using Collector.ViewSystem;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// Oda ve yakın plan görselleri üzerinde ölçülen her şey tek yerde: raf ön yüzü, monitör ekranı,
    /// raf slotları. Piksel koordinatları sol-üst orijinlidir. Görseller değişirse yalnızca
    /// buradaki sayıları güncelle.
    /// </summary>
    public static class RoomLayout
    {
        const float SrcPxPerUnit = 1672f / 20f;   // 83.6 kaynak px = 1 dünya birimi (640x360 @ 32 PPU)
        const float SrcCropY = 0.25f;             // 941 px yükseklik 16:9'a kırpılırken üstten atılan

        // ---------------------------------------------------------------- raf

        // Rafın ön yüzü (dikdörtgen tahta çerçeve), perspektifli: sol kenar uzakta, sağ kenar yakında
        static readonly Vector2 ShelfTL = new Vector2(772f, 193f), ShelfTR = new Vector2(1620f, 111f);
        static readonly Vector2 ShelfBR = new Vector2(1620f, 859f), ShelfBL = new Vector2(772f, 744f);

        public static Homography ShelfFace => Homography.SquareToQuad(ShelfTL, ShelfTR, ShelfBR, ShelfBL);

        /// <summary>
        /// Raf yakın plan görselinin (Reference_Shelf.png, 1672x941 -> 640x360) odadaki raf ön yüzünde
        /// kapladığı alan, ön yüz birim koordinatında. İki görseldeki bölme kenarları ve raf tahtaları
        /// en küçük kareler ile eşleştirilerek bulundu; geçiş efekti düz görseli bu alandan açar.
        /// (Görsel, odadan düzleştirilen rafın yapay zekâyla temiz çizilmesiyle üretildi; bu yüzden
        /// kompozisyonu AI_Inputs/Raf_Duz_Referans.png ile neredeyse aynı.)
        /// </summary>
        public static readonly Rect ShelfViewUv = Rect.MinMaxRect(-0.0134f, -0.0675f, 1.025f, 1.03f);

        // Oyuncak durma noktaları (raf tahtası üst yüzeyi), Shelf_Front'ta (640x360, sol-üst orijin).
        // 3 bölme x 3 slot = 9 sütun, 5 sıra = 45. Tahtalar düz olduğu için her sütunda aynı yükseklik.
        static readonly float[] ViewSlotX = { 57.9f, 121.5f, 185.2f, 259.3f, 320.2f, 381.1f, 456f, 521.3f, 586.7f };
        static readonly float[] ViewRowFloors = { 88.7f, 147.7f, 206.2f, 264.8f, 323.3f };

        public const int ShelfSlotCount = 45;

        /// <summary>Slot i'nin (0 tabanlı; satır satır, soldan sağa) raf yakın planındaki pikseli (sol-üst orijin).</summary>
        static Vector2 ShelfSlotViewPx(int index) => new Vector2(ViewSlotX[index % 9], ViewRowFloors[index / 9]);

        /// <summary>Slotun düzleştirilmiş raf yakın planındaki (ShelfView) yerel konumu.</summary>
        public static Vector2 ShelfSlotViewLocal(int index)
        {
            var px = ShelfSlotViewPx(index);
            return UnitToViewLocal(px.x / 640f, px.y / 360f);
        }

        /// <summary>Aynı slotun oda görselindeki (RoomView) yerel konumu: yakın plandan homografiyle geri eğilir.</summary>
        public static Vector2 ShelfSlotRoomLocal(int index) => ViewPxToRoomLocal(ShelfSlotViewPx(index));

        /// <summary>
        /// Slotta, yakın planda 1 piksel olan şeyin odada kaç piksel göründüğü (x ve y ayrı; perspektif yüzünden
        /// sol bölmelerde daha küçük, yatayda daha sıkışık). Odadaki küçük oyuncak bu ölçekle çizilir.
        /// </summary>
        public static Vector2 ShelfSlotRoomScale(int index)
        {
            const float step = 4f;
            var px = ShelfSlotViewPx(index);
            var origin = ViewPxToRoomLocal(px);
            float sx = (ViewPxToRoomLocal(px + new Vector2(step, 0f)) - origin).magnitude * 32f / step;
            float sy = (ViewPxToRoomLocal(px - new Vector2(0f, step)) - origin).magnitude * 32f / step;
            return new Vector2(sx, sy);
        }

        static Vector2 ViewPxToRoomLocal(Vector2 viewPx)
        {
            float u = ShelfViewUv.xMin + viewPx.x / 640f * ShelfViewUv.width;
            float v = ShelfViewUv.yMin + viewPx.y / 360f * ShelfViewUv.height;
            return SrcToRoomLocal(ShelfFace.Map(u, v));
        }

        /// <summary>Raf yakın plan görselinin oda görselindeki dört köşesi (RoomView yerel): SÜ, SağÜ, SağA, SA.</summary>
        public static Vector2[] ShelfRoomQuad()
        {
            var face = ShelfFace;
            var r = ShelfViewUv;
            return new[]
            {
                SrcToRoomLocal(face.Map(r.xMin, r.yMin)), SrcToRoomLocal(face.Map(r.xMax, r.yMin)),
                SrcToRoomLocal(face.Map(r.xMax, r.yMax)), SrcToRoomLocal(face.Map(r.xMin, r.yMax))
            };
        }

        // ---------------------------------------------------------------- bilgisayar

        // Monitörün koyu cam alanı
        static readonly Vector2 ScreenTL = new Vector2(363f, 404f), ScreenTR = new Vector2(486f, 408f);
        static readonly Vector2 ScreenBR = new Vector2(486f, 507f), ScreenBL = new Vector2(363f, 510f);

        /// <summary>
        /// Bilgisayar yakın plan görselinin oda görselindeki köşeleri. Görseldeki ekran dikdörtgeni
        /// (<see cref="ViewArt.ComputerScreenRect"/>) odadaki cam alanına oturacak şekilde dışa uzatılır.
        /// </summary>
        public static Vector2[] ComputerRoomQuad()
        {
            var screen = Homography.SquareToQuad(ScreenTL, ScreenTR, ScreenBR, ScreenBL);
            var rect = ViewArt.ComputerScreenRect;                     // alttan ölçülü piksel
            float u0 = rect.xMin / 640f, u1 = rect.xMax / 640f;
            float v0 = (360f - rect.yMax) / 360f, v1 = (360f - rect.yMin) / 360f; // üstten
            Vector2 Corner(float u, float v) =>
                SrcToRoomLocal(screen.Map((u - u0) / (u1 - u0), (v - v0) / (v1 - v0)));
            return new[] { Corner(0f, 0f), Corner(1f, 0f), Corner(1f, 1f), Corner(0f, 1f) };
        }

        // ---------------------------------------------------------------- dönüşümler

        public static Vector2 SrcToRoomLocal(Vector2 src) =>
            new Vector2(src.x / SrcPxPerUnit - 10f, 5.625f - (src.y - SrcCropY) / SrcPxPerUnit);

        /// <summary>Tam ekran görünümde birim koordinat (0..1, üstten) -> görünüm yerel birimi.</summary>
        static Vector2 UnitToViewLocal(float u, float v) => new Vector2((u - 0.5f) * 20f, (0.5f - v) * 11.25f);
    }
}
