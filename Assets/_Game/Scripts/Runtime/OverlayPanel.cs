using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>
    /// Базовая полноэкранная панель. Её фон перехватывает касания, поэтому предметы
    /// комнаты под ней не нажимаются. Кнопки защищены от повторного нажатия до следующего показа.
    /// </summary>
    public abstract class OverlayPanel : MonoBehaviour
    {
        [SerializeField] protected GameObject root;
        /// <summary>
        /// Пауза после показа панели/реплики, в течение которой касания игнорируются:
        /// второе касание двойного тапа не должно «провалиться» в кнопку новой панели
        /// (найдено в Play Mode: двойной клик по «Вернуться» пролистывал первую реплику диалога).
        /// </summary>
        public const float InputDelay = 0.3f;

        private float _shownAt = float.NegativeInfinity;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>Панель открыта и прошла пауза после последнего показа содержимого.</summary>
        protected bool InputReady => IsOpen && Time.unscaledTime - _shownAt >= InputDelay;

        /// <summary>Отметить новое содержимое (новая реплика и т. п.) — снова включает паузу ввода.</summary>
        protected void MarkShown() => _shownAt = Time.unscaledTime;

        protected void SetOpen(bool open)
        {
            if (open) MarkShown();
            if (root != null && root.activeSelf != open) root.SetActive(open);
        }
    }
}
