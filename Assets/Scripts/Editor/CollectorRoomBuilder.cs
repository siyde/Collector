using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Collector.EditorTools
{
    /// <summary>
    /// CollectorRoom sahnesini kurar (tıkla-ve-bak).
    /// Oyuncu yürümez; oda tek sabit görüntüdür. Bilgisayar / raf tıklanınca ilgili yakın plan
    /// görünüme (View) yaklaşarak geçilir. Görünümler dünyada yan yana durur.
    /// Görseller:
    ///   Room_Background — Reference_Room'un pixel art hali
    ///   Shelf_Front     — Reference_Shelf'in pixel art hali, odanın paletiyle
    ///   Computer_Front  — odadaki monitörün kodla çizilmiş yakın planı
    /// Menü: Collector > Aşama 1 > CollectorRoom Sahnesini Oluştur
    /// </summary>
    public static class CollectorRoomBuilder
    {
        const string ScenePath = "Assets/Scenes/CollectorRoom.unity";
        const string ReferencePath = "Assets/Art/Reference/Reference_Room.png";
        const string ShelfReferencePath = "Assets/Art/Reference/Reference_Shelf.png";
        const int RefWidth = AIArtPixelator.TargetWidth;        // 640
        const int RefHeight = AIArtPixelator.TargetHeight;      // 360
        const int PPU = PixelArtTextureImporter.PixelsPerUnit;  // 32  -> ekran 20 x 11.25 birim

        // Görünümlerin dünya X konumları (kamera bu noktalara gider)
        public const float RoomViewX = 0f, ShelfViewX = 25f, ComputerViewX = 50f;

        public static readonly string[] SortingLayers =
        {
            "Background", "FurnitureBack", "CollectiblesBack",
            "FurnitureFront", "CollectiblesFront", "Effects", "UI"
        };
        static readonly string[] ObsoleteSortingLayers = { "Player" }; // yürüme kalktığı için

        static readonly string[] Folders =
        {
            "Assets/Art/Views", "Assets/Art/Reference", "Assets/Art/Collectibles", "Assets/Art/UI",
            "Assets/Scripts/Interaction", "Assets/Scripts/Inventory", "Assets/Scripts/Collectibles",
            "Assets/Scripts/ShelfSystem", "Assets/Scripts/QuestSystem", "Assets/Scripts/UI", "Assets/Scripts/SaveSystem",
            "Assets/Prefabs/Collectibles", "Assets/Prefabs/UI",
            "Assets/ScriptableObjects/Collectibles",
            "Assets/Scenes"
        };

        static readonly Dictionary<string, string> SpritePaths = new Dictionary<string, string>
        {
            { "Room_Background", "Assets/Art/Views/Room_Background.png" },
            { "Shelf_Front", "Assets/Art/Views/Shelf_Front.png" },
            { "Computer_Front", "Assets/Art/Views/Computer_Front.png" },
        };

        // ---------------------------------------------------------------- menü

        [MenuItem("Collector/Aşama 1/CollectorRoom Sahnesini Oluştur")]
        public static void BuildAll()
        {
            if (!StageSetup.CanModifyScene("CollectorRoom")) return;

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("CollectorRoom",
                    "CollectorRoom.unity zaten var. Sahne sıfırdan yeniden oluşturulsun mu?", "Evet", "İptal"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            EnsureSortingLayers();
            if (!GenerateArt()) return;
            BuildScene();

            Debug.Log("[Collector] Aşama 1 tamam: " + ScenePath);
        }

        [MenuItem("Collector/Aşama 1/Görünüm Görsellerini Yeniden Üret")]
        public static void RegenerateArt()
        {
            EnsureFolders();
            GenerateArt();
        }

        // ---------------------------------------------------------------- proje ayarları

        static void EnsureFolders()
        {
            foreach (var folder in Folders)
            {
                var parts = folder.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }

        static void EnsureSortingLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("m_SortingLayers");

            for (int i = layers.arraySize - 1; i >= 0; i--)
                if (ObsoleteSortingLayers.Contains(layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue))
                    layers.DeleteArrayElementAtIndex(i);

            var existingIds = new HashSet<int>();
            var existingNames = new HashSet<string>();
            for (int i = 0; i < layers.arraySize; i++)
            {
                var element = layers.GetArrayElementAtIndex(i);
                existingNames.Add(element.FindPropertyRelative("name").stringValue);
                existingIds.Add(element.FindPropertyRelative("uniqueID").intValue);
            }

            var rng = new System.Random();
            foreach (var layerName in SortingLayers)
            {
                if (existingNames.Contains(layerName)) continue;

                int id;
                do { id = rng.Next(1, int.MaxValue); } while (existingIds.Contains(id));
                existingIds.Add(id);

                int index = layers.arraySize;
                layers.InsertArrayElementAtIndex(index);
                var element = layers.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("name").stringValue = layerName;
                element.FindPropertyRelative("uniqueID").intValue = id;
                element.FindPropertyRelative("locked").intValue = 0;
            }

            tagManager.ApplyModifiedProperties();

            foreach (var layerName in SortingLayers)
                if (!SortingLayer.IsValid(SortingLayer.NameToID(layerName)))
                    Debug.LogWarning("[Collector] Sorting layer oluşturulamadı: " + layerName);
        }

        // ---------------------------------------------------------------- görseller

        // Views altındaki PNG'ler türetilmiş dosyalardır ve her seferinde baştan üretilir;
        // değişikliği Assets/Art/Reference/Reference_Room.png'de ya da RoomLayout/ViewArt'ta yap.
        static bool GenerateArt()
        {
            if (!File.Exists(ReferencePath))
            {
                EditorUtility.DisplayDialog("CollectorRoom", "Oda görseli bulunamadı: " + ReferencePath, "Tamam");
                return false;
            }

            var roomPath = SpritePaths["Room_Background"];
            AIArtPixelator.Pixelate(ReferencePath, roomPath, RefWidth, RefHeight, AIArtPixelator.PaletteSize);
            Debug.Log("[Collector] Üretildi: " + roomPath);

            // Raf: önden çizilmiş raf görseli, odanın paletine oturtulur -> geçişte renk farkı kalmaz
            if (!File.Exists(ShelfReferencePath))
            {
                EditorUtility.DisplayDialog("CollectorRoom", "Raf görseli bulunamadı: " + ShelfReferencePath, "Tamam");
                return false;
            }
            var shelfPath = SpritePaths["Shelf_Front"];
            AIArtPixelator.Pixelate(ShelfReferencePath, shelfPath, RefWidth, RefHeight, AIArtPixelator.PaletteSize,
                AIArtPixelator.PaletteFromImage(roomPath));
            Debug.Log("[Collector] Üretildi: " + shelfPath);

            var computerPath = SpritePaths["Computer_Front"];
            ViewArt.ComputerScreen().SavePng(computerPath);
            Debug.Log("[Collector] Üretildi: " + computerPath);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return true;
        }

        // ---------------------------------------------------------------- sahne

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Kamera --------------------------------------------------------
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(RoomViewX, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = RefHeight / (2f * PPU); // 5.625
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PixelCanvas.C("#1A1423");
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            camGo.AddComponent<AudioListener>();

            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            PixelPerfectSetup.ConfigureCamera(ppc);
            camGo.AddComponent<Collector.CameraSystem.PixelPerfectDebugOverlay>();

            // Işık (görseller zaten ışıklı çizildiği için nötr global ışık) ------
            var lighting = Group("Lighting");
            var lightGo = new GameObject("Global Light 2D");
            lightGo.transform.SetParent(lighting, false);
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = Color.white;
            light.intensity = 1f;

            var views = Group("Views");

            // 1) Oda görünümü -------------------------------------------------
            var room = View("RoomView", views, RoomViewX);
            AddSprite("Room_Background", room, Vector2.zero, "Room_Background", "Background", 0);
            // Tıklanabilir alanlar (Reference_Room kompozisyonuna göre)
            var hotspots = Group("Hotspots", room);
            Hotspot("Hotspot_Computer", hotspots, new Vector2(-4.55f, 0f), new Vector2(3.9f, 2.1f));
            Hotspot("Hotspot_Shelf", hotspots, new Vector2(4.5f, -0.2f), new Vector2(10.6f, 9f));

            // Kutuların (başlangıç kutusu, kargo kutuları) odadaki yeri; kutunun kendisini Aşama 7 kurar
            var boxSpot = new GameObject("CardboardBoxSpot");
            boxSpot.transform.SetParent(room, false);
            boxSpot.transform.localPosition = BoxSetup.BoxPosition;
            SetIcon(boxSpot, "sv_icon_dot1_pix16_gizmo");

            // Oda görselindeki rafın 45 noktası: yakın plandaki Slot_N ile aynı numara (satır satır,
            // soldan sağa). Oyuncak rafa konunca burada küçük hali görünecek (Aşama 8).
            var roomShelfSlots = Group("RoomShelfSlots", room);
            for (int i = 0; i < RoomLayout.ShelfSlotCount; i++)
                SlotMarker($"Slot_{i + 1:00}", roomShelfSlots, RoomLayout.ShelfSlotRoomLocal(i));

            // 2) Raf yakın planı: aynı 45 nokta, önden görünen rafta -----------------
            var shelfView = View("ShelfView", views, ShelfViewX);
            AddSprite("Shelf_Front", shelfView, Vector2.zero, "Shelf_Front", "Background", 0);
            var shelfSlots = Group("Slots", shelfView);
            for (int i = 0; i < RoomLayout.ShelfSlotCount; i++)
                SlotMarker($"Slot_{i + 1:00}", shelfSlots, RoomLayout.ShelfSlotViewLocal(i));

            // 3) Bilgisayar yakın planı ---------------------------------------
            var computerView = View("ComputerView", views, ComputerViewX);
            AddSprite("Computer_Front", computerView, Vector2.zero, "Computer_Front", "Background", 0);
            // İlan arayüzü (Aşama 12) monitör camının içine yerleşecek
            var screenRect = ViewArt.ComputerScreenRect;
            var screen = new GameObject("ScreenArea");
            screen.transform.SetParent(computerView, false);
            screen.transform.localPosition = PxToLocal(screenRect.center.x, screenRect.center.y);
            var screenCol = screen.AddComponent<BoxCollider2D>();
            screenCol.isTrigger = true;
            screenCol.size = new Vector2(screenRect.width / (float)PPU, screenRect.height / (float)PPU);

            // Aşama 3 ve sonrası: geçişler, etkileşim, envanter, kutu, raf, görevler
            StageSetup.ApplyAll();

            EditorSceneManager.SaveScene(scene, ScenePath);

            var buildScenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        // 640x360 görsel pikselini (sol-alt orijin) görünümün yerel koordinatına çevirir
        static Vector2 PxToLocal(float px, float py) =>
            new Vector2((px - RefWidth * 0.5f) / PPU, (py - RefHeight * 0.5f) / PPU);

        static Transform Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Transform View(string name, Transform parent, float x)
        {
            var view = Group(name, parent);
            view.localPosition = new Vector3(x, 0f, 0f);
            return view;
        }

        static SpriteRenderer AddSprite(string name, Transform parent, Vector2 localPos, string spriteKey,
            string sortingLayer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePaths[spriteKey]);
            if (sr.sprite == null) Debug.LogWarning("[Collector] Sprite bulunamadı: " + SpritePaths[spriteKey]);
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            return sr;
        }

        // Fare tıklaması Physics2D ile bu trigger alanlarından algılanır
        static void Hotspot(string name, Transform parent, Vector2 localPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
        }

        static void SlotMarker(string name, Transform parent, Vector2 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            SetIcon(go, "sv_icon_dot4_pix16_gizmo");
        }

        static void SetIcon(GameObject go, string iconName)
        {
            var icon = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
            if (icon != null) EditorGUIUtility.SetIconForObject(go, icon);
        }
    }
}
