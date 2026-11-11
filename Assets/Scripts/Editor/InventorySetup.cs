using Collector.Collectibles;
using Collector.Inventory;
using Collector.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 6: Envanteri açık sahneye kurar: GameSystems/Inventory (InventoryManager) ve kameraya bağlı
    /// HUD (envanter penceresi + sol alttaki seçili oyuncak kutusu + "TAB ENVANTER" ipucu).
    /// HUD kameranın çocuğu olduğu için her görünümde ekranda sabit durur.
    /// Menü: Collector > Aşama 6 > Envanteri Kur
    /// </summary>
    public static class InventorySetup
    {
        const int ScreenW = 640, ScreenH = 360;
        const int WindowW = 404, WindowH = 280;
        const int CardW = 190, CardH = 44, CardGap = 8, CardRowGap = 6, Columns = 2, Rows = 4;
        const int HudOrder = 30, DimOrder = 40, WindowOrder = 41, CardOrder = 42, ContentOrder = 43;

        [MenuItem("Collector/Aşama 6/Envanteri Kur")]
        static void SetupMenu()
        {
            StageSetup.RunFromMenu("Aşama 6", "Envanter kuruldu.\n\nPlay'e bas, F2 ile test oyuncağı ekle, Tab ile envanteri aç.");
        }

        public static bool Setup()
        {
            var systems = GameObject.Find("GameSystems");
            var cam = Camera.main;
            if (systems == null || cam == null)
            {
                EditorUtility.DisplayDialog("Aşama 6", "GameSystems veya Main Camera bulunamadı. Önce önceki aşamaların kurulumu yapılmalı.", "Tamam");
                return false;
            }

            UiKit.EnsureSprites();

            // Envanter verisi
            var oldManager = systems.transform.Find("Inventory");
            if (oldManager != null) Object.DestroyImmediate(oldManager.gameObject);
            var managerGo = new GameObject("Inventory");
            managerGo.transform.SetParent(systems.transform, false);
            var database = AssetDatabase.LoadAssetAtPath<CollectibleDatabase>(CollectibleSetup.DatabasePath);
            if (database == null) Debug.LogWarning("[Collector] Oyuncak veritabanı yok; önce Aşama 5'i çalıştır (F2 testi çalışmaz).");
            managerGo.AddComponent<InventoryManager>().Configure(database);

            // Kameraya bağlı HUD; sol-üst köşesi ekranın sol-üst köşesi
            var oldHud = cam.transform.Find("HUD");
            if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);
            var hud = new GameObject("HUD");
            hud.transform.SetParent(cam.transform, false);
            hud.transform.localPosition = new Vector3(-ScreenW * 0.5f / UiKit.PPU, ScreenH * 0.5f / UiKit.PPU, 10f);

            // Sol alt: [TAB ENVANTER][M HARİTA][seçili oyuncak]; TAB'ın üstünde ESC GERİ (yakın planlarda)
            float keyHintWidth = CreateKeyHint(hud.transform);
            float mapHintX = 8 + keyHintWidth + 4;
            float mapHintWidth = CreateMapHint(hud.transform, mapHintX);
            CreateBackHint(hud.transform);
            CreateSelectedItemHud(hud.transform, mapHintX + mapHintWidth + 4);
            CreateInventoryWindow(hud.transform);
            return true;
        }

        // Sol alt: "TAB  ENVANTER"; genişliği döner
        static float CreateKeyHint(Transform hud)
        {
            var size = KeyPanelSize("TAB", "Envanter", out int keyW);
            var root = UiKit.Node("KeyHint", hud, new Vector2(8, ScreenH - 8 - size.y)).transform;
            UiKit.CreatePanel("Panel", root, UiKit.Panel, Vector2.zero, size, HudOrder);
            UiKit.CreateText("Key", root, "TAB", UiKit.Accent, new Vector2(5, UiKit.PanelBorder), HudOrder + 1);
            UiKit.CreateText("Label", root, "Envanter", UiKit.Cream, new Vector2(5 + keyW + 6, UiKit.PanelBorder), HudOrder + 1);
            return size.x;
        }

        // Sol alt, TAB ipucunun sağı: "M  HARİTA" (tıklanabilirliği Aşama 12 ekler); genişliği döner
        static float CreateMapHint(Transform hud, float x)
        {
            var size = KeyPanelSize("M", "Harita", out int keyW);
            var root = UiKit.Node("MapHint", hud, new Vector2(x, ScreenH - 8 - size.y)).transform;
            UiKit.CreatePanel("Panel", root, UiKit.Panel, Vector2.zero, size, HudOrder);
            UiKit.CreateText("Key", root, "M", UiKit.Accent, new Vector2(5, UiKit.PanelBorder), HudOrder + 1);
            UiKit.CreateText("Label", root, "Harita", UiKit.Cream, new Vector2(5 + keyW + 6, UiKit.PanelBorder), HudOrder + 1);
            return size.x;
        }

        // Sol alt, TAB ipucunun üstü: "ESC  GERİ" (tıklanabilir; yalnızca raf/bilgisayar görünümünde görünür)
        static void CreateBackHint(Transform hud)
        {
            var size = KeyPanelSize("ESC", "Geri", out int keyW);
            var root = UiKit.Node("BackHint", hud, new Vector2(8, ScreenH - 8 - size.y - 4 - size.y));
            var visual = UiKit.Node("Visual", root.transform);
            UiKit.CreatePanel("Panel", visual.transform, UiKit.Panel, Vector2.zero, size, HudOrder);
            UiKit.CreateText("Key", visual.transform, "ESC", UiKit.Accent, new Vector2(5, UiKit.PanelBorder), HudOrder + 1);
            UiKit.CreateText("Label", visual.transform, "Geri", UiKit.Cream, new Vector2(5 + keyW + 6, UiKit.PanelBorder), HudOrder + 1);
            UiKit.AddArea(root, Vector2.zero, size);                               // önce alan, sonra buton bileşeni
            var button = root.AddComponent<Collector.Interaction.BackButton>();
            button.SetTexts("Geri", "", tooltip: false, order: 100);
            button.ConfigureVisual(visual);
        }

        static Vector2 KeyPanelSize(string key, string label, out int keyWidth)
        {
            keyWidth = PixelFont.MeasureLine(key);
            int labelW = PixelFont.MeasureLine(PixelFont.Normalize(label));
            return new Vector2(5 + keyWidth + 6 + labelW + 5, PixelFont.GlyphHeight + UiKit.PanelBorder * 2 + 1);
        }

        // Sol alt, TAB ipucunun sağı: seçili oyuncak (ikon + "SEÇİLİ" + ad); alt kenarı TAB ile hizalı
        static void CreateSelectedItemHud(Transform hud, float x)
        {
            const int height = 44;
            var root = UiKit.Node("SelectedItem", hud, new Vector2(x, ScreenH - 8));                  // sol-alt köşe
            var box = UiKit.Node("Box", root.transform);
            var panel = UiKit.CreatePanel("Panel", box.transform, UiKit.Panel, new Vector2(0, -height), new Vector2(120, height), HudOrder);
            var icon = UiKit.CreateSprite("Icon", box.transform, null, new Vector2(6 + 16, -6), HudOrder + 1);
            var label = UiKit.CreateText("Label", box.transform, "Seçili", UiKit.Dim, new Vector2(44, -height + 7), HudOrder + 1);
            var itemName = UiKit.CreateText("Name", box.transform, "", UiKit.Cream, new Vector2(44, -height + 19), HudOrder + 1);
            root.AddComponent<SelectedItemHud>().Configure(box, panel, icon, label, itemName);
        }

        static void CreateInventoryWindow(Transform hud)
        {
            var root = UiKit.Node("Inventory", hud);
            var ui = root.AddComponent<InventoryUI>();
            var window = UiKit.Node("Window", root.transform);

            // Karartma perdesi: tüm ekran; dışarı tıklayınca kapatır
            var dim = UiKit.CreateSprite("Dim", window.transform, UiKit.Pixel, new Vector2(ScreenW * 0.5f, ScreenH * 0.5f), DimOrder);
            dim.transform.localScale = new Vector3(ScreenW, ScreenH, 1f);
            dim.color = new Color(0.03f, 0.02f, 0.06f, 0.6f);
            // Not: InteractableHotspot bir Collider2D ister; önce alan, sonra bileşen eklenmeli
            var outsideGo = UiKit.Node("OutsideBlocker", window.transform);
            UiKit.AddArea(outsideGo, Vector2.zero, new Vector2(ScreenW, ScreenH));
            var outside = outsideGo.AddComponent<UiClickBlocker>();
            outside.SetTexts("", "", tooltip: false, order: 50, highlight: false);
            UnityEventTools.AddPersistentListener(outside.OnClick, ui.Close);

            // Pencere
            var origin = new Vector2((ScreenW - WindowW) / 2, (ScreenH - WindowH) / 2);
            var frame = UiKit.Node("Frame", window.transform, origin);
            UiKit.CreatePanel("Panel", frame.transform, UiKit.Panel, Vector2.zero, new Vector2(WindowW, WindowH), WindowOrder);
            UiKit.AddArea(frame, Vector2.zero, new Vector2(WindowW, WindowH));
            var inside = frame.AddComponent<UiClickBlocker>();                 // pencerenin boş yerine tıklamak bir şey yapmaz
            inside.SetTexts("", "", tooltip: false, order: 60, highlight: false);

            var title = UiKit.CreateText("Title", frame.transform, "Envanter", UiKit.Accent, new Vector2(10, 5), ContentOrder);
            var count = UiKit.CreateText("Count", frame.transform, "0 oyuncak", UiKit.Dim, new Vector2(WindowW - 10, 5), ContentOrder,
                PixelTextRenderer.Alignment.Right);

            var slots = new InventorySlotUI[Columns * Rows];
            for (int i = 0; i < slots.Length; i++)
            {
                int col = i % Columns, row = i / Columns;
                var topLeft = new Vector2(CardGap + col * (CardW + CardGap), 24 + row * (CardH + CardRowGap));
                slots[i] = CreateCard(frame.transform, ui, "Card_" + (i + 1), topLeft);
            }

            int footerY = 24 + Rows * (CardH + CardRowGap) + 2;
            var prev = CreatePageButton(frame.transform, ui, "PreviousPage", "<", new Vector2(WindowW / 2 - 44, footerY), -1);
            var next = CreatePageButton(frame.transform, ui, "NextPage", ">", new Vector2(WindowW / 2 + 30, footerY), 1);
            var page = UiKit.CreateText("Page", frame.transform, "1/1", UiKit.Cream, new Vector2(WindowW / 2, footerY + 3), ContentOrder,
                PixelTextRenderer.Alignment.Center);

            var hint = UiKit.CreateText("Hint", frame.transform, "Tıkla: seç      Tab / ESC: kapat", UiKit.Dim,
                new Vector2(10, WindowH - 20), ContentOrder);
            var empty = UiKit.CreateText("Empty", frame.transform, "Envanter boş", UiKit.Dim, new Vector2(WindowW / 2, 110), ContentOrder,
                PixelTextRenderer.Alignment.Center);

            ui.Configure(window, title, hint, slots, prev, next, count, page, empty.gameObject);
            window.SetActive(false);
        }

        static InventorySlotUI CreateCard(Transform frame, InventoryUI ui, string name, Vector2 topLeft)
        {
            var card = UiKit.Node(name, frame, topLeft);
            var size = new Vector2(CardW, CardH);
            var background = UiKit.CreatePanel("Background", card.transform, UiKit.Card, Vector2.zero, size, CardOrder);
            var icon = UiKit.CreateSprite("Icon", card.transform, null, new Vector2(6 + 16, CardH - 6), ContentOrder); // alt-orta pivot
            var itemName = UiKit.CreateText("Name", card.transform, "", UiKit.Cream, new Vector2(44, 1), ContentOrder);
            var rarity = UiKit.CreateText("Rarity", card.transform, "", UiKit.Cream, new Vector2(44, 13), ContentOrder);
            var condition = UiKit.CreateText("Condition", card.transform, "", UiKit.Dim, new Vector2(44, 25), ContentOrder);
            UiKit.AddArea(card, Vector2.zero, size);

            var slot = card.AddComponent<InventorySlotUI>();
            slot.SetTexts("", "", tooltip: false, order: 100);
            slot.Configure(ui, background, icon, itemName, rarity, condition, UiKit.Card, UiKit.CardSelected);
            return slot;
        }

        static InventoryPageButton CreatePageButton(Transform frame, InventoryUI ui, string name, string arrow, Vector2 topLeft, int direction)
        {
            var go = UiKit.Node(name, frame, topLeft);
            var size = new Vector2(14, 18);
            UiKit.CreatePanel("Panel", go.transform, UiKit.Panel, Vector2.zero, size, CardOrder);
            UiKit.CreateText("Arrow", go.transform, arrow, UiKit.Accent, new Vector2(5, 3), ContentOrder);
            UiKit.AddArea(go, Vector2.zero, size);
            var button = go.AddComponent<InventoryPageButton>();
            button.SetTexts("", "", tooltip: false, order: 100);
            button.Configure(ui, direction);
            return button;
        }
    }
}
