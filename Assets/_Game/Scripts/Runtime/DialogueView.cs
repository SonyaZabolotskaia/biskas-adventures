using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    public sealed class DialogueView : OverlayPanel
    {
        [SerializeField] private Text speakerName;
        [SerializeField] private Text lineText;
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private Text leftPortraitLabel;
        [SerializeField] private Text rightPortraitLabel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;
        [SerializeField] private CharacterAnimator leftCharacter;    // анимированный Бискас
        [SerializeField] private CharacterAnimator rightCharacter;   // анимированный Врискас
        [SerializeField] private Button[] choiceButtons = new Button[0];
        [SerializeField] private Text[] choiceLabels = new Text[0];

        public event Action NextPressed;
        public event Action<int> ChoicePressed;
        private int _shownFrame = -1;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(() => { if (Accept()) NextPressed?.Invoke(); });
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int idx = i;
                if (choiceButtons[i] != null)
                    choiceButtons[i].onClick.AddListener(() => { if (Accept()) ChoicePressed?.Invoke(idx); });
            }
            SetOpen(false);
        }

        // Одно действие на показанную реплику; касания в первые InputDelay секунд после показа игнорируются.
        private bool Accept()
        {
            if (!InputReady || _shownFrame < 0) return false;
            _shownFrame = -1;
            return true;
        }

        public void ShowLine(string speakerId, string speaker, string text, IList<string> choices, string nextText,
                             string leftName, string rightName)
        {
            SetOpen(true);
            _shownFrame = Time.frameCount;
            MarkShown();
            if (speakerName != null) speakerName.text = speaker;
            if (lineText != null) lineText.text = text;
            if (leftPortraitLabel != null) leftPortraitLabel.text = leftName;
            if (rightPortraitLabel != null) rightPortraitLabel.text = rightName;
            // Говорящий подсвечен, второй затемнён (место под будущие портреты).
            if (leftCharacter != null)
            {
                if (leftPortrait != null) leftPortrait.color = speakerId == "biskas" ? new Color(0.3f, 0.2f, 0.12f) : new Color(0.12f, 0.1f, 0.09f);
                leftCharacter.SetTalking(speakerId == "biskas");
                leftCharacter.SetDimmed(speakerId != "biskas");
            }
            else if (leftPortrait != null)
                leftPortrait.color = speakerId == "biskas" ? new Color(0.93f, 0.55f, 0.2f) : new Color(0.45f, 0.3f, 0.2f);
            if (rightCharacter != null)
            {
                // Фон портрета нейтральный, персонаж говорит/затемняется сам.
                if (rightPortrait != null) rightPortrait.color = speakerId == "vriskas" ? new Color(0.16f, 0.24f, 0.2f) : new Color(0.1f, 0.12f, 0.14f);
                rightCharacter.SetTalking(speakerId == "vriskas");
                rightCharacter.SetDimmed(speakerId != "vriskas");
            }
            else if (rightPortrait != null)
                rightPortrait.color = speakerId == "vriskas" ? new Color(0.3f, 0.85f, 0.4f) : new Color(0.15f, 0.35f, 0.2f);

            bool hasChoices = choices != null && choices.Count > 0;
            if (nextButton != null) nextButton.gameObject.SetActive(!hasChoices);
            if (nextLabel != null) nextLabel.text = nextText;
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool show = hasChoices && i < choices.Count;
                if (choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(show);
                if (show && i < choiceLabels.Length && choiceLabels[i] != null) choiceLabels[i].text = choices[i];
            }
        }

        public void Hide()
        {
            if (rightCharacter != null) rightCharacter.SetTalking(false);
            if (leftCharacter != null) leftCharacter.SetTalking(false);
            _shownFrame = -1;
            SetOpen(false);
        }
    }
}
