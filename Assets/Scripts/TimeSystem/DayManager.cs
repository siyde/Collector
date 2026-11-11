using System;
using System.Collections;
using Collector.UI;
using UnityEngine;

namespace Collector.TimeSystem
{
    public enum Weekday { Pazartesi, Sali, Carsamba, Persembe, Cuma, Cumartesi, Pazar }

    /// <summary>
    /// Oyun günleri. Yatakta uyuyunca <see cref="Sleep"/> ekranı karartır, sonraki güne geçer ve
    /// <see cref="DayStarted"/> olayını yayınlar (ilanlar, bit pazarı vb. buna göre yenilenir).
    /// </summary>
    public class DayManager : MonoBehaviour
    {
        public static DayManager Instance { get; private set; }

        [Tooltip("1. günün haftanın hangi günü olduğu. Cuma: ilk bit pazarı (Pazar) 3. güne denk gelir.")]
        [SerializeField] Weekday firstWeekday = Weekday.Cuma;
        [SerializeField, Min(0.1f)] float fadeTime = 0.6f;
        [SerializeField, Min(0.1f)] float titleTime = 1.4f;

        public int Day { get; private set; } = 1;
        public Weekday Weekday => (Weekday)(((int)firstWeekday + Day - 1) % 7);
        public bool IsWeekend => Weekday == Weekday.Cumartesi || Weekday == Weekday.Pazar;
        public bool IsSleeping { get; private set; }

        /// <summary>Yeni gün başladığında (gün numarası).</summary>
        public event Action<int> DayStarted;

        void Awake() => Instance = this;

        void Update()
        {
            // Geliştirme kısayolu: F3 onay sormadan uyuyup sonraki güne geçer (oyun build'inde çalışmaz)
            if (Debug.isDebugBuild && !IsSleeping && !UIState.IsModalOpen &&
                UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame)
                Sleep();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static string WeekdayName(Weekday day) => day switch
        {
            Weekday.Pazartesi => "Pazartesi",
            Weekday.Sali => "Salı",
            Weekday.Carsamba => "Çarşamba",
            Weekday.Persembe => "Perşembe",
            Weekday.Cuma => "Cuma",
            Weekday.Cumartesi => "Cumartesi",
            _ => "Pazar"
        };

        public void Sleep()
        {
            if (!IsSleeping) StartCoroutine(SleepRoutine());
        }

        IEnumerator SleepRoutine()
        {
            IsSleeping = true;
            UIState.OpenModal();                                       // uyku sırasında hiçbir şeye tıklanamaz
            var fader = ScreenFader.Instance;

            if (fader != null) yield return fader.Fade(1f, fadeTime);
            Day++;
            if (fader != null)
            {
                fader.ShowTitle(Day + ". Gün", WeekdayName(Weekday));
                yield return new WaitForSeconds(titleTime);
                fader.HideTitle();
            }
            DayStarted?.Invoke(Day);
            if (fader != null) yield return fader.Fade(0f, fadeTime);

            UIState.CloseModal();
            IsSleeping = false;
        }
    }
}
