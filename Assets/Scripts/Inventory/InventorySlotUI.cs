using Collector.Collectibles;
using Collector.Interaction;
using Collector.UI;
using UnityEngine;

namespace Collector.Inventory
{
    /// <summary>Envanter penceresindeki tek kart: ikon, ad, nadirlik (renkli), durum. Tıklayınca seçer.</summary>
    public class InventorySlotUI : InteractableHotspot
    {
        const int MaxNameWidth = 140;

        [SerializeField] InventoryUI owner;
        [SerializeField] SpriteRenderer background;
        [SerializeField] SpriteRenderer icon;
        [SerializeField] PixelTextRenderer nameText;
        [SerializeField] PixelTextRenderer rarityText;
        [SerializeField] PixelTextRenderer conditionText;
        [SerializeField] Sprite normalSprite;
        [SerializeField] Sprite selectedSprite;

        public OwnedItem Item { get; private set; }

        public override bool CanInteract => base.CanInteract && Item != null;

        public void Configure(InventoryUI inventoryUI, SpriteRenderer backgroundRenderer, SpriteRenderer iconRenderer,
            PixelTextRenderer itemName, PixelTextRenderer rarity, PixelTextRenderer condition, Sprite normal, Sprite selected)
        {
            owner = inventoryUI;
            background = backgroundRenderer;
            icon = iconRenderer;
            nameText = itemName;
            rarityText = rarity;
            conditionText = condition;
            normalSprite = normal;
            selectedSprite = selected;
        }

        public void Bind(OwnedItem item, bool selected)
        {
            Item = item;
            gameObject.SetActive(item != null);
            if (item == null) return;

            var data = item.Data;
            icon.sprite = data.InventoryIcon;
            nameText.Text = Fit(data.ItemName);
            rarityText.Color = data.Rarity.ToColor();
            rarityText.Text = data.Rarity.ToDisplayName();
            conditionText.Text = "Durum: " + item.Condition.ToDisplayName();
            background.sprite = selected ? selectedSprite : normalSprite;
        }

        public override void Interact() => owner.OnSlotClicked(this);

        // Uzun adları kart genişliğine sığacak şekilde kısaltır
        static string Fit(string text)
        {
            var normalized = PixelFont.Normalize(text);
            if (PixelFont.MeasureLine(normalized) <= MaxNameWidth) return normalized;
            while (normalized.Length > 1 && PixelFont.MeasureLine(normalized + "..") > MaxNameWidth)
                normalized = normalized.Substring(0, normalized.Length - 1);
            return normalized.TrimEnd() + "..";
        }
    }
}
