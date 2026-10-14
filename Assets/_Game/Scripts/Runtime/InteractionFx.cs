using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>
    /// Эффекты результата взаимодействия с предметом:
    /// «Появление» — предмет «выпрыгивает» (масштаб), за ним вспыхивает свечение и искра;
    /// «Ничего нет» — облачко пыли у предмета и всплывающий пузырь «?».
    /// Работает на верхнем слое (FX_Layer), касания не перехватывает.
    /// </summary>
    public sealed class InteractionFx : MonoBehaviour
    {
        [SerializeField] private RectTransform puff;          // облачко пыли
        [SerializeField] private RectTransform question;      // пузырь «?»
        [SerializeField] private RectTransform revealGlow;    // мягкое свечение
        [SerializeField] private RectTransform revealSparkle; // искра

        private Coroutine _nothing, _reveal;
        private RectTransform _revealTarget;

        private void Awake() => HideAll();

        public void HideAll()
        {
            StopAllCoroutines();
            if (_revealTarget != null) _revealTarget.localScale = Vector3.one;
            _revealTarget = null;
            foreach (var r in new[] { puff, question, revealGlow, revealSparkle })
                if (r != null) r.gameObject.SetActive(false);
        }

        public void PlayNothing(RectTransform target)
        {
            if (target == null) return;
            if (_nothing != null) StopCoroutine(_nothing);
            _nothing = StartCoroutine(Nothing(target));
        }

        public void PlayReveal(RectTransform target)
        {
            if (target == null) return;
            if (_reveal != null) StopCoroutine(_reveal);
            if (_revealTarget != null) _revealTarget.localScale = Vector3.one; // прерванный «выпрыг»
            _revealTarget = target;
            _reveal = StartCoroutine(Reveal(target));
        }

        private IEnumerator Nothing(RectTransform target)
        {
            float unit = Unit();
            Vector3 center = target.position;
            Vector3 bottom = center - new Vector3(0f, target.rect.height * 0.35f * unit, 0f);
            Vector3 top = center + new Vector3(0f, target.rect.height * 0.55f * unit, 0f);

            var puffImg = puff != null ? puff.GetComponent<Image>() : null;
            if (puff != null) { puff.gameObject.SetActive(true); puff.position = bottom; }
            if (question != null) { question.gameObject.SetActive(true); question.position = top; question.localScale = Vector3.zero; }
            var qGroup = question != null ? question.GetComponentsInChildren<Graphic>(true) : new Graphic[0];

            yield return Tween(0.55f, k =>
            {
                if (puff != null)
                {
                    float s = Mathf.Lerp(0.3f, 1.4f, 1f - (1f - k) * (1f - k));
                    puff.localScale = new Vector3(s, s, 1f);
                    puff.position = bottom + new Vector3(0f, 30f * k * unit, 0f);
                    if (puffImg != null) puffImg.color = new Color(1f, 1f, 1f, 0.9f * (1f - k));
                }
                if (question != null)
                {
                    float qk = Mathf.Clamp01(k / 0.4f);
                    float s = EaseOutBack(qk);
                    question.localScale = new Vector3(s, s, 1f);
                    question.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 4f) * 10f * (1f - k));
                    SetAlpha(qGroup, 1f);
                }
            });
            if (puff != null) puff.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return Tween(0.35f, k =>
            {
                if (question == null) return;
                question.position = top + new Vector3(0f, 40f * k * unit, 0f);
                SetAlpha(qGroup, 1f - k);
            });
            if (question != null) question.gameObject.SetActive(false);
            _nothing = null;
        }

        private IEnumerator Reveal(RectTransform target)
        {
            Vector3 baseScale = Vector3.one;
            var glowImg = revealGlow != null ? revealGlow.GetComponent<Image>() : null;
            var sparkImg = revealSparkle != null ? revealSparkle.GetComponent<Image>() : null;
            if (revealGlow != null) { revealGlow.gameObject.SetActive(true); revealGlow.position = target.position; }
            if (revealSparkle != null) { revealSparkle.gameObject.SetActive(true); revealSparkle.position = target.position; }

            yield return Tween(0.6f, k =>
            {
                float pop = 1f + 0.18f * Mathf.Sin(Mathf.Clamp01(k / 0.6f) * Mathf.PI);
                target.localScale = new Vector3(baseScale.x * pop, baseScale.y * pop, baseScale.z);
                if (revealGlow != null)
                {
                    float s = Mathf.Lerp(0.6f, 1.8f, k);
                    revealGlow.localScale = new Vector3(s, s, 1f);
                    if (glowImg != null) glowImg.color = new Color(1f, 0.92f, 0.5f, 0.8f * Mathf.Sin(k * Mathf.PI));
                }
                if (revealSparkle != null)
                {
                    float s = Mathf.Lerp(0.3f, 1.6f, k);
                    revealSparkle.localScale = new Vector3(s, s, 1f);
                    revealSparkle.localRotation = Quaternion.Euler(0, 0, 160f * k);
                    if (sparkImg != null) sparkImg.color = new Color(1f, 1f, 1f, 1f - k);
                }
            });
            target.localScale = baseScale;
            if (revealGlow != null) revealGlow.gameObject.SetActive(false);
            if (revealSparkle != null) revealSparkle.gameObject.SetActive(false);
            _reveal = null;
        }

        private float Unit()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform.lossyScale.y : 1f;
        }

        private static void SetAlpha(Graphic[] gs, float a)
        {
            foreach (var g in gs)
            {
                var c = g.color;
                c.a = a;
                g.color = c;
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private static IEnumerator Tween(float time, Action<float> step)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / time));
                yield return null;
            }
            step(1f);
        }
    }
}
