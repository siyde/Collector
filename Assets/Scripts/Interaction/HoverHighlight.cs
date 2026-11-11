using UnityEngine;

namespace Collector.Interaction
{
    /// <summary>
    /// Üzerine gelinen nesnenin çevresinde dört köşe işareti; retro seçim imleci gibi 1 piksel
    /// içeri-dışarı atar. Köşeler piksel ızgarasına oturtulur.
    /// </summary>
    public class HoverHighlight : MonoBehaviour
    {
        const float PixelsPerUnit = 32f;

        [Tooltip("Sol-Üst, Sağ-Üst, Sağ-Alt, Sol-Alt")]
        [SerializeField] SpriteRenderer[] corners = new SpriteRenderer[4];
        [SerializeField, Min(0.05f)] float pulseInterval = 0.3f;

        Bounds target;

        public void Configure(SpriteRenderer[] cornerRenderers) => corners = cornerRenderers;

        public void Show(Bounds bounds)
        {
            target = bounds;
            gameObject.SetActive(true);
            Place();
        }

        public void Hide() => gameObject.SetActive(false);

        void Update() => Place();

        void Place()
        {
            int pulse = (int)(Time.unscaledTime / pulseInterval) % 2;
            float gap = (1 + pulse) / PixelsPerUnit;
            var min = target.min;
            var max = target.max;
            Vector2 ext = corners[0].sprite.bounds.extents;

            SetCorner(0, min.x - gap + ext.x, max.y + gap - ext.y);
            SetCorner(1, max.x + gap - ext.x, max.y + gap - ext.y);
            SetCorner(2, max.x + gap - ext.x, min.y - gap + ext.y);
            SetCorner(3, min.x - gap + ext.x, min.y - gap + ext.y);
        }

        void SetCorner(int index, float x, float y)
        {
            corners[index].transform.position = new Vector3(Snap(x), Snap(y), transform.position.z);
        }

        static float Snap(float v) => Mathf.Round(v * PixelsPerUnit) / PixelsPerUnit;
    }
}
