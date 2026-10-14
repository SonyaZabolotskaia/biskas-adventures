using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    public sealed class EndView : OverlayPanel
    {
        [SerializeField] private Text title;
        [SerializeField] private Text body;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Text newGameLabel;

        public event Action NewGamePressed;

        private void Awake()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(() => { if (InputReady) NewGamePressed?.Invoke(); });
            SetOpen(false);
        }

        public void Show(string titleText, string bodyText, string newGameText)
        {
            if (title != null) title.text = titleText;
            if (body != null) body.text = bodyText;
            if (newGameLabel != null) newGameLabel.text = newGameText;
            SetOpen(true);
        }

        public void Hide() => SetOpen(false);
    }
}
