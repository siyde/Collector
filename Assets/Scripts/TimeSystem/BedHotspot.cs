using Collector.Interaction;
using Collector.UI;

namespace Collector.TimeSystem
{
    /// <summary>Odadaki yatak: onay sorar, evet denirse uyunur ve sonraki güne geçilir.</summary>
    public class BedHotspot : InteractableHotspot
    {
        public override bool CanInteract => base.CanInteract && DayManager.Instance != null && !DayManager.Instance.IsSleeping;

        public override void Interact()
        {
            var dialog = ConfirmDialog.Instance;
            if (dialog == null) { DayManager.Instance.Sleep(); return; }
            dialog.Show("Uyuyup yeni güne başlansın mı?", () => DayManager.Instance.Sleep());
        }
    }
}
