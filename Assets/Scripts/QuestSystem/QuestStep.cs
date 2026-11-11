using System;
using UnityEngine;

namespace Collector.QuestSystem
{
    /// <summary>Bir görev adımını tamamlayan oyun olayı.</summary>
    public enum QuestTrigger
    {
        [Tooltip("Odada 3 farklı şeyin üzerine fareyle gelmek")] ExploreRoom,
        OpenBox,
        AcquireItem,
        OpenInventory,
        PlaceOnShelf,
        VisitShelf,
        VisitComputer
    }

    /// <summary>Görevin tek adımı: açıklama, onu tamamlayan olay ve XP ödülü.</summary>
    [Serializable]
    public class QuestStep
    {
        [SerializeField] string description;
        [SerializeField] QuestTrigger trigger;
        [SerializeField, Min(0)] int xpReward = 10;

        public string Description => description;
        public QuestTrigger Trigger => trigger;
        public int XpReward => xpReward;

        public QuestStep() { }

        public QuestStep(string text, QuestTrigger completedBy, int xp = 10)
        {
            description = text;
            trigger = completedBy;
            xpReward = xp;
        }
    }
}
