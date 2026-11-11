using System.Collections.Generic;
using Collector.UI;
using Collector.ViewSystem;
using UnityEngine;

namespace Collector.QuestSystem
{
    /// <summary>
    /// Sol üstteki görev paneli: başlık ve kutucuklu adımlar. Satırlar görevdeki adım sayısına göre
    /// çalışma anında oluşturulur. Yakın plan görünümlerinde tek satıra ("BAŞLIK 3/6") küçülür;
    /// görev bitince birkaç saniye sonra gizlenir.
    /// Kök objenin konumu panelin sol-üst köşesidir.
    /// </summary>
    public class QuestPanelUI : MonoBehaviour
    {
        const float PixelsPerUnit = 32f;
        const int Side = 6, Border = 3, FirstRow = 16, RowHeight = 12, CheckX = 6, TextX = 18, Order = 30;

        [SerializeField] GameObject box;
        [SerializeField] SpriteRenderer panel;
        [SerializeField] PixelTextRenderer title;
        [SerializeField] Material textMaterial;
        [SerializeField] Sprite checkEmpty;
        [SerializeField] Sprite checkDone;
        [SerializeField, Min(1f)] float hideAfterComplete = 6f;

        static readonly Color32 Pending = new Color32(0xF2, 0xE8, 0xD0, 0xFF);
        static readonly Color32 Done = new Color32(0x8A, 0x8F, 0xB0, 0xFF);

        readonly List<(SpriteRenderer check, PixelTextRenderer text)> rows = new List<(SpriteRenderer, PixelTextRenderer)>();
        QuestManager quests;
        bool compact;
        float hideAt = -1f;

        public void Configure(GameObject boxRoot, SpriteRenderer panelRenderer, PixelTextRenderer titleText, Material material,
            Sprite emptySprite, Sprite doneSprite)
        {
            box = boxRoot;
            panel = panelRenderer;
            title = titleText;
            textMaterial = material;
            checkEmpty = emptySprite;
            checkDone = doneSprite;
        }

        void Start()
        {
            quests = QuestManager.Instance;
            if (quests == null) { box.SetActive(false); enabled = false; return; }
            quests.Changed += Rebuild;
            if (ViewManager.Instance != null) ViewManager.Instance.ViewChanged += OnViewChanged;
            Rebuild();
        }

        void OnDestroy()
        {
            if (quests != null) quests.Changed -= Rebuild;
            if (ViewManager.Instance != null) ViewManager.Instance.ViewChanged -= OnViewChanged;
        }

        void OnViewChanged(ViewId view)
        {
            compact = view != ViewId.Room;
            Rebuild();
        }

        void Update()
        {
            if (hideAt > 0f && Time.unscaledTime >= hideAt)
            {
                box.SetActive(false);
                hideAt = -1f;
            }
        }

        void Rebuild()
        {
            var quest = quests.Current;
            if (quest == null) { box.SetActive(false); return; }

            while (rows.Count < quest.Steps.Count) rows.Add(CreateRow(rows.Count));
            for (int i = 0; i < rows.Count; i++)
            {
                bool used = i < quest.Steps.Count;
                bool visible = used && !compact;
                rows[i].check.gameObject.SetActive(visible);
                if (!used) continue;
                bool done = quests.IsStepDone(i);
                rows[i].check.sprite = done ? checkDone : checkEmpty;
                rows[i].text.Color = done ? Done : Pending;
                rows[i].text.Text = quest.Steps[i].Description;
            }

            title.Text = compact ? $"{quest.Title}  {quests.CompletedCount}/{quest.Steps.Count}" : quest.Title;
            Layout(quest.Steps.Count);

            if (quests.IsComplete && hideAt < 0f && box.activeSelf) hideAt = Time.unscaledTime + hideAfterComplete;
        }

        void Layout(int stepCount)
        {
            int width = title.PixelSize.x + Side * 2;
            int height = PixelFont.GlyphHeight + Border * 2 + 1;
            if (!compact)
            {
                for (int i = 0; i < stepCount; i++) width = Mathf.Max(width, TextX + rows[i].text.PixelSize.x + Side);
                height = FirstRow + stepCount * RowHeight + Border + 1;
            }
            panel.size = new Vector2(width, height) / PixelsPerUnit;
            panel.transform.localPosition = new Vector3(width * 0.5f, -height * 0.5f) / PixelsPerUnit;
            title.transform.localPosition = new Vector3(Side, -Border) / PixelsPerUnit;
        }

        (SpriteRenderer, PixelTextRenderer) CreateRow(int index)
        {
            int y = FirstRow + index * RowHeight;

            var row = new GameObject("Step_" + (index + 1));
            row.transform.SetParent(box.transform, false);
            row.transform.localPosition = new Vector3(CheckX + 4.5f, -(y + 6.5f)) / PixelsPerUnit;  // 9x9 kutucuk, piksele oturur
            var check = row.AddComponent<SpriteRenderer>();
            check.sortingLayerName = "UI";
            check.sortingOrder = Order + 1;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(row.transform, false);
            textGo.transform.localPosition = new Vector3(TextX - (CheckX + 4.5f), (y + 6.5f) - y) / PixelsPerUnit;
            var text = textGo.AddComponent<PixelTextRenderer>();
            var meshRenderer = textGo.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = textMaterial;
            meshRenderer.sortingLayerName = "UI";
            meshRenderer.sortingOrder = Order + 1;
            return (check, text);
        }
    }
}
