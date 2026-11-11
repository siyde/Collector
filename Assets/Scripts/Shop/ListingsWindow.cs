using Collector.Collectibles;
using Collector.Economy;
using Collector.UI;
using Collector.ViewSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collector.Shop
{
    /// <summary>
    /// Bilgisayar ekranındaki "İkinci El İlanları" penceresi: ilan listesi ve ilan sayfası (pazarlık).
    /// Masaüstündeki İLANLAR simgesi açar; ESC / sağ tık önce ilan sayfasından listeye döner, sonra kapatır.
    /// Açıkken pencere sayılır (ESC bilgisayardan çıkarmaz).
    /// </summary>
    public class ListingsWindow : ListingHost
    {
        [SerializeField] GameObject window;
        [SerializeField] GameObject listPage;
        [SerializeField] ListingDetailUI detailPage;
        [SerializeField] ListingRowUI[] rows;
        [SerializeField] PixelTextRenderer balanceText;
        [SerializeField] PixelTextRenderer dayText;
        [SerializeField] GameObject emptyMessage;

        ListingsManager listings;
        Wallet wallet;

        public bool IsOpen => window.activeSelf;
        bool InDetail => detailPage.gameObject.activeSelf;

        public void Configure(GameObject windowRoot, GameObject list, ListingDetailUI detail, ListingRowUI[] listingRows,
            PixelTextRenderer balance, PixelTextRenderer day, GameObject empty)
        {
            window = windowRoot;
            listPage = list;
            detailPage = detail;
            rows = listingRows;
            balanceText = balance;
            dayText = day;
            emptyMessage = empty;
        }

        /// <summary>Açık zemin üzerinde okunsun diye nadirlik rengi biraz koyulaştırılır.</summary>
        public static Color32 RarityColorOnLight(Rarity rarity) =>
            Color32.Lerp(rarity.ToColor(), new Color32(0, 0, 0, 255), 0.35f);

        void Start()
        {
            listings = ListingsManager.Instance;
            wallet = Wallet.Instance;
            if (listings != null) listings.Changed += Refresh;
            if (wallet != null) wallet.Changed += OnBalanceChanged;
            if (ViewManager.Instance != null) ViewManager.Instance.ViewChanged += OnViewChanged;
            window.SetActive(false);
        }

        void OnDestroy()
        {
            if (listings != null) listings.Changed -= Refresh;
            if (wallet != null) wallet.Changed -= OnBalanceChanged;
            if (ViewManager.Instance != null) ViewManager.Instance.ViewChanged -= OnViewChanged;
        }

        public void Open()
        {
            if (IsOpen || UIState.IsModalOpen) return;
            window.SetActive(true);
            UIState.OpenModal();
            ShowList();
        }

        public void Close()
        {
            if (!IsOpen) return;
            window.SetActive(false);
            UIState.CloseModal();
        }

        public void ShowList()
        {
            detailPage.gameObject.SetActive(false);
            listPage.SetActive(true);
            Refresh();
        }

        public void ShowDetail(int index)
        {
            if (listings == null || index >= listings.Listings.Count) return;
            listPage.SetActive(false);
            detailPage.gameObject.SetActive(true);
            detailPage.Bind(listings.Listings[index]);
            Refresh();
        }

        void Update()
        {
            if (!IsOpen) return;
            if (ConfirmDialog.Instance != null && ConfirmDialog.Instance.IsOpen) return;   // önce onay penceresi
            if (UIState.ChangedThisFrame) return;                                        // onay bu karede kapandıysa ESC ona aitti
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame))
            {
                if (InDetail) ShowList();
                else Close();
            }
        }

        // Güvenlik: bilgisayardan başka yoldan çıkılırsa pencere kapansın
        void OnViewChanged(ViewId view)
        {
            if (view != ViewId.Computer) Close();
        }

        public void OnBuy(int index)
        {
            if (listings != null && index < listings.Listings.Count) BuyListing(listings.Listings[index]);
        }

        public override void BuyListing(Listing listing)
        {
            if (listing == null || listing.Sold) return;
            if (wallet != null && !wallet.CanAfford(listing.CurrentPrice))
            {
                ToastMessage.Instance?.Show("Yeterli paran yok (" + Wallet.Format(listing.CurrentPrice) + ")");
                return;
            }

            var question = $"{listing.Item.ItemName}, {Wallet.Format(listing.CurrentPrice)}. Satın alınsın mı?";
            if (ConfirmDialog.Instance != null) ConfirmDialog.Instance.Show(question, () => listings.Purchase(listing));
            else listings.Purchase(listing);
        }

        public override void CloseDetail() => ShowList();

        protected override void OnListingChanged() => Refresh();

        void OnBalanceChanged(int balance) => Refresh();

        void Refresh()
        {
            if (!IsOpen || listings == null) return;
            for (int i = 0; i < rows.Length; i++)
                rows[i].Bind(i < listings.Listings.Count ? listings.Listings[i] : null);
            emptyMessage.SetActive(listings.Listings.Count == 0 && !InDetail);
            if (InDetail) detailPage.Refresh();
            if (wallet != null) balanceText.Text = "Paran: " + Wallet.Format(wallet.Balance);
            var days = Collector.TimeSystem.DayManager.Instance;
            if (days != null) dayText.Text = "Gün " + days.Day;
        }
    }
}
