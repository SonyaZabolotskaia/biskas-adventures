using System;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>
    /// Интерактивный предмет комнаты. Кнопка (прозрачная, крупнее визуала) — область касания;
    /// визуал — заглушка с подписью, позже заменяется спрайтом.
    /// </summary>
    public sealed class RoomHotspot : MonoBehaviour
    {
        [SerializeField] private string objectId;
        [SerializeField] private Button button;
        [SerializeField] private Image visual;
        [SerializeField] private Text label;
        [SerializeField] private GameObject highlight;
        [SerializeField] private StateSprite[] stateSprites = new StateSprite[0];

        [Serializable]
        public sealed class StateSprite
        {
            public string key;
            public Sprite sprite;
        }

        public string ObjectId => objectId;
        public event Action<string> Tapped;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        private void OnClick() => Tapped?.Invoke(objectId);

        /// <summary>Показывает состояние: спрайт по ключу; если его нет — цветная заглушка.</summary>
        public void ApplyState(string stateKey, string labelText, Color fallbackColor, bool highlighted)
        {
            Sprite sprite = null;
            foreach (var s in stateSprites)
                if (s != null && s.key == stateKey) { sprite = s.sprite; break; }
            if (sprite != null)
            {
                if (visual != null) { visual.sprite = sprite; visual.color = Color.white; visual.preserveAspect = true; }
                if (label != null) label.text = labelText;
                if (highlight != null && highlight.activeSelf != highlighted) highlight.SetActive(highlighted);
                return;
            }
            Apply(labelText, fallbackColor, highlighted);
        }

        public RectTransform VisualRect => visual != null ? visual.rectTransform : (RectTransform)transform;

        public void Apply(string labelText, Color color, bool highlighted, Sprite sprite = null)
        {
            if (label != null) label.text = labelText;
            if (visual != null)
            {
                visual.color = color;
                if (sprite != null) visual.sprite = sprite;
            }
            if (highlight != null && highlight.activeSelf != highlighted) highlight.SetActive(highlighted);
        }
    }
}
