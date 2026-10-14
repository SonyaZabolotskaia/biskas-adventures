namespace Biskas.Core
{
    /// <summary>Стабильные строковые ID. Не меняйте значения: они попадают в сохранения и данные.</summary>
    public static class ObjectIds
    {
        public const string Pass = "pass";
        public const string Cache = "cache";
        public const string Door = "door";
    }

    public static class ItemIds
    {
        public const string LabKey = "key_lab";
    }

    public static class FlagIds
    {
        public const string PassInspected = "pass_inspected";
        public const string MemorySeen = "memory_seen";          // подсказка получена
        public const string DialogueDone = "dialogue_done";
        public const string TrustVriskas = "trust_vriskas";      // выбор в диалоге: А
        public const string RelyOnSelf = "rely_on_self";         // выбор в диалоге: Б
        public const string CacheRevealed = "cache_revealed";
        public const string KeyTaken = "key_taken";
        public const string DoorOpened = "door_opened";
    }

    public static class DialogueIds
    {
        public const string AfterMemory = "dlg_after_memory";
    }

    public enum AbilityMode
    {
        Normal = 0,
        Memory = 1,
        XRay = 2
    }

    /// <summary>Этап эпизода (вычисляется из флагов; пишется в сохранение для читаемости).</summary>
    public enum EpisodeStage
    {
        Start = 0,
        PassInspected = 1,
        HintKnown = 2,
        CacheFound = 3,
        HasKey = 4,
        Completed = 5
    }

    public enum EpisodeEvent
    {
        None = 0,
        ShowMemory,
        CacheRevealed,
        KeyObtained,
        EpisodeCompleted
    }
}
