using Collector.Interaction;
using UnityEngine;

namespace Collector.ViewSystem
{
    /// <summary>Odada tıklanınca bir yakın plan görünüme geçiren alan (trigger Collider2D).</summary>
    public class ViewHotspot : InteractableHotspot
    {
        [SerializeField] ViewId target;

        public ViewId Target => target;

        public void Configure(ViewId targetView) => target = targetView;

        public override void Interact() => ViewManager.Instance?.Open(target);
    }
}
