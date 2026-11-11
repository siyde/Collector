using Collector.Interaction;
using Collector.Shop;
using UnityEngine;

namespace Collector.Market
{
    /// <summary>Tezgahta duran oyuncak; tıklanınca tezgah penceresinde ilan sayfası açılır. Satılınca kaybolur.</summary>
    public class MarketItem : InteractableHotspot
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        Listing listing;
        int stall;

        public override string DisplayName => listing != null ? listing.Item.ItemName : "";
        public override string ActionText => "İncele";
        public override bool CanInteract => base.CanInteract && listing != null && !listing.Sold;

        public void Configure(SpriteRenderer renderer) => spriteRenderer = renderer;

        public void Bind(Listing target, int stallIndex)
        {
            listing = target;
            stall = stallIndex;
            Refresh();
        }

        public void Refresh()
        {
            bool visible = listing != null && !listing.Sold;
            spriteRenderer.sprite = visible ? listing.Item.ShelfSprite : null;
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            MarketStallWindow.Instance?.Show(listing, MarketManager.Instance != null ? MarketManager.Instance.StallName(stall) : "");
        }
    }
}
