using System.Collections;
using UnityEngine;

namespace Collector.UI
{
    /// <summary>Tüm ekranı karartan perde ve ortasında başlık (ör. "2. GÜN / CUMARTESİ").</summary>
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] SpriteRenderer curtain;
        [SerializeField] GameObject titleRoot;
        [SerializeField] PixelTextRenderer title;
        [SerializeField] PixelTextRenderer subtitle;
        [SerializeField] Color curtainColor = new Color(0.03f, 0.02f, 0.06f, 1f);

        public void Configure(SpriteRenderer curtainRenderer, GameObject titleObject, PixelTextRenderer titleText, PixelTextRenderer subtitleText)
        {
            curtain = curtainRenderer;
            titleRoot = titleObject;
            title = titleText;
            subtitle = subtitleText;
        }

        void Awake()
        {
            Instance = this;
            SetAlpha(0f);
            titleRoot.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Perdeyi hedef saydamlığa getirir (1 = tamamen karanlık).</summary>
        public IEnumerator Fade(float target, float seconds)
        {
            float start = curtain.color.a;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                // Pixel art'a uygun: yumuşak geçiş yerine 6 kademe
                SetAlpha(Mathf.Round(Mathf.Lerp(start, target, t) * 6f) / 6f);
                yield return null;
            }
            SetAlpha(target);
        }

        public void ShowTitle(string text, string subtext)
        {
            title.Text = text;
            subtitle.Text = subtext;
            titleRoot.SetActive(true);
        }

        public void HideTitle() => titleRoot.SetActive(false);

        void SetAlpha(float alpha)
        {
            curtain.color = new Color(curtainColor.r, curtainColor.g, curtainColor.b, alpha);
            curtain.enabled = alpha > 0f;
        }
    }
}
