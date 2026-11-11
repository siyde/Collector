using Collector.Interaction;
using Collector.UI;
using Collector.ViewSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 4: Fareyle etkileşim sistemini açık sahneye kurar: PointerInteractor, vurgu köşeleri,
    /// etiket kutusu, hotspot adları ve yakın plan görünümlerdeki "Geri" butonları.
    /// Builder da sahneyi oluştururken bunu çağırır.
    /// Menü: Collector > Aşama 4 > Etkileşim Sistemini Kur
    /// </summary>
    public static class InteractionSetup
    {
        // UI sorting order'ları: vurgu ve etiket her şeyin (envanter penceresi dahil) üstünde
        const int HighlightOrder = 80, TooltipOrder = 81;   // onay penceresinin (70-73) de üstünde

        [MenuItem("Collector/Aşama 4/Etkileşim Sistemini Kur")]
        static void SetupMenu()
        {
            StageSetup.RunFromMenu("Aşama 4", "Etkileşim sistemi kuruldu. Play'e basıp fareyi rafın ve bilgisayarın üzerinde gezdir.");
        }

        public static bool Setup()
        {
            var systems = GameObject.Find("GameSystems");
            if (systems == null)
            {
                EditorUtility.DisplayDialog("Aşama 4", "GameSystems bulunamadı. Önce Aşama 3 kurulumu yapılmalı.", "Tamam");
                return false;
            }

            UiKit.EnsureSprites();

            // Eski kurulum varsa baştan kur
            var old = systems.transform.Find("Interaction");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = new GameObject("Interaction");
            root.transform.SetParent(systems.transform, false);

            var highlight = CreateHighlight(root.transform);
            var tooltip = CreateTooltip(root.transform);
            root.AddComponent<PointerInteractor>().Configure(Camera.main, highlight, tooltip);

            ConfigureHotspot("Views/RoomView/Hotspots/Hotspot_Shelf", "Raf", "Rafa Bak");
            ConfigureHotspot("Views/RoomView/Hotspots/Hotspot_Computer", "Bilgisayar", "Bilgisayarı Kullan");

            // Geri butonu artık HUD'da (Aşama 9 düzeni, bkz. InventorySetup); eski görünüm içi butonları kaldır
            RemoveBackButton("Views/ShelfView");
            RemoveBackButton("Views/ComputerView");
            return true;
        }

        static void RemoveBackButton(string viewPath)
        {
            var old = GameObject.Find(viewPath)?.transform.Find("BackButton");
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        static HoverHighlight CreateHighlight(Transform parent)
        {
            var go = new GameObject("HoverHighlight");
            go.transform.SetParent(parent, false);
            string[] names = { "Corner_TL", "Corner_TR", "Corner_BR", "Corner_BL" };
            bool[] flipX = { false, true, true, false };
            bool[] flipY = { false, false, true, true };
            var corners = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                var sr = UiKit.CreateSprite(names[i], go.transform, UiKit.Corner, Vector2.zero, HighlightOrder);
                sr.flipX = flipX[i];
                sr.flipY = flipY[i];
                corners[i] = sr;
            }
            var highlight = go.AddComponent<HoverHighlight>();
            highlight.Configure(corners);
            return highlight;
        }

        static InteractionTooltip CreateTooltip(Transform parent)
        {
            var go = new GameObject("Tooltip");
            go.transform.SetParent(parent, false);
            var panel = UiKit.CreatePanel("Panel", go.transform, UiKit.Panel, Vector2.zero, new Vector2(16, 16), TooltipOrder);
            var title = UiKit.CreateText("Title", go.transform, "", UiKit.Accent, Vector2.zero, TooltipOrder + 1);
            var action = UiKit.CreateText("Action", go.transform, "", UiKit.Cream, Vector2.zero, TooltipOrder + 1);
            var tooltip = go.AddComponent<InteractionTooltip>();
            tooltip.Configure(panel, title, action);
            return tooltip;
        }

        static void ConfigureHotspot(string path, string displayName, string action)
        {
            var go = GameObject.Find(path);
            if (go == null || !go.TryGetComponent(out ViewHotspot hotspot))
            {
                Debug.LogWarning("[Collector] Hotspot bulunamadı: " + path);
                return;
            }
            hotspot.SetTexts(displayName, action);
            EditorUtility.SetDirty(hotspot);
        }
    }
}
