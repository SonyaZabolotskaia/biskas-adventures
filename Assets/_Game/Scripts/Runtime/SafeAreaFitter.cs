using UnityEngine;

namespace Biskas.Runtime
{
    /// <summary>
    /// Растягивает RectTransform на Screen.safeArea. Обновляется при смене размера,
    /// ориентации и после возврата приложения из фона.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;
        private ScreenOrientation _lastOrientation;

        private void OnEnable()
        {
            _rect = GetComponent<RectTransform>();
            Apply(true);
        }

        private void Update() => Apply(false);

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) Apply(true);
        }

        public void Apply(bool force)
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == _lastSafeArea && screen == _lastScreen && Screen.orientation == _lastOrientation)
                return;

            _lastSafeArea = safe;
            _lastScreen = screen;
            _lastOrientation = Screen.orientation;
            if (screen.x <= 0 || screen.y <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= screen.x; min.y /= screen.y;
            max.x /= screen.x; max.y /= screen.y;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
