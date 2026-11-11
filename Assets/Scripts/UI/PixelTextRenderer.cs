using System.Collections.Generic;
using UnityEngine;

namespace Collector.UI
{
    /// <summary>
    /// <see cref="PixelFont"/> ile dünyada metin çizer. Her harf 1 sanat pikseli = 1/32 birim olacak şekilde
    /// yerleştirilir; Pixel Perfect kamerayla sanatla aynı ızgarada, keskin görünür.
    /// Yerel orijin metnin sol-üst köşesidir (hizalama Center/Right ise yatayda kayar).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PixelTextRenderer : MonoBehaviour
    {
        public enum Alignment { Left, Center, Right }

        const float PixelsPerUnit = 32f;
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField, TextArea] string text = "METİN";
        [SerializeField] Color32 color = new Color32(242, 232, 208, 255);
        [SerializeField] Alignment alignment = Alignment.Left;
        [Tooltip("Tam sayı büyütme (2 = her harf pikseli 2x2 sanat pikseli).")]
        [SerializeField, Min(1)] int scale = 1;

        Mesh mesh;
        MaterialPropertyBlock block;
        bool dirty = true;

        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<int> triangles = new List<int>();

        /// <summary>Metnin piksel cinsinden boyutu (genişlik, yükseklik).</summary>
        public Vector2Int PixelSize { get; private set; }

        public string Text
        {
            get => text;
            set { if (text == value) return; text = value; Rebuild(); }
        }

        public Color32 Color
        {
            get => color;
            set { color = value; Rebuild(); }
        }

        public void Configure(string value, Color32 textColor, Alignment align, int textScale = 1)
        {
            text = value;
            color = textColor;
            alignment = align;
            scale = Mathf.Max(1, textScale);
            Rebuild();
        }

        void OnEnable() => Rebuild();
        void OnValidate() => dirty = true;

        void LateUpdate()
        {
            if (dirty) Rebuild();
        }

        void OnDestroy()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }

        public void Rebuild()
        {
            dirty = false;
            if (mesh == null)
            {
                mesh = new Mesh { name = "PixelText", hideFlags = HideFlags.DontSave };
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            vertices.Clear(); uvs.Clear(); colors.Clear(); triangles.Clear();

            var lines = PixelFont.Normalize(text).Split('\n');
            int widest = 0;
            for (int li = 0; li < lines.Length; li++)
            {
                var line = lines[li];
                int width = PixelFont.MeasureLine(line);
                widest = Mathf.Max(widest, width);
                int x = alignment == Alignment.Left ? 0 : alignment == Alignment.Center ? -width / 2 : -width;
                int yTop = -li * PixelFont.LineHeight;

                foreach (var c in line)
                {
                    if (c == ' ' || !PixelFont.TryGetGlyph(c, out var glyph)) { x += PixelFont.SpaceAdvance; continue; }
                    AddQuad(x, yTop, glyph);
                    x += glyph.Width + 1;
                }
            }

            PixelSize = new Vector2Int(widest, lines.Length * PixelFont.LineHeight - (PixelFont.LineHeight - PixelFont.GlyphHeight)) * scale;

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            var meshRenderer = GetComponent<MeshRenderer>();
            block ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            block.SetTexture(MainTexId, PixelFont.Atlas);
            meshRenderer.SetPropertyBlock(block);
        }

        void AddQuad(int x, int yTop, PixelFont.Glyph glyph)
        {
            float s = scale / PixelsPerUnit;
            float x0 = x * s, x1 = (x + glyph.Width) * s;
            float y1 = yTop * s, y0 = (yTop - PixelFont.GlyphHeight) * s;
            int i = vertices.Count;
            vertices.Add(new Vector3(x0, y0)); vertices.Add(new Vector3(x0, y1));
            vertices.Add(new Vector3(x1, y1)); vertices.Add(new Vector3(x1, y0));
            var uv = glyph.Uv;
            uvs.Add(new Vector2(uv.xMin, uv.yMin)); uvs.Add(new Vector2(uv.xMin, uv.yMax));
            uvs.Add(new Vector2(uv.xMax, uv.yMax)); uvs.Add(new Vector2(uv.xMax, uv.yMin));
            for (int k = 0; k < 4; k++) colors.Add(color);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
        }
    }
}
