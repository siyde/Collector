using System;
using System.Collections.Generic;
using Collector.Collectibles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collector.Inventory
{
    /// <summary>
    /// Oyuncunun elindeki (rafa konmamış) oyuncaklar ve seçili oyuncak.
    /// Oyuncaklar kutulardan / satın almalardan <see cref="Add"/> ile gelir, rafa konunca <see cref="Remove"/> ile çıkar.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] CollectibleDatabase database;
        [Tooltip("Test için oyuna bu oyuncaklarla başla (normal oyunda boş).")]
        [SerializeField] List<CollectibleItemData> startingItems = new List<CollectibleItemData>();

        readonly List<OwnedItem> items = new List<OwnedItem>();

        public IReadOnlyList<OwnedItem> Items => items;
        public OwnedItem Selected { get; private set; }
        public CollectibleDatabase Database => database;

        /// <summary>Envanter veya seçim değişince tetiklenir.</summary>
        public event Action Changed;
        public event Action<OwnedItem> ItemAdded;
        public event Action<OwnedItem> ItemRemoved;

        public void Configure(CollectibleDatabase collectibleDatabase) => database = collectibleDatabase;

        void Awake()
        {
            Instance = this;
            foreach (var data in startingItems)
                if (data != null) items.Add(new OwnedItem(data, data.Condition));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public OwnedItem Add(CollectibleItemData data) => Add(data, data.Condition);

        public OwnedItem Add(CollectibleItemData data, ItemCondition condition) => Add(new OwnedItem(data, condition));

        /// <summary>Var olan bir kopyayı geri ekler (ör. raftan alınan oyuncak; kimliği ve durumu korunur).</summary>
        public OwnedItem Add(OwnedItem item)
        {
            if (item == null || items.Contains(item)) return item;
            items.Add(item);
            ItemAdded?.Invoke(item);
            Changed?.Invoke();
            return item;
        }

        public bool Remove(OwnedItem item)
        {
            if (!items.Remove(item)) return false;
            if (Selected == item) Selected = null;
            ItemRemoved?.Invoke(item);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Oyuncağı seçer; null seçimi kaldırır.</summary>
        public void Select(OwnedItem item)
        {
            if (item != null && !items.Contains(item)) return;
            if (Selected == item) return;
            Selected = item;
            Changed?.Invoke();
        }

        void Update()
        {
            // Geliştirme kısayolu: F2 veritabanından rastgele bir oyuncak ekler (oyun build'inde çalışmaz)
            if (!Debug.isDebugBuild || database == null || database.Items.Count == 0) return;
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                var data = database.Items[UnityEngine.Random.Range(0, database.Items.Count)];
                if (data != null)
                {
                    Add(data);
                    Debug.Log("[Collector] Test: envantere eklendi -> " + data.ItemName);
                }
            }
        }
    }
}
