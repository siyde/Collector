using Collector.ViewSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 3: Görünüm geçiş sistemini açık sahneye kurar (GameView, ViewHotspot, ViewManager,
    /// geçiş düzlemi ve materyali). Builder da sahneyi oluştururken bunu çağırır.
    /// Menü: Collector > Aşama 3 > Görünüm Geçişlerini Kur
    /// </summary>
    public static class ViewSystemSetup
    {
        const string MaterialPath = "Assets/Shaders/ViewTransition.mat";
        public const string ShaderName = "Collector/UnlitTextureColor";


        [MenuItem("Collector/Aşama 3/Görünüm Geçişlerini Kur")]
        static void SetupMenu()
        {
            StageSetup.RunFromMenu("Aşama 3", "Görünüm geçişleri kuruldu. Play'e basıp rafa veya bilgisayara tıkla; ESC / sağ tık ile geri dön.");
        }

        public static bool Setup()
        {
            var roomGo = GameObject.Find("Views/RoomView");
            var shelfGo = GameObject.Find("Views/ShelfView");
            var computerGo = GameObject.Find("Views/ComputerView");
            if (roomGo == null || shelfGo == null || computerGo == null)
            {
                EditorUtility.DisplayDialog("Aşama 3", "Views/RoomView, ShelfView veya ComputerView bulunamadı. Önce CollectorRoom sahnesini aç.", "Tamam");
                return false;
            }

            var room = ConfigureView(roomGo, ViewId.Room, "Room_Background", new Vector2[4]);
            // Yakın plan görsellerinin oda görselindeki köşeleri (RoomLayout'ta ölçüldü)
            var shelf = ConfigureView(shelfGo, ViewId.Shelf, "Shelf_Front", RoomLayout.ShelfRoomQuad());
            var computer = ConfigureView(computerGo, ViewId.Computer, "Computer_Front", RoomLayout.ComputerRoomQuad());

            ConfigureHotspot("Views/RoomView/Hotspots/Hotspot_Shelf", ViewId.Shelf);
            ConfigureHotspot("Views/RoomView/Hotspots/Hotspot_Computer", ViewId.Computer);

            var systems = GameObject.Find("GameSystems") ?? new GameObject("GameSystems");

            // Geçiş düzlemi (kapalı başlar, ViewManager açar)
            var quadTransform = systems.transform.Find("ViewTransitionQuad");
            var quadGo = quadTransform != null ? quadTransform.gameObject : new GameObject("ViewTransitionQuad");
            quadGo.transform.SetParent(systems.transform, false);
            GetOrAdd<MeshFilter>(quadGo);
            var meshRenderer = GetOrAdd<MeshRenderer>(quadGo);
            meshRenderer.sharedMaterial = LoadOrCreateMaterial();
            meshRenderer.sortingLayerName = "Effects";
            meshRenderer.sortingOrder = 0;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            var quad = GetOrAdd<ViewTransitionQuad>(quadGo);
            quadGo.SetActive(false);

            var managerTransform = systems.transform.Find("ViewManager");
            var managerGo = managerTransform != null ? managerTransform.gameObject : new GameObject("ViewManager");
            managerGo.transform.SetParent(systems.transform, false);
            var manager = GetOrAdd<ViewManager>(managerGo);
            manager.Configure(Camera.main, new[] { room, shelf, computer }, quad);
            EditorUtility.SetDirty(manager);

            return true;
        }

        static GameView ConfigureView(GameObject go, ViewId id, string backgroundName, Vector2[] quad)
        {
            var background = go.transform.Find(backgroundName)?.GetComponent<SpriteRenderer>();
            if (background == null) Debug.LogWarning($"[Collector] {go.name}/{backgroundName} bulunamadı.");
            var view = GetOrAdd<GameView>(go);
            view.Configure(id, background, quad);
            EditorUtility.SetDirty(view);
            return view;
        }

        static void ConfigureHotspot(string path, ViewId target)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning("[Collector] Hotspot bulunamadı: " + path); return; }
            var hotspot = GetOrAdd<ViewHotspot>(go);
            hotspot.Configure(target);
            EditorUtility.SetDirty(hotspot);
        }

        static Material LoadOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[Collector] Shader bulunamadı: " + ShaderName);
                return null;
            }
            material = new Material(shader) { name = "ViewTransition" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T component) ? component : go.AddComponent<T>();
    }
}
