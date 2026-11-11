using System.Collections.Generic;
using System.IO;
using Collector.QuestSystem;
using Collector.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 9: "İlk Koleksiyon" görevini (QuestData asset), görev/seviye yöneticilerini ve HUD'daki
    /// görev paneli (sol üst) ile koleksiyoncu seviye göstergesini (sağ üst) kurar.
    /// Var olan görev asset'indeki değişikliklere dokunmaz.
    /// Menü: Collector > Aşama 9 > Görev Sistemini Kur
    /// </summary>
    public static class QuestSetup
    {
        const string QuestFolder = "Assets/ScriptableObjects/Quests";
        const string FirstQuestPath = QuestFolder + "/IlkKoleksiyon.asset";
        const int ScreenW = 640;
        const int HudOrder = 30;

        [MenuItem("Collector/Aşama 9/Görev Sistemini Kur")]
        static void SetupMenu()
        {
            StageSetup.RunFromMenu("Aşama 9", "Görev sistemi kuruldu. Play'e bas; sol üstte İlk Koleksiyon görevi görünecek.");
        }

        public static bool Setup()
        {
            var systems = GameObject.Find("GameSystems");
            var hud = Camera.main != null ? Camera.main.transform.Find("HUD") : null;
            if (systems == null || hud == null)
            {
                EditorUtility.DisplayDialog("Aşama 9", "GameSystems veya HUD bulunamadı. Önce önceki aşamaların kurulumu yapılmalı.", "Tamam");
                return false;
            }

            UiKit.EnsureSprites();
            var quest = LoadOrCreateFirstQuest();

            var old = systems.transform.Find("Quests");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var questsGo = new GameObject("Quests");
            questsGo.transform.SetParent(systems.transform, false);
            questsGo.AddComponent<PlayerProgress>();
            questsGo.AddComponent<QuestManager>().Configure(quest);

            CreateQuestPanel(hud);
            CreateProgressHud(hud);
            return true;
        }

        static QuestData LoadOrCreateFirstQuest()
        {
            var quest = AssetDatabase.LoadAssetAtPath<QuestData>(FirstQuestPath);
            if (quest != null) return quest;

            Directory.CreateDirectory(QuestFolder);
            quest = ScriptableObject.CreateInstance<QuestData>();
            AssetDatabase.CreateAsset(quest, FirstQuestPath);

            var so = new SerializedObject(quest);
            so.FindProperty("questId").stringValue = "ilk_koleksiyon";
            so.FindProperty("title").stringValue = "İlk Koleksiyon";
            so.FindProperty("completionXp").intValue = 20;
            var steps = new List<(string text, QuestTrigger trigger)>
            {
                ("Odanı keşfet", QuestTrigger.ExploreRoom),
                ("Eski kutuyu aç", QuestTrigger.OpenBox),
                ("İlk oyuncağını al", QuestTrigger.AcquireItem),
                ("Envanterine bak", QuestTrigger.OpenInventory),
                ("Oyuncağı rafa yerleştir", QuestTrigger.PlaceOnShelf),
                ("Bilgisayarı kontrol et", QuestTrigger.VisitComputer),
            };
            var list = so.FindProperty("steps");
            list.arraySize = steps.Count;
            for (int i = 0; i < steps.Count; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("description").stringValue = steps[i].text;
                element.FindPropertyRelative("trigger").enumValueIndex = (int)steps[i].trigger;
                element.FindPropertyRelative("xpReward").intValue = 10;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return quest;
        }

        // Sol üst: görev paneli (boyutunu ve satırlarını QuestPanelUI çalışma anında ayarlar)
        static void CreateQuestPanel(Transform hud)
        {
            var old = hud.Find("QuestPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = UiKit.Node("QuestPanel", hud, new Vector2(8, 8));
            var box = UiKit.Node("Box", root.transform);
            var panel = UiKit.CreatePanel("Panel", box.transform, UiKit.Panel, Vector2.zero, new Vector2(120, 90), HudOrder);
            var title = UiKit.CreateText("Title", box.transform, "İlk Koleksiyon", UiKit.Accent, new Vector2(6, UiKit.PanelBorder), HudOrder + 1);
            root.AddComponent<QuestPanelUI>().Configure(box, panel, title, UiKit.TextMaterial, UiKit.CheckEmpty, UiKit.CheckDone);
        }

        // Sağ üst: "KOLEKSİYONCU / LV. 1 [çubuk] 0/50"
        static void CreateProgressHud(Transform hud)
        {
            var old = hud.Find("ProgressHud");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            const int width = 150, height = 30;
            const int barX = 40, barY = 18, barW = 60, barInnerH = 3;
            var root = UiKit.Node("ProgressHud", hud, new Vector2(ScreenW - 8 - width, 8));
            UiKit.CreatePanel("Panel", root.transform, UiKit.Panel, Vector2.zero, new Vector2(width, height), HudOrder);
            UiKit.CreateText("Title", root.transform, "Koleksiyoncu", UiKit.Cream, new Vector2(8, 2), HudOrder + 1);
            var level = UiKit.CreateText("Level", root.transform, "Lv. 1", UiKit.Accent, new Vector2(8, 14), HudOrder + 1);

            // Çubuk: kart çerçevesi + içte sarı dolgu (1x1 piksel ölçeklenir)
            UiKit.CreatePanel("BarFrame", root.transform, UiKit.Card, new Vector2(barX - 2, barY - 2), new Vector2(barW + 4, barInnerH + 4), HudOrder + 1);
            var fill = UiKit.CreateSprite("BarFill", root.transform, UiKit.Pixel, new Vector2(barX, barY + 1.5f), HudOrder + 2);
            fill.color = UiKit.Accent;
            var xp = UiKit.CreateText("Xp", root.transform, "0/50", UiKit.Dim, new Vector2(width - 8, 14), HudOrder + 1,
                PixelTextRenderer.Alignment.Right);

            root.AddComponent<ProgressHudUI>().Configure(level, xp, fill, new Vector2(barX, barY + 1.5f), barW, barInnerH);
        }
    }
}
