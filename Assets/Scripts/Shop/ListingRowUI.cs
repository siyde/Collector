using Collector.Collectibles;
using Collector.Economy;
using Collector.UI;
using UnityEngine;

namespace Collector.Shop
{
    /// <summary>
    /// İlan listesindeki tek satır: ikon, ad, nadirlik, durum, satıcı notu, fiyat ve SATIN AL.
    /// Satırın kendisine tıklamak ilan sayfasını açar.
    /// </summary>
    public class ListingRowUI : MonoBehaviour
    {
        const int MaxNameWidth = 150, MaxNoteWidth = 168;

        [SerializeField] ListingsWindow owner;
        [SerializeField] int index;
        [SerializeField] SpriteRenderer icon;
        [SerializeField] PixelTextRenderer nameText;
        [SerializeField] PixelTextRenderer rarityText;
        [SerializeField] PixelTextRenderer conditionText;
        [SerializeField] PixelTextRenderer noteText;
        [SerializeField] PixelTextRenderer priceText;
        [SerializeField] PixelTextRenderer sellerText;
        [SerializeField] GameObject buyButton;
        [SerializeField] GameObject soldLabel;

        public void Configure(ListingsWindow window, int rowIndex, SpriteRenderer iconRenderer, PixelTextRenderer itemName,
            PixelTextRenderer rarity, PixelTextRenderer condition, PixelTextRenderer note, PixelTextRenderer price,
            PixelTextRenderer seller, GameObject buy, GameObject sold)
        {
            owner = window; index = rowIndex; icon = iconRenderer; nameText = itemName; rarityText = rarity;
            conditionText = condition; noteText = note; priceText = price; sellerText = seller; buyButton = buy; soldLabel = sold;
        }

        public void Bind(Listing listing)
        {
            gameObject.SetActive(listing != null);
            if (listing == null) return;

            icon.sprite = listing.Item.WorldSprite;
            nameText.Text = Fit(listing.Item.ItemName, MaxNameWidth);
            rarityText.Color = ListingsWindow.RarityColorOnLight(listing.Item.Rarity);
            rarityText.Text = listing.Item.Rarity.ToDisplayName();
            conditionText.Text = "Durum: " + listing.Condition.ToDisplayName();
            noteText.Text = Fit("\"" + listing.Note + "\"", MaxNoteWidth);
            // Pazarlıkla düştüyse yeni fiyat
            priceText.Text = listing.CurrentPrice < listing.OriginalPrice
                ? "* " + Wallet.Format(listing.CurrentPrice)
                : Wallet.Format(listing.CurrentPrice);
            sellerText.Text = "@" + listing.Seller;
            buyButton.SetActive(!listing.Sold);
            soldLabel.SetActive(listing.Sold);
        }

        /// <summary>SATIN AL butonu buna bağlıdır.</summary>
        public void Buy() => owner.OnBuy(index);

        /// <summary>Satırın kendisine tıklamak ilan sayfasını açar.</summary>
        public void Open() => owner.ShowDetail(index);

        static string Fit(string text, int maxWidth)
        {
            var normalized = PixelFont.Normalize(text);
            if (PixelFont.MeasureLine(normalized) <= maxWidth) return normalized;
            while (normalized.Length > 1 && PixelFont.MeasureLine(normalized + "..") > maxWidth)
                normalized = normalized.Substring(0, normalized.Length - 1);
            return normalized.TrimEnd() + "..";
        }
    }
}
