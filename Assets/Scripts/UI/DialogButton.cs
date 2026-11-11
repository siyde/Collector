using Collector.Interaction;
using UnityEngine;

namespace Collector.UI
{
    /// <summary>Onay penceresindeki EVET / HAYIR butonu.</summary>
    public class DialogButton : InteractableHotspot
    {
        [SerializeField] ConfirmDialog dialog;
        [SerializeField] bool isYes;

        public void Configure(ConfirmDialog owner, bool yes)
        {
            dialog = owner;
            isYes = yes;
        }

        public override void Interact() => dialog.Answer(isYes);
    }
}
