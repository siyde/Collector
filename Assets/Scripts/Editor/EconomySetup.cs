using Collector.Economy;
using Collector.TimeSystem;
using Collector.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 10: Para ve gün sistemi. GameSystems/Economy (Wallet + DayManager), odadaki yatak,
    /// HUD'da gün/para paneli (sağ üst), evet/hayır onay penceresi ve ekran karartma perdesi.
    /// Menü: Collector > Aşama 10 > Para ve Gün Sistemini Kur
    /// </summary>
    public static class EconomySetup
    {
        const int ScreenW = 640, ScreenH = 360;
        const int HudOrder = 30, DialogDimOrder = 70, DialogOrder = 71, DialogContentOrder = 72, FaderOrder = 90;

        // Yatak: odanın sol altındaki ekose battaniye (640x360 görselde x 0..82, y 255..360)
        static readonly Vector2 BedCenter = new Vector2(-8.72f, -3.98f), BedSize = new Vector2(2.56f, 3.28f);

        [MenuItem("Collector/Aşama 10/Para ve Gün Sistemini Kur")]
        static void SetupMenu() => StageSetup.RunFromMenu("Aşama 10",
            "Para ve gün sistemi kuruldu.\n\nPlay'e bas; sağ üstte gün ve para var. Yatağa tıklayıp uyu.");

        public static bool Setup()
        {
            var systems = GameObject.Find("GameSystems");
            var hotspots = GameObject.Find("Views/RoomView/Hotspots");
            var hud = Camera.main != null ? Camera.main.transform.Find("HUD") : null;
            if (systems == null || hotspots == null || hud == null)
            {
                EditorUtility.DisplayDialog("Aşama 10", "GameSystems, oda hotspot'ları veya HUD bulunamadı.", "Tamam");
                return false;
            }

            UiKit.EnsureSprites();

            Replace(systems.transform, "Economy");
            var economy = new GameObject("Economy");
            economy.transform.SetParent(systems.transform, false);
            economy.AddComponent<Wallet>();
            economy.AddComponent<DayManager>();

            CreateBed(hotspots.transform);
            CreateStatusHud(hud);
            CreateConfirmDialog(hud);
            CreateScreenFader(hud);
            return true;
        }

        static void CreateBed(Transform hotspots)
        {
            Replace(hotspots, "Hotspot_Bed");
            var go = new GameObject("Hotspot_Bed");
            go.transform.SetParent(hotspots, false);
            go.transform.localPosition = BedCenter;
            var col = go.AddComponent<BoxCollider2D>();                              // önce collider
            col.isTrigger = true;
            col.size = BedSize;
            go.AddComponent<BedHotspot>().SetTexts("Yatak", "Uyu (Sonraki Gün)");
        }

        // Sağ üst, seviye göstergesinin altı: "GÜN 1 ... CUMA" / (para) "300 TL"
        static void CreateStatusHud(Transform hud)
        {
            Replace(hud, "StatusHud");
            const int width = 150, height = 30;
            var root = UiKit.Node("StatusHud", hud, new Vector2(ScreenW - 8 - width, 8 + 30 + 4));
            UiKit.CreatePanel("Panel", root.transform, UiKit.Panel, Vector2.zero, new Vector2(width, height), HudOrder);
            var day = UiKit.CreateText("Day", root.transform, "Gün 1", UiKit.Cream, new Vector2(8, 2), HudOrder + 1);
            var weekday = UiKit.CreateText("Weekday", root.transform, "Cuma", UiKit.Dim, new Vector2(width - 8, 2), HudOrder + 1,
                PixelTextRenderer.Alignment.Right);
            UiKit.CreateSprite("Coin", root.transform, UiKit.Coin, new Vector2(11.5f, 20.5f), HudOrder + 1);
            var money = UiKit.CreateText("Money", root.transform, "300 TL", UiKit.Accent, new Vector2(18, 14), HudOrder + 1);
            root.AddComponent<StatusHudUI>().Configure(day, weekday, money);
        }

        // Ortada evet/hayır penceresi; arkası kararır, dışarı tıklamak "hayır" demektir
        static void CreateConfirmDialog(Transform hud)
        {
            Replace(hud, "ConfirmDialog");
            var root = UiKit.Node("ConfirmDialog", hud);
            var dialog = root.AddComponent<ConfirmDialog>();
            var window = UiKit.Node("Window", root.transform);

            var dim = UiKit.CreateSprite("Dim", window.transform, UiKit.Pixel, new Vector2(ScreenW * 0.5f, ScreenH * 0.5f), DialogDimOrder);
            dim.transform.localScale = new Vector3(ScreenW, ScreenH, 1f);
            dim.color = new Color(0.03f, 0.02f, 0.06f, 0.5f);
            var outside = UiKit.Node("OutsideBlocker", window.transform);
            UiKit.AddArea(outside, Vector2.zero, new Vector2(ScreenW, ScreenH));
            var outsideBlocker = outside.AddComponent<UiClickBlocker>();
            outsideBlocker.SetTexts("", "", tooltip: false, order: 150, highlight: false);
            UnityEventTools.AddPersistentListener(outsideBlocker.OnClick, dialog.Cancel);

            // Pencere: ekranın ortası orijin; panel boyutunu ConfirmDialog mesaja göre ayarlar
            const int height = 58;
            var frame = UiKit.Node("Frame", window.transform, new Vector2(ScreenW * 0.5f, ScreenH * 0.5f));
            var panelGo = UiKit.Node("Panel", frame.transform);
            var panel = panelGo.AddComponent<SpriteRenderer>();
            panel.sprite = UiKit.Panel;
            panel.drawMode = SpriteDrawMode.Sliced;
            panel.size = new Vector2(180, height) / UiKit.PPU;
            panel.sortingLayerName = "UI";
            panel.sortingOrder = DialogOrder;
            UiKit.AddArea(frame, new Vector2(-120, -height * 0.5f), new Vector2(240, height));
            frame.AddComponent<UiClickBlocker>().SetTexts("", "", tooltip: false, order: 160, highlight: false);

            var message = UiKit.CreateText("Message", frame.transform, "", UiKit.Cream, new Vector2(0, -22), DialogContentOrder,
                PixelTextRenderer.Alignment.Center);

            CreateDialogButton(frame.transform, dialog, "Yes", "Evet", new Vector2(-62, 6), true);
            CreateDialogButton(frame.transform, dialog, "No", "Hayır", new Vector2(6, 6), false);

            dialog.Configure(window, frame.transform, panel, message);
            window.SetActive(false);
        }

        static void CreateDialogButton(Transform frame, ConfirmDialog dialog, string name, string label, Vector2 topLeft, bool yes)
        {
            var size = new Vector2(56, 18);
            var go = UiKit.Node(name, frame, topLeft);
            UiKit.CreatePanel("Panel", go.transform, yes ? UiKit.CardSelected : UiKit.Card, Vector2.zero, size, DialogContentOrder);
            UiKit.CreateText("Label", go.transform, label, yes ? UiKit.Accent : UiKit.Cream, new Vector2(size.x * 0.5f, 3),
                DialogContentOrder + 1, PixelTextRenderer.Alignment.Center);
            UiKit.AddArea(go, Vector2.zero, size);
            var button = go.AddComponent<DialogButton>();
            button.SetTexts("", "", tooltip: false, order: 200);
            button.Configure(dialog, yes);
        }

        // Tüm ekranı karartan perde + ortada büyük gün başlığı
        static void CreateScreenFader(Transform hud)
        {
            Replace(hud, "ScreenFader");
            var root = UiKit.Node("ScreenFader", hud);
            var curtain = UiKit.CreateSprite("Curtain", root.transform, UiKit.Pixel, new Vector2(ScreenW * 0.5f, ScreenH * 0.5f), FaderOrder);
            curtain.transform.localScale = new Vector3(ScreenW, ScreenH, 1f);
            curtain.color = new Color(0.03f, 0.02f, 0.06f, 0f);

            var titleRoot = UiKit.Node("Title", root.transform);
            var title = UiKit.CreateText("Day", titleRoot.transform, "2. Gün", UiKit.Accent, new Vector2(ScreenW * 0.5f, ScreenH * 0.5f - 26),
                FaderOrder + 1, PixelTextRenderer.Alignment.Center, scale: 2);
            var subtitle = UiKit.CreateText("Weekday", titleRoot.transform, "Cumartesi", UiKit.Cream, new Vector2(ScreenW * 0.5f, ScreenH * 0.5f + 2),
                FaderOrder + 1, PixelTextRenderer.Alignment.Center);
            root.AddComponent<ScreenFader>().Configure(curtain, titleRoot, title, subtitle);
        }

        static void Replace(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
    }
}
