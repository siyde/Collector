using System.Collections.Generic;
using UnityEngine;

namespace Collector.QuestSystem
{
    /// <summary>
    /// Bir görev (ör. "İlk Koleksiyon"). Kod yazmadan yeni görev: Create > Collector > Quest,
    /// adımları listeye ekle ve her adıma bir tetikleyici seç.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "Collector/Quest", order = 2)]
    public class QuestData : ScriptableObject
    {
        [SerializeField] string questId;
        [SerializeField] string title;
        [SerializeField] List<QuestStep> steps = new List<QuestStep>();
        [Tooltip("Bütün adımlar bitince verilen ek XP.")]
        [SerializeField, Min(0)] int completionXp = 20;

        public string QuestId => questId;
        public string Title => title;
        public IReadOnlyList<QuestStep> Steps => steps;
        public int CompletionXp => completionXp;

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(questId)) questId = name.Trim().ToLowerInvariant().Replace(' ', '_');
            if (string.IsNullOrWhiteSpace(title)) title = name;
        }
    }
}
