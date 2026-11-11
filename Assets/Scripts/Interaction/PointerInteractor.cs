using Collector.ViewSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Collector.Interaction
{
    /// <summary>
    /// Fareyi takip eder: altındaki <see cref="IInteractable"/>'ı bulur, vurgular, etiketini gösterir
    /// ve sol tıkta <see cref="IInteractable.Interact"/>'i çağırır. Görünüm geçişi sırasında devre dışıdır.
    /// </summary>
    public class PointerInteractor : MonoBehaviour
    {
        /// <summary>Fare yeni bir nesnenin üzerine geldiğinde (görevler için, ör. "Odanı keşfet").</summary>
        public static event System.Action<IInteractable> AnyHovered;

        [SerializeField] Camera targetCamera;
        [SerializeField] HoverHighlight highlight;
        [SerializeField] InteractionTooltip tooltip;

        IInteractable hovered;
        Vector2 halfView;

        public void Configure(Camera cam, HoverHighlight hoverHighlight, InteractionTooltip interactionTooltip)
        {
            targetCamera = cam;
            highlight = hoverHighlight;
            tooltip = interactionTooltip;
        }

        void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            var ppc = targetCamera.GetComponent<PixelPerfectCamera>();
            halfView = ppc != null
                ? new Vector2(ppc.refResolutionX, ppc.refResolutionY) * 0.5f / ppc.assetsPPU
                : new Vector2(targetCamera.orthographicSize * targetCamera.aspect, targetCamera.orthographicSize);
            highlight.Hide();
            tooltip.Hide();
        }

        void Update()
        {
            var mouse = Mouse.current;
            var views = ViewManager.Instance;
            bool blocked = mouse == null || (views != null && views.IsTransitioning);

            IInteractable found = null;
            Vector2 pointer = default;
            if (!blocked)
            {
                pointer = targetCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                found = FindAt(pointer);
            }

            SetHovered(found);
            if (found == null) return;

            if (found.ShowTooltip)
            {
                Vector2 cam = targetCamera.transform.position;
                tooltip.MoveTo(pointer, new Rect(cam - halfView, halfView * 2f));
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                SetHovered(null);
                found.Interact();
            }
        }

        static IInteractable FindAt(Vector2 point)
        {
            IInteractable best = null;
            foreach (var col in Physics2D.OverlapPointAll(point))
                if (col.TryGetComponent(out IInteractable candidate) && candidate.CanInteract &&
                    (best == null || candidate.Priority > best.Priority))
                    best = candidate;
            return best;
        }

        void SetHovered(IInteractable target)
        {
            if (ReferenceEquals(hovered, target)) return;
            hovered = target;

            if (target == null)
            {
                highlight.Hide();
                tooltip.Hide();
                return;
            }

            if (target.ShowHighlight) highlight.Show(target.HighlightBounds);
            else highlight.Hide();
            if (target.ShowTooltip) tooltip.Show(target);
            else tooltip.Hide();
            AnyHovered?.Invoke(target);
        }
    }
}
