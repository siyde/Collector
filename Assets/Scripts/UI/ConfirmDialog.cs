using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collector.UI
{
    /// <summary>
    /// Ortada çıkan evet/hayır penceresi (uyku, satın alma onayı...). Enter = evet, ESC / sağ tık / dışarı tıklama = hayır.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        public static ConfirmDialog Instance { get; private set; }

        const float PixelsPerUnit = 32f;
        const int MinWidth = 180, Side = 12, Height = 58;

        [SerializeField] GameObject window;
        [SerializeField] SpriteRenderer panel;
        [SerializeField] PixelTextRenderer message;
        [SerializeField] Transform frame;

        Action onYes;
        Action onNo;

        public bool IsOpen => window.activeSelf;

        public void Configure(GameObject windowRoot, Transform frameRoot, SpriteRenderer panelRenderer, PixelTextRenderer messageText)
        {
            window = windowRoot;
            frame = frameRoot;
            panel = panelRenderer;
            message = messageText;
        }

        void Awake()
        {
            Instance = this;
            window.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string text, Action yes, Action no = null)
        {
            if (IsOpen) return;
            onYes = yes;
            onNo = no;
            message.Text = text;

            int width = Mathf.Max(MinWidth, message.PixelSize.x + Side * 2);
            if (width % 2 == 1) width++;
            panel.size = new Vector2(width, Height) / PixelsPerUnit;
            message.transform.localPosition = new Vector3(0f, (Height * 0.5f - 7f)) / PixelsPerUnit;   // üst kısımda ortalı

            window.SetActive(true);
            UIState.OpenModal();
        }

        /// <summary>Pencere dışına tıklama gibi durumlar için "hayır".</summary>
        public void Cancel() => Answer(false);

        public void Answer(bool yes)
        {
            if (!IsOpen) return;
            window.SetActive(false);
            UIState.CloseModal();
            var callback = yes ? onYes : onNo;
            onYes = onNo = null;
            callback?.Invoke();
        }

        void Update()
        {
            if (!IsOpen) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) Answer(true);
            else if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame))
                Answer(false);
        }
    }
}
