using UnityEngine;

namespace Collector.ViewSystem
{
    /// <summary>
    /// Tam ekran bir görünüm (oda, raf yakın planı, bilgisayar...). Kamera görünüme geçince
    /// bu objenin konumuna gider.
    /// Yakın plan görünümlerde <see cref="roomQuad"/>, bu görünümün arka planının oda görselinde
    /// kapladığı dörtgendir: yaklaşma efekti düz görseli bu dörtgenden tam ekrana açar.
    /// </summary>
    public class GameView : MonoBehaviour
    {
        [SerializeField] ViewId id;
        [SerializeField] SpriteRenderer background;

        [Tooltip("Arka plan görselinin oda görselindeki köşeleri (RoomView'e göre yerel): Sol-Üst, Sağ-Üst, Sağ-Alt, Sol-Alt. " +
                 "Scene görünümünde objeyi seçince sarı çizgilerle görünür.")]
        [SerializeField] Vector2[] roomQuad = new Vector2[4];

        public ViewId Id => id;
        public SpriteRenderer Background => background;
        public Vector2[] RoomQuad => roomQuad;
        public bool HasRoomQuad => roomQuad != null && roomQuad.Length == 4 && roomQuad[0] != roomQuad[2];

        public void Configure(ViewId viewId, SpriteRenderer backgroundRenderer, Vector2[] quad)
        {
            id = viewId;
            background = backgroundRenderer;
            roomQuad = quad;
        }

        void OnDrawGizmosSelected()
        {
            if (!HasRoomQuad) return;
            var room = FindRoomOrigin();
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(room + (Vector3)roomQuad[i], room + (Vector3)roomQuad[(i + 1) % 4]);
        }

        Vector3 FindRoomOrigin()
        {
            foreach (var view in FindObjectsByType<GameView>(FindObjectsSortMode.None))
                if (view.id == ViewId.Room) return view.transform.position;
            return Vector3.zero;
        }
    }
}
