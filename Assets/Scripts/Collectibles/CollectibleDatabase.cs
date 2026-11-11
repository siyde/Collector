using System.Collections.Generic;
using UnityEngine;

namespace Collector.Collectibles
{
    /// <summary>
    /// Oyundaki bütün oyuncakların listesi; kayıt/yükleme ve ilan sistemi oyuncakları ID ile buradan bulur.
    /// Editor'de liste otomatik güncellenir (yeni CollectibleItemData oluşturulunca / silinince).
    /// </summary>
    [CreateAssetMenu(fileName = "CollectibleDatabase", menuName = "Collector/Collectible Database", order = 1)]
    public class CollectibleDatabase : ScriptableObject
    {
        [SerializeField] List<CollectibleItemData> items = new List<CollectibleItemData>();

        Dictionary<string, CollectibleItemData> lookup;

        public IReadOnlyList<CollectibleItemData> Items => items;

        public CollectibleItemData GetById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(itemId, out var item) ? item : null;
        }

        public void SetItems(List<CollectibleItemData> newItems)
        {
            items = newItems;
            lookup = null;
        }

        void OnEnable() => lookup = null;

        void OnValidate()
        {
            lookup = null;
            var seen = new HashSet<string>();
            foreach (var item in items)
                if (item != null && !seen.Add(item.ItemID))
                    Debug.LogWarning($"[Collector] Aynı ItemID birden fazla oyuncakta: '{item.ItemID}'", item);
        }

        void BuildLookup()
        {
            lookup = new Dictionary<string, CollectibleItemData>();
            foreach (var item in items)
                if (item != null && !string.IsNullOrEmpty(item.ItemID))
                    lookup[item.ItemID] = item;
        }
    }
}
