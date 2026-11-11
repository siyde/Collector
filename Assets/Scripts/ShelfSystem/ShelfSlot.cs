using System.Collections;
using Collector.Interaction;
using Collector.Inventory;
using UnityEngine;

namespace Collector.ShelfSystem
{
    /// <summary>
    /// Raftaki tek yer. Yakın planda oyuncağın büyük hali (ShelfSprite), odadaki rafta küçük hali
    /// (WorldSprite, perspektif ölçeğiyle) gösterilir. Tıklama <see cref="Shelf"/>'e iletilir.
    /// </summary>
    public class ShelfSlot : InteractableHotspot
    {
        const float PixelsPerUnit = 32f;
        const float DropTime = 0.14f;
        const int DropHeight = 6;

        [SerializeField] Shelf shelf;
        [SerializeField] int index;
        [Tooltip("Yakın plandaki oyuncak (alt-orta pivot = raf tahtası).")]
        [SerializeField] SpriteRenderer itemRenderer;
        [Tooltip("Odadaki raftaki küçük hali (ebeveyni perspektif ölçeğini taşır).")]
        [SerializeField] SpriteRenderer roomRenderer;

        public int Index => index;
        public OwnedItem StoredItem { get; private set; }
        public bool IsOccupied => StoredItem != null;

        public override string DisplayName => IsOccupied ? StoredItem.Data.ItemName : "Boş Raf Yeri";

        public override string ActionText
        {
            get
            {
                if (IsOccupied) return "Raftan Al";
                var selected = InventoryManager.Instance != null ? InventoryManager.Instance.Selected : null;
                return selected != null ? selected.Data.ItemName + " Koy" : "Oyuncak Koy";
            }
        }

        public void Configure(Shelf owner, int slotIndex, SpriteRenderer item, SpriteRenderer roomItem)
        {
            shelf = owner;
            index = slotIndex;
            itemRenderer = item;
            roomRenderer = roomItem;
        }

        public void Store(OwnedItem item)
        {
            StoredItem = item;
            itemRenderer.sprite = item.Data.ShelfSprite;
            roomRenderer.sprite = item.Data.WorldSprite;
            if (isActiveAndEnabled) StartCoroutine(Drop());
        }

        public OwnedItem Take()
        {
            var item = StoredItem;
            StoredItem = null;
            itemRenderer.sprite = null;
            roomRenderer.sprite = null;
            return item;
        }

        public override void Interact() => shelf.OnSlotClicked(this);

        // Oyuncak birkaç piksel yukarıdan tahtaya düşer
        IEnumerator Drop()
        {
            var t = itemRenderer.transform;
            for (float time = 0f; time < 1f; time += Time.deltaTime / DropTime)
            {
                float e = time * time;
                t.localPosition = new Vector3(0f, Mathf.Round((1f - e) * DropHeight) / PixelsPerUnit, 0f);
                yield return null;
            }
            t.localPosition = Vector3.zero;
        }
    }
}
