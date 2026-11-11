using Collector.UI;
using UnityEngine;

namespace Collector.Inventory
{
    /// <summary>
    /// Sol alttaki küçük kutu: seçili oyuncağın ikonu ve adı. Seçim yoksa gizlenir.
    /// Kök objenin konumu kutunun sol-ALT köşesidir; kutu yukarı doğru büyür.
    /// </summary>
    public class SelectedItemHud : MonoBehaviour
    {
        const float PixelsPerUnit = 32f;
        const int Height = 44, IconArea = 44, RightPadding = 6;

        [SerializeField] GameObject box;
        [SerializeField] SpriteRenderer panel;
        [SerializeField] SpriteRenderer icon;
        [SerializeField] PixelTextRenderer label;
        [SerializeField] PixelTextRenderer itemName;

        InventoryManager inventory;

        public void Configure(GameObject boxRoot, SpriteRenderer panelRenderer, SpriteRenderer iconRenderer,
            PixelTextRenderer labelText, PixelTextRenderer nameText)
        {
            box = boxRoot;
            panel = panelRenderer;
            icon = iconRenderer;
            label = labelText;
            itemName = nameText;
        }

        void Start()
        {
            inventory = InventoryManager.Instance;
            inventory.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }

        void Refresh()
        {
            var selected = inventory.Selected;
            box.SetActive(selected != null);
            if (selected == null) return;

            icon.sprite = selected.Data.InventoryIcon;
            itemName.Text = selected.Data.ItemName;
            int width = IconArea + Mathf.Max(label.PixelSize.x, itemName.PixelSize.x) + RightPadding;
            panel.size = new Vector2(width, Height) / PixelsPerUnit;
            panel.transform.localPosition = new Vector3(width * 0.5f, Height * 0.5f) / PixelsPerUnit;
        }
    }
}
