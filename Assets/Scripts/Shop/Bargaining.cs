using Collector.Collectibles;
using Collector.Economy;
using UnityEngine;

namespace Collector.Shop
{
    /// <summary>
    /// Fiyatlama ve pazarlık kuralları (internet ilanları ve bit pazarı ortak kullanır).
    /// Satıcının gizli en düşük fiyatı ve sabrı vardır:
    /// en düşük fiyatın üstü kabul, biraz altı karşı teklif, çok altı ters cevap; sabır bitince pazarlık kapanır.
    /// </summary>
    public static class Bargaining
    {
        static readonly string[] AcceptReplies = { "Tamam, {0} olsun. Anlaştık!", "Peki, senin olsun: {0}.", "Hadi bakalım, {0}. Anlaştık." };
        static readonly string[] CounterReplies = { "{0} olmaz ama {1} veririm.", "Biraz daha yaklaş: {1}.", "{1} son, daha inmem gibi." };
        static readonly string[] LowballReplies = { "Şaka mı yapıyorsun?", "Bu fiyata olmaz.", "Emek var bunda, biraz ciddi ol." };
        static readonly string[] Greetings = { "Merhaba! Teklifin varsa söyle.", "İlgileniyorsan teklif ver.", "Selam, fiyatta konuşuruz." };
        const string ClosedReply = "Pazarlığı bitirdim, fiyat belli.";
        const string StubbornReply = "Dedim ya, son fiyat.";
        public const string StubbornGreeting = "Son fiyat, pazarlık yok.";

        public static int RoundTo5(float value) => Mathf.Max(5, Mathf.RoundToInt(value / 5f) * 5);

        /// <summary>Taban fiyat: nadirlik x durum, ± <paramref name="variance"/> oynaklık, 5 TL'ye yuvarlı.</summary>
        public static int BasePrice(CollectibleItemData item, ItemCondition condition, System.Random rng, float variance)
        {
            float baseCost = item.Rarity switch
            {
                Rarity.Common => 25f, Rarity.Uncommon => 60f, Rarity.Rare => 140f, Rarity.Epic => 260f, _ => 500f
            };
            float conditionFactor = condition switch
            {
                ItemCondition.Poor => 0.5f, ItemCondition.Used => 0.75f, ItemCondition.Good => 1f, ItemCondition.VeryGood => 1.3f, _ => 1.8f
            };
            float swing = 1f + ((float)rng.NextDouble() * 2f - 1f) * variance;
            return RoundTo5(baseCost * conditionFactor * swing);
        }

        /// <summary>Satıcı kişiliğiyle ilan oluşturur (esneklik: en düşük fiyat / fiyat; 1 = hiç inmez).</summary>
        public static Listing CreateListing(CollectibleItemData item, ItemCondition condition, int price, float flexibility,
            string seller, string note, System.Random rng)
        {
            bool stubborn = flexibility >= 0.999f;
            int minPrice = Mathf.Min(price, RoundTo5(price * flexibility));
            int patience = stubborn ? 1 : rng.Next(2, 5);
            string greeting = stubborn ? StubbornGreeting : Greetings[rng.Next(Greetings.Length)];
            return new Listing(item, condition, price, minPrice, patience, seller, note, greeting);
        }

        /// <summary>Teklifi değerlendirir, ilanı günceller ve satıcının cevabını döner.</summary>
        public static string Negotiate(Listing listing, int offer)
        {
            if (listing == null || listing.Sold) return "";
            string reply;

            if (!listing.CanNegotiate)
                reply = listing.MinPrice >= listing.OriginalPrice ? StubbornReply : ClosedReply;
            else if (offer >= listing.CurrentPrice)
            {
                listing.Agreed = true;
                reply = string.Format(AcceptReplies[0], Wallet.Format(listing.CurrentPrice));
            }
            else if (offer >= listing.MinPrice)
            {
                listing.CurrentPrice = offer;
                listing.Agreed = true;
                reply = string.Format(AcceptReplies[Random.Range(0, AcceptReplies.Length)], Wallet.Format(offer));
            }
            else
            {
                listing.Patience--;
                if (listing.MinPrice >= listing.OriginalPrice)
                    reply = StubbornReply;
                else if (offer >= listing.MinPrice * 0.85f)
                {
                    // Karşı teklif: en düşük fiyatla şu anki fiyatın ortası, en düşük fiyatın altına inmez
                    int counter = Mathf.Max(listing.MinPrice, RoundTo5((listing.MinPrice + listing.CurrentPrice) * 0.5f));
                    if (counter < listing.CurrentPrice) listing.CurrentPrice = counter;
                    reply = string.Format(CounterReplies[Random.Range(0, CounterReplies.Length)],
                        Wallet.Format(offer), Wallet.Format(listing.CurrentPrice));
                }
                else
                    reply = LowballReplies[Random.Range(0, LowballReplies.Length)];

                if (listing.Patience <= 0)
                {
                    listing.NegotiationClosed = true;
                    reply += " Pazarlık bitti.";
                }
            }

            listing.LastReply = reply;
            return reply;
        }
    }
}
