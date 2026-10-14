using System.Collections.Generic;

namespace Biskas.Core
{
    /// <summary>
    /// Сохраняемое состояние прохождения: предметы и сюжетные флаги.
    /// Режим способности, выделение и подсветки сюда не входят — это состояние представления.
    /// </summary>
    public sealed class EpisodeState
    {
        private readonly HashSet<string> _flags = new HashSet<string>();
        private readonly List<string> _items = new List<string>();

        public IReadOnlyCollection<string> Flags => _flags;
        public IReadOnlyList<string> Items => _items;

        public bool HasFlag(string id) => !string.IsNullOrEmpty(id) && _flags.Contains(id);

        /// <returns>true, если флаг был добавлен впервые.</returns>
        public bool SetFlag(string id) => !string.IsNullOrEmpty(id) && _flags.Add(id);

        public bool ClearFlag(string id) => !string.IsNullOrEmpty(id) && _flags.Remove(id);

        public bool HasItem(string id) => _items.Contains(id);

        /// <returns>true, если предмет добавлен; повторная выдача игнорируется.</returns>
        public bool AddItem(string id)
        {
            if (string.IsNullOrEmpty(id) || _items.Contains(id)) return false;
            _items.Add(id);
            return true;
        }

        public bool RemoveItem(string id) => _items.Remove(id);

        public int CountItem(string id)
        {
            int n = 0;
            foreach (var i in _items) if (i == id) n++;
            return n;
        }

        public EpisodeStage Stage
        {
            get
            {
                if (HasFlag(FlagIds.DoorOpened)) return EpisodeStage.Completed;
                if (HasFlag(FlagIds.KeyTaken)) return EpisodeStage.HasKey;
                if (HasFlag(FlagIds.CacheRevealed)) return EpisodeStage.CacheFound;
                if (HasFlag(FlagIds.MemorySeen)) return EpisodeStage.HintKnown;
                if (HasFlag(FlagIds.PassInspected)) return EpisodeStage.PassInspected;
                return EpisodeStage.Start;
            }
        }

        public bool IsCompleted => HasFlag(FlagIds.DoorOpened);

        public EpisodeState Clone()
        {
            var c = new EpisodeState();
            foreach (var f in _flags) c._flags.Add(f);
            c._items.AddRange(_items);
            return c;
        }

        public bool ContentEquals(EpisodeState other)
        {
            if (other == null) return false;
            if (!_flags.SetEquals(other._flags)) return false;
            if (_items.Count != other._items.Count) return false;
            for (int i = 0; i < _items.Count; i++)
                if (_items[i] != other._items[i]) return false;
            return true;
        }
    }
}
