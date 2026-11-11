using Collector.TimeSystem;
using Collector.UI;
using UnityEngine;

namespace Collector.Economy
{
    /// <summary>Sağ üstte, seviye göstergesinin altında: "GÜN 1 ... CUMA" ve para ("300 TL").</summary>
    public class StatusHudUI : MonoBehaviour
    {
        [SerializeField] PixelTextRenderer dayText;
        [SerializeField] PixelTextRenderer weekdayText;
        [SerializeField] PixelTextRenderer moneyText;

        Wallet wallet;
        DayManager days;

        public void Configure(PixelTextRenderer day, PixelTextRenderer weekday, PixelTextRenderer money)
        {
            dayText = day;
            weekdayText = weekday;
            moneyText = money;
        }

        void Start()
        {
            wallet = Wallet.Instance;
            days = DayManager.Instance;
            if (wallet != null) wallet.Changed += OnMoneyChanged;
            if (days != null) days.DayStarted += OnDayStarted;
            Refresh();
        }

        void OnDestroy()
        {
            if (wallet != null) wallet.Changed -= OnMoneyChanged;
            if (days != null) days.DayStarted -= OnDayStarted;
        }

        void OnMoneyChanged(int balance) => Refresh();
        void OnDayStarted(int day) => Refresh();

        void Refresh()
        {
            if (days != null)
            {
                dayText.Text = "Gün " + days.Day;
                weekdayText.Text = DayManager.WeekdayName(days.Weekday);
            }
            if (wallet != null) moneyText.Text = Wallet.Format(wallet.Balance);
        }
    }
}
