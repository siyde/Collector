using System;
using System.Collections.Generic;
using Collector.Collectibles;
using Collector.Economy;
using Collector.TimeSystem;
using Collector.UI;
using UnityEngine;

namespace Collector.Shop
{
    /// <summary>
    /// Bilgisayardaki ikinci el ilanları. Her gün veritabanındaki oyuncaklardan yeni ilanlar üretir
    /// (aynı gün içinde hep aynı ilanlar: gün numarasıyla tohumlanır). Satın alınca para düşer ve
    /// odaya kargo kutusu gelir. Pazarlık kuralları <see cref="Bargaining"/>'de.
    /// </summary>
    public class ListingsManager : MonoBehaviour
    {
        public static ListingsManager Instance { get; private set; }

        [SerializeField] CollectibleDatabase database;
        [SerializeField] CargoSpawner cargo;
        [SerializeField, Range(1, 4)] int listingsPerDay = 4;
        [Tooltip("Fiyat oynaklığı: 0.2 = taban fiyatın ±%20'si.")]
        [SerializeField, Range(0f, 0.5f)] float priceVariance = 0.2f;

        [SerializeField] string[] sellers =
        {
            "RetroSevda", "OyuncakAvcisi", "Mehmet_1978", "KadikoySahaf", "EskiKasetci",
            "Nostalji_Ayse", "TenekeUsta", "Pazarci_Ali", "KoleksiyonerX", "AtariCocugu"
        };

        [SerializeField] string[] notes =
        {
            "Dedemin tavan arasından çıktı.", "Çocukluğumdan kalma, kutusu yok.", "Az kullanıldı, ufak çizik var.",
            "Takas da olur.", "Acil satılık!", "Koleksiyonumu küçültüyorum.", "Annem atmadan satıyorum.",
            "Sadece elden değil, kargo var.", "Son fiyat, pazarlık yok.", "Bayramda hediye gelmişti."
        };

        [Tooltip("Bu cümleyi içeren notlu satıcılar hiç pazarlık yapmaz.")]
        [SerializeField] string noBargainNote = "pazarlık yok";
        [Tooltip("Satıcıların inebileceği en düşük fiyat oranları (rastgele biri seçilir). 0.7 = %30 indirim.")]
        [SerializeField] float[] flexibility = { 0.70f, 0.78f, 0.85f, 0.92f };

        readonly List<Listing> listings = new List<Listing>();
        DayManager days;

        public IReadOnlyList<Listing> Listings => listings;

        /// <summary>İlanlar yenilenince veya bir ilan satılınca.</summary>
        public event Action Changed;

        public void Configure(CollectibleDatabase collectibleDatabase, CargoSpawner cargoSpawner)
        {
            database = collectibleDatabase;
            cargo = cargoSpawner;
        }

        void Awake() => Instance = this;

        void Start()
        {
            days = DayManager.Instance;
            if (days != null) days.DayStarted += Generate;
            Generate(days != null ? days.Day : 1);
        }

        void OnDestroy()
        {
            if (days != null) days.DayStarted -= Generate;
            if (Instance == this) Instance = null;
        }

        void Generate(int day)
        {
            listings.Clear();
            if (database == null || database.Items.Count == 0) { Changed?.Invoke(); return; }

            var rng = new System.Random(day * 7919 + 17);
            for (int i = 0; i < listingsPerDay; i++)
            {
                var item = database.Items[rng.Next(database.Items.Count)];
                if (item == null) continue;

                // Durum oyuncağın varsayılanından bir kademe aşağı / yukarı sapabilir
                int shifted = Mathf.Clamp((int)item.Condition + rng.Next(-1, 2), 0, (int)ItemCondition.Mint);
                var condition = (ItemCondition)shifted;
                int price = Bargaining.BasePrice(item, condition, rng, priceVariance);
                string note = notes[rng.Next(notes.Length)];
                float flex = note.ToLowerInvariant().Contains(noBargainNote) ? 1f : flexibility[rng.Next(flexibility.Length)];

                listings.Add(Bargaining.CreateListing(item, condition, price, flex, sellers[rng.Next(sellers.Length)], note, rng));
            }
            Changed?.Invoke();
        }

        /// <summary>Onay alındıktan sonra çağrılır. Para yetmezse false döner ve bildirim gösterir.</summary>
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
            cargo?.Spawn(listing.Item, listing.Condition);
            ToastMessage.Instance?.Show("Satın alındı! Kargo kutusu odana geldi.");
            Changed?.Invoke();
            return true;
        }
    }
}
