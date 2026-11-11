using UnityEngine;

namespace Collector.UI
{
    /// <summary>
    /// Ekranın üst ortasında birkaç saniye görünen bildirim ("Envantere eklendi: ..." vb.).
    /// Kök objenin konumu kutunun üst-ORTA noktasıdır.
    /// </summary>
    public class ToastMessage : MonoBehaviour
    {
        public static ToastMessage Instance { get; private set; }

        const float PixelsPerUnit = 32f;
        const int SidePadding = 6, Border = 3;

        [SerializeField] GameObject box;
        [SerializeField] SpriteRenderer panel;
        [SerializeField] PixelTextRenderer text;
        [SerializeField, Min(0.5f)] float duration = 2.5f;

        float hideAt;
        readonly System.Collections.Generic.Queue<(string text, float seconds)> queue =
            new System.Collections.Generic.Queue<(string, float)>();

        public void Configure(GameObject boxRoot, SpriteRenderer panelRenderer, PixelTextRenderer textRenderer)
        {
            box = boxRoot;
            panel = panelRenderer;
            text = textRenderer;
        }

        void Awake()
        {
            Instance = this;
            box.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Bildirimi gösterir; ekranda başka bildirim varsa sıraya alınır.</summary>
        public void Show(string message, float seconds = -1f)
        {
            if (box.activeSelf) { queue.Enqueue((message, seconds)); return; }
            Display(message, seconds);
        }

        void Display(string message, float seconds)
        {
            text.Text = message;
            int width = text.PixelSize.x + SidePadding * 2;
            int height = PixelFont.GlyphHeight + Border * 2 + 1;
            if (width % 2 == 1) width++;                                           // ortalanınca piksele otursun

            panel.size = new Vector2(width, height) / PixelsPerUnit;
            panel.transform.localPosition = new Vector3(0f, -height * 0.5f) / PixelsPerUnit;
            text.transform.localPosition = new Vector3(-width * 0.5f + SidePadding, -Border) / PixelsPerUnit;

            box.SetActive(true);
            hideAt = Time.unscaledTime + (seconds > 0f ? seconds : duration);
        }

        void Update()
        {
            if (!box.activeSelf || Time.unscaledTime < hideAt) return;
            if (queue.Count > 0)
            {
                var (message, seconds) = queue.Dequeue();
                Display(message, seconds);
            }
            else box.SetActive(false);
        }
    }
}
