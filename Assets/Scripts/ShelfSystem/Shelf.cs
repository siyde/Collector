using System;
using System.Collections.Generic;
using Collector.Inventory;
using Collector.UI;
using UnityEngine;

namespace Collector.ShelfSystem
{
    /// <summary>
    /// Koleksiyon rafı: 45 <see cref="ShelfSlot"/>'u yönetir.
    /// Boş yere tıklanınca seçili oyuncağı koyar (seçim yoksa envanteri "rafa koy" modunda açar);
    /// dolu yere tıklanınca oyuncağı envantere geri alır.
    /// </summary>
    public class Shelf : MonoBehaviour
    {
        /// <summary>Herhangi bir rafa oyuncak konunca (görevler için).</summary>
        public static event Action<ShelfSlot, OwnedItem> AnyItemPlaced;
        /// <summary>Herhangi bir raftan oyuncak alınınca.</summary>
        public static event Action<ShelfSlot, OwnedItem> AnyItemRemoved;

        [SerializeField] ShelfSlot[] slots = new ShelfSlot[0];

        public IReadOnlyList<ShelfSlot> Slots => slots;

        /// <summary>Raftaki oyuncakların toplam koleksiyon puanı.</summary>
        public int CollectionScore
        {
            get
            {
                int score = 0;
                foreach (var slot in slots)
                    if (slot.IsOccupied) score += slot.StoredItem.Data.CollectionValue;
                return score;
            }
        }

        public void Configure(ShelfSlot[] shelfSlots) => slots = shelfSlots;

        public void OnSlotClicked(ShelfSlot slot)
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null) return;

            if (slot.IsOccupied)
            {
                var item = slot.Take();
                inventory.Add(item);
                ToastMessage.Instance?.Show("Envantere geri alındı: " + item.Data.ItemName);
                AnyItemRemoved?.Invoke(slot, item);
                return;
            }

            if (inventory.Selected != null)
            {
                Place(slot, inventory.Selected);
                return;
            }

            if (inventory.Items.Count == 0)
            {
                ToastMessage.Instance?.Show("Envanterde koyacak oyuncak yok");
                return;
            }

            InventoryUI.Instance?.OpenForPick("Rafa Koy", "Tıkla: rafa koy      ESC: vazgeç", picked =>
            {
                if (picked != null && !slot.IsOccupied) Place(slot, picked);
            });
        }

        public bool Place(ShelfSlot slot, OwnedItem item)
        {
            if (slot.IsOccupied || !InventoryManager.Instance.Remove(item)) return false;
            slot.Store(item);
            AnyItemPlaced?.Invoke(slot, item);
            return true;
        }
    }
}
