using System.Collections.Generic;
using Collector.Boxes;
using Collector.Collectibles;
using UnityEngine;

namespace Collector.Shop
{
    /// <summary>
    /// Satın alınan oyuncak için odaya kargo kutusu koyar: sahnedeki kapalı şablon kutu kopyalanır,
    /// içine oyuncak konur ve boş bir kutu yerine yerleştirilir.
    /// </summary>
    public class CargoSpawner : MonoBehaviour
    {
        [Tooltip("Pasif şablon kutu (RoomView altında).")]
        [SerializeField] ItemBox template;
        [Tooltip("Kutuların konabileceği yerler (RoomView'e göre yerel, alt-orta pivot). Dolu değilse ilki kullanılır.")]
        [SerializeField] Vector2[] spots = new Vector2[0];

        public void Configure(ItemBox boxTemplate, Vector2[] boxSpots)
        {
            template = boxTemplate;
            spots = boxSpots;
        }

        public ItemBox Spawn(CollectibleItemData item, ItemCondition condition)
        {
            var parent = template.transform.parent;
            var box = Instantiate(template, parent);
            box.name = "CargoBox";
            box.transform.localPosition = FreeSpot(parent);
            box.SetTexts("Kargo Kutusu", "Kutuyu Aç", tooltip: true, order: 5);
            box.SetContents(new List<BoxItem> { new BoxItem(item, condition) });
            box.gameObject.SetActive(true);
            return box;
        }

        // Başka bir açılmamış kutunun durmadığı ilk yer; hepsi doluysa hafif kaydırılmış ilk yer
        Vector3 FreeSpot(Transform parent)
        {
            var boxes = parent.GetComponentsInChildren<ItemBox>(includeInactive: false);
            foreach (var spot in spots)
            {
                bool taken = false;
                foreach (var other in boxes)
                    if (Vector2.Distance(other.transform.localPosition, spot) < 0.5f) { taken = true; break; }
                if (!taken) return spot;
            }
            return spots.Length > 0 ? (Vector3)spots[0] + new Vector3(Random.Range(-0.3f, 0.3f), 0f) : Vector3.zero;
        }
    }
}
