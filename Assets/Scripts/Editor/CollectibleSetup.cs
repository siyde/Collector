using System.IO;
using System.Linq;
using Collector.Collectibles;
using UnityEditor;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 5: İlk 5 placeholder oyuncağı (görseller + CollectibleItemData) ve oyuncak veritabanını oluşturur.
    /// Var olan asset'lerdeki değerlere dokunmaz; yalnızca eksikleri tamamlar.
    /// Menü: Collector > Aşama 5 > Placeholder Oyuncakları Oluştur
    /// </summary>
    public static class CollectibleSetup
    {
        const string ArtFolder = "Assets/Art/Collectibles";
        const string DataFolder = "Assets/ScriptableObjects/Collectibles";
        public const string DatabasePath = "Assets/ScriptableObjects/CollectibleDatabase.asset";
        const int SmallSize = 32;   // oda ve envanter için küçük hal (48 px'in 2/3'ü)

        readonly struct ToyDefinition
        {
            public readonly string Id, AssetName, Name, Description, Collection;
            public readonly CollectibleCategory Category;
            public readonly Rarity Rarity;
            public readonly ItemCondition Condition;
            public readonly int Year, Value;
            public readonly System.Func<PixelCanvas> Draw;

            public ToyDefinition(string id, string assetName, string name, string description, CollectibleCategory category,
                Rarity rarity, ItemCondition condition, int year, string collection, int value, System.Func<PixelCanvas> draw)
            {
                Id = id; AssetName = assetName; Name = name; Description = description; Category = category;
                Rarity = rarity; Condition = condition; Year = year; Collection = collection; Value = value; Draw = draw;
            }
        }

        static readonly ToyDefinition[] Toys =
        {
            new ToyDefinition("retro_robot", "RetroRobot", "Retro Robot",
                "80'lerden kalma teneke robot. Kurmalı mekanizması hâlâ tıkırdıyor, göğsündeki ışıklar yanıp sönüyor.",
                CollectibleCategory.Robot, Rarity.Rare, ItemCondition.Good, 1987, "Teneke Robotlar", 25, ToyArt.RetroRobot),
            new ToyDefinition("red_toy_car", "RedToyCar", "Kırmızı Oyuncak Araba",
                "Döküm metal kırmızı spor araba. Tekerlekleri hâlâ fırıl fırıl dönüyor; kaputta birkaç küçük çizik var.",
                CollectibleCategory.ToyCar, Rarity.Uncommon, ItemCondition.VeryGood, 1994, "Hız Tutkunları", 10, ToyArt.RedToyCar),
            new ToyDefinition("old_taso", "OldTaso", "Eski Taso",
                "Cips paketlerinden çıkan efsane tasolardan biri. Kenarları yıpranmış ama yıldızı hâlâ parlıyor.",
                CollectibleCategory.Taso, Rarity.Common, ItemCondition.Used, 1998, "Taso Serisi 1", 5, ToyArt.OldTaso),
            new ToyDefinition("mini_figure", "MiniFigure", "Mini Figür",
                "Pelerinli küçük kahraman. Bir zamanlar bir menüden çıkan promosyon serisinin parçası.",
                CollectibleCategory.MiniFigure, Rarity.Uncommon, ItemCondition.Good, 1996, "Mini Kahramanlar", 10, ToyArt.MiniFigure),
            new ToyDefinition("handheld_toy", "HandheldToy", "El Oyun Cihazı",
                "Tek oyunluk LCD el konsolu. Pili takılınca hâlâ bip sesiyle açılıyor.",
                CollectibleCategory.RetroHandheld, Rarity.Epic, ItemCondition.VeryGood, 1999, "Cep Oyunları", 40, ToyArt.HandheldToy),
        };

        [MenuItem("Collector/Aşama 5/Placeholder Oyuncakları Oluştur")]
        static void CreateMenu()
        {
            CreateToys(regenerateArt: false);
            EditorUtility.DisplayDialog("Aşama 5",
                $"{Toys.Length} oyuncak hazır:\n{DataFolder}\n\nVeritabanı: {DatabasePath}", "Tamam");
        }

        [MenuItem("Collector/Aşama 5/Placeholder Oyuncak Görsellerini Yeniden Çiz")]
        static void RegenerateArtMenu() => CreateToys(regenerateArt: true);

        [MenuItem("Collector/Aşama 5/Oyuncak Veritabanını Güncelle")]
        public static void RefreshDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<CollectibleDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<CollectibleDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            var items = AssetDatabase.FindAssets("t:" + nameof(CollectibleItemData))
                .Select(guid => AssetDatabase.LoadAssetAtPath<CollectibleItemData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(item => item != null)
                .OrderBy(item => item.ItemID)
                .ToList();

            if (database.Items.SequenceEqual(items)) return;
            database.SetItems(items);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Collector] Oyuncak veritabanı güncellendi: {items.Count} oyuncak");
        }

        static void CreateToys(bool regenerateArt)
        {
            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(DataFolder);

            // 1) Görseller (kendi çizimini aynı ada koyduysan üzerine yazılmaz)
            foreach (var toy in Toys)
            {
                var shelfPath = $"{ArtFolder}/{toy.AssetName}_Shelf.png";
                var smallPath = $"{ArtFolder}/{toy.AssetName}_Small.png";
                if (!regenerateArt && File.Exists(shelfPath) && File.Exists(smallPath)) continue;

                var shelf = toy.Draw();
                shelf.SavePng(shelfPath);
                var small = shelf.Downscaled(SmallSize, SmallSize);
                small.OutlineInner(PixelCanvas.C("#2B1E2F"));
                small.SavePng(smallPath);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // 2) Veri asset'leri (var olan değerlere dokunulmaz, yalnızca boş görseller doldurulur)
            foreach (var toy in Toys)
            {
                var path = $"{DataFolder}/{toy.AssetName}.asset";
                var data = AssetDatabase.LoadAssetAtPath<CollectibleItemData>(path);
                bool isNew = data == null;
                if (isNew)
                {
                    data = ScriptableObject.CreateInstance<CollectibleItemData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                var so = new SerializedObject(data);
                if (isNew)
                {
                    so.FindProperty("itemId").stringValue = toy.Id;
                    so.FindProperty("itemName").stringValue = toy.Name;
                    so.FindProperty("description").stringValue = toy.Description;
                    so.FindProperty("category").enumValueIndex = (int)toy.Category;
                    so.FindProperty("rarity").enumValueIndex = (int)toy.Rarity;
                    so.FindProperty("condition").enumValueIndex = (int)toy.Condition;
                    so.FindProperty("year").intValue = toy.Year;
                    so.FindProperty("collectionName").stringValue = toy.Collection;
                    so.FindProperty("collectionValue").intValue = toy.Value;
                }
                var small = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{toy.AssetName}_Small.png");
                var shelf = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{toy.AssetName}_Shelf.png");
                SetIfEmpty(so, "inventoryIcon", small);
                SetIfEmpty(so, "worldSprite", small);
                SetIfEmpty(so, "shelfSprite", shelf);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            RefreshDatabase();
        }

        static void SetIfEmpty(SerializedObject so, string property, Object value)
        {
            var prop = so.FindProperty(property);
            if (prop.objectReferenceValue == null) prop.objectReferenceValue = value;
        }
    }

    /// <summary>Bir CollectibleItemData oluşturulunca, taşınınca veya silinince veritabanını günceller.</summary>
    class CollectibleDatabaseUpdater : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool relevant = imported.Concat(moved).Any(p => p.EndsWith(".asset") &&
                                AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(CollectibleItemData))
                            || deleted.Any(p => p.EndsWith(".asset"));
            if (relevant) EditorApplication.delayCall += CollectibleSetup.RefreshDatabase;
        }
    }
}
