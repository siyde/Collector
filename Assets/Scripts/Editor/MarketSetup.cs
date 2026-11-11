using System.IO;
using Collector.Collectibles;
using Collector.Market;
using Collector.UI;
using Collector.ViewSystem;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 12: Bit pazarı. Views/MarketView (arka plan + 3 tezgahta 9 oyuncak yeri + tabela yazıları),
    /// GameSystems/Market (MarketManager), HUD'da şehir haritası (M) ve tezgah penceresi.
    /// Menü: Collector > Aşama 12 > Bit Pazarını Kur
    /// </summary>
    public static class MarketSetup
    {
        const float MarketViewX = 75f;
        const string ReferencePath = "Assets/Art/Reference/Reference_Market.png";
        const string BackgroundPath = "Assets/Art/Views/Market_Background.png";
        const string MapPath = "Assets/Art/UI/UI_Map.png";
        const string HomePinPath = "Assets/Art/UI/UI_PinHome.png";
        const string TentPinPath = "Assets/Art/UI/UI_PinMarket.png";
        const int ScreenW = 640, ScreenH = 360;
        const int DimOrder = 40, FrameOrder = 41, ContentOrder = 43, WindowBase = 44;

        static readonly string[] StallNames = { "Hasan Amca", "Nermin Teyze", "Kaan" };
        static readonly Color32 PaperInk = PixelCanvas.C("#3A2A20");
        static readonly Color32 PaperRed = PixelCanvas.C("#B0413E");

        [MenuItem("Collector/Aşama 12/Bit Pazarını Kur")]
        static void SetupMenu() => StageSetup.RunFromMenu("Aşama 12",
            "Bit pazarı kuruldu.\n\nPlay'e bas, M ile haritayı aç. Bit pazarı sadece Pazar (3. gün) açık; iki kez uyuyup git.");

        public static bool Setup()
        {
            var views = GameObject.Find("Views");
            var systems = GameObject.Find("GameSystems");
            var hud = Camera.main != null ? Camera.main.transform.Find("HUD") : null;
            if (views == null || systems == null || hud == null)
            {
                EditorUtility.DisplayDialog("Aşama 12", "Views, GameSystems veya HUD bulunamadı.", "Tamam");
                return false;
            }

            UiKit.EnsureSprites();
            var sprites = BuildSprites();

            var items = CreateMarketView(views.transform, sprites.background);

            Replace(systems.transform, "Market");
            var marketGo = new GameObject("Market");
            marketGo.transform.SetParent(systems.transform, false);
            var database = AssetDatabase.LoadAssetAtPath<CollectibleDatabase>(CollectibleSetup.DatabasePath);
            marketGo.AddComponent<MarketManager>().Configure(database, items);

            var map = CreateMapWindow(hud, sprites.map, sprites.home, sprites.tent);
            CreateStallWindow(hud);
            HookMapHint(hud, map);
            return true;
        }

        // ---------------------------------------------------------------- görseller

        static (Sprite background, Sprite map, Sprite home, Sprite tent) BuildSprites()
        {
            if (File.Exists(ReferencePath))
                AIArtPixelator.Pixelate(ReferencePath, BackgroundPath, ScreenW, ScreenH, AIArtPixelator.PaletteSize);
            else
                MarketArt.Market().SavePng(BackgroundPath);
            MarketArt.Map().SavePng(MapPath);
            MarketArt.HomePin().SavePng(HomePinPath);
            MarketArt.TentPin().SavePng(TentPinPath);
            foreach (var path in new[] { BackgroundPath, MapPath, HomePinPath, TentPinPath })
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return (AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath), AssetDatabase.LoadAssetAtPath<Sprite>(MapPath),
                    AssetDatabase.LoadAssetAtPath<Sprite>(HomePinPath), AssetDatabase.LoadAssetAtPath<Sprite>(TentPinPath));
        }

        // ---------------------------------------------------------------- pazar görünümü

        static MarketItem[] CreateMarketView(Transform views, Sprite background)
        {
            Replace(views, "MarketView");
            var view = new GameObject("MarketView");
            view.transform.SetParent(views, false);
            view.transform.localPosition = new Vector3(MarketViewX, 0f);

            var bgGo = new GameObject("Market_Background");
            bgGo.transform.SetParent(view.transform, false);
            var bg = bgGo.AddComponent<SpriteRenderer>();
            bg.sprite = background;
            bg.sortingLayerName = "Background";
            view.AddComponent<GameView>().Configure(ViewId.Market, bg, new Vector2[4]);

            // Piksel ofsetli çocuklar için görünümün sol-üst köşesi
            var topLeft = new GameObject("Layout");
            topLeft.transform.SetParent(view.transform, false);
            topLeft.transform.localPosition = new Vector3(-ScreenW * 0.5f / UiKit.PPU, ScreenH * 0.5f / UiKit.PPU);

            // Tabela yazıları
            for (int s = 0; s < MarketArt.StallCenters.Length; s++)
                UiKit.CreateText("Sign_" + (s + 1), topLeft.transform, StallNames[s], PaperInk,
                    new Vector2(MarketArt.StallCenters[s], 126), 1, PixelTextRenderer.Alignment.Center);

            // Masalardaki oyuncak yerleri (alt-orta pivot = masa üstü)
            var items = new MarketItem[MarketManager.StallCount * MarketManager.ItemsPerStall];
            int i = 0;
            foreach (int center in MarketArt.StallCenters)
                foreach (int offset in MarketArt.ItemOffsets)
                {
                    var go = UiKit.Node("Item_" + (i + 1), topLeft.transform, new Vector2(center + offset, MarketArt.TableTopY));
                    var spriteGo = new GameObject("Sprite");
                    spriteGo.transform.SetParent(go.transform, false);
                    var sr = spriteGo.AddComponent<SpriteRenderer>();
                    sr.sortingLayerName = "CollectiblesBack";

                    var col = go.AddComponent<BoxCollider2D>();                    // önce collider
                    col.isTrigger = true;
                    col.size = new Vector2(44f, 44f) / UiKit.PPU;
                    col.offset = new Vector2(0f, 22f) / UiKit.PPU;

                    var item = go.AddComponent<MarketItem>();
                    item.SetTexts("", "İncele", tooltip: true, order: 10);
                    item.Configure(sr);
                    items[i++] = item;
                }
            return items;
        }

        // ---------------------------------------------------------------- harita

        static MapUI CreateMapWindow(Transform hud, Sprite mapSprite, Sprite homePin, Sprite tentPin)
        {
            Replace(hud, "MapWindow");
            var root = UiKit.Node("MapWindow", hud);
            var map = root.AddComponent<MapUI>();
            var window = UiKit.Node("Window", root.transform);
            AddDimAndOutside(window.transform, map.Close);

            var size = new Vector2(300, 220);
            var frame = UiKit.Node("Frame", window.transform, new Vector2((ScreenW - size.x) / 2, (ScreenH - size.y) / 2));
            UiKit.CreatePanel("Panel", frame.transform, UiKit.Panel, Vector2.zero, size, FrameOrder);
            UiKit.AddArea(frame, Vector2.zero, size);
            frame.AddComponent<UiClickBlocker>().SetTexts("", "", tooltip: false, order: 60, highlight: false);

            UiKit.CreateText("Title", frame.transform, "Şehir Haritası", UiKit.Accent, new Vector2(8, 3), ContentOrder);
            UiKit.CreateText("Hint", frame.transform, "M / ESC", UiKit.Dim, new Vector2(size.x - 8, 3), ContentOrder,
                PixelTextRenderer.Alignment.Right);
            var mapOrigin = new Vector2(20, 20);
            UiKit.CreateSprite("Map", frame.transform, mapSprite, mapOrigin + new Vector2(MarketArt.MapW, MarketArt.MapH) * 0.5f, FrameOrder + 1);

            // Ev
            var home = mapOrigin + (Vector2)MarketArt.HomeOnMap;
            UiKit.CreateSprite("HomePin", frame.transform, homePin, home + new Vector2(0.5f, 0.5f), ContentOrder);
            UiKit.CreateText("HomeLabel", frame.transform, "Evin", PaperInk, home + new Vector2(0, 9), ContentOrder,
                PixelTextRenderer.Alignment.Center);
            var homeMarker = UiKit.CreateText("HomeHere", frame.transform, "Buradasın", PaperRed, home + new Vector2(0, -20), ContentOrder,
                PixelTextRenderer.Alignment.Center);
            CreateMapButton(frame.transform, "GoHome", home + new Vector2(-24, -10), new Vector2(48, 32), "Evin", map.GoHome);

            // Bit pazarı
            var market = mapOrigin + (Vector2)MarketArt.MarketOnMap;
            UiKit.CreateSprite("MarketPin", frame.transform, tentPin, market + new Vector2(0.5f, 0.5f), ContentOrder);
            UiKit.CreateText("MarketLabel", frame.transform, "Bit Pazarı", PaperInk, market + new Vector2(0, 9), ContentOrder,
                PixelTextRenderer.Alignment.Center);
            var status = UiKit.CreateText("MarketStatus", frame.transform, "", PaperRed, market + new Vector2(0, 21), ContentOrder,
                PixelTextRenderer.Alignment.Center);
            var marketMarker = UiKit.CreateText("MarketHere", frame.transform, "Buradasın", PaperRed, market + new Vector2(0, -20), ContentOrder,
                PixelTextRenderer.Alignment.Center);
            CreateMapButton(frame.transform, "GoMarket", market + new Vector2(-40, -10), new Vector2(80, 44), "Bit Pazarı", map.GoMarket);

            UiKit.CreateText("Footer", frame.transform, "Bir yere tıkla ve oraya git.", UiKit.Dim, new Vector2(8, size.y - 16), ContentOrder);

            map.Configure(window, homeMarker.gameObject, marketMarker.gameObject, status);
            return map;
        }

        static void CreateMapButton(Transform parent, string name, Vector2 topLeft, Vector2 size, string label,
            UnityEngine.Events.UnityAction action)
        {
            var go = UiKit.Node(name, parent);
            UiKit.AddArea(go, topLeft, size);                                      // önce alan
            var button = go.AddComponent<UiButton>();
            button.SetTexts(label, "Git", tooltip: true, order: 100);
            UnityEventTools.AddPersistentListener(button.OnClick, action);
        }

        // ---------------------------------------------------------------- tezgah penceresi

        static void CreateStallWindow(Transform hud)
        {
            Replace(hud, "StallWindow");
            var root = UiKit.Node("StallWindow", hud);
            var stall = root.AddComponent<MarketStallWindow>();
            var window = UiKit.Node("Window", root.transform);
            AddDimAndOutside(window.transform, stall.Close);

            var size = ShopSetup.HostedWindowSize;
            var frame = UiKit.Node("Frame", window.transform, new Vector2((ScreenW - size.x) / 2, (ScreenH - size.y) / 2));
            UiKit.AddArea(frame, Vector2.zero, size);
            frame.AddComponent<UiClickBlocker>().SetTexts("", "", tooltip: false, order: 60, highlight: false);

            var (title, balance, detail) = ShopSetup.BuildHostedWindow(frame.transform, stall, stall.Close, WindowBase, "< Kapat");
            stall.Configure(window, title, balance, detail);
        }

        // Karartma perdesi + dışarı tıklayınca kapatan engelleyici
        static void AddDimAndOutside(Transform window, UnityEngine.Events.UnityAction close)
        {
            var dim = UiKit.CreateSprite("Dim", window, UiKit.Pixel, new Vector2(ScreenW * 0.5f, ScreenH * 0.5f), DimOrder);
            dim.transform.localScale = new Vector3(ScreenW, ScreenH, 1f);
            dim.color = new Color(0.03f, 0.02f, 0.06f, 0.55f);
            var outside = UiKit.Node("OutsideBlocker", window);
            UiKit.AddArea(outside, Vector2.zero, new Vector2(ScreenW, ScreenH));
            var blocker = outside.AddComponent<UiClickBlocker>();
            blocker.SetTexts("", "", tooltip: false, order: 50, highlight: false);
            UnityEventTools.AddPersistentListener(blocker.OnClick, close);
        }

        // Sol alttaki "M HARİTA" ipucu tıklanınca haritayı aç/kapa
        static void HookMapHint(Transform hud, MapUI map)
        {
            var hint = hud.Find("MapHint");
            if (hint == null) return;
            foreach (var old in hint.GetComponents<UiButton>()) Object.DestroyImmediate(old);
            foreach (var old in hint.GetComponents<BoxCollider2D>()) Object.DestroyImmediate(old);
            var panel = hint.Find("Panel")?.GetComponent<SpriteRenderer>();
            var size = panel != null ? panel.size * UiKit.PPU : new Vector2(50, 18);
            UiKit.AddArea(hint.gameObject, Vector2.zero, size);
            var button = hint.gameObject.AddComponent<UiButton>();
            button.SetTexts("", "", tooltip: false, order: 100);
            UnityEventTools.AddPersistentListener(button.OnClick, map.Toggle);
        }

        static void Replace(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
    }
}
