using System;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>
    /// Процедурная анимация персонажа из частей (UI Image): дыхание, покачивание хвоста и головы,
    /// моргание, речь (рот + кивки), свечение глаз в X-Ray, подпрыгивание-реакция, затемнение.
    /// Части лежат на общем холсте и вращаются вокруг pivot своих суставов. Без Animator и клипов —
    /// новые утверждённые спрайты подставляются в те же Image без перенастройки.
    /// </summary>
    public sealed class CharacterAnimator : MonoBehaviour
    {
        [Header("Части")]
        [SerializeField] private RectTransform body;
        [SerializeField] private RectTransform head;
        [SerializeField] private RectTransform tail;
        [SerializeField] private GameObject eyesOpen;
        [SerializeField] private GameObject eyesClosed;
        [SerializeField] private GameObject mouthTalk;
        [SerializeField] private Image eyesGlow;

        [Header("Движение")]
        [SerializeField] private float breatheAmplitude = 0.018f;
        [SerializeField] private float breatheSpeed = 1.7f;
        [SerializeField] private float tailAmplitude = 9f;
        [SerializeField] private float tailSpeed = 1.3f;
        [SerializeField] private float headAmplitude = 2.5f;
        [SerializeField] private float talkMouthRate = 7f;
        [SerializeField] private Vector2 blinkInterval = new Vector2(2.2f, 5.5f);
        [SerializeField] private float blinkDuration = 0.12f;
        [SerializeField] private float bounceDuration = 0.45f;
        [SerializeField] private float bounceHeight = 0.12f;  // доля высоты персонажа
        [SerializeField] private Color dimColor = new Color(0.5f, 0.55f, 0.55f, 1f);

        [Header("Ходьба и жесты")]
        [SerializeField] private float walkHopHeight = 0.07f;   // доля высоты персонажа
        [SerializeField] private float walkStepsPerSecond = 3.2f;
        [SerializeField] private float reachDuration = 0.35f;
        [SerializeField] private float shakeDuration = 0.6f;

        private bool _talking;
        private float _talkUntil;
        private bool _xray;
        private bool _dimmed;
        private float _nextBlink;
        private float _blinkEnd;
        private float _bounceStart = -10f;
        private float _phase;
        private Image[] _images;
        private RectTransform _rect;
        private Vector2 _basePos;
        private Vector2 _homePos;
        private bool _moving;
        private Vector2 _moveFrom, _moveTo;
        private float _moveStart, _moveDuration;
        private Action _onArrived;
        private float _reachStart = -10f;
        private float _shakeStart = -10f;

        public bool IsTalking => _talking || Time.unscaledTime < _talkUntil;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _basePos = _rect.anchoredPosition;
            _homePos = _basePos;
            _images = GetComponentsInChildren<Image>(true);
            _phase = UnityEngine.Random.value * 10f;
            ScheduleBlink();
            ApplyDim();
            if (eyesGlow != null) eyesGlow.gameObject.SetActive(false);
            if (mouthTalk != null) mouthTalk.SetActive(false);
            if (eyesClosed != null) eyesClosed.SetActive(false);
        }

        private void OnDisable()
        {
            if (_rect != null) _rect.anchoredPosition = _basePos;
        }

        /// <summary>Говорит, пока не будет вызвано SetTalking(false).</summary>
        public void SetTalking(bool talking) => _talking = talking;

        /// <summary>Говорит заданное время (для реплик в строке сообщений).</summary>
        public void Talk(float seconds) => _talkUntil = Mathf.Max(_talkUntil, Time.unscaledTime + seconds);

        /// <summary>Свечение глаз (X-Ray у Врискаса, память у Бискаса).</summary>
        public void SetGlow(bool on) => SetXRay(on);

        public void SetXRay(bool on)
        {
            _xray = on;
            if (eyesGlow != null && eyesGlow.gameObject.activeSelf != on) eyesGlow.gameObject.SetActive(on);
        }

        public void SetDimmed(bool dimmed)
        {
            if (_dimmed == dimmed) return;
            _dimmed = dimmed;
            ApplyDim();
        }

        /// <summary>Короткий прыжок-реакция (например, тайник найден).</summary>
        public void Bounce() => _bounceStart = Time.unscaledTime;

        // ---------------- ходьба и жесты ----------------

        public bool IsMoving => _moving;
        /// <summary>Исходная позиция (anchoredPosition), где персонаж стоит при запуске сцены.</summary>
        public Vector2 HomePosition => _homePos;
        public Vector2 Position => _basePos;
        public bool FacingLeft => _rect != null && _rect.localScale.x < 0f;

        /// <summary>Идёт (вприпрыжку) к точке anchoredPosition и вызывает onArrived по прибытии.</summary>
        public void MoveTo(Vector2 target, float speed, Action onArrived)
        {
            float dist = Vector2.Distance(_basePos, target);
            if (dist < 4f || speed <= 0f)
            {
                _basePos = target;
                onArrived?.Invoke();
                return;
            }
            SetFacing(target.x < _basePos.x);
            _moveFrom = _basePos;
            _moveTo = target;
            _moveStart = Time.unscaledTime;
            _moveDuration = Mathf.Max(0.25f, dist / speed);
            _onArrived = onArrived;
            _moving = true;
        }

        /// <summary>Остановиться и (по желанию) вернуться домой мгновенно; колбэк прибытия отменяется.</summary>
        public void StopMove(bool returnHome)
        {
            _moving = false;
            _onArrived = null;
            if (returnHome) { _basePos = _homePos; SetFacing(false); }
        }

        public void SetFacing(bool left)
        {
            if (_rect == null) _rect = (RectTransform)transform;
            var sc = _rect.localScale;
            float x = Mathf.Abs(sc.x) * (left ? -1f : 1f);
            if (!Mathf.Approximately(sc.x, x)) _rect.localScale = new Vector3(x, sc.y, sc.z);
        }

        /// <summary>Тянется к предмету: наклон головы вперёд и шаг-подскок.</summary>
        public void Reach() => _reachStart = Time.unscaledTime;

        /// <summary>«Ничего нет»: качает головой.</summary>
        public void ShakeHead() => _shakeStart = Time.unscaledTime;

        private void ApplyDim()
        {
            if (_images == null) return;
            foreach (var img in _images)
                if (img != null && img != eyesGlow) img.color = _dimmed ? dimColor : Color.white;
        }

        private void ScheduleBlink()
        {
            _nextBlink = Time.unscaledTime + UnityEngine.Random.Range(blinkInterval.x, blinkInterval.y);
        }

        private void Update()
        {
            float t = Time.unscaledTime + _phase;
            bool talking = IsTalking;

            if (body != null)
                body.localScale = new Vector3(1f - breatheAmplitude * 0.4f * Mathf.Sin(t * breatheSpeed),
                                              1f + breatheAmplitude * Mathf.Sin(t * breatheSpeed), 1f);

            if (tail != null)
            {
                float speed = _xray ? tailSpeed * 2.2f : tailSpeed;   // в X-Ray хвост «нервничает»
                tail.localRotation = Quaternion.Euler(0, 0, tailAmplitude * Mathf.Sin(t * speed));
            }

            // Ходьба
            float walkHop = 0f;
            if (_moving)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - _moveStart) / _moveDuration);
                float e = k * k * (3f - 2f * k);
                _basePos = Vector2.LerpUnclamped(_moveFrom, _moveTo, e);
                walkHop = Mathf.Abs(Mathf.Sin(k * _moveDuration * walkStepsPerSecond * Mathf.PI)) * walkHopHeight * _rect.rect.height;
                if (tail != null) tail.localRotation = Quaternion.Euler(0, 0, tailAmplitude * 1.6f * Mathf.Sin(t * 9f));
                if (k >= 1f)
                {
                    _moving = false;
                    var cb = _onArrived;
                    _onArrived = null;
                    cb?.Invoke();
                }
            }

            float rk = (Time.unscaledTime - _reachStart) / reachDuration;
            bool reaching = rk >= 0f && rk <= 1f;
            float sk = (Time.unscaledTime - _shakeStart) / shakeDuration;
            bool shaking = sk >= 0f && sk <= 1f;

            if (head != null)
            {
                float nod = talking ? headAmplitude * 1.8f * Mathf.Sin(t * 9f) : headAmplitude * Mathf.Sin(t * 0.9f);
                if (_moving) nod = headAmplitude * 1.5f * Mathf.Sin(t * 12f);
                if (reaching) nod = -14f * Mathf.Sin(rk * Mathf.PI);            // наклон к предмету (знак учитывает разворот)
                if (shaking) nod = 12f * Mathf.Sin(sk * Mathf.PI * 6f) * (1f - sk);
                head.localRotation = Quaternion.Euler(0, 0, nod);
                float lift = breatheAmplitude * Mathf.Sin(t * breatheSpeed) * _rect.rect.height * 0.5f;
                head.anchoredPosition = new Vector2(0f, lift);
            }

            if (mouthTalk != null)
            {
                bool open = talking && Mathf.Sin(Time.unscaledTime * talkMouthRate * Mathf.PI) > 0f;
                if (mouthTalk.activeSelf != open) mouthTalk.SetActive(open);
            }

            // Моргание (в X-Ray глаза не закрываются)
            if (!_xray && Time.unscaledTime >= _nextBlink)
            {
                _blinkEnd = Time.unscaledTime + blinkDuration;
                ScheduleBlink();
            }
            bool closed = !_xray && Time.unscaledTime < _blinkEnd;
            if (eyesClosed != null && eyesClosed.activeSelf != closed) eyesClosed.SetActive(closed);
            if (eyesOpen != null && eyesOpen.activeSelf == closed) eyesOpen.SetActive(!closed);

            if (_xray && eyesGlow != null)
                eyesGlow.color = new Color(1f, 1f, 1f, 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f));

            // Прыжок
            float b = (Time.unscaledTime - _bounceStart) / bounceDuration;
            float hop = (b >= 0f && b <= 1f) ? Mathf.Sin(b * Mathf.PI) * bounceHeight * _rect.rect.height : 0f;
            float reachX = reaching ? Mathf.Sin(rk * Mathf.PI) * 0.08f * _rect.rect.width * (FacingLeft ? -1f : 1f) : 0f;
            float reachY = reaching ? Mathf.Sin(rk * Mathf.PI) * 0.05f * _rect.rect.height : 0f;
            _rect.anchoredPosition = _basePos + new Vector2(reachX, hop + walkHop + reachY);
        }
    }
}
