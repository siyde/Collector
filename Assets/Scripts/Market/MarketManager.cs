using System;
using System.Collections.Generic;
using Collector.Collectibles;
using Collector.Economy;
using Collector.Inventory;
using Collector.Shop;
using Collector.TimeSystem;
using Collector.UI;
using UnityEngine;

namespace Collector.Market
{
    /// <summary>
    /// Bit pazarı: yalnızca Pazar günleri kurulur. Her Pazar tezgahlara yeni oyuncaklar dizilir
    /// (internetten ucuz ama durumları genelde daha kötü). Her tezgahçının pazarlık huyu farklıdır.
    /// Satın alınan oyuncak kargo beklemeden doğrudan envantere girer.
    /// </summary>
    public class MarketManager : MonoBehaviour
    {
        public static MarketManager Instance { get; private set; }

        public const int StallCount = 3, ItemsPerStall = 3;

        [SerializeField] CollectibleDatabase database;
        [Tooltip("Tezgahlardaki oyuncak yerleri: tezgah 1'in 3 yeri, sonra tezgah 2, tezgah 3.")]
        [SerializeField] MarketItem[] items = new MarketItem[0];
        [SerializeField] string[] stallOwners = { "Hasan Amca", "Nermin Teyze", "Kaan" };
        [Tooltip("Tezgahçıların inebileceği en düşük fiyat oranı (1 = hiç inmez).")]
        [SerializeField] float[] stallFlexibility = { 0.65f, 0.8f, 0.95f };
        [Tooltip("İnternet fiyatına göre çarpan (0.8 = %20 ucuz).")]
        [SerializeField, Range(0.3f, 1.5f)] float priceFactor = 0.8f;
        [SerializeField, Range(0f, 0.5f)] float priceVariance = 0.25f;

        [SerializeField] string[] notes =
        {
            "Hepsi dedemden kalma.", "Bakmak bedava!", "Sabah erkenden geldin, iyi fiyat yaparım.",
            "Tavan arasını boşalttık.", "Kutusu yok ama sağlam.", "Toplu alana indirim var.", "Bunlar çok arandı."
        };

        readonly List<Listing> stock = new List<Listing>();
        DayManager days;

        public bool IsOpenToday => days == null || days.Weekday == Weekday.Pazar;

        /// <summary>Bir sonraki Pazar'a kaç gün var (bugün Pazar ise 0).</summary>
        public int DaysUntilOpen => days == null ? 0 : ((int)Weekday.Pazar - (int)days.Weekday + 7) % 7;

        public event Action Changed;

        public void Configure(CollectibleDatabase collectibleDatabase, MarketItem[] marketItems)
        {
            database = collectibleDatabase;
            items = marketItems;
        }

        public string StallName(int stall) => stall >= 0 && stall < stallOwners.Length ? stallOwners[stall] : "Tezgah";

        void Awake() => Instance = this;

        void Start()
        {
            days = DayManager.Instance;
            if (days != null) days.DayStarted += OnDayStarted;
            OnDayStarted(days != null ? days.Day : 1);
        }

        void OnDestroy()
        {
            if (days != null) days.DayStarted -= OnDayStarted;
            if (Instance == this) Instance = null;
        }

        void OnDayStarted(int day)
        {
            stock.Clear();
            if (IsOpenToday && database != null && database.Items.Count > 0)
            {
                var rng = new System.Random(day * 104729 + 3);
                for (int s = 0; s < StallCount; s++)
                    for (int k = 0; k < ItemsPerStall; k++)
                        stock.Add(CreateStock(s, rng));
            }
            for (int i = 0; i < items.Length; i++)
                items[i].Bind(i < stock.Count ? stock[i] : null, i / ItemsPerStall);
            Changed?.Invoke();
        }

        Listing CreateStock(int stall, System.Random rng)
        {
            var item = database.Items[rng.Next(database.Items.Count)];
            // Bit pazarında oyuncaklar genelde daha yıpranmış: -2..+1 kademe
            int shifted = Mathf.Clamp((int)item.Condition + rng.Next(-2, 2), 0, (int)ItemCondition.Mint);
            var condition = (ItemCondition)shifted;
            int price = Bargaining.RoundTo5(Bargaining.BasePrice(item, condition, rng, priceVariance) * priceFactor);
            float flex = stall < stallFlexibility.Length ? stallFlexibility[stall] : 0.85f;
            return Bargaining.CreateListing(item, condition, price, flex, StallName(stall), notes[rng.Next(notes.Length)], rng);
        }

        /// <summary>Onay alındıktan sonra çağrılır: para düşer, oyuncak doğrudan envantere girer.</summary>
        public bool Purchase(Listing listing)
        {
            if (listing == null || listing.Sold) return false;
            var wallet = Wallet.Instance;
            if (wallet == null || !wallet.TrySpend(listing.CurrentPrice))
            {
                ToastMessage.Instance?.Show("Yeterli paran yok (" + Wallet.Format(listing.CurrentPrice) + ")");
                return false;
            }

            listing.Sold = true;
            InventoryManager.Instance?.Add(listing.Item, listing.Condition);
            ToastMessage.Instance?.Show("Envantere eklendi: " + listing.Item.ItemName);
            foreach (var item in items) item.Refresh();
            Changed?.Invoke();
            return true;
        }
    }
}
