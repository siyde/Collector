using System.Collections.Generic;
using Collector.Boxes;
using Collector.Collectibles;
using Collector.Shop;
using Collector.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 11: Bilgisayardaki ikinci el ilanları. GameSystems/Shop (ListingsManager + CargoSpawner),
    /// odada pasif kargo kutusu şablonu, masaüstündeki İLANLAR simgesi ve CRT ekranın içindeki ilan penceresi.
    /// Menü: Collector > Aşama 11 > İlan Sistemini Kur
    /// </summary>
    public static class ShopSetup
    {
        // Pencere, bilgisayar görünümünde (640x360, sol-üst orijin): masaüstü simgelerinin sağı, HUD'un solu
        static readonly Vector2 WindowPos = new Vector2(188, 44), WindowSize = new Vector2(290, 266);
        const int RowWidth = 282, RowHeight = 56, RowGap = 2, RowTop = 32, Rows = 4;
        static int Base = 20;  // UI sorting order tabanı (HUD 30+ bunun üstünde kalır; HUD pencereleri için geçici değişir)

        // Masaüstündeki İLANLAR simgesi + etiketi (ViewArt.ComputerScreen çizimi)
        static readonly Vector2 IconPos = new Vector2(140, 42), IconSize = new Vector2(48, 42);

        // Kargo kutularının odadaki yerleri (alt-orta pivot; 640x360 görselde y=355, x=585/528/471)
        static readonly Vector2[] CargoSpots =
        {
            new Vector2((585 - 320) / 32f, (180 - 355) / 32f),
            new Vector2((528 - 320) / 32f, (180 - 355) / 32f),
            new Vector2((471 - 320) / 32f, (180 - 355) / 32f),
        };

        static readonly Color32 Ink = PixelCanvas.C("#2B1E2F");
        static readonly Color32 WindowBody = PixelCanvas.C("#C9C0B2");
        static readonly Color32 BevelLight = PixelCanvas.C("#F0E8DA");
        static readonly Color32 BevelDark = PixelCanvas.C("#7C7466");
        static readonly Color32 TitleBar = PixelCanvas.C("#1E2A5A");
        static readonly Color32 RowBody = PixelCanvas.C("#EDE6D6");
        static readonly Color32 RowEdge = PixelCanvas.C("#8A8070");
        static readonly Color32 PriceRed = PixelCanvas.C("#B0413E");
        static readonly Color32 Muted = PixelCanvas.C("#5A5248");
        static readonly Color32 Faint = PixelCanvas.C("#7A7064");

        [MenuItem("Collector/Aşama 11/İlan Sistemini Kur")]
        static void SetupMenu() => StageSetup.RunFromMenu("Aşama 11",
            "İlan sistemi kuruldu.\n\nPlay'e bas, bilgisayara gir ve masaüstündeki İLANLAR simgesine tıkla.");

        public static bool Setup()
        {
            var systems = GameObject.Find("GameSystems");
            var room = GameObject.Find("Views/RoomView");
            var computer = GameObject.Find("Views/ComputerView");
            var tutorialBox = room != null ? room.transform.Find("TutorialBox") : null;
            if (systems == null || room == null || computer == null || tutorialBox == null)
            {
                EditorUtility.DisplayDialog("Aşama 11", "GameSystems, oda, bilgisayar görünümü veya başlangıç kutusu bulunamadı.", "Tamam");
                return false;
            }

            UiKit.EnsureSprites();

            // Kargo kutusu şablonu: başlangıç kutusunun boş, pasif bir kopyası
            Replace(room.transform, "CargoBoxTemplate");
            var template = Object.Instantiate(tutorialBox.gameObject, room.transform);
            template.name = "CargoBoxTemplate";
            var templateBox = template.GetComponent<ItemBox>();
            templateBox.SetContents(new List<BoxItem>());
            templateBox.SetTexts("Kargo Kutusu", "Kutuyu Aç", tooltip: true, order: 5);
            template.SetActive(false);

            Replace(systems.transform, "Shop");
            var shop = new GameObject("Shop");
            shop.transform.SetParent(systems.transform, false);
            var cargo = shop.AddComponent<CargoSpawner>();
            cargo.Configure(templateBox, CargoSpots);
            var database = AssetDatabase.LoadAssetAtPath<CollectibleDatabase>(CollectibleSetup.DatabasePath);
            shop.AddComponent<ListingsManager>().Configure(database, cargo);

            // Bilgisayar ekranı: kök, görünümün sol-üst köşesi (piksel ofsetleri bundan)
            Replace(computer.transform, "Screen");
            var screen = new GameObject("Screen");
            screen.transform.SetParent(computer.transform, false);
            screen.transform.localPosition = new Vector3(-10f, 5.625f);

            var app = UiKit.Node("ListingsApp", screen.transform);
            var window = app.AddComponent<ListingsWindow>();
            CreateDesktopIcon(screen.transform, window);
            CreateWindow(app.transform, window);
            return true;
        }

        static void CreateDesktopIcon(Transform screen, ListingsWindow window)
        {
            var icon = UiKit.Node("Icon_Ilanlar", screen);
            UiKit.AddArea(icon, IconPos, IconSize);                                  // önce alan
            var button = icon.AddComponent<UiButton>();
            button.SetTexts("İlanlar", "Aç", tooltip: true, order: 10);
            UnityEventTools.AddPersistentListener(button.OnClick, window.Open);
        }

        static void CreateWindow(Transform app, ListingsWindow listingsWindow)
        {
            var window = UiKit.Node("Window", app, WindowPos);
            var w = WindowSize;

            // Arkaya tıklanmasın
            UiKit.AddArea(window, Vector2.zero, w);
            window.AddComponent<UiClickBlocker>().SetTexts("", "", tooltip: false, order: 50, highlight: false);

            DrawWindowFrame(window.transform, w, "İkinci El İlanları", listingsWindow.Close);

            // Üst bilgi satırı (her iki sayfada da görünür)
            var day = UiKit.CreateText("Day", window.transform, "Gün 1", Ink, new Vector2(6, 17), Base + 3);
            var balance = UiKit.CreateText("Balance", window.transform, "Paran: 300 TL", Ink, new Vector2(w.x - 6, 17), Base + 3,
                PixelTextRenderer.Alignment.Right);

            // Liste sayfası
            var listPage = UiKit.Node("ListPage", window.transform);
            var rows = new ListingRowUI[Rows];
            for (int i = 0; i < Rows; i++)
                rows[i] = CreateRow(listPage.transform, listingsWindow, i, new Vector2(4, RowTop + i * (RowHeight + RowGap)));
            var empty = UiKit.CreateText("Empty", listPage.transform, "Bugün ilan yok", Muted, new Vector2(w.x * 0.5f, 120), Base + 3,
                PixelTextRenderer.Alignment.Center);

            // İlan sayfası (pazarlık)
            var detail = CreateDetailPage(window.transform, listingsWindow);

            listingsWindow.Configure(window, listPage, detail, rows, balance, day, empty.gameObject);
            window.SetActive(false);
        }

        /// <summary>
        /// Retro pencere (gri gövde, kabarık kenar, lacivert başlık çubuğu, [X]) ve içinde ilan sayfası kurar.
        /// Bit pazarındaki tezgah penceresi de bunu kullanır; <paramref name="baseOrder"/> UI sorting tabanıdır.
        /// </summary>
        public static (PixelTextRenderer title, PixelTextRenderer balance, ListingDetailUI detail) BuildHostedWindow(
            Transform window, ListingHost host, UnityEngine.Events.UnityAction close, int baseOrder, string backLabel)
        {
            int previous = Base;
            Base = baseOrder;
            var title = DrawWindowFrame(window, WindowSize, "", close);
            var balance = UiKit.CreateText("Balance", window, "", Ink, new Vector2(WindowSize.x - 6, 17), Base + 3,
                PixelTextRenderer.Alignment.Right);
            var detail = CreateDetailPage(window, host, backLabel);
            detail.gameObject.SetActive(true);
            Base = previous;
            return (title, balance, detail);
        }

        public static Vector2 HostedWindowSize => WindowSize;

        // Çerçeve: siyah dış çizgi, gri gövde, kabarık kenarlar (üst/sol açık, alt/sağ koyu), başlık çubuğu ve [X]
        static PixelTextRenderer DrawWindowFrame(Transform window, Vector2 w, string titleText, UnityEngine.Events.UnityAction close)
        {
            UiKit.CreateRect("Outline", window, Vector2.zero, w, Ink, Base);
            UiKit.CreateRect("Body", window, Vector2.one, w - Vector2.one * 2, WindowBody, Base + 1);
            UiKit.CreateRect("BevelTop", window, new Vector2(1, 1), new Vector2(w.x - 2, 1), BevelLight, Base + 2);
            UiKit.CreateRect("BevelLeft", window, new Vector2(1, 1), new Vector2(1, w.y - 2), BevelLight, Base + 2);
            UiKit.CreateRect("BevelBottom", window, new Vector2(1, w.y - 2), new Vector2(w.x - 2, 1), BevelDark, Base + 2);
            UiKit.CreateRect("BevelRight", window, new Vector2(w.x - 2, 1), new Vector2(1, w.y - 2), BevelDark, Base + 2);

            UiKit.CreateRect("TitleBar", window, new Vector2(3, 3), new Vector2(w.x - 6, 12), TitleBar, Base + 2);
            var title = UiKit.CreateText("Title", window, titleText, UiKit.Cream, new Vector2(6, 2), Base + 3);
            var closeGo = UiKit.Node("CloseButton", window, new Vector2(w.x - 18, 4));
            UiKit.CreateRect("Face", closeGo.transform, Vector2.zero, new Vector2(12, 10), WindowBody, Base + 3);
            UiKit.CreateRect("Edge", closeGo.transform, new Vector2(0, 9), new Vector2(12, 1), BevelDark, Base + 4);
            UiKit.CreateText("X", closeGo.transform, "X", Ink, new Vector2(4, 0), Base + 4);
            UiKit.AddArea(closeGo, Vector2.zero, new Vector2(12, 10));
            var closeButton = closeGo.AddComponent<UiButton>();
            closeButton.SetTexts("", "", tooltip: false, order: 100);
            UnityEventTools.AddPersistentListener(closeButton.OnClick, close);
            return title;
        }

        // İlan sayfası: görsel + bilgiler, açıklama, satıcı, fiyat, cevap kutusu, teklif satırı, alt butonlar
        static ListingDetailUI CreateDetailPage(Transform window, ListingHost owner, string backLabel = "< İlanlar")
        {
            var page = UiKit.Node("DetailPage", window);
            var t = page.transform;
            var detail = page.AddComponent<ListingDetailUI>();
            var refs = new ListingDetailUI.Refs();

            UiKit.CreateRect("ImageEdge", t, new Vector2(6, 30), new Vector2(62, 62), RowEdge, Base + 2);
            UiKit.CreateRect("ImageBody", t, new Vector2(7, 31), new Vector2(60, 60), RowBody, Base + 3);
            refs.image = UiKit.CreateSprite("Image", t, null, new Vector2(37, 86), Base + 4);          // 48 px, alt-orta pivot

            refs.itemName = UiKit.CreateText("Name", t, "", Ink, new Vector2(74, 30), Base + 4);
            refs.rarity = UiKit.CreateText("Rarity", t, "", Ink, new Vector2(74, 42), Base + 4);
            refs.condition = UiKit.CreateText("Condition", t, "", Muted, new Vector2(74, 54), Base + 4);
            refs.year = UiKit.CreateText("Year", t, "", Muted, new Vector2(74, 66), Base + 4);
            refs.series = UiKit.CreateText("Series", t, "", Muted, new Vector2(74, 78), Base + 4);
            refs.description = UiKit.CreateText("Description", t, "", Ink, new Vector2(6, 96), Base + 4);

            refs.seller = UiKit.CreateText("Seller", t, "", Faint, new Vector2(6, 136), Base + 4);
            refs.note = UiKit.CreateText("Note", t, "", Faint, new Vector2(6, 148), Base + 4);
            refs.price = UiKit.CreateText("Price", t, "", PriceRed, new Vector2(6, 163), Base + 4);
            refs.originalPrice = UiKit.CreateText("OriginalPrice", t, "", Faint, new Vector2(284, 163), Base + 4,
                PixelTextRenderer.Alignment.Right);

            // Satıcının son cevabı
            UiKit.CreateRect("ReplyEdge", t, new Vector2(6, 177), new Vector2(278, 28), RowEdge, Base + 2);
            UiKit.CreateRect("ReplyBody", t, new Vector2(7, 178), new Vector2(276, 26), RowBody, Base + 3);
            refs.reply = UiKit.CreateText("Reply", t, "", Ink, new Vector2(10, 180), Base + 4);

            // Teklif satırı: TEKLİF  [-25][-5] [ 120 TL ] [+5][+25]  [TEKLİF VER]
            var controls = UiKit.Node("OfferControls", t);
            var c = controls.transform;
            UiKit.CreateText("Label", c, "Teklif:", Ink, new Vector2(6, 211), Base + 4);
            CreateButton(c, "Minus25", "-25", new Vector2(44, 209), new Vector2(22, 16), false, detail.OfferMinus25);
            CreateButton(c, "Minus5", "-5", new Vector2(68, 209), new Vector2(18, 16), false, detail.OfferMinus5);
            UiKit.CreateRect("OfferEdge", c, new Vector2(88, 209), new Vector2(54, 16), Ink, Base + 3);
            UiKit.CreateRect("OfferBody", c, new Vector2(89, 210), new Vector2(52, 14), RowBody, Base + 4);
            refs.offer = UiKit.CreateText("Offer", c, "", Ink, new Vector2(115, 211), Base + 5, PixelTextRenderer.Alignment.Center);
            CreateButton(c, "Plus5", "+5", new Vector2(144, 209), new Vector2(18, 16), false, detail.OfferPlus5);
            CreateButton(c, "Plus25", "+25", new Vector2(164, 209), new Vector2(22, 16), false, detail.OfferPlus25);
            CreateButton(c, "Submit", "Teklif Ver", new Vector2(200, 209), new Vector2(84, 16), true, detail.SubmitOffer);
            refs.offerControls = controls;

            refs.status = UiKit.CreateText("Status", t, "", PriceRed, new Vector2(145, 211), Base + 4, PixelTextRenderer.Alignment.Center);

            CreateButton(t, "Back", backLabel, new Vector2(6, 240), new Vector2(70, 18), false, detail.Back);
            refs.buyButton = CreateButton(t, "Buy", "Satın Al", new Vector2(206, 240), new Vector2(78, 18), true, detail.Buy);

            detail.Configure(owner, refs);
            page.SetActive(false);
            return detail;
        }

        /// <summary>Kart görünümlü buton; birincil butonlar sarı çerçeveli.</summary>
        static GameObject CreateButton(Transform parent, string name, string label, Vector2 topLeft, Vector2 size, bool primary,
            UnityEngine.Events.UnityAction action)
        {
            var go = UiKit.Node(name, parent, topLeft);
            UiKit.CreatePanel("Panel", go.transform, primary ? UiKit.CardSelected : UiKit.Card, Vector2.zero, size, Base + 4);
            UiKit.CreateText("Label", go.transform, label, primary ? UiKit.Accent : UiKit.Cream,
                new Vector2(size.x * 0.5f, size.y >= 18 ? 3 : 2), Base + 5, PixelTextRenderer.Alignment.Center);
            UiKit.AddArea(go, Vector2.zero, size);                                   // önce alan, sonra buton
            var button = go.AddComponent<UiButton>();
            button.SetTexts("", "", tooltip: false, order: 100);
            UnityEventTools.AddPersistentListener(button.OnClick, action);
            return go;
        }

        static ListingRowUI CreateRow(Transform window, ListingsWindow owner, int index, Vector2 topLeft)
        {
            var row = UiKit.Node("Row_" + (index + 1), window, topLeft);
            var t = row.transform;
            UiKit.CreateRect("Edge", t, Vector2.zero, new Vector2(RowWidth, RowHeight), RowEdge, Base + 2);
            UiKit.CreateRect("Body", t, Vector2.one, new Vector2(RowWidth - 2, RowHeight - 2), RowBody, Base + 3);

            var icon = UiKit.CreateSprite("Icon", t, null, new Vector2(20, 46), Base + 4);       // alt-orta pivot
            var itemName = UiKit.CreateText("Name", t, "", Ink, new Vector2(42, 3), Base + 4);
            var rarity = UiKit.CreateText("Rarity", t, "", Ink, new Vector2(42, 15), Base + 4);
            var condition = UiKit.CreateText("Condition", t, "", Muted, new Vector2(42, 27), Base + 4);
            var note = UiKit.CreateText("Note", t, "", Faint, new Vector2(42, 39), Base + 4);
            var price = UiKit.CreateText("Price", t, "", PriceRed, new Vector2(RowWidth - 6, 3), Base + 4, PixelTextRenderer.Alignment.Right);
            var seller = UiKit.CreateText("Seller", t, "", Faint, new Vector2(RowWidth - 6, 15), Base + 4, PixelTextRenderer.Alignment.Right);

            // Satırın kendisi tıklanabilir: ilan sayfasını açar (SATIN AL butonu daha öncelikli)
            UiKit.AddArea(row, Vector2.zero, new Vector2(RowWidth, RowHeight));
            var rowButton = row.AddComponent<UiButton>();
            rowButton.SetTexts("", "", tooltip: false, order: 90);
            var rowUi = row.AddComponent<ListingRowUI>();
            UnityEventTools.AddPersistentListener(rowButton.OnClick, rowUi.Open);

            // SATIN AL butonu (lacivert kart, sarı yazı)
            var buySize = new Vector2(58, 18);
            var buy = CreateButton(t, "BuyButton", "Satın Al", new Vector2(RowWidth - 6 - buySize.x, 32), buySize, false, rowUi.Buy);

            var sold = UiKit.CreateText("Sold", t, "Satıldı", PriceRed, new Vector2(RowWidth - 6 - buySize.x * 0.5f, 36), Base + 4,
                PixelTextRenderer.Alignment.Center);

            rowUi.Configure(owner, index, icon, itemName, rarity, condition, note, price, seller, buy, sold.gameObject);
            return rowUi;
        }

        static void Replace(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
    }
}
