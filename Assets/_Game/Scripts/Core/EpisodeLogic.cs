using System.Collections.Generic;

namespace Biskas.Core
{
    public readonly struct ActionResult
    {
        /// <summary>ID реплики/сообщения из данных эпизода (может быть пустым).</summary>
        public readonly string MessageId;
        /// <summary>Сюжетное состояние изменилось — нужно сохранить.</summary>
        public readonly bool StateChanged;
        public readonly EpisodeEvent Event;

        public ActionResult(string messageId, bool stateChanged = false, EpisodeEvent evt = EpisodeEvent.None)
        {
            MessageId = messageId;
            StateChanged = stateChanged;
            Event = evt;
        }

        public override string ToString() => $"{MessageId} changed={StateChanged} evt={Event}";
    }

    /// <summary>
    /// Правила одной лабораторной комнаты. Не знает про Unity, UI и тексты:
    /// возвращает ID сообщений и события. Все действия идемпотентны относительно
    /// повторных касаний: ключ выдаётся только один раз, завершение — один раз.
    /// </summary>
    public sealed class EpisodeLogic
    {
        // Все ID сообщений, которые может вернуть логика (проверяются самопроверкой данных).
        public static readonly string[] AllMessageIds =
        {
            "pass_inspect_first", "pass_inspect_again", "memory_pass_need_inspect", "memory_pass_seen",
            "memory_pass_replay", "xray_pass",
            "cache_wall_plain", "cache_key_taken", "cache_empty", "memory_wall", "memory_cache_after",
            "xray_wall_no_hint", "xray_cache_found", "xray_cache_open_hint", "xray_cache_empty",
            "door_locked", "door_select_key", "memory_door", "xray_door", "door_open", "door_already_open",
            "mode_normal", "mode_memory", "mode_xray",
            "item_selected_key", "item_deselected", "unknown_object"
        };

        public EpisodeState State { get; private set; }
        public AbilityMode Mode { get; private set; } = AbilityMode.Normal;
        public string SelectedItem { get; private set; }

        public EpisodeLogic(EpisodeState state)
        {
            State = state ?? new EpisodeState();
        }

        /// <summary>Новая игра: полный сброс сюжетного состояния и представления.</summary>
        public void Reset(EpisodeState state = null)
        {
            State = state ?? new EpisodeState();
            Mode = AbilityMode.Normal;
            SelectedItem = null;
        }

        /// <summary>Нажатие на кнопку способности: включает режим, повторное нажатие возвращает обычный.</summary>
        public ActionResult ToggleMode(AbilityMode mode)
        {
            Mode = (Mode == mode) ? AbilityMode.Normal : mode;
            switch (Mode)
            {
                case AbilityMode.Memory: return new ActionResult("mode_memory");
                case AbilityMode.XRay: return new ActionResult("mode_xray");
                default: return new ActionResult("mode_normal");
            }
        }

        public ActionResult ToggleSelectItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !State.HasItem(itemId))
            {
                SelectedItem = null;
                return new ActionResult(null);
            }
            if (SelectedItem == itemId)
            {
                SelectedItem = null;
                return new ActionResult("item_deselected");
            }
            SelectedItem = itemId;
            return new ActionResult(itemId == ItemIds.LabKey ? "item_selected_key" : null);
        }

        public ActionResult TapObject(string objectId)
        {
            switch (objectId)
            {
                case ObjectIds.Pass: return TapPass();
                case ObjectIds.Cache: return TapCache();
                case ObjectIds.Door: return TapDoor();
                default: return new ActionResult("unknown_object");
            }
        }

        private ActionResult TapPass()
        {
            switch (Mode)
            {
                case AbilityMode.Memory:
                    if (!State.HasFlag(FlagIds.PassInspected))
                        return new ActionResult("memory_pass_need_inspect");
                    if (State.SetFlag(FlagIds.MemorySeen))
                        return new ActionResult("memory_pass_seen", true, EpisodeEvent.ShowMemory);
                    return new ActionResult("memory_pass_replay", false, EpisodeEvent.ShowMemory);

                case AbilityMode.XRay:
                    return new ActionResult("xray_pass");

                default:
                    if (State.SetFlag(FlagIds.PassInspected))
                        return new ActionResult("pass_inspect_first", true);
                    return new ActionResult("pass_inspect_again");
            }
        }

        private ActionResult TapCache()
        {
            bool revealed = State.HasFlag(FlagIds.CacheRevealed);
            bool taken = State.HasFlag(FlagIds.KeyTaken);

            switch (Mode)
            {
                case AbilityMode.Memory:
                    return new ActionResult(taken ? "memory_cache_after" : "memory_wall");

                case AbilityMode.XRay:
                    if (taken) return new ActionResult("xray_cache_empty");
                    if (revealed) return new ActionResult("xray_cache_open_hint");
                    if (!State.HasFlag(FlagIds.MemorySeen)) return new ActionResult("xray_wall_no_hint");
                    State.SetFlag(FlagIds.CacheRevealed);
                    return new ActionResult("xray_cache_found", true, EpisodeEvent.CacheRevealed);

                default:
                    if (taken) return new ActionResult("cache_empty");
                    if (!revealed) return new ActionResult("cache_wall_plain");
                    // Ключ выдаётся ровно один раз: флаг и предмет ставятся вместе.
                    State.SetFlag(FlagIds.KeyTaken);
                    State.AddItem(ItemIds.LabKey);
                    return new ActionResult("cache_key_taken", true, EpisodeEvent.KeyObtained);
            }
        }

        private ActionResult TapDoor()
        {
            if (State.HasFlag(FlagIds.DoorOpened))
                return new ActionResult("door_already_open");

            bool hasKey = State.HasItem(ItemIds.LabKey);
            if (hasKey && SelectedItem == ItemIds.LabKey)
            {
                State.SetFlag(FlagIds.DoorOpened);
                State.RemoveItem(ItemIds.LabKey);
                SelectedItem = null;
                return new ActionResult("door_open", true, EpisodeEvent.EpisodeCompleted);
            }
            if (hasKey) return new ActionResult("door_select_key");

            switch (Mode)
            {
                case AbilityMode.Memory: return new ActionResult("memory_door");
                case AbilityMode.XRay: return new ActionResult("xray_door");
                default: return new ActionResult("door_locked");
            }
        }

        /// <summary>Нужно ли (снова) показать диалог после воспоминания — например после загрузки сохранения.</summary>
        public bool IsDialoguePending => State.HasFlag(FlagIds.MemorySeen) && !State.HasFlag(FlagIds.DialogueDone);
    }
}
