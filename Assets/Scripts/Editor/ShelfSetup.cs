using Collector.ShelfSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 8: Raf yakın planındaki 45 slotu tıklanabilir yapar (ShelfSlot + tıklama alanı + oyuncak sprite'ı)
    /// ve odadaki raf noktalarına oyuncakların küçük halleri için perspektif ölçekli sprite'lar ekler.
    /// Menü: Collector > Aşama 8 > Raf Sistemini Kur
    /// </summary>
    public static class ShelfSetup
    {
        // Tıklama alanı: slotun altındaki tahtadan yukarı doğru (piksel). Yan yana slotlar ~61 px arayla.
        const float AreaWidth = 58f, AreaHeight = 52f;

        [MenuItem("Collector/Aşama 8/Raf Sistemini Kur")]
        static void SetupMenu()
        {
            StageSetup.RunFromMenu("Aşama 8", "Raf sistemi kuruldu.\n\nPlay'e bas, kutudan robotu al (ya da F2), rafa tıkla ve boş bir yere tıkla.");
        }

        public static bool Setup()
        {
            var slotsRoot = GameObject.Find("Views/ShelfView/Slots");
            var roomSlotsRoot = GameObject.Find("Views/RoomView/RoomShelfSlots");
            if (slotsRoot == null || roomSlotsRoot == null)
            {
                EditorUtility.DisplayDialog("Aşama 8", "ShelfView/Slots veya RoomView/RoomShelfSlots bulunamadı. Sahneyi Aşama 1 ile yeniden oluştur.", "Tamam");
                return false;
            }

            var shelf = GetOrAdd<Shelf>(slotsRoot);
            var slots = new ShelfSlot[RoomLayout.ShelfSlotCount];

            for (int i = 0; i < slots.Length; i++)
            {
                string name = $"Slot_{i + 1:00}";
                var slotTransform = slotsRoot.transform.Find(name);
                var roomTransform = roomSlotsRoot.transform.Find(name);
                if (slotTransform == null || roomTransform == null)
                {
                    Debug.LogError("[Collector] Slot bulunamadı: " + name);
                    return false;
                }
                slots[i] = SetupSlot(shelf, i, slotTransform.gameObject, roomTransform);
            }

            shelf.Configure(slots);
            EditorUtility.SetDirty(shelf);
            return true;
        }

        static ShelfSlot SetupSlot(Shelf shelf, int index, GameObject slotGo, Transform roomSlot)
        {
            // Yeniden kurulabilsin: eski parçaları temizle
            foreach (var old in slotGo.GetComponents<ShelfSlot>()) Object.DestroyImmediate(old);
            foreach (var old in slotGo.GetComponents<BoxCollider2D>()) Object.DestroyImmediate(old);
            DestroyChild(slotGo.transform, "Item");
            DestroyChild(roomSlot, "Item");

            // Önce collider (ShelfSlot bir Collider2D ister)
            var col = slotGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(AreaWidth, AreaHeight) / UiKit.PPU;
            col.offset = new Vector2(0f, AreaHeight * 0.5f) / UiKit.PPU;

            var item = CreateItemRenderer(slotGo.transform);

            // Odadaki küçük hal: ebeveyn perspektif ölçeğini taşır (32 px'lik WorldSprite, 48 px'lik oyuncağı temsil eder)
            var scale = RoomLayout.ShelfSlotRoomScale(index) * (48f / 32f);
            roomSlot.localScale = new Vector3(scale.x, scale.y, 1f);
            var roomItem = CreateItemRenderer(roomSlot);

            var slot = slotGo.AddComponent<ShelfSlot>();
            slot.SetTexts("", "", tooltip: true, order: 10);
            slot.Configure(shelf, index, item, roomItem);
            EditorUtility.SetDirty(slot);
            return slot;
        }

        static SpriteRenderer CreateItemRenderer(Transform parent)
        {
            var go = new GameObject("Item");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "CollectiblesBack";
            sr.sortingOrder = 0;
            return sr;
        }

        static void DestroyChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T component) ? component : go.AddComponent<T>();
    }
}
