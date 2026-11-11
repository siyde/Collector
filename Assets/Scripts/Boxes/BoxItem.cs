using System;
using Collector.Collectibles;
using UnityEngine;

namespace Collector.Boxes
{
    /// <summary>Kutudaki tek oyuncak; durumu isteğe bağlı olarak oyuncağın varsayılanından farklı olabilir (ör. ilandaki durum).</summary>
    [Serializable]
    public struct BoxItem
    {
        public CollectibleItemData data;
        public bool overrideCondition;
        public ItemCondition condition;

        public ItemCondition Condition => overrideCondition || data == null ? condition : data.Condition;

        public BoxItem(CollectibleItemData item)
        {
            data = item;
            overrideCondition = false;
            condition = item != null ? item.Condition : ItemCondition.Good;
        }

        public BoxItem(CollectibleItemData item, ItemCondition itemCondition)
        {
            data = item;
            overrideCondition = true;
            condition = itemCondition;
        }
    }
}
