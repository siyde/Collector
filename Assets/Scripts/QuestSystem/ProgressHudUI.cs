using Collector.UI;
using UnityEngine;

namespace Collector.QuestSystem
{
    /// <summary>Sağ üstteki "KOLEKSİYONCU / LV. 1 [XP çubuğu] 0/50" göstergesi. Çubuk yavaşça dolar.</summary>
    public class ProgressHudUI : MonoBehaviour
    {
        const float PixelsPerUnit = 32f;

        [SerializeField] PixelTextRenderer levelText;
        [SerializeField] PixelTextRenderer xpText;
        [SerializeField] SpriteRenderer barFill;
        [Tooltip("Çubuğun iç kısmının sol kenarı ve dikey merkezi (ebeveyne göre piksel, aşağı pozitif).")]
        [SerializeField] Vector2 barOriginPx;
        [SerializeField] int barWidth = 60;
        [SerializeField] int barHeight = 3;
        [SerializeField, Min(1f)] float fillSpeed = 60f;   // piksel/saniye

        PlayerProgress progress;
        float shownFill;
        int targetFill;
        int shownLevel;

        public void Configure(PixelTextRenderer level, PixelTextRenderer xp, SpriteRenderer fill, Vector2 originPx, int width, int height)
        {
            levelText = level;
            xpText = xp;
            barFill = fill;
            barOriginPx = originPx;
            barWidth = width;
            barHeight = height;
        }

        void Start()
        {
            progress = PlayerProgress.Instance;
            if (progress == null) { enabled = false; return; }
            progress.Changed += Refresh;
            shownLevel = progress.Level;
            Refresh();
            shownFill = targetFill;
            ApplyFill();
        }

        void OnDestroy()
        {
            if (progress != null) progress.Changed -= Refresh;
        }

        void Refresh()
        {
            levelText.Text = "Lv. " + progress.Level;
            xpText.Text = progress.Xp + "/" + progress.XpToNextLevel;
            if (progress.Level != shownLevel) { shownLevel = progress.Level; shownFill = 0f; }  // seviye atlayınca baştan dolar
            targetFill = Mathf.RoundToInt(barWidth * progress.Xp / (float)progress.XpToNextLevel);
        }

        void Update()
        {
            if (Mathf.Approximately(shownFill, targetFill)) return;
            shownFill = Mathf.MoveTowards(shownFill, targetFill, fillSpeed * Time.unscaledDeltaTime);
            ApplyFill();
        }

        void ApplyFill()
        {
            int width = Mathf.RoundToInt(shownFill);
            barFill.enabled = width > 0;
            barFill.transform.localScale = new Vector3(width, barHeight, 1f);               // UI_Pixel = 1 piksel
            barFill.transform.localPosition = new Vector3(barOriginPx.x + width * 0.5f, -barOriginPx.y) / PixelsPerUnit;
        }
    }
}
