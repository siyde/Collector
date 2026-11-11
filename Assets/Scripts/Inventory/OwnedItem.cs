using System;
using Collector.Collectibles;
using UnityEngine;

namespace Collector.Inventory
{
    /// <summary>
    /// Oyuncunun sahip olduğu bir oyuncak kopyası. Aynı oyuncaktan birden fazla olabilir ve her
    /// kopyanın durumu ayrı değişebilir (temizleme/tamir). Kayıt dosyasında InstanceId + ItemID + durum tutulur.
    /// </summary>
    [Serializable]
    public class OwnedItem
    {
        [SerializeField] string instanceId;
        [SerializeField] CollectibleItemData data;
        [SerializeField] ItemCondition condition;

        public string InstanceId => instanceId;
        public CollectibleItemData Data => data;
        public ItemCondition Condition => condition;

        public OwnedItem(CollectibleItemData itemData, ItemCondition itemCondition, string id = null)
        {
            data = itemData;
            condition = itemCondition;
            instanceId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
        }

        public void SetCondition(ItemCondition newCondition) => condition = newCondition;
    }
}
