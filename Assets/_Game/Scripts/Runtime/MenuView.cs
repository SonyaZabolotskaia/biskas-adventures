using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>Стартовое меню / пауза: продолжить, новая игра, сообщение о сохранении.</summary>
    public sealed class MenuView : OverlayPanel
    {
        [SerializeField] private Text title;
        [SerializeField] private Text subtitle;
        [SerializeField] private Text status;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text continueLabel;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Text newGameLabel;

        public event Action ContinuePressed;
        public event Action NewGamePressed;

        private void Awake()
        {
            if (continueButton != null) continueButton.onClick.AddListener(() => { if (InputReady) ContinuePressed?.Invoke(); });
            if (newGameButton != null) newGameButton.onClick.AddListener(() => { if (InputReady) NewGamePressed?.Invoke(); });
        }

        public void Show(string titleText, string subtitleText, string statusText, bool canContinue,
                         string continueText, string newGameText)
        {
            if (title != null) title.text = titleText;
            if (subtitle != null) subtitle.text = subtitleText;
            SetStatus(statusText);
            if (continueButton != null) continueButton.gameObject.SetActive(canContinue);
            if (continueLabel != null) continueLabel.text = continueText;
            if (newGameLabel != null) newGameLabel.text = newGameText;
            SetOpen(true);
        }

        public void SetStatus(string statusText)
        {
            if (status != null) status.text = statusText ?? "";
        }

        public void Hide() => SetOpen(false);
    }
}
