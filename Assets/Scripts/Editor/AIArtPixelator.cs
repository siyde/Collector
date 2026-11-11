using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// Yapay zekâ ile üretilmiş görselleri gerçek pixel art ızgarasına oturtur:
    /// 1) 16:9 oranına ortadan kırpar, 2) alan ortalamasıyla 640x360'a küçültür,
    /// 3) renkleri sınırlı bir palete (varsayılan 48 renk) indirger.
    /// Menü: Project penceresinde görseli seç > Collector > Araçlar > Seçili Görseli Pixel Art Yap
    /// </summary>
    public static class AIArtPixelator
    {
        public const int TargetWidth = 640;
        public const int TargetHeight = 360;
        public const int PaletteSize = 48;
        const string OutputFolder = "Assets/Art/Views";

        [MenuItem("Collector/Araçlar/Seçili Görseli Pixel Art Yap (640x360)")]
        static void PixelateSelected()
        {
            var srcPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var dstPath = $"{OutputFolder}/{Path.GetFileNameWithoutExtension(srcPath)}_Pixel.png";
            Pixelate(srcPath, dstPath, TargetWidth, TargetHeight, PaletteSize);
            AssetDatabase.ImportAsset(dstPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath);
            Debug.Log("[Collector] Pixel art oluşturuldu: " + dstPath);
        }

        [MenuItem("Collector/Araçlar/Seçili Görseli Pixel Art Yap (640x360)", true)]
        static bool PixelateSelectedValidate() => Selection.activeObject is Texture2D;

        /// <summary>
        /// Görseli hedef orana göre ortadan kırpıp küçültür. <paramref name="palette"/> verilirse renkler
        /// k-means yerine o palete oturtulur (ör. yakın planı odanın renkleriyle uyumlu yapmak için).
        /// </summary>
        public static void Pixelate(string srcPath, string dstPath, int width, int height, int colors, Color32[] palette = null)
        {
            var size = ReadSize(srcPath);
            float scale = Mathf.Min(size.x / (float)width, size.y / (float)height);
            var region = new Rect((size.x - width * scale) * 0.5f, (size.y - height * scale) * 0.5f, width * scale, height * scale);
            PixelateRegion(srcPath, dstPath, width, height, colors, region, 0f, palette);
        }

        /// <summary>
        /// Kaynak görselin belirli bir bölgesini (piksel, sol-ÜST köşe orijinli) hedef boyuta sığdırır.
        /// Bölge görselin dışına taşarsa kenar pikselleri uzatılır ve taşan kısım
        /// <paramref name="outsideDarken"/> oranına kadar kademeli karartılır (ör. dikey raf görselini 16:9 yapmak için).
        /// </summary>
        public static void PixelateRegion(string srcPath, string dstPath, int width, int height, int colors,
            Rect regionTopDown, float outsideDarken, Color32[] palette = null)
        {
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(srcPath));
            var sp = src.GetPixels32();
            int sw = src.width, sh = src.height;
            UnityEngine.Object.DestroyImmediate(src);

            float scaleX = regionTopDown.width / width;
            float scaleY = regionTopDown.height / height;
            var darkColor = new Color32(26, 20, 35, 255);

            var outPx = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                // Texture satırları alttan başlar; bölge üstten ölçülür
                float syTop = regionTopDown.y + (height - 1 - y) * scaleY;
                int ty0 = Mathf.FloorToInt(syTop), ty1 = Mathf.CeilToInt(syTop + scaleY);
                for (int x = 0; x < width; x++)
                {
                    float sxLeft = regionTopDown.x + x * scaleX;
                    int tx0 = Mathf.FloorToInt(sxLeft), tx1 = Mathf.CeilToInt(sxLeft + scaleX);
                    long r = 0, g = 0, b = 0, a = 0; int n = 0;
                    for (int j = ty0; j < Mathf.Max(ty1, ty0 + 1); j++)
                    {
                        int row = sh - 1 - Mathf.Clamp(j, 0, sh - 1);
                        for (int i = tx0; i < Mathf.Max(tx1, tx0 + 1); i++)
                        {
                            var c = sp[row * sw + Mathf.Clamp(i, 0, sw - 1)];
                            r += c.r; g += c.g; b += c.b; a += c.a; n++;
                        }
                    }
                    var color = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), (byte)(a / n));

                    if (outsideDarken > 0f)
                    {
                        float cx = sxLeft + scaleX * 0.5f;
                        float t = cx < 0f ? -cx / Mathf.Max(1f, -regionTopDown.x)
                                : cx > sw ? (cx - sw) / Mathf.Max(1f, regionTopDown.xMax - sw)
                                : 0f;
                        if (t > 0f) color = Color32.Lerp(color, darkColor, Mathf.Clamp01(t) * outsideDarken);
                    }
                    outPx[y * width + x] = color;
                }
            }

            if (palette != null && palette.Length > 0) MapToPalette(outPx, palette);
            else if (colors > 0) QuantizeInPlace(outPx, colors);
            SavePng(outPx, width, height, dstPath);
        }

        /// <summary>
        /// Kaynak görseldeki perspektifli bir yüzeyi düzleştirerek hedef boyuta çizer.
        /// <paramref name="srcFromUnit"/> birim kareyi kaynaktaki dörtgene (piksel, sol-üst orijin) eşler;
        /// <paramref name="uvRange"/> hedef görselin bu birim karede kapsadığı alandır (dışarı taşabilir).
        /// Her hedef piksel 3x3 alt örnekle ortalanır; <paramref name="palette"/> verilirse renkler ona oturtulur.
        /// </summary>
        public static void PixelateQuad(string srcPath, string dstPath, int width, int height,
            Collector.ViewSystem.Homography srcFromUnit, Rect uvRange, Color32[] palette)
        {
            var (sp, sw, sh) = Load(srcPath);
            const int ss = 3;
            var outPx = new Color32[width * height];
            for (int yTop = 0; yTop < height; yTop++)
                for (int x = 0; x < width; x++)
                {
                    int r = 0, g = 0, b = 0;
                    for (int j = 0; j < ss; j++)
                        for (int i = 0; i < ss; i++)
                        {
                            float u = uvRange.xMin + (x + (i + 0.5f) / ss) / width * uvRange.width;
                            float v = uvRange.yMin + (yTop + (j + 0.5f) / ss) / height * uvRange.height;
                            var p = srcFromUnit.Map(u, v);
                            int ix = Mathf.Clamp(Mathf.FloorToInt(p.x), 0, sw - 1);
                            int iy = Mathf.Clamp(Mathf.FloorToInt(p.y), 0, sh - 1);
                            var c = sp[(sh - 1 - iy) * sw + ix];
                            r += c.r; g += c.g; b += c.b;
                        }
                    const int n = ss * ss;
                    outPx[(height - 1 - yTop) * width + x] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
                }

            if (palette != null && palette.Length > 0) MapToPalette(outPx, palette);
            else QuantizeInPlace(outPx, PaletteSize);
            SavePng(outPx, width, height, dstPath);
        }

        /// <summary>Bir görseldeki farklı renklerin listesi (ör. pixel art'a çevrilmiş odanın paleti).</summary>
        public static Color32[] PaletteFromImage(string path)
        {
            var (px, _, _) = Load(path);
            var seen = new System.Collections.Generic.HashSet<int>();
            var palette = new System.Collections.Generic.List<Color32>();
            foreach (var c in px)
                if (seen.Add(c.r << 16 | c.g << 8 | c.b)) palette.Add(new Color32(c.r, c.g, c.b, 255));
            return palette.ToArray();
        }

        /// <summary>Her pikseli paletteki en yakın renge oturtur.</summary>
        public static void MapToPalette(Color32[] px, Color32[] palette)
        {
            var cache = new System.Collections.Generic.Dictionary<int, Color32>();
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                int key = p.r << 16 | p.g << 8 | p.b;
                if (!cache.TryGetValue(key, out var best))
                {
                    int bestD = int.MaxValue;
                    foreach (var c in palette)
                    {
                        int dr = p.r - c.r, dg = p.g - c.g, db = p.b - c.b;
                        int d = dr * dr * 3 + dg * dg * 4 + db * db * 2; // göz yeşile daha duyarlı
                        if (d < bestD) { bestD = d; best = c; }
                    }
                    cache[key] = best;
                }
                px[i] = new Color32(best.r, best.g, best.b, p.a);
            }
        }

        public static void SavePng(Color32[] px, int width, int height, string dstPath)
        {
            var dst = new Texture2D(width, height, TextureFormat.RGBA32, false);
            dst.SetPixels32(px);
            dst.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
            File.WriteAllBytes(dstPath, dst.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(dst);
        }

        /// <summary>PNG'yi tam çözünürlükte bir <see cref="PixelCanvas"/>'a yükler.</summary>
        public static PixelCanvas LoadCanvas(string path)
        {
            var (pixels, width, height) = Load(path);
            var canvas = new PixelCanvas(width, height);
            Array.Copy(pixels, canvas.Pixels, pixels.Length);
            return canvas;
        }

        static (Color32[] pixels, int width, int height) Load(string path)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            var result = (tex.GetPixels32(), tex.width, tex.height);
            UnityEngine.Object.DestroyImmediate(tex);
            return result;
        }

        static Vector2Int ReadSize(string path)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            var size = new Vector2Int(tex.width, tex.height);
            UnityEngine.Object.DestroyImmediate(tex);
            return size;
        }

        // Basit k-means renk indirgeme. Başlangıç merkezleri parlaklık sırasına göre eşit aralıklı seçilir.
        public static void QuantizeInPlace(Color32[] px, int k, int iterations = 10)
        {
            int n = px.Length;
            var lum = new float[n];
            var order = new int[n];
            for (int i = 0; i < n; i++) { lum[i] = px[i].r * 0.299f + px[i].g * 0.587f + px[i].b * 0.114f; order[i] = i; }
            Array.Sort(lum, order);

            var cr = new float[k]; var cg = new float[k]; var cb = new float[k];
            for (int c = 0; c < k; c++)
            {
                var p = px[order[(int)((c + 0.5f) * n / k)]];
                cr[c] = p.r; cg[c] = p.g; cb[c] = p.b;
            }

            var assign = new int[n];
            var sr = new double[k]; var sg = new double[k]; var sb = new double[k]; var cnt = new int[k];
            for (int it = 0; it < iterations; it++)
            {
                Array.Clear(sr, 0, k); Array.Clear(sg, 0, k); Array.Clear(sb, 0, k); Array.Clear(cnt, 0, k);
                for (int i = 0; i < n; i++)
                {
                    var p = px[i];
                    int best = 0; float bestD = float.MaxValue;
                    for (int c = 0; c < k; c++)
                    {
                        float dr = p.r - cr[c], dg = p.g - cg[c], db = p.b - cb[c];
                        float d = dr * dr + dg * dg + db * db;
                        if (d < bestD) { bestD = d; best = c; }
                    }
                    assign[i] = best;
                    sr[best] += p.r; sg[best] += p.g; sb[best] += p.b; cnt[best]++;
                }
                for (int c = 0; c < k; c++)
                    if (cnt[c] > 0) { cr[c] = (float)(sr[c] / cnt[c]); cg[c] = (float)(sg[c] / cnt[c]); cb[c] = (float)(sb[c] / cnt[c]); }
            }

            for (int i = 0; i < n; i++)
            {
                int c = assign[i];
                px[i] = new Color32((byte)Mathf.RoundToInt(cr[c]), (byte)Mathf.RoundToInt(cg[c]), (byte)Mathf.RoundToInt(cb[c]), 255);
            }
        }
    }
}
