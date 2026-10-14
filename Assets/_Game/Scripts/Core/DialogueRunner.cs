using System.Collections.Generic;

namespace Biskas.Core
{
    /// <summary>
    /// Проходит диалог из данных: узлы, условия (requiresFlag), эффекты (setFlags) и выбор.
    /// Повторные вызовы Advance/Choose в неподходящем узле игнорируются.
    /// </summary>
    public sealed class DialogueRunner
    {
        private const int MaxSkipChain = 64;

        private DialogueData _data;
        private EpisodeState _state;

        public DialogueNode Current { get; private set; }
        public bool IsActive => Current != null;
        /// <summary>Сюжетные флаги изменились с момента последнего ConsumeStateChanged().</summary>
        public bool StateChanged { get; private set; }

        public bool Start(DialogueData data, EpisodeState state)
        {
            _data = data;
            _state = state;
            Current = null;
            if (data == null || state == null) return false;
            Enter(data.start);
            return IsActive;
        }

        public bool HasChoices => Current != null && Current.choices != null && Current.choices.Count > 0;

        /// <summary>Переход к следующей реплике. Не работает в узле с выбором.</summary>
        public bool Advance()
        {
            if (Current == null || HasChoices) return false;
            Enter(Current.next);
            return true;
        }

        public bool Choose(int index)
        {
            if (!HasChoices || index < 0 || index >= Current.choices.Count) return false;
            var node = Current;
            var choice = node.choices[index];
            // Выбор взаимоисключающий: снимаем флаги других вариантов этого узла
            // (важно при повторном прохождении диалога после восстановления сохранения).
            for (int i = 0; i < node.choices.Count; i++)
            {
                if (i == index) continue;
                if (_state.ClearFlag(node.choices[i].setFlag)) StateChanged = true;
            }
            if (_state.SetFlag(choice.setFlag)) StateChanged = true;
            Enter(choice.next);
            return true;
        }

        public bool ConsumeStateChanged()
        {
            bool c = StateChanged;
            StateChanged = false;
            return c;
        }

        public void Stop() => Current = null;

        private void Enter(string nodeId)
        {
            Current = null;
            for (int guard = 0; guard < MaxSkipChain && !string.IsNullOrEmpty(nodeId); guard++)
            {
                var node = _data.Find(nodeId);
                if (node == null) return;
                if (!string.IsNullOrEmpty(node.requiresFlag) && !_state.HasFlag(node.requiresFlag))
                {
                    nodeId = node.next;
                    continue;
                }
                if (node.setFlags != null)
                    foreach (var f in node.setFlags)
                        if (_state.SetFlag(f)) StateChanged = true;
                Current = node;
                return;
            }
        }
    }
}
