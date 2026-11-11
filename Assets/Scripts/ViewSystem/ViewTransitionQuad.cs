using UnityEngine;

namespace Collector.ViewSystem
{
    /// <summary>
    /// Yaklaşma efektinde kullanılan, dört köşesi serbestçe taşınabilen doku düzlemi.
    /// Izgara noktaları köşelerden projektif dönüşümle (<see cref="Homography"/>) yerleştirilir;
    /// böylece yamuk hâlindeyken de gerçek perspektif gibi görünür.
    /// Sahnede kapalı durur; ViewManager geçiş sırasında açar.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ViewTransitionQuad : MonoBehaviour
    {
        const int Segments = 16;
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        Mesh mesh;
        Vector3[] vertices;
        Color32[] colors;
        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            block = new MaterialPropertyBlock();
            BuildMesh();
        }

        void BuildMesh()
        {
            int n = Segments + 1;
            vertices = new Vector3[n * n];
            colors = new Color32[n * n];
            var uvs = new Vector2[n * n];
            var triangles = new int[Segments * Segments * 6];

            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    uvs[y * n + x] = new Vector2(x / (float)Segments, y / (float)Segments);

            int t = 0;
            for (int y = 0; y < Segments; y++)
                for (int x = 0; x < Segments; x++)
                {
                    int i = y * n + x;
                    triangles[t++] = i; triangles[t++] = i + n; triangles[t++] = i + 1;
                    triangles[t++] = i + 1; triangles[t++] = i + n; triangles[t++] = i + n + 1;
                }

            mesh = new Mesh { name = "ViewTransitionQuad" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        public void SetSprite(Sprite sprite)
        {
            meshRenderer.GetPropertyBlock(block);
            block.SetTexture(MainTexId, sprite.texture);
            meshRenderer.SetPropertyBlock(block);
        }

        /// <summary>Köşeler dünya koordinatında: Sol-Üst, Sağ-Üst, Sağ-Alt, Sol-Alt.</summary>
        public void SetCorners(Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft, float alpha)
        {
            int n = Segments + 1;
            var color = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha) * 255f));
            var homography = Homography.SquareToQuad(topLeft, topRight, bottomRight, bottomLeft);
            for (int y = 0; y < n; y++)
            {
                float v = 1f - y / (float)Segments;          // mesh UV'si alttan, homografi üstten başlar
                for (int x = 0; x < n; x++)
                {
                    Vector3 world = homography.Map(x / (float)Segments, v);
                    vertices[y * n + x] = transform.InverseTransformPoint(world);
                    colors[y * n + x] = color;
                }
            }
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.RecalculateBounds();
        }
    }
}
