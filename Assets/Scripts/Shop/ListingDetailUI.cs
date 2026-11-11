using System;
using Collector.Collectibles;
using Collector.Economy;
using Collector.UI;
using UnityEngine;

namespace Collector.Shop
{
    /// <summary>
    /// İlan sayfası: oyuncağın büyük görseli ve bilgileri, satıcı notu, fiyat, satıcının son cevabı,
    /// teklif ayarlama (-25/-5/+5/+25) ve TEKLİF VER / SATIN AL / İLANLAR'a dön.
    /// </summary>
    public class ListingDetailUI : MonoBehaviour
    {
        const int NameWidth = 208, DescriptionWidth = 276, ReplyWidth = 270;

        [Serializable]
        public class Refs
        {
            public SpriteRenderer image;
            public PixelTextRenderer itemName, rarity, condition, year, series, description;
            public PixelTextRenderer seller, note, price, originalPrice, reply, offer, status;
            public GameObject offerControls, buyButton;
        }

        [SerializeField] ListingHost owner;
        [SerializeField] Refs refs = new Refs();

        Listing listing;
        int offer;

        public void Configure(ListingHost window, Refs references)
        {
            owner = window;
            refs = references;
        }

        public void Bind(Listing target)
        {
            listing = target;
            offer = ClampOffer(Mathf.RoundToInt(target.CurrentPrice * 0.8f / 5f) * 5);
            Refresh();
        }

        public void Refresh()
        {
            if (listing == null) return;
            var item = listing.Item;

            refs.image.sprite = item.ShelfSprite;
            refs.itemName.Text = PixelFont.Wrap(item.ItemName, NameWidth, 1);
            refs.rarity.Color = ListingsWindow.RarityColorOnLight(item.Rarity);
            refs.rarity.Text = item.Rarity.ToDisplayName() + "  -  " + item.Category.ToDisplayName();
            refs.condition.Text = "Durum: " + listing.Condition.ToDisplayName();
            refs.year.Text = "Yıl: " + item.Year;
            refs.series.Text = PixelFont.Wrap("Seri: " + item.CollectionName, NameWidth, 1);
            refs.description.Text = PixelFont.Wrap(item.Description, DescriptionWidth, 3);

            refs.seller.Text = "@" + listing.Seller;
            refs.note.Text = PixelFont.Wrap("\"" + listing.Note + "\"", DescriptionWidth, 1);
            refs.price.Text = "Fiyat: " + Wallet.Format(listing.CurrentPrice);
            bool lowered = listing.CurrentPrice < listing.OriginalPrice;
            refs.originalPrice.gameObject.SetActive(lowered);
            refs.originalPrice.Text = "İlk fiyat: " + Wallet.Format(listing.OriginalPrice);
            refs.reply.Text = PixelFont.Wrap(listing.LastReply ?? "", ReplyWidth, 2);

            offer = ClampOffer(offer);
            refs.offer.Text = Wallet.Format(offer);
            refs.offerControls.SetActive(listing.CanNegotiate);
            refs.buyButton.SetActive(!listing.Sold);
            refs.status.Text = listing.Sold ? "Satıldı" : listing.Agreed ? "Anlaşıldı! Satın alabilirsin."
                : listing.NegotiationClosed ? "Pazarlık bitti." : "";
            refs.status.gameObject.SetActive(!listing.CanNegotiate);
        }

        // Teklif butonları (UiButton'lar bunlara bağlıdır)
        public void OfferMinus25() => ChangeOffer(-25);
        public void OfferMinus5() => ChangeOffer(-5);
        public void OfferPlus5() => ChangeOffer(5);
        public void OfferPlus25() => ChangeOffer(25);

        public void SubmitOffer()
        {
            if (listing == null || !listing.CanNegotiate) return;
            owner.Negotiate(listing, offer);
            Refresh();
        }

        public void Buy()
        {
            if (listing != null) owner.BuyListing(listing);
        }

        public void Back() => owner.CloseDetail();

        void ChangeOffer(int delta)
        {
            offer = ClampOffer(offer + delta);
            refs.offer.Text = Wallet.Format(offer);
        }

        int ClampOffer(int value) => listing == null ? value : Mathf.Clamp(value, 5, listing.CurrentPrice);
    }
}
