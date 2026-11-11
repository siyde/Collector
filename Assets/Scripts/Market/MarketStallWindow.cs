using Collector.Economy;
using Collector.Shop;
using Collector.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collector.Market
{
    /// <summary>
    /// Bit pazarında oyuncağa tıklayınca açılan tezgah penceresi: ilan sayfası ve yüz yüze pazarlık.
    /// Satın alınca pencere kapanır. [X] / ESC / sağ tık / dışarı tıklama kapatır.
    /// </summary>
    public class MarketStallWindow : ListingHost
    {
        public static MarketStallWindow Instance { get; private set; }

        [SerializeField] GameObject window;
        [SerializeField] PixelTextRenderer title;
        [SerializeField] PixelTextRenderer balanceText;
        [SerializeField] ListingDetailUI detail;

        public bool IsOpen => window.activeSelf;

        public void Configure(GameObject windowRoot, PixelTextRenderer titleText, PixelTextRenderer balance, ListingDetailUI detailPage)
        {
            window = windowRoot;
            title = titleText;
            balanceText = balance;
            detail = detailPage;
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

        public void Show(Listing listing, string stallName)
        {
            if (IsOpen || UIState.IsModalOpen) return;
            title.Text = stallName + " - Tezgah";
            window.SetActive(true);
            UIState.OpenModal();
            detail.gameObject.SetActive(true);
            detail.Bind(listing);
            RefreshBalance();
        }

        public void Close()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            UIState.CloseModal();
        }

        void Update()
        {
            if (!IsOpen) return;
            if (ConfirmDialog.Instance != null && ConfirmDialog.Instance.IsOpen) return;
            if (UIState.ChangedThisFrame) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame))
                Close();
        }

        public override void BuyListing(Listing listing)
        {
            if (listing == null || listing.Sold) return;
            var wallet = Wallet.Instance;
            if (wallet != null && !wallet.CanAfford(listing.CurrentPrice))
            {
                ToastMessage.Instance?.Show("Yeterli paran yok (" + Wallet.Format(listing.CurrentPrice) + ")");
                return;
            }

            var question = $"{listing.Item.ItemName}, {Wallet.Format(listing.CurrentPrice)}. Alınsın mı?";
            ConfirmDialog.Instance?.Show(question, () =>
            {
                if (MarketManager.Instance != null && MarketManager.Instance.Purchase(listing)) Close();
            });
        }

        public override void CloseDetail() => Close();

        protected override void OnListingChanged()
        {
            detail.Refresh();
            RefreshBalance();
        }

        void RefreshBalance()
        {
            if (Wallet.Instance != null) balanceText.Text = "Paran: " + Wallet.Format(Wallet.Instance.Balance);
        }
    }
}
