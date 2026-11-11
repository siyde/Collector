using UnityEngine;

namespace Collector.UI
{
    /// <summary>
    /// Açık pencere (envanter vb.) takibi. Pencere açıkken ya da pencerenin kapandığı karede
    /// oyun dünyası girişleri (ESC ile görünümden çıkma gibi) yok sayılır; böylece aynı tuşa
    /// basış iki işi birden yapmaz.
    /// </summary>
    public static class UIState
    {
        static int openModals;
        static int lastChangeFrame = -1;

        public static bool IsModalOpen => openModals > 0;

        /// <summary>Bu karede bir pencere açıldı ya da kapandı mı? (aynı tuşun iki pencereyi birden kapatmaması için)</summary>
        public static bool ChangedThisFrame => lastChangeFrame == Time.frameCount;

        /// <summary>Dünya girişleri bu karede engelli mi?</summary>
        public static bool BlocksWorldInput => openModals > 0 || lastChangeFrame == Time.frameCount;

        public static void OpenModal()
        {
            openModals++;
            lastChangeFrame = Time.frameCount;
        }

        public static void CloseModal()
        {
            openModals = Mathf.Max(0, openModals - 1);
            lastChangeFrame = Time.frameCount;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            openModals = 0;
            lastChangeFrame = -1;
        }
    }
}
