using Collector.Collectibles;

namespace Collector.Shop
{
    /// <summary>
    /// İkinci el sitesindeki tek ilan. Pazarlık için satıcının gizli en düşük fiyatı ve sabrı vardır;
    /// kabul edilen / karşı teklifle düşen fiyat <see cref="CurrentPrice"/>'tır.
    /// </summary>
    public class Listing
    {
        public CollectibleItemData Item { get; }
        public ItemCondition Condition { get; }
        public string Seller { get; }
        public string Note { get; }

        public int OriginalPrice { get; }
        /// <summary>Şu anki satış fiyatı (pazarlıkla düşebilir).</summary>
        public int CurrentPrice { get; set; }
        /// <summary>Satıcının inebileceği en düşük fiyat (oyuncu görmez).</summary>
        public int MinPrice { get; }
        /// <summary>Satıcının kaç kötü teklife daha katlanacağı.</summary>
        public int Patience { get; set; }

        public bool Sold { get; set; }
        public bool Agreed { get; set; }
        public bool NegotiationClosed { get; set; }
        public string LastReply { get; set; }

        public bool CanNegotiate => !Sold && !NegotiationClosed && !Agreed;

        public Listing(CollectibleItemData item, ItemCondition condition, int price, int minPrice, int patience,
            string seller, string note, string greeting)
        {
            Item = item;
            Condition = condition;
            OriginalPrice = CurrentPrice = price;
            MinPrice = minPrice;
            Patience = patience;
            Seller = seller;
            Note = note;
            LastReply = greeting;
        }
    }
}
