using System;
using System.Collections.Generic;
using Collector.Inventory;
using Collector.ShelfSystem;
using Collector.UI;
using UnityEngine;

namespace Collector.QuestSystem
{
    /// <summary>
    /// Koleksiyoncu seviyesi ve XP. XP görev adımlarından ve bir oyuncağın rafa ilk kez konmasından
    /// (oyuncağın koleksiyon puanı kadar) gelir. Seviye N'den N+1'e geçmek için 50 x N XP gerekir.
    /// </summary>
    public class PlayerProgress : MonoBehaviour
    {
        public static PlayerProgress Instance { get; private set; }

        [SerializeField, Min(1)] int xpPerLevel = 50;

        readonly HashSet<string> rewardedPlacements = new HashSet<string>();

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int XpToNextLevel => xpPerLevel * Level;

        public event Action Changed;
        public event Action<int> LeveledUp;

        void Awake() => Instance = this;
        void OnEnable() => Shelf.AnyItemPlaced += OnItemPlaced;
        void OnDisable() => Shelf.AnyItemPlaced -= OnItemPlaced;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;
            while (Xp >= XpToNextLevel)
            {
                Xp -= XpToNextLevel;
                Level++;
                LeveledUp?.Invoke(Level);
                ToastMessage.Instance?.Show($"Seviye atladın! Koleksiyoncu Lv. {Level}");
            }
            Changed?.Invoke();
        }

        // Aynı oyuncak kopyası rafa ikinci kez konunca tekrar XP vermez
        void OnItemPlaced(ShelfSlot slot, OwnedItem item)
        {
            if (rewardedPlacements.Add(item.InstanceId))
                AddXp(item.Data.CollectionValue);
        }
    }
}
