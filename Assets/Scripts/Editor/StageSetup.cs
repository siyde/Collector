using UnityEditor;
using UnityEditor.SceneManagement;

namespace Collector.EditorTools
{
    /// <summary>
    /// Açık sahneye 3. aşamadan itibaren bütün kurulumları sırayla uygular. Aşamalar birbirinin
    /// kurduğu nesneleri kullandığı ve değiştirdiği için tek bir aşamayı çalıştırmak sahneyi yarım
    /// bırakabilir; bu yüzden her aşama menüsü bunu çağırır.
    /// Menü: Collector > Sahneyi Güncelle (Tüm Aşamalar)
    /// </summary>
    public static class StageSetup
    {
        public static bool ApplyAll()
        {
            return ViewSystemSetup.Setup()      // 3: görünüm geçişleri
                && InteractionSetup.Setup()     // 4: fareyle etkileşim
                && InventorySetup.Setup()       // 6: envanter + HUD
                && BoxSetup.Setup()             // 7: başlangıç kutusu, bildirim
                && ShelfSetup.Setup()           // 8: raf
                && QuestSetup.Setup()           // 9: görevler, seviye
                && EconomySetup.Setup()         // 10: para, gün, yatak, onay penceresi
                && ShopSetup.Setup()            // 11: bilgisayardaki ilanlar, kargo kutuları
                && MarketSetup.Setup();         // 12: bit pazarı, şehir haritası
        }

        /// <summary>
        /// Sahne yalnızca Play modu dışında ve derleme bittikten sonra değiştirilebilir. Play modunda yapılan
        /// değişiklikler Play'den çıkınca kaybolur; derleme bitmeden çalışırsa eski kodla kurulur.
        /// </summary>
        public static bool CanModifyScene(string title)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(title, "Play modundayken sahne kurulamaz (değişiklikler kaybolur).\nÖnce Play'den çık, sonra tekrar dene.", "Tamam");
                return false;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog(title, "Unity hâlâ script derliyor. Sağ alttaki dönen simge durunca tekrar dene.", "Tamam");
                return false;
            }
            return true;
        }

        [MenuItem("Collector/Sahneyi Güncelle (Tüm Aşamalar)", priority = 0)]
        static void Menu() => RunFromMenu("Collector", "Sahne güncellendi. Bütün aşamalar kuruldu.");

        /// <summary>Aşama menülerinin ortak gövdesi: derleme kontrolü, kurulum, kaydetme, bilgi penceresi.</summary>
        public static void RunFromMenu(string title, string message)
        {
            if (!CanModifyScene(title)) return;
            if (!ApplyAll()) return;

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog(title, message, "Tamam");
        }
    }
}
