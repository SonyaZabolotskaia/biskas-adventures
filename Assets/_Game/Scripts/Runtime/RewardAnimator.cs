using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>
    /// Анимация открытия тайника и получения ключа:
    /// 1) створка панели поворачивается на петле (сжатие по X) и открывает нишу;
    /// 2) ключ поднимается из ниши, покачиваясь, вокруг — вспышка;
    /// 3) ключ летит по дуге в ячейку инвентаря, ячейка «подпрыгивает».
    /// Работает на unscaled time, вызывающий код блокирует ввод до onDone.
    /// </summary>
    public sealed class RewardAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform cacheFlap;      // створка (поверх визуала тайника), pivot на петле
        [SerializeField] private RectTransform cacheCenter;    // откуда появляется ключ
        [SerializeField] private RectTransform flyingKey;      // Image ключа на верхнем слое
        [SerializeField] private RectTransform sparkle;        // вспышка
        [SerializeField] private float flapTime = 0.35f;
        [SerializeField] private float riseTime = 0.5f;
        [SerializeField] private float riseHeight = 160f;
        [SerializeField] private float flyTime = 0.6f;

        public bool IsPlaying { get; private set; }

        private void Awake() => ResetVisuals();

        public void ResetVisuals()
        {
            if (cacheFlap != null) { cacheFlap.gameObject.SetActive(false); cacheFlap.localScale = Vector3.one; }
            if (flyingKey != null) flyingKey.gameObject.SetActive(false);
            if (sparkle != null) sparkle.gameObject.SetActive(false);
        }

        public void Stop()
        {
            StopAllCoroutines();
            IsPlaying = false;
            ResetVisuals();
        }

        public void PlayKeyPickup(RectTransform target, Action onDone)
        {
            StopAllCoroutines();
            StartCoroutine(Sequence(target, onDone));
        }

        private IEnumerator Sequence(RectTransform target, Action onDone)
        {
            IsPlaying = true;

            // 1. створка открывается на петле
            if (cacheFlap != null)
            {
                cacheFlap.gameObject.SetActive(true);
                yield return Tween(flapTime, k =>
                {
                    float e = k * k;
                    cacheFlap.localScale = new Vector3(Mathf.Lerp(1f, 0.05f, e), 1f + 0.06f * Mathf.Sin(k * Mathf.PI), 1f);
                });
                cacheFlap.gameObject.SetActive(false);
            }

            // 2. ключ поднимается, вспышка
            Vector3 start = cacheCenter != null ? cacheCenter.position : transform.position;
            float unit = flyingKey != null ? UnitsToWorld(flyingKey) : 1f;
            Vector3 top = start + new Vector3(0f, riseHeight * unit, 0f);
            if (flyingKey != null)
            {
                flyingKey.gameObject.SetActive(true);
                flyingKey.position = start;
            }
            if (sparkle != null)
            {
                sparkle.gameObject.SetActive(true);
                sparkle.position = start;
            }
            var sparkImg = sparkle != null ? sparkle.GetComponent<Image>() : null;
            yield return Tween(riseTime, k =>
            {
                float e = 1f - (1f - k) * (1f - k);
                if (flyingKey != null)
                {
                    flyingKey.position = Vector3.Lerp(start, top, e);
                    float s = Mathf.Lerp(0.4f, 1.4f, e);
                    flyingKey.localScale = new Vector3(s, s, 1f);
                    flyingKey.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 3f) * 15f);
                }
                if (sparkle != null)
                {
                    sparkle.position = Vector3.Lerp(start, top, e);
                    float s = Mathf.Lerp(0.4f, 2.2f, k);
                    sparkle.localScale = new Vector3(s, s, 1f);
                    sparkle.localRotation = Quaternion.Euler(0, 0, k * 120f);
                    if (sparkImg != null) sparkImg.color = new Color(1f, 1f, 1f, 1f - k);
                }
            });
            if (sparkle != null) sparkle.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.12f);

            // 3. полёт по дуге в инвентарь
            if (flyingKey != null && target != null)
            {
                Vector3 end = target.position;
                Vector3 control = (top + end) * 0.5f + new Vector3(0f, 220f * unit, 0f);
                yield return Tween(flyTime, k =>
                {
                    float e = k * k * (3f - 2f * k);
                    Vector3 a = Vector3.Lerp(top, control, e), b = Vector3.Lerp(control, end, e);
                    flyingKey.position = Vector3.Lerp(a, b, e);
                    float s = Mathf.Lerp(1.4f, 0.55f, e);
                    flyingKey.localScale = new Vector3(s, s, 1f);
                    flyingKey.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, 360f, e));
                });
            }
            if (flyingKey != null) flyingKey.gameObject.SetActive(false);

            IsPlaying = false;
            onDone?.Invoke();

            // 4. ячейка «подпрыгивает»
            if (target != null)
                yield return Tween(0.25f, k =>
                {
                    float s = 1f + 0.25f * Mathf.Sin(k * Mathf.PI);
                    target.localScale = new Vector3(s, s, 1f);
                });
        }

        private static float UnitsToWorld(RectTransform rt)
        {
            var canvas = rt.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform.lossyScale.y : 1f;
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
