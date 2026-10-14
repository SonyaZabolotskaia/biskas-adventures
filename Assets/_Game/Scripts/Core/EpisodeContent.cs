using System;
using System.Collections.Generic;

namespace Biskas.Core
{
    // Данные эпизода (тексты, воспоминание, диалоги) хранятся в JSON:
    // Assets/_Game/Data/episode1_ru.json. Классы совместимы с UnityEngine.JsonUtility
    // (публичные поля, [Serializable]) и с System.Text.Json (IncludeFields).

    [Serializable]
    public class EpisodeContent
    {
        public int version;
        public List<SpeakerEntry> speakers = new List<SpeakerEntry>();
        public List<MessageEntry> messages = new List<MessageEntry>();
        public MemoryData memory = new MemoryData();
        public List<DialogueData> dialogues = new List<DialogueData>();

        /// <summary>
        /// Первая запись с этим ID, условие которой выполнено. Записи с условием
        /// располагаются в файле перед записью по умолчанию (без условия).
        /// </summary>
        public MessageEntry ResolveMessage(string id, EpisodeState state)
        {
            if (string.IsNullOrEmpty(id) || messages == null) return null;
            foreach (var m in messages)
            {
                if (m == null || m.id != id) continue;
                if (string.IsNullOrEmpty(m.requiresFlag) || (state != null && state.HasFlag(m.requiresFlag)))
                    return m;
            }
            return null;
        }

        public string Text(string id, EpisodeState state = null)
        {
            var m = ResolveMessage(id, state);
            return m != null ? m.text : "[" + id + "]";
        }

        public string SpeakerName(string speakerId)
        {
            if (string.IsNullOrEmpty(speakerId) || speakers == null) return "";
            foreach (var s in speakers)
                if (s != null && s.id == speakerId) return s.name ?? "";
            return "";
        }

        public DialogueData FindDialogue(string id)
        {
            if (dialogues == null) return null;
            foreach (var d in dialogues) if (d != null && d.id == id) return d;
            return null;
        }
    }

    [Serializable]
    public class SpeakerEntry
    {
        public string id;
        public string name;
    }

    [Serializable]
    public class MessageEntry
    {
        public string id;
        public string speaker;
        public string text;
        /// <summary>Условие: реплика выбирается, только если флаг установлен. Пусто — по умолчанию.</summary>
        public string requiresFlag;
    }

    [Serializable]
    public class MemoryData
    {
        public string id;
        public string title;
        /// <summary>Шаги воспоминания: картинка в облаке + подпись + объяснение Бискаса.</summary>
        public List<MemoryStep> steps = new List<MemoryStep>();
    }

    [Serializable]
    public class MemoryStep
    {
        /// <summary>Имя спрайта-иконки в облаке (Assets/_Game/Art/Memory).</summary>
        public string icon;
        /// <summary>Короткая подпись сцены внутри облака.</summary>
        public string caption;
        /// <summary>Объяснение Бискаса под облаком.</summary>
        public string explain;
    }

    [Serializable]
    public class DialogueData
    {
        public string id;
        public string start;
        public List<DialogueNode> nodes = new List<DialogueNode>();

        public DialogueNode Find(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || nodes == null) return null;
            foreach (var n in nodes) if (n != null && n.id == nodeId) return n;
            return null;
        }
    }

    [Serializable]
    public class DialogueNode
    {
        public string id;
        public string speaker;
        public string text;
        /// <summary>Если задано и флаг не установлен — узел пропускается (переход к next).</summary>
        public string requiresFlag;
        public string next;
        /// <summary>Эффекты при входе в узел.</summary>
        public List<string> setFlags = new List<string>();
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [Serializable]
    public class DialogueChoice
    {
        public string text;
        public string setFlag;
        public string next;
    }
}
