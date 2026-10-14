using System;
using System.Collections;
using System.Collections.Generic;
using Biskas.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>
    /// «Память Бискаса»: над Бискасом появляется облако-мысль (пузырьки → облако), в облаке по шагам
    /// показываются картинка и подпись сцены, а Бискас внизу объясняет увиденное (печатающийся текст,
    /// говорящий персонаж). «Далее» во время печати дописывает текст, после — следующий шаг;
    /// на последнем шаге кнопка закрывает воспоминание.
    /// </summary>
    public sealed class MemoryView : OverlayPanel
    {
        [Header("Облако")]
        [SerializeField] private RectTransform cloud;
        [SerializeField] private RectTransform[] bubbles = new RectTransform[0];
        [SerializeField] private Image icon;
        [SerializeField] private Text caption;
        [SerializeField] private Text title;
        [SerializeField] private Sprite[] iconSprites = new Sprite[0];

        [Header("Бискас объясняет")]
        [SerializeField] private CharacterAnimator narrator;
        [SerializeField] private Text speakerName;
        [SerializeField] private Text explainText;

        [Header("Кнопка")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;

        [Header("Темп")]
        [SerializeField] private float charsPerSecond = 38f;
        [SerializeField] private float cloudGrowTime = 0.45f;
        [SerializeField] private float bubbleStep = 0.12f;

        private MemoryData _data;
        private int _step;
        private string _nextText, _closeText;
        private Action _onClosed;
        private bool _closing;
        private bool _typing;
        private Coroutine _routine;
        private Vector2 _cloudBasePos;
        private bool _cloudReady;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (cloud != null) _cloudBasePos = cloud.anchoredPosition;
            SetOpen(false);
        }

        public void Show(MemoryData data, string speaker, string nextText, string closeText, Action onClosed)
        {
            _data = data;
            _onClosed = onClosed;
            _nextText = nextText;
            _closeText = closeText;
            _closing = false;
            _step = 0;
            if (title != null) title.text = data != null ? data.title : "";
            if (speakerName != null) speakerName.text = speaker;
            SetOpen(true);
            StopRoutine();
            _routine = StartCoroutine(OpenSequence());
        }

        public void HideSilently()
        {
            StopRoutine();
            _onClosed = null;
            if (narrator != null) narrator.SetTalking(false);
            SetOpen(false);
        }

        /// <summary>Имена иконок из данных, для которых нет спрайта (для самопроверки).</summary>
        public List<string> MissingIcons(MemoryData data)
        {
            var missing = new List<string>();
            if (data?.steps == null) return missing;
            foreach (var s in data.steps)
                if (s != null && FindIcon(s.icon) == null) missing.Add(s.icon);
            return missing;
        }

        // ---------------- последовательность ----------------

        private IEnumerator OpenSequence()
        {
            _cloudReady = false;
            if (explainText != null) explainText.text = "";
            if (caption != null) caption.text = "";
            if (icon != null) icon.enabled = false;
            foreach (var b in bubbles) if (b != null) b.localScale = Vector3.zero;
            if (cloud != null) cloud.localScale = Vector3.zero;
            SetButton(false);

            // пузырьки от головы Бискаса к облаку
            foreach (var b in bubbles)
            {
                if (b == null) continue;
                yield return Scale(b, 0f, 1f, bubbleStep * 1.6f, EaseOutBack);
            }
            if (cloud != null) yield return Scale(cloud, 0f, 1f, cloudGrowTime, EaseOutBack);
            _cloudReady = true;
            yield return StepSequence();
        }

        private IEnumerator StepSequence()
        {
            MarkShown();
            var step = CurrentStep();
            SetButton(true);
            if (nextLabel != null) nextLabel.text = IsLastStep() ? _closeText : _nextText;
            if (step == null) yield break;

            // картинка и подпись в облаке
            if (icon != null)
            {
                icon.sprite = FindIcon(step.icon);
                icon.enabled = icon.sprite != null;
                if (icon.enabled) StartCoroutine(Scale(icon.rectTransform, 0.5f, 1f, 0.3f, EaseOutBack));
            }
            if (caption != null) caption.text = step.caption;

            // объяснение Бискаса печатается, пока он говорит
            _typing = true;
            if (narrator != null) narrator.SetTalking(true);
            string full = step.explain ?? "";
            float shown = 0f;
            while (_typing && shown < full.Length)
            {
                shown += Time.unscaledDeltaTime * charsPerSecond;
                if (explainText != null) explainText.text = full.Substring(0, Mathf.Min(full.Length, (int)shown));
                yield return null;
            }
            FinishTyping();
        }

        private void FinishTyping()
        {
            _typing = false;
            var step = CurrentStep();
            if (explainText != null && step != null) explainText.text = step.explain;
            if (narrator != null) narrator.SetTalking(false);
        }

        private void OnNext()
        {
            if (_closing || !InputReady || !_cloudReady) return;
            if (_typing)
            {
                // первое касание дописывает текст, а не пропускает шаг
                _typing = false;
                MarkShown();
                return;
            }
            if (!IsLastStep())
            {
                _step++;
                StopRoutine();
                _routine = StartCoroutine(StepSequence());
                return;
            }
            _closing = true;
            StopRoutine();
            _routine = StartCoroutine(CloseSequence());
        }

        private IEnumerator CloseSequence()
        {
            SetButton(false);
            if (narrator != null) narrator.SetTalking(false);
            if (cloud != null) yield return Scale(cloud, 1f, 0f, 0.22f, t => t * t);
            SetOpen(false);
            var cb = _onClosed;
            _onClosed = null;
            cb?.Invoke();
        }

        private void Update()
        {
            // облако мягко «парит»
            if (cloud != null && IsOpen && _cloudReady)
                cloud.anchoredPosition = _cloudBasePos + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 1.6f) * 6f);
        }

        // ---------------- помощники ----------------

        private MemoryStep CurrentStep() =>
            _data?.steps != null && _step >= 0 && _step < _data.steps.Count ? _data.steps[_step] : null;

        private bool IsLastStep() => _data?.steps == null || _step >= _data.steps.Count - 1;

        private Sprite FindIcon(string name)
        {
            foreach (var s in iconSprites) if (s != null && s.name == name) return s;
            return null;
        }

        private void SetButton(bool visible)
        {
            if (nextButton != null && nextButton.gameObject.activeSelf != visible) nextButton.gameObject.SetActive(visible);
        }

        private void StopRoutine()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _typing = false;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private static IEnumerator Scale(RectTransform rt, float from, float to, float time, Func<float, float> ease)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                float k = ease(Mathf.Clamp01(t / time));
                float s = Mathf.LerpUnclamped(from, to, k);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = new Vector3(to, to, 1f);
        }
    }
}
