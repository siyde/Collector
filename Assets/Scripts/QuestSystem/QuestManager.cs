using System;
using System.Collections.Generic;
using Collector.Boxes;
using Collector.Interaction;
using Collector.Inventory;
using Collector.ShelfSystem;
using Collector.UI;
using Collector.ViewSystem;
using UnityEngine;

namespace Collector.QuestSystem
{
    /// <summary>
    /// Aktif görevi takip eder: oyun olaylarını dinler, eşleşen adımları tamamlar, XP verir.
    /// Adımlar herhangi bir sırayla tamamlanabilir; hepsi bitince görev tamamlanır.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        const int ExploreTargetCount = 2;

        [SerializeField] QuestData startingQuest;

        readonly HashSet<IInteractable> explored = new HashSet<IInteractable>();
        bool[] completed = new bool[0];
        bool subscribedToScene;

        public QuestData Current { get; private set; }
        public bool IsComplete { get; private set; }

        public int CompletedCount
        {
            get { int n = 0; foreach (var done in completed) if (done) n++; return n; }
        }

        /// <summary>Bir adım tamamlanınca veya görev değişince.</summary>
        public event Action Changed;
        public event Action<QuestData> QuestCompleted;

        public void Configure(QuestData quest) => startingQuest = quest;

        public bool IsStepDone(int index) => index >= 0 && index < completed.Length && completed[index];

        void Awake()
        {
            Instance = this;
            StartQuest(startingQuest);
        }

        void OnEnable()
        {
            ItemBox.AnyOpened += OnBoxOpened;
            Shelf.AnyItemPlaced += OnItemPlaced;
            InventoryUI.AnyOpened += OnInventoryOpened;
            PointerInteractor.AnyHovered += OnHovered;
        }

        void OnDisable()
        {
            ItemBox.AnyOpened -= OnBoxOpened;
            Shelf.AnyItemPlaced -= OnItemPlaced;
            InventoryUI.AnyOpened -= OnInventoryOpened;
            PointerInteractor.AnyHovered -= OnHovered;
            if (subscribedToScene)
            {
                if (InventoryManager.Instance != null) InventoryManager.Instance.ItemAdded -= OnItemAdded;
                if (ViewManager.Instance != null) ViewManager.Instance.ViewChanged -= OnViewChanged;
                subscribedToScene = false;
            }
        }

        // Sahnedeki yöneticilerin Awake'i bitmiş olsun diye örnek olaylarına Start'ta abone olunur
        void Start()
        {
            if (InventoryManager.Instance != null) InventoryManager.Instance.ItemAdded += OnItemAdded;
            if (ViewManager.Instance != null) ViewManager.Instance.ViewChanged += OnViewChanged;
            subscribedToScene = true;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartQuest(QuestData quest)
        {
            Current = quest;
            IsComplete = false;
            explored.Clear();
            completed = new bool[quest != null ? quest.Steps.Count : 0];
            Changed?.Invoke();
        }

        public void Trigger(QuestTrigger trigger)
        {
            if (Current == null || IsComplete) return;

            bool any = false;
            for (int i = 0; i < completed.Length; i++)
            {
                if (completed[i] || Current.Steps[i].Trigger != trigger) continue;
                completed[i] = true;
                any = true;
                PlayerProgress.Instance?.AddXp(Current.Steps[i].XpReward);
            }
            if (!any) return;

            if (Array.TrueForAll(completed, done => done))
            {
                IsComplete = true;
                PlayerProgress.Instance?.AddXp(Current.CompletionXp);
                ToastMessage.Instance?.Show(Current.Title + " tamamlandı!");
                QuestCompleted?.Invoke(Current);
            }
            Changed?.Invoke();
        }

        void OnBoxOpened(ItemBox box) => Trigger(QuestTrigger.OpenBox);
        void OnItemAdded(OwnedItem item) => Trigger(QuestTrigger.AcquireItem);
        void OnInventoryOpened() => Trigger(QuestTrigger.OpenInventory);
        void OnItemPlaced(ShelfSlot slot, OwnedItem item) => Trigger(QuestTrigger.PlaceOnShelf);

        void OnViewChanged(ViewId view)
        {
            if (view == ViewId.Shelf) Trigger(QuestTrigger.VisitShelf);
            else if (view == ViewId.Computer) Trigger(QuestTrigger.VisitComputer);
        }

        // "Odanı keşfet": odada etiketli en az iki farklı şeyin üzerine gelmek
        void OnHovered(IInteractable target)
        {
            if (!target.ShowTooltip) return;
            if (ViewManager.Instance != null && ViewManager.Instance.Current != ViewId.Room) return;
            if (explored.Add(target) && explored.Count >= ExploreTargetCount)
                Trigger(QuestTrigger.ExploreRoom);
        }
    }
}
