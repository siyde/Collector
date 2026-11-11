using Collector.TimeSystem;
using Collector.UI;
using Collector.ViewSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collector.Market
{
    /// <summary>
    /// Şehir haritası (M): Evin ve Bit Pazarı. Bulunulan yerin üstünde "BURADASIN" yazar; bit pazarı
    /// yalnızca Pazar açıktır. M / ESC / sağ tık / dışarı tıklama kapatır.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        public static MapUI Instance { get; private set; }

        [SerializeField] GameObject window;
        [SerializeField] GameObject homeMarker;
        [SerializeField] GameObject marketMarker;
        [SerializeField] PixelTextRenderer marketStatus;

        public bool IsOpen => window.activeSelf;

        public void Configure(GameObject windowRoot, GameObject home, GameObject market, PixelTextRenderer status)
        {
            window = windowRoot;
            homeMarker = home;
            marketMarker = market;
            marketStatus = status;
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

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (ViewManager.Instance != null && ViewManager.Instance.IsTransitioning) return;

            if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
                return;
            }

            if (IsOpen && !UIState.ChangedThisFrame &&
                ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame)))
                Close();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || UIState.IsModalOpen) return;
            var current = ViewManager.Instance != null ? ViewManager.Instance.Current : ViewId.Room;
            homeMarker.SetActive(current != ViewId.Market);
            marketMarker.SetActive(current == ViewId.Market);

            var market = MarketManager.Instance;
            if (market == null || market.IsOpenToday) marketStatus.Text = "Bugün açık!";
            else marketStatus.Text = $"Pazar ({market.DaysUntilOpen} gün sonra)";

            window.SetActive(true);
            UIState.OpenModal();
        }

        public void Close()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            UIState.CloseModal();
        }

        public void GoHome()
        {
            Close();
            ViewManager.Instance?.Travel(ViewId.Room);
        }

        public void GoMarket()
        {
            var market = MarketManager.Instance;
            if (market != null && !market.IsOpenToday)
            {
                ToastMessage.Instance?.Show($"Bit pazarı sadece Pazar kurulur ({market.DaysUntilOpen} gün sonra)");
                return;
            }
            Close();
            ViewManager.Instance?.Travel(ViewId.Market);
        }
    }
}
