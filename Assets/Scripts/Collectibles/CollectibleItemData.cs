using UnityEngine;

namespace Collector.Collectibles
{
    /// <summary>
    /// Bir koleksiyon oyuncağının tanımı. Yeni oyuncak eklemek için kod gerekmez:
    /// Project penceresinde sağ tık > Create > Collector > Collectible Item.
    /// Oluşturulan her oyuncak <see cref="CollectibleDatabase"/>'e otomatik eklenir.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCollectible", menuName = "Collector/Collectible Item", order = 0)]
    public class CollectibleItemData : ScriptableObject
    {
        [Header("Kimlik")]
        [Tooltip("Benzersiz kimlik (kayıt dosyaları bunu kullanır). Boş bırakılırsa asset adından üretilir.")]
        [SerializeField] string itemId;
        [SerializeField] string itemName;
        [SerializeField, TextArea(2, 5)] string description;

        [Header("Sınıflandırma")]
        [SerializeField] CollectibleCategory category;
        [SerializeField] Rarity rarity;
        [SerializeField] ItemCondition condition = ItemCondition.Good;
        [SerializeField, Range(1970, 2015)] int year = 1995;
        [SerializeField] string collectionName;
        [Tooltip("Rafa konunca kazandırdığı koleksiyon puanı.")]
        [SerializeField, Min(0)] int collectionValue = 10;

        [Header("Görseller")]
        [Tooltip("Envanter listesindeki simge.")]
        [SerializeField] Sprite inventoryIcon;
        [Tooltip("Oda görünümündeki hali (kutudan çıkınca, odadaki rafta küçük hali).")]
        [SerializeField] Sprite worldSprite;
        [Tooltip("Raf yakın planındaki hali (alt-orta pivot = raf tahtasına basan nokta).")]
        [SerializeField] Sprite shelfSprite;

        public string ItemID => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public CollectibleCategory Category => category;
        public Rarity Rarity => rarity;
        public ItemCondition Condition => condition;
        public int Year => year;
        public string CollectionName => collectionName;
        public int CollectionValue => collectionValue;
        public Sprite InventoryIcon => inventoryIcon != null ? inventoryIcon : worldSprite;
        public Sprite WorldSprite => worldSprite;
        public Sprite ShelfSprite => shelfSprite != null ? shelfSprite : worldSprite;

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = name.Trim().ToLowerInvariant().Replace(' ', '_');
            else
                itemId = itemId.Trim();

            if (string.IsNullOrWhiteSpace(itemName)) itemName = name;
        }
    }
}
