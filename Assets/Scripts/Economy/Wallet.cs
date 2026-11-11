using System;
using UnityEngine;

namespace Collector.Economy
{
    /// <summary>
    /// Oyuncunun parası. Şimdilik yalnızca başlangıç parası var; alışverişler <see cref="TrySpend"/> ile ödenir.
    /// </summary>
    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }

        [SerializeField, Min(0)] int startingMoney = 300;

        public int Balance { get; private set; }

        /// <summary>Bakiye değişince (yeni bakiye).</summary>
        public event Action<int> Changed;

        void Awake()
        {
            Instance = this;
            Balance = startingMoney;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool CanAfford(int amount) => amount <= Balance;

        /// <summary>Para yetiyorsa düşer ve true döner; yetmiyorsa hiçbir şey değişmez.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Balance) return false;
            Balance -= amount;
            Changed?.Invoke(Balance);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            Changed?.Invoke(Balance);
        }

        /// <summary>"1.250 TL" biçiminde (Türkçe binlik ayracı; kültür verisine bağlı değil).</summary>
        public static string Format(int amount)
        {
            var digits = Math.Abs(amount).ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0) sb.Append('.');
                sb.Append(digits[i]);
            }
            return (amount < 0 ? "-" : "") + sb + " TL";
        }
    }
}
