using System;
using System.IO;
using System.Text;

namespace Biskas.Core
{
    public enum LoadStatus
    {
        NoSave,
        Ok,
        RestoredFromBackup,
        Corrupt,
        Incompatible
    }

    public readonly struct LoadResult
    {
        public readonly LoadStatus Status;
        public readonly EpisodeState State;
        public LoadResult(LoadStatus status, EpisodeState state) { Status = status; State = state; }
        public bool HasState => State != null;
    }

    /// <summary>
    /// Локальное хранилище: запись во временный файл → резервная копия старого → замена.
    /// Не использует File.Replace (на части Android-ФС он ненадёжен).
    /// </summary>
    public sealed class SaveStore
    {
        public const string FileName = "episode1.save";

        public string MainPath { get; }
        public string TempPath => MainPath + ".tmp";
        public string BackupPath => MainPath + ".bak";
        public string LastError { get; private set; }

        public SaveStore(string directory)
        {
            MainPath = Path.Combine(directory, FileName);
        }

        public bool Save(EpisodeState state)
        {
            LastError = null;
            try
            {
                string dir = Path.GetDirectoryName(MainPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                byte[] bytes = new UTF8Encoding(false).GetBytes(SaveCodec.Encode(state));
                using (var fs = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);
                }

                if (File.Exists(MainPath))
                {
                    // Резервная копия — только из корректного основного файла.
                    if (IsValidFile(MainPath)) File.Copy(MainPath, BackupPath, true);
                    File.Delete(MainPath);
                }
                File.Move(TempPath, MainPath);
                return true;
            }
            catch (Exception e)
            {
                LastError = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        public LoadResult Load()
        {
            LastError = null;
            bool anyFile = File.Exists(MainPath) || File.Exists(BackupPath) || File.Exists(TempPath);
            if (!anyFile) return new LoadResult(LoadStatus.NoSave, null);

            var main = TryRead(MainPath, out var mainState);
            if (main == SaveDecodeStatus.Ok) return new LoadResult(LoadStatus.Ok, mainState);
            if (main == SaveDecodeStatus.Incompatible) return new LoadResult(LoadStatus.Incompatible, null);

            // Прерванная запись: основной файл мог быть удалён до переименования временного.
            if (!File.Exists(MainPath) && TryRead(TempPath, out var tmpState) == SaveDecodeStatus.Ok)
                return new LoadResult(LoadStatus.RestoredFromBackup, tmpState);

            if (TryRead(BackupPath, out var bakState) == SaveDecodeStatus.Ok)
                return new LoadResult(LoadStatus.RestoredFromBackup, bakState);

            return new LoadResult(LoadStatus.Corrupt, null);
        }

        public void DeleteAll()
        {
            foreach (var p in new[] { MainPath, TempPath, BackupPath })
            {
                try { if (File.Exists(p)) File.Delete(p); }
                catch (Exception e) { LastError = e.GetType().Name + ": " + e.Message; }
            }
        }

        private static bool IsValidFile(string path) => TryRead(path, out _) == SaveDecodeStatus.Ok;

        private static SaveDecodeStatus TryRead(string path, out EpisodeState state)
        {
            state = null;
            try
            {
                if (!File.Exists(path)) return SaveDecodeStatus.Corrupt;
                return SaveCodec.TryDecode(File.ReadAllText(path, Encoding.UTF8), out state);
            }
            catch (Exception)
            {
                return SaveDecodeStatus.Corrupt;
            }
        }
    }
}
