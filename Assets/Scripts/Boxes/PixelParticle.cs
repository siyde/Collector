using UnityEngine;

namespace Collector.Boxes
{
    /// <summary>Tek bir kare piksel parçacığı (toz, parıltı): hareket eder, yavaşlar, solar ve yok olur.</summary>
    public class PixelParticle : MonoBehaviour
    {
        const float PixelsPerUnit = 32f;

        SpriteRenderer spriteRenderer;
        Vector2 velocity;
        Color color;
        float life, age;

        public static PixelParticle Spawn(Sprite sprite, Vector3 position, Vector2 velocity, Color color, int size, float life,
            string sortingLayer, int order, Transform parent = null)
        {
            var go = new GameObject("Particle");
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size, size, 1f);               // UI_Pixel = 1 sanat pikseli

            var particle = go.AddComponent<PixelParticle>();
            particle.spriteRenderer = go.AddComponent<SpriteRenderer>();
            particle.spriteRenderer.sprite = sprite;
            particle.spriteRenderer.color = color;
            particle.spriteRenderer.sortingLayerName = sortingLayer;
            particle.spriteRenderer.sortingOrder = order;
            particle.velocity = velocity;
            particle.color = color;
            particle.life = life;
            return particle;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }

            velocity *= 1f - 3f * Time.deltaTime;                                 // hava direnci
            var pos = transform.position + (Vector3)(velocity * Time.deltaTime);
            transform.position = pos;
            spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * (1f - age / life));
        }
    }
}
