using UnityEngine;

namespace Collector.Interaction
{
    /// <summary>Fareyle üzerine gelinip tıklanabilen her şeyin ortak arayüzü (raf, bilgisayar, kutu, oyuncak...).</summary>
    public interface IInteractable
    {
        /// <summary>Etikette görünen ad, ör. "Raf".</summary>
        string DisplayName { get; }

        /// <summary>Tıklayınca ne olacağı, ör. "Rafa Bak".</summary>
        string ActionText { get; }

        bool CanInteract { get; }
        bool ShowTooltip { get; }

        /// <summary>Üzerine gelince köşe vurgusu gösterilsin mi (tıklama engelleyici paneller için kapalı).</summary>
        bool ShowHighlight { get; }

        /// <summary>Üst üste binen alanlarda yüksek öncelikli olan seçilir.</summary>
        int Priority { get; }

        /// <summary>Vurgu köşelerinin çizileceği dünya sınırları.</summary>
        Bounds HighlightBounds { get; }

        void Interact();
    }
}
