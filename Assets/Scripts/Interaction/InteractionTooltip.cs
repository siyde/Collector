using Collector.UI;
using UnityEngine;

namespace Collector.Interaction
{
    /// <summary>
    /// Farenin yanında çıkan küçük etiket: üstte nesne adı (sarı), altta "TIKLA: eylem" (krem).
    /// Kök objenin konumu kutunun sol-üst köşesidir; ekrandan taşarsa imlecin öbür tarafına geçer.
    /// </summary>
    public class InteractionTooltip : MonoBehaviour
    {
        const float PixelsPerUnit = 32f;
        const int Border = 3;       // UI_Panel çerçeve kalınlığı
        const int SidePadding = 5;
        const int CursorOffsetX = 10, CursorOffsetY = 12;

        [SerializeField] SpriteRenderer panel;
        [SerializeField] PixelTextRenderer title;
        [SerializeField] PixelTextRenderer action;

        Vector2Int size;

        public void Configure(SpriteRenderer panelRenderer, PixelTextRenderer titleText, PixelTextRenderer actionText)
        {
            panel = panelRenderer;
            title = titleText;
            action = actionText;
        }

        public void Show(IInteractable target)
        {
            title.Text = target.DisplayName;
            action.Text = string.IsNullOrEmpty(target.ActionText) ? string.Empty : "Tıkla: " + target.ActionText;
            Layout();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Kutuyu imlecin sağ altına koyar; görünür alanın dışına taşmasını engeller.</summary>
        public void MoveTo(Vector2 pointer, Rect visible)
        {
            float w = size.x / PixelsPerUnit, h = size.y / PixelsPerUnit;
            float x = pointer.x + CursorOffsetX / PixelsPerUnit;
            float y = pointer.y - CursorOffsetY / PixelsPerUnit;
            if (x + w > visible.xMax) x = pointer.x - w - 4f / PixelsPerUnit;
            if (y - h < visible.yMin) y = pointer.y + h + 4f / PixelsPerUnit;
            transform.position = new Vector3(Snap(x), Snap(y), transform.position.z);
        }

        void Layout()
        {
            bool hasAction = !string.IsNullOrEmpty(action.Text);
            action.gameObject.SetActive(hasAction);
            int textW = Mathf.Max(title.PixelSize.x, hasAction ? action.PixelSize.x : 0);
            int textH = hasAction ? PixelFont.LineHeight + PixelFont.GlyphHeight : PixelFont.GlyphHeight;
            // Harflerin üstündeki 3 satır (aksan + boşluk) iç boşluk görevi görür; İ/Ö/Ü noktaları çerçeveye taşmaz
            size = new Vector2Int(textW + SidePadding * 2, textH + Border * 2 + 1);

            panel.size = (Vector2)size / PixelsPerUnit;
            panel.transform.localPosition = new Vector3(size.x * 0.5f, -size.y * 0.5f) / PixelsPerUnit;
            title.transform.localPosition = new Vector3(SidePadding, -Border) / PixelsPerUnit;
            action.transform.localPosition = new Vector3(SidePadding, -Border - PixelFont.LineHeight) / PixelsPerUnit;
        }

        static float Snap(float v) => Mathf.Round(v * PixelsPerUnit) / PixelsPerUnit;
    }
}
