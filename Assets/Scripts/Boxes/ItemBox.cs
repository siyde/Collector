using System;
using System.Collections;
using System.Collections.Generic;
using Collector.Interaction;
using UnityEngine;

namespace Collector.Boxes
{
    /// <summary>
    /// Odada açılabilen kutu (başlangıçtaki eski karton kutu, ileride kargo kutuları).
    /// Tıklanınca sallanır, açılır, toz çıkarır ve içindeki oyuncakları sırayla <see cref="RevealedItem"/>
    /// olarak dışarı çıkarır. Hepsi alınınca kutu solup kaybolur.
    /// </summary>
    public class ItemBox : InteractableHotspot
    {
        enum State { Closed, Opening, Revealing, Empty }

        const float PixelsPerUnit = 32f;

        /// <summary>Herhangi bir kutu açıldığında (görevler için).</summary>
        public static event Action<ItemBox> AnyOpened;

        public event Action Opened;
        public event Action Emptied;

        [SerializeField] SpriteRenderer boxRenderer;
        [SerializeField] Sprite closedSprite;
        [SerializeField] Sprite openSprite;
        [SerializeField] List<BoxItem> contents = new List<BoxItem>();

        [Header("Efektler")]
        [SerializeField] Sprite particleSprite;
        [SerializeField] Sprite sparkleSprite;
        [Tooltip("Alınan oyuncağın uçacağı hedef (HUD'daki envanter ipucu).")]
        [SerializeField] Transform inventoryTarget;
        [SerializeField, Tooltip("Oyuncağın kutudan çıkınca yükseleceği yükseklik (piksel).")] int revealHeight = 14;
        [SerializeField, Tooltip("Kutunun ağzının (oyuncağın çıktığı nokta) kutu sprite'ının pivot'una göre konumu, piksel.")]
        Vector2 mouthOffsetPx = new Vector2(-3f, 22f);

        State state = State.Closed;

        public IReadOnlyList<BoxItem> Contents => contents;
        public Transform InventoryTarget => inventoryTarget;
        public Sprite SparkleSprite => sparkleSprite;

        public override bool CanInteract => base.CanInteract && state == State.Closed;

        /// <summary>Kutunun içeriğini değiştirir (ör. kargo kutusu oluşturulurken).</summary>
        public void SetContents(List<BoxItem> items) => contents = items;

        public void Configure(SpriteRenderer renderer, Sprite closed, Sprite open, List<BoxItem> items,
            Sprite particle, Sprite sparkle, Transform target, Vector2 mouthPx)
        {
            boxRenderer = renderer;
            closedSprite = closed;
            openSprite = open;
            contents = items;
            particleSprite = particle;
            sparkleSprite = sparkle;
            inventoryTarget = target;
            mouthOffsetPx = mouthPx;
        }

        public override void Interact()
        {
            if (state != State.Closed) return;
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            state = State.Opening;
            var basePos = boxRenderer.transform.localPosition;

            // Sallanma: 1 piksel sağa-sola
            float[] shake = { 1, -1, 1, -1, 1, 0 };
            foreach (var dx in shake)
            {
                boxRenderer.transform.localPosition = basePos + new Vector3(dx / PixelsPerUnit, 0f);
                yield return new WaitForSeconds(0.06f);
            }

            boxRenderer.sprite = openSprite;
            SpawnDust();
            Opened?.Invoke();
            AnyOpened?.Invoke(this);
            yield return new WaitForSeconds(0.15f);

            state = State.Revealing;
            foreach (var item in contents)
            {
                if (item.data == null) continue;
                var revealed = RevealedItem.Spawn(this, item.data, item.Condition, TopOfBox(), revealHeight / PixelsPerUnit);
                yield return new WaitUntil(() => revealed == null);             // oyuncak alınınca yok olur
                yield return new WaitForSeconds(0.1f);
            }

            state = State.Empty;
            Emptied?.Invoke();
            yield return Fade(0.5f);
            gameObject.SetActive(false);
        }

        // Kutunun ağzı (dünya koordinatı); sallanma ofsetinden etkilenmesin diye kök objeden hesaplanır
        Vector3 TopOfBox() => transform.position + (Vector3)(mouthOffsetPx / PixelsPerUnit);

        IEnumerator Fade(float seconds)
        {
            var color = boxRenderer.color;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                boxRenderer.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }
        }

        void SpawnDust()
        {
            if (particleSprite == null) return;
            var origin = TopOfBox();
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.Lerp(20f, 160f, i / 9f) * Mathf.Deg2Rad;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(0.8f, 1.6f);
                PixelParticle.Spawn(particleSprite, origin, velocity, new Color(0.9f, 0.85f, 0.75f, 0.9f),
                    size: UnityEngine.Random.Range(1, 3), life: UnityEngine.Random.Range(0.35f, 0.6f),
                    sortingLayer: "Effects", order: 0, parent: transform);
            }
        }
    }
}
