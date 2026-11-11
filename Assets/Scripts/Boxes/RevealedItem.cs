using System.Collections;
using Collector.Collectibles;
using Collector.Interaction;
using Collector.Inventory;
using Collector.UI;
using UnityEngine;

namespace Collector.Boxes
{
    /// <summary>
    /// Kutudan çıkıp havada süzülen oyuncak. Tıklanınca envanter ipucuna doğru uçar, envantere eklenir
    /// ve bildirim gösterilir; sonra kendini yok eder.
    /// </summary>
    public class RevealedItem : InteractableHotspot
    {
        const float PixelsPerUnit = 32f;
        const float RiseTime = 0.45f, FlyTime = 0.4f, BobInterval = 0.4f;

        CollectibleItemData data;
        ItemCondition condition;
        ItemBox source;
        SpriteRenderer spriteRenderer;
        SpriteRenderer[] sparkles;
        Vector3 startLocal, restLocal;   // odaya göre yerel: oda yakınlaşma efektinde büyürken oyuncak da onunla hareket eder
        bool ready, taking;

        public override string DisplayName => data != null ? data.ItemName : "";
        public override string ActionText => "Al";
        public override bool CanInteract => base.CanInteract && ready && !taking;

        public static RevealedItem Spawn(ItemBox box, CollectibleItemData item, ItemCondition itemCondition, Vector3 mouthPosition, float riseHeight)
        {
            var go = new GameObject("Revealed_" + item.ItemID);
            go.transform.SetParent(box.transform.parent, true);
            go.transform.position = mouthPosition;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = item.WorldSprite;
            renderer.sortingLayerName = "CollectiblesFront";
            renderer.sortingOrder = 5;
            go.AddComponent<BoxCollider2D>().isTrigger = true;                    // önce collider (sprite'a göre boyutlanır)

            var revealed = go.AddComponent<RevealedItem>();
            revealed.SetTexts(item.ItemName, "Al", tooltip: true, order: 20);
            revealed.data = item;
            revealed.condition = itemCondition;
            revealed.source = box;
            revealed.spriteRenderer = renderer;
            revealed.startLocal = go.transform.localPosition;
            revealed.restLocal = go.transform.localPosition + new Vector3(0f, riseHeight, 0f);
            revealed.CreateSparkles(box.SparkleSprite);
            revealed.StartCoroutine(revealed.Rise());
            return revealed;
        }

        void CreateSparkles(Sprite sprite)
        {
            if (sprite == null) { sparkles = new SpriteRenderer[0]; return; }
            Vector2[] offsets = { new Vector2(-16, 22), new Vector2(15, 26), new Vector2(-12, 6), new Vector2(17, 10) };
            sparkles = new SpriteRenderer[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                var go = new GameObject("Sparkle");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = offsets[i] / PixelsPerUnit;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Effects";
                sr.sortingOrder = 1;
                sr.enabled = false;
                sparkles[i] = sr;
            }
        }

        IEnumerator Rise()
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / RiseTime)
            {
                float e = EaseOutBack(Mathf.Clamp01(t));
                transform.localPosition = Vector3.LerpUnclamped(startLocal, restLocal, e);
                float s = Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(t * 1.5f));
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            transform.localPosition = restLocal;
            transform.localScale = Vector3.one;
            ready = true;
        }

        void Update()
        {
            if (!ready || taking) return;

            // 1 piksel süzülme ve sırayla yanıp sönen parıltılar
            int beat = (int)(Time.time / BobInterval);
            transform.localPosition = restLocal + new Vector3(0f, (beat % 2) / PixelsPerUnit, 0f);
            for (int i = 0; i < sparkles.Length; i++)
                sparkles[i].enabled = (beat + i) % 3 == 0;
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            StartCoroutine(Take());
        }

        IEnumerator Take()
        {
            taking = true;
            foreach (var s in sparkles) s.enabled = false;

            var target = source != null && source.InventoryTarget != null
                ? source.InventoryTarget.position
                : transform.position + Vector3.down * 3f;
            var start = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / FlyTime)
            {
                float e = t * t;                                                  // hızlanarak uçar
                var pos = Vector3.Lerp(start, target, e);
                pos.y += Mathf.Sin(t * Mathf.PI) * 0.8f;                          // hafif kavis
                transform.position = pos;
                float s = Mathf.Lerp(1f, 0.4f, e);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            InventoryManager.Instance?.Add(data, condition);
            ToastMessage.Instance?.Show("Envantere eklendi: " + data.ItemName);
            Destroy(gameObject);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
