using Collector.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Collector.UI
{
    /// <summary>
    /// Pencerelerin arkasındaki dünyaya tıklanmasını engelleyen görünmez alan (vurgu ve etiket göstermez).
    /// İsteğe bağlı olarak tıklanınca bir eylem çalıştırır (ör. pencere dışına tıklayınca kapat).
    /// </summary>
    public class UiClickBlocker : InteractableHotspot
    {
        [SerializeField] UnityEvent onClick = new UnityEvent();

        public UnityEvent OnClick => onClick;

        public override void Interact() => onClick.Invoke();
    }
}
