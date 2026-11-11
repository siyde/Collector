using Collector.ViewSystem;
using UnityEngine;

namespace Collector.Interaction
{
    /// <summary>
    /// Sol alttaki "ESC GERİ" butonu: yalnızca yakın plan görünümlerde görünür ve odaya döner
    /// (ESC / sağ tık ile aynı).
    /// </summary>
    public class BackButton : InteractableHotspot
    {
        [SerializeField] GameObject visual;

        public void ConfigureVisual(GameObject visualRoot) => visual = visualRoot;

        static bool InCloseUp => ViewManager.Instance != null && ViewManager.Instance.Current != ViewId.Room;

        public override bool CanInteract => base.CanInteract && InCloseUp && !ViewManager.Instance.IsTransitioning
                                            && !Collector.UI.UIState.IsModalOpen;

        void Update()
        {
            if (visual != null && visual.activeSelf != InCloseUp) visual.SetActive(InCloseUp);
        }

        public override void Interact() => ViewManager.Instance?.Close();
    }
}
