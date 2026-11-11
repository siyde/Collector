using System.Collections.Generic;
using System.IO;
using Collector.Boxes;
using Collector.Collectibles;
using Collector.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 7: Odaya içinde Retro Robot olan eski karton kutuyu koyar ve HUD'a bildirim kutusunu ekler.
    /// Kutu görselleri Assets/Art/Reference/Reference_Box.png'den üretilir (yoksa kodla çizilir) ve
    /// Assets/Art/Props altına yazılır.
    /// Menü: Collector > Aşama 7 > Başlangıç Kutusunu Kur
    /// </summary>
    public static class BoxSetup
    {
        const string PropsFolder = "Assets/Art/Props";
        const string ClosedPath = PropsFolder + "/CardboardBox_Closed.png";
        const string OpenPath = PropsFolder + "/CardboardBox_Open.png";
        const string ReferencePath = "Assets/Art/Reference/Reference_Box.png";
        const string RoomPalettePath = "Assets/Art/Views/Room_Background.png";
        const string StarterItemPath = "Assets/ScriptableObjects/Collectibles/RetroRobot.asset";
        const int ToastOrder = 35;

        /// <summary>
        /// Kutunun odadaki yeri (alt-orta pivot): sağ altta, rafın sağ alt köşesinin önündeki zemin
        /// (640x360 görselde x=585, y=355; tam piksele oturur).
        /// </summary>
        public static readonly Vector2 BoxPosition = new Vector2((585 - 320) / 32f, (180 - 355) / 32f);

        // Sprite içindeki gövde (tıklama alanı) ve kutunun ağzı, pivot'a göre piksel
        readonly struct BoxGeometry
        {
            public readonly Vector2 BodyCenter, BodySize, Mouth;
            public BoxGeometry(Vector2 bodyCenter, Vector2 bodySize, Vector2 mouth) { BodyCenter = bodyCenter; BodySize = bodySize; Mouth = mouth; }
        }

        [MenuItem("Collector/Aşama 7/Başlangıç Kutusunu Kur")]
        static void SetupMenu()
        {
            StageSetup.RunFromMenu("Aşama 7", "Eski kutu odaya kondu. Play'e bas ve kutuya tıkla.");
        }

        [MenuItem("Collector/Aşama 7/Kutu Görsellerini Yeniden Çiz")]
        static void RedrawMenu() => BuildSprites();

        public static bool Setup()
        {
            var room = GameObject.Find("Views/RoomView");
            var hud = Camera.main != null ? Camera.main.transform.Find("HUD") : null;
            if (room == null || hud == null)
            {
                EditorUtility.DisplayDialog("Aşama 7", "RoomView veya HUD bulunamadı. Önce önceki aşamaların kurulumu yapılmalı (Aşama 6).", "Tamam");
                return false;
            }

            var starter = AssetDatabase.LoadAssetAtPath<CollectibleItemData>(StarterItemPath);
            if (starter == null)
            {
                EditorUtility.DisplayDialog("Aşama 7", "Retro Robot bulunamadı: " + StarterItemPath + "\nÖnce Aşama 5'i çalıştır.", "Tamam");
                return false;
            }

            UiKit.EnsureSprites();
            var geometry = BuildSprites();
            var closed = AssetDatabase.LoadAssetAtPath<Sprite>(ClosedPath);
            var open = AssetDatabase.LoadAssetAtPath<Sprite>(OpenPath);

            // Eski yer tutucu tıklama alanı artık kutunun kendisi
            var oldHotspot = room.transform.Find("Hotspots/Hotspot_Box");
            if (oldHotspot != null) Object.DestroyImmediate(oldHotspot.gameObject);
            var oldBox = room.transform.Find("TutorialBox");
            if (oldBox != null) Object.DestroyImmediate(oldBox.gameObject);

            var spot = room.transform.Find("CardboardBoxSpot");
            if (spot != null) spot.localPosition = BoxPosition;
            var box = new GameObject("TutorialBox");
            box.transform.SetParent(room.transform, false);
            box.transform.localPosition = BoxPosition;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(box.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = closed;
            sr.sortingLayerName = "FurnitureBack";
            sr.sortingOrder = 0;

            // Tıklama alanı: kutunun gövdesi (gölge ve kapaklar hariç), alt-orta pivota göre
            var col = box.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = geometry.BodySize / UiKit.PPU;
            col.offset = geometry.BodyCenter / UiKit.PPU;

            var itemBox = box.AddComponent<ItemBox>();
            itemBox.SetTexts("Eski Kutu", "Kutuyu Aç", tooltip: true, order: 5);
            itemBox.Configure(sr, closed, open, new List<BoxItem> { new BoxItem(starter) },
                UiKit.Pixel, UiKit.Sparkle, hud.Find("KeyHint"), geometry.Mouth);

            CreateToast(hud);
            return true;
        }

        // Kutu sprite'larını üretir (türetilmiş dosyalar, her seferinde baştan) ve ölçülerini döner
        static BoxGeometry BuildSprites()
        {
            Directory.CreateDirectory(PropsFolder);
            BoxGeometry geometry;

            if (File.Exists(ReferencePath))
            {
                var palette = File.Exists(RoomPalettePath) ? AIArtPixelator.PaletteFromImage(RoomPalettePath) : null;
                var result = BoxArt.FromReference(AIArtPixelator.LoadCanvas(ReferencePath), palette);
                result.Closed.SavePng(ClosedPath);
                result.Open.SavePng(OpenPath);
                float pivotX = result.Closed.W * 0.5f;
                geometry = new BoxGeometry(
                    new Vector2(result.Body.center.x - pivotX, result.Body.center.y),
                    new Vector2(result.Body.width, result.Body.height),
                    result.MouthFromPivot);
            }
            else
            {
                // Yedek: kodla çizilmiş kutu (48x44; gövde x 4..39, y 3..29)
                PropArt.CardboardBoxClosed().SavePng(ClosedPath);
                PropArt.CardboardBoxOpen().SavePng(OpenPath);
                geometry = new BoxGeometry(new Vector2(-2.5f, 16.5f), new Vector2(36f, 27f), new Vector2(-3f, 22f));
            }

            AssetDatabase.ImportAsset(ClosedPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(OpenPath, ImportAssetOptions.ForceUpdate);
            return geometry;
        }

        // Üst orta: birkaç saniyelik bildirim
        static void CreateToast(Transform hud)
        {
            var old = hud.Find("Toast");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = UiKit.Node("Toast", hud, new Vector2(320, 8));
            var box = UiKit.Node("Box", root.transform);
            var panel = UiKit.CreatePanel("Panel", box.transform, UiKit.Panel, new Vector2(-40, 0), new Vector2(80, 18), ToastOrder);
            var text = UiKit.CreateText("Text", box.transform, "", UiKit.Cream, new Vector2(-34, UiKit.PanelBorder), ToastOrder + 1);
            root.AddComponent<ToastMessage>().Configure(box, panel, text);
        }
    }
}
