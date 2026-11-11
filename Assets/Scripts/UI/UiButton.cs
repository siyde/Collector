using Collector.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Collector.UI
{
    /// <summary>Genel tıklanabilir buton/simge: tıklanınca Inspector'dan bağlanan eylemi çalıştırır.</summary>
    public class UiButton : InteractableHotspot
    {
        [SerializeField] UnityEvent onClick = new UnityEvent();

        public UnityEvent OnClick => onClick;

        public override void Interact() => onClick.Invoke();
    }
}
