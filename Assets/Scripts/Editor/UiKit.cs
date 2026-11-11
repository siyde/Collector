using Collector.UI;
using UnityEditor;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// Editor kurulum araçlarının ortak arayüz parçaları: palet, kodla çizilen UI sprite'ları,
    /// 9-slice panel ve piksel yazı oluşturma. Tüm arayüz dünyada, UI sorting layer'ında çizilir.
    /// </summary>
    public static class UiKit
    {
        public const float PPU = 32f;
        public const int PanelBorder = 3;
        const string UiFolder = "Assets/Art/UI";
        const string TextMaterialPath = "Assets/Shaders/PixelText.mat";

        // Arayüz paleti: koyu lacivert, siyah, krem, sarı/turuncu vurgu
        public static readonly Color32 Black = PixelCanvas.C("#07070D");
        public static readonly Color32 Cream = PixelCanvas.C("#F2E8D0");
        public static readonly Color32 NavyDark = PixelCanvas.C("#141833");
        public static readonly Color32 Navy = PixelCanvas.C("#1C2344");
        public static readonly Color32 Accent = PixelCanvas.C("#F2C14E");
        public static readonly Color32 Dim = PixelCanvas.C("#8A8FB0");

        public static Sprite Panel => Load("UI_Panel");
        public static Sprite Corner => Load("UI_Corner");
        public static Sprite Card => Load("UI_Card");
        public static Sprite CardSelected => Load("UI_CardSelected");
        public static Sprite Pixel => Load("UI_Pixel");
        public static Sprite Sparkle => Load("UI_Sparkle");
        public static Sprite CheckEmpty => Load("UI_CheckEmpty");
        public static Sprite CheckDone => Load("UI_CheckDone");
        public static Sprite Coin => Load("UI_Coin");

        /// <summary>Bütün UI sprite'larını çizer ve 9-slice kenarlarını ayarlar.</summary>
        public static void EnsureSprites()
        {
            // 9x9 panel: siyah dış çizgi, krem iç çizgi, koyu lacivert gölge, lacivert dolgu (kenar 3 px)
            var panel = new PixelCanvas(9, 9);
            panel.Rect(0, 0, 9, 9, Black);
            panel.Rect(1, 1, 7, 7, Cream);
            panel.Rect(2, 2, 5, 5, NavyDark);
            panel.Rect(3, 3, 3, 3, Navy);
            Save("UI_Panel", panel, PanelBorder);

            // 6x6 sol-üst köşe işareti (diğer köşeler aynalanır): sarı L, siyah kontur
            string[] rows = { "KKKKKK", "KYYYYK", "KYKKKK", "KYK...", "KYK...", "KKK..." };
            var corner = new PixelCanvas(6, 6);
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 6; c++)
                    if (rows[r][c] != '.') corner.Set(c, 5 - r, rows[r][c] == 'Y' ? Accent : Black);
            Save("UI_Corner", corner, 0);

            // Envanter kartları (5x5, kenar 2 px): normal ve seçili
            Save("UI_Card", Card5(PixelCanvas.C("#3A4270"), PixelCanvas.C("#2A3160"), PixelCanvas.C("#232A52")), 2);
            Save("UI_CardSelected", Card5(Accent, PixelCanvas.C("#B8872E"), PixelCanvas.C("#2E3566")), 2);

            // 1x1 beyaz piksel (karartma perdesi, toz parçacıkları)
            var pixel = new PixelCanvas(1, 1);
            pixel.Set(0, 0, new Color32(255, 255, 255, 255));
            Save("UI_Pixel", pixel, 0);

            // 5x5 parıltı: beyaz merkez, sarı kollar
            var sparkle = new PixelCanvas(5, 5);
            sparkle.HLine(0, 2, 5, Accent);
            sparkle.VLine(2, 0, 5, Accent);
            sparkle.Set(2, 2, new Color32(255, 255, 255, 255));
            Save("UI_Sparkle", sparkle, 0);

            // 7x7 madeni para (para göstergesi)
            string[] coinRows = { "..###..", ".#ooo#.", "#ooWoo#", "#ooooo#", "#ooooo#", ".#ooo#.", "..###.." };
            var coin = new PixelCanvas(7, 7);
            for (int r = 0; r < 7; r++)
                for (int col = 0; col < 7; col++)
                {
                    char ch = coinRows[r][col];
                    if (ch == '.') continue;
                    coin.Set(col, 6 - r, ch == '#' ? PixelCanvas.C("#B8872E") : ch == 'W' ? PixelCanvas.C("#FFF4C8") : Accent);
                }
            Save("UI_Coin", coin, 0);

            // 9x9 görev kutucukları: boş ve işaretli (sarı tik)
            Save("UI_CheckEmpty", CheckBox(null), 0);
            Save("UI_CheckDone", CheckBox(new[] { ".....", "....Y", "...Y.", "Y.Y..", ".Y..." }), 0);
        }

        static PixelCanvas CheckBox(string[] mark)
        {
            var c = new PixelCanvas(9, 9);
            c.Rect(0, 0, 9, 9, Black);
            c.Rect(1, 1, 7, 7, Cream);
            c.Rect(2, 2, 5, 5, NavyDark);
            if (mark != null)
                for (int r = 0; r < 5; r++)
                    for (int col = 0; col < 5; col++)
                        if (mark[r][col] == 'Y') c.Set(2 + col, 6 - r, Accent);
            return c;
        }

        static PixelCanvas Card5(Color32 edge, Color32 inner, Color32 fill)
        {
            var c = new PixelCanvas(5, 5);
            c.Rect(0, 0, 5, 5, edge);
            c.Rect(1, 1, 3, 3, inner);
            c.Set(2, 2, fill);
            return c;
        }

        static void Save(string name, PixelCanvas canvas, int border)
        {
            var path = $"{UiFolder}/{name}.png";
            canvas.SavePng(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var target = new Vector4(border, border, border, border);
            if (importer.spriteBorder != target)
            {
                importer.spriteBorder = target;
                importer.SaveAndReimport();
            }
        }

        static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiFolder}/{name}.png");

        public static Material TextMaterial
        {
            get
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(TextMaterialPath);
                if (material != null) return material;
                var shader = Shader.Find(ViewSystemSetup.ShaderName);
                if (shader == null) { Debug.LogError("[Collector] Shader bulunamadı: " + ViewSystemSetup.ShaderName); return null; }
                material = new Material(shader) { name = "PixelText" };
                AssetDatabase.CreateAsset(material, TextMaterialPath);
                return material;
            }
        }

        // ---------------------------------------------------------------- nesne oluşturma

        public static GameObject Node(string name, Transform parent, Vector2 pixelOffset = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pixelOffset.x, -pixelOffset.y) / PPU; // piksel, aşağı doğru pozitif
            return go;
        }

        /// <summary>9-slice panel; <paramref name="topLeftPx"/> ebeveyne göre sol-üst köşe (piksel, aşağı pozitif).</summary>
        public static SpriteRenderer CreatePanel(string name, Transform parent, Sprite sprite, Vector2 topLeftPx, Vector2 sizePx, int order)
        {
            var go = Node(name, parent, topLeftPx + sizePx * 0.5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = sizePx / PPU;
            sr.sortingLayerName = "UI";
            sr.sortingOrder = order;
            return sr;
        }

        public static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Vector2 offsetPx, int order)
        {
            var go = Node(name, parent, offsetPx);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "UI";
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Düz renkli dikdörtgen (1x1 pikselin ölçeklenmesiyle); sol-üst köşe ve boyut piksel.</summary>
        public static SpriteRenderer CreateRect(string name, Transform parent, Vector2 topLeftPx, Vector2 sizePx, Color32 color, int order)
        {
            var sr = CreateSprite(name, parent, Pixel, topLeftPx + sizePx * 0.5f, order);
            sr.transform.localScale = new Vector3(sizePx.x, sizePx.y, 1f);
            sr.color = color;
            return sr;
        }

        /// <summary>Piksel yazı; <paramref name="offsetPx"/> yazının sol-üst köşesi (aksan satırları dahil).</summary>
        public static PixelTextRenderer CreateText(string name, Transform parent, string text, Color32 color, Vector2 offsetPx,
            int order, PixelTextRenderer.Alignment alignment = PixelTextRenderer.Alignment.Left, int scale = 1)
        {
            var go = Node(name, parent, offsetPx);
            var textRenderer = go.AddComponent<PixelTextRenderer>();
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = TextMaterial;
            meshRenderer.sortingLayerName = "UI";
            meshRenderer.sortingOrder = order;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            textRenderer.Configure(text, color, alignment, scale);
            return textRenderer;
        }

        /// <summary>Tıklama alanı: sol-üst köşeden başlayan trigger kutu.</summary>
        public static BoxCollider2D AddArea(GameObject go, Vector2 topLeftPx, Vector2 sizePx)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = sizePx / PPU;
            col.offset = new Vector2(topLeftPx.x + sizePx.x * 0.5f, -(topLeftPx.y + sizePx.y * 0.5f)) / PPU;
            return col;
        }
    }
}
