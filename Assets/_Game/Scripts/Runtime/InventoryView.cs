using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>Ячейки инвентаря. Касание ячейки выбирает/снимает выбор предмета.</summary>
    public sealed class InventoryView : MonoBehaviour
    {
        [Serializable]
        public sealed class Slot
        {
            public Button button;
            public Image background;
            public Text label;
            public Image icon;
        }

        [Serializable]
        public sealed class ItemSprite
        {
            public string itemId;
            public Sprite sprite;
        }

        [SerializeField] private List<ItemSprite> itemSprites = new List<ItemSprite>();

        [SerializeField] private List<Slot> slots = new List<Slot>();
        [SerializeField] private Color emptyColor = new Color(0.16f, 0.18f, 0.22f, 0.9f);
        [SerializeField] private Color filledColor = new Color(0.78f, 0.62f, 0.25f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f, 1f);

        private readonly List<string> _shownItems = new List<string>();
        public event Action<string> ItemTapped;

        private void Awake()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                int index = i;
                if (slots[i].button != null) slots[i].button.onClick.AddListener(() => OnSlot(index));
            }
        }

        private void OnSlot(int index)
        {
            if (index < _shownItems.Count) ItemTapped?.Invoke(_shownItems[index]);
        }

        /// <summary>Ячейка, куда «прилетает» новый предмет (первая свободная после текущих предметов).</summary>
        public RectTransform SlotRect(int index)
        {
            if (slots.Count == 0) return (RectTransform)transform;
            index = Mathf.Clamp(index, 0, slots.Count - 1);
            var b = slots[index].button;
            return b != null ? (RectTransform)b.transform : (RectTransform)transform;
        }

        private Sprite SpriteFor(string itemId)
        {
            foreach (var i in itemSprites) if (i != null && i.itemId == itemId) return i.sprite;
            return null;
        }

        public void Refresh(IReadOnlyList<string> items, string selected, Func<string, string> labelFor)
        {
            _shownItems.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                bool has = i < items.Count;
                string id = has ? items[i] : null;
                if (has) _shownItems.Add(id);
                if (s.label != null) s.label.text = has ? labelFor(id) : "";
                if (s.icon != null)
                {
                    Sprite sp = has ? SpriteFor(id) : null;
                    s.icon.sprite = sp;
                    s.icon.enabled = sp != null;
                }
                if (s.background != null)
                    s.background.color = !has ? emptyColor : (id == selected ? selectedColor : filledColor);
                if (s.button != null) s.button.interactable = has;
            }
        }
    }
}
