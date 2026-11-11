using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collector.ViewSystem
{
    /// <summary>
    /// Görünümler arası geçiş. <see cref="Open"/> ile yakın plan görünüme "yaklaşarak" geçer
    /// (tıklamayı PointerInteractor + ViewHotspot yapar); ESC veya sağ tık ile odaya geri döner.
    ///
    /// Yaklaşma efekti: oda, hedef nesnenin merkezine doğru büyüyüp kararırken hedef görünümün düz
    /// arka planı, nesnenin oda görselindeki eğik dörtgeninden başlayıp tam ekrana açılır (düzleşir).
    /// Geçiş bitince kamera hedef görünüme atlar ve efekt objeleri eski hallerine döner.
    /// </summary>
    public class ViewManager : MonoBehaviour
    {
        public static ViewManager Instance { get; private set; }

        [SerializeField] Camera targetCamera;
        [SerializeField] GameView[] views;
        [SerializeField] ViewTransitionQuad transitionQuad;
        [SerializeField, Min(0.05f)] float transitionDuration = 0.45f;
        [SerializeField] Color roomDimColor = new Color(0.35f, 0.32f, 0.4f, 1f);
        [Tooltip("Düz görselin tamamen görünür olduğu an (geçişin yüzdesi olarak).")]
        [SerializeField, Range(0.05f, 1f)] float fadeInPortion = 0.35f;

        public ViewId Current { get; private set; } = ViewId.Room;
        public bool IsTransitioning { get; private set; }

        /// <summary>Yeni görünüme varıldığında (geçiş bittikten sonra) tetiklenir.</summary>
        public event Action<ViewId> ViewChanged;

        GameView room;

        public void Configure(Camera cam, GameView[] allViews, ViewTransitionQuad quad)
        {
            targetCamera = cam;
            views = allViews;
            transitionQuad = quad;
        }

        void Awake()
        {
            Instance = this;
            // Sahnedeki bütün görünümler (sonradan eklenen yerler, ör. bit pazarı, otomatik bulunur)
            views = FindObjectsByType<GameView>(FindObjectsSortMode.None);
            room = Find(ViewId.Room);
            if (targetCamera == null) targetCamera = Camera.main;
            transitionQuad.gameObject.SetActive(false);
            SnapCameraTo(room);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            // Envanter gibi bir pencere açıksa (ya da bu karede kapandıysa) ESC/sağ tık pencereye aittir
            if (IsTransitioning || Current == ViewId.Room || Collector.UI.UIState.BlocksWorldInput) return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            bool back = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                     || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            if (back) Close();
        }

        public void Open(ViewId id)
        {
            if (IsTransitioning || Current != ViewId.Room || id == ViewId.Room) return;
            var view = Find(id);
            if (view == null || !view.HasRoomQuad) { Debug.LogWarning("[Collector] Görünüm bulunamadı veya oda dörtgeni yok: " + id); return; }
            StartCoroutine(Transition(view, opening: true));
        }

        public void Close()
        {
            if (IsTransitioning || Current == ViewId.Room) return;
            var view = Find(Current);
            if (view.HasRoomQuad) StartCoroutine(Transition(view, opening: false));
            else Travel(ViewId.Room);                                   // bit pazarı gibi ayrı yerlerden eve dönüş
        }

        /// <summary>
        /// Başka bir yere gider (harita): ekran kararır, kamera hedef görünüme geçer, ekran açılır.
        /// Odadaki nesneye yakınlaşmayan görünümler (bit pazarı) ve eve dönüş için.
        /// </summary>
        public void Travel(ViewId id)
        {
            if (IsTransitioning || id == Current) return;
            var view = Find(id);
            if (view == null) { Debug.LogWarning("[Collector] Görünüm bulunamadı: " + id); return; }
            StartCoroutine(FadeTravel(view));
        }

        IEnumerator FadeTravel(GameView view)
        {
            IsTransitioning = true;
            var fader = Collector.UI.ScreenFader.Instance;
            if (fader != null) yield return fader.Fade(1f, 0.35f);
            Current = view.Id;
            SnapCameraTo(view);
            if (fader != null) yield return fader.Fade(0f, 0.35f);
            IsTransitioning = false;
            ViewChanged?.Invoke(Current);
        }

        IEnumerator Transition(GameView view, bool opening)
        {
            IsTransitioning = true;
            SnapCameraTo(room);

            var roomTransform = room.transform;
            var roomRestPos = roomTransform.position;
            var roomRestScale = roomTransform.localScale;
            var roomBackground = room.Background;
            var roomColor = roomBackground.color;

            // Hedef: görünümün arka planı ekranı tam kaplar (kamera oda merkezinde)
            Vector3 center = new Vector3(roomRestPos.x, roomRestPos.y, 0f);
            Vector3 ext = view.Background.sprite.bounds.extents;
            var screen = new[]
            {
                center + new Vector3(-ext.x, ext.y), center + new Vector3(ext.x, ext.y),
                center + new Vector3(ext.x, -ext.y), center + new Vector3(-ext.x, -ext.y)
            };

            // Kaynak: nesnenin oda görselindeki dörtgeni (odaya göre yerel)
            var quadLocal = new Vector3[4];
            for (int i = 0; i < 4; i++) quadLocal[i] = view.RoomQuad[i];
            var quadCenterLocal = (quadLocal[0] + quadLocal[1] + quadLocal[2] + quadLocal[3]) * 0.25f;
            float width = ((quadLocal[1].x - quadLocal[0].x) + (quadLocal[2].x - quadLocal[3].x)) * 0.5f;
            float height = ((quadLocal[0].y - quadLocal[3].y) + (quadLocal[1].y - quadLocal[2].y)) * 0.5f;
            float zoom = Mathf.Max(2f * ext.x / width, 2f * ext.y / height);

            transitionQuad.gameObject.SetActive(true);
            transitionQuad.SetSprite(view.Background.sprite);

            var corners = new Vector3[4];
            for (float t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / transitionDuration);
                float progress = opening ? t : 1f - t;   // 0 = oda, 1 = yakın plan
                float e = EaseInOutCubic(progress);

                // Oda: nesne merkezine doğru büyür ve ekran ortasına kayar
                float scale = Mathf.Lerp(1f, zoom, e);
                var focus = Vector3.Lerp(center + quadCenterLocal, center, e);
                roomTransform.localScale = roomRestScale * scale;
                roomTransform.position = new Vector3(focus.x - quadCenterLocal.x * scale, focus.y - quadCenterLocal.y * scale, roomRestPos.z);
                roomBackground.color = Color.Lerp(roomColor, roomDimColor, e);

                // Düz görsel: büyüyen odadaki nesneyle birlikte hareket eder ve tam ekrana düzleşir
                for (int i = 0; i < 4; i++)
                {
                    var zoomed = new Vector3(roomTransform.position.x, roomTransform.position.y) + quadLocal[i] * scale;
                    corners[i] = Vector3.Lerp(zoomed, screen[i], e);
                }
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, fadeInPortion, progress));
                transitionQuad.SetCorners(corners[0], corners[1], corners[2], corners[3], alpha);

                yield return null;
            }

            roomTransform.localScale = roomRestScale;
            roomTransform.position = roomRestPos;
            roomBackground.color = roomColor;
            transitionQuad.gameObject.SetActive(false);

            Current = opening ? view.Id : ViewId.Room;
            SnapCameraTo(opening ? view : room);
            IsTransitioning = false;
            ViewChanged?.Invoke(Current);
        }

        void SnapCameraTo(GameView view)
        {
            var camPos = targetCamera.transform.position;
            var viewPos = view.transform.position;
            targetCamera.transform.position = new Vector3(viewPos.x, viewPos.y, camPos.z);
        }

        GameView Find(ViewId id)
        {
            foreach (var view in views)
                if (view != null && view.Id == id) return view;
            return null;
        }

        static float EaseInOutCubic(float x) =>
            x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f;
    }
}
