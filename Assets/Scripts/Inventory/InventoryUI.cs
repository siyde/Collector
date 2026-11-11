using System;
using Collector.UI;
using Collector.ViewSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collector.Inventory
{
    /// <summary>
    /// Tab ile açılıp kapanan envanter penceresi (2x4 kart, sayfalı). Karta tıklamak oyuncağı seçer;
    /// seçim pencere kapansa da korunur. Tab / ESC / sağ tık / pencere dışına tıklama kapatır.
    /// <see cref="OpenForPick"/> ile "seç ve kullan" modunda açılır (ör. boş raf yerine tıklayınca).
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        /// <summary>Envanter penceresi açıldığında (görevler için).</summary>
        public static event Action AnyOpened;

        const string NormalTitle = "Envanter", NormalHint = "Tıkla: seç      Tab / ESC: kapat";

        [SerializeField] GameObject window;
        [SerializeField] PixelTextRenderer titleText;
        [SerializeField] PixelTextRenderer hintText;
        [SerializeField] InventorySlotUI[] slots;
        [SerializeField] InventoryPageButton previousPage;
        [SerializeField] InventoryPageButton nextPage;
        [SerializeField] PixelTextRenderer countText;
        [SerializeField] PixelTextRenderer pageText;
        [SerializeField] GameObject emptyMessage;

        int page;
        InventoryManager inventory;
        Action<OwnedItem> pickCallback;

        public bool IsOpen => window.activeSelf;

        public void Configure(GameObject windowRoot, PixelTextRenderer title, PixelTextRenderer hint, InventorySlotUI[] cards,
            InventoryPageButton previous, InventoryPageButton next, PixelTextRenderer count, PixelTextRenderer pageLabel, GameObject empty)
        {
            window = windowRoot;
            titleText = title;
            hintText = hint;
            slots = cards;
            previousPage = previous;
            nextPage = next;
            countText = count;
            pageText = pageLabel;
            emptyMessage = empty;
        }

        void Awake() => Instance = this;

        void Start()
        {
            if (window == null || slots == null || slots.Length == 0)
            {
                Debug.LogError("[Collector] Envanter penceresi bağlı değil. Collector > Aşama 6 > Envanteri Kur menüsünü tekrar çalıştır.", this);
                enabled = false;
                return;
            }
            inventory = InventoryManager.Instance;
            inventory.Changed += Refresh;
            window.SetActive(false);
        }

        void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (ViewManager.Instance != null && ViewManager.Instance.IsTransitioning) return;

            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else if (!UIState.IsModalOpen) Open();                  // başka pencere (onay, uyku) açıkken açılmaz
                return;
            }

            if (IsOpen && ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                           (mouse != null && mouse.rightButton.wasPressedThisFrame)))
                Close();
        }

        public void Open()
        {
            if (IsOpen) return;
            pickCallback = null;
            SetHeader(NormalTitle, NormalHint);
            Show();
        }

        /// <summary>
        /// Pencereyi "seç ve kullan" modunda açar: bir karta tıklamak pencereyi kapatır ve oyuncağı
        /// <paramref name="onPicked"/>'e verir (seçim değişmez). Vazgeçilirse çağrılmaz.
        /// </summary>
        public void OpenForPick(string title, string hint, Action<OwnedItem> onPicked)
        {
            if (IsOpen) return;
            pickCallback = onPicked;
            SetHeader(title, hint);
            Show();
        }

        public void Close()
        {
            if (!IsOpen) return;
            pickCallback = null;
            window.SetActive(false);
            UIState.CloseModal();
        }

        void Show()
        {
            window.SetActive(true);
            UIState.OpenModal();
            Refresh();
            AnyOpened?.Invoke();
        }

        void SetHeader(string title, string hint)
        {
            if (titleText != null) titleText.Text = title;
            if (hintText != null) hintText.Text = hint;
        }

        public void ChangePage(int direction)
        {
            page += direction;
            Refresh();
        }

        public void OnSlotClicked(InventorySlotUI slot)
        {
            if (pickCallback != null)
            {
                var callback = pickCallback;
                var picked = slot.Item;
                Close();
                callback(picked);
                return;
            }
            inventory.Select(inventory.Selected == slot.Item ? null : slot.Item);
        }

        void Refresh()
        {
            if (!IsOpen) return;

            var items = inventory.Items;
            int perPage = slots.Length;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(items.Count / (float)perPage));
            page = (page % pageCount + pageCount) % pageCount;

            for (int i = 0; i < perPage; i++)
            {
                int index = page * perPage + i;
                var item = index < items.Count ? items[index] : null;
                slots[i].Bind(item, item != null && item == inventory.Selected);
            }

            countText.Text = items.Count + " oyuncak";
            bool paged = pageCount > 1;
            previousPage.gameObject.SetActive(paged);
            nextPage.gameObject.SetActive(paged);
            pageText.gameObject.SetActive(paged);
            pageText.Text = (page + 1) + "/" + pageCount;
            emptyMessage.SetActive(items.Count == 0);
        }
    }
}
