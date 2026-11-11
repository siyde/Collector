using UnityEngine;

namespace Collector.Interaction
{
    /// <summary>
    /// Collider2D ile tanımlanan tıklanabilir alanların temel sınıfı. Alt sınıflar yalnızca
    /// <see cref="Interact"/>'i yazar; ad, eylem metni ve öncelik Inspector'dan ayarlanır.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class InteractableHotspot : MonoBehaviour, IInteractable
    {
        [SerializeField] string displayName;
        [SerializeField] string actionText;
        [SerializeField] bool showTooltip = true;
        [SerializeField] bool showHighlight = true;
        [SerializeField] int priority;

        Collider2D area;

        public virtual string DisplayName => displayName;
        public virtual string ActionText => actionText;
        public virtual bool CanInteract => isActiveAndEnabled;
        public bool ShowTooltip => showTooltip;
        public bool ShowHighlight => showHighlight;
        public int Priority => priority;

        public Bounds HighlightBounds
        {
            get
            {
                if (area == null) area = GetComponent<Collider2D>();
                return area.bounds;
            }
        }

        public void SetTexts(string name, string action, bool tooltip = true, int order = 0, bool highlight = true)
        {
            displayName = name;
            actionText = action;
            showTooltip = tooltip;
            priority = order;
            showHighlight = highlight;
        }

        public abstract void Interact();
    }
}
