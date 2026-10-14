using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Biskas.Core
{
    public enum SaveDecodeStatus
    {
        Ok,
        Corrupt,
        Incompatible
    }

    /// <summary>
    /// Версионированный текстовый формат сохранения с контрольной суммой (FNV-1a 32).
    /// Пример:
    ///   BISKAS_SAVE
    ///   version=1
    ///   stage=HasKey
    ///   items=key_lab
    ///   flags=memory_seen,pass_inspected
    ///   checksum=1a2b3c4d
    /// </summary>
    public static class SaveCodec
    {
        public const string Header = "BISKAS_SAVE";
        public const int CurrentVersion = 1;

        public static string Encode(EpisodeState state)
        {
            var flags = new List<string>(state.Flags);
            flags.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            sb.Append(Header).Append('\n');
            sb.Append("version=").Append(CurrentVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("stage=").Append(state.Stage.ToString()).Append('\n');
            sb.Append("items=").Append(string.Join(",", state.Items)).Append('\n');
            sb.Append("flags=").Append(string.Join(",", flags)).Append('\n');
            string body = sb.ToString();
            return body + "checksum=" + Checksum(body) + "\n";
        }

        public static SaveDecodeStatus TryDecode(string text, out EpisodeState state)
        {
            state = null;
            if (string.IsNullOrEmpty(text)) return SaveDecodeStatus.Corrupt;
            try
            {
                string normalized = text.Replace("\r\n", "\n");
                int csIndex = normalized.LastIndexOf("checksum=", StringComparison.Ordinal);
                if (csIndex <= 0) return SaveDecodeStatus.Corrupt;
                string body = normalized.Substring(0, csIndex);
                string cs = normalized.Substring(csIndex + "checksum=".Length).Trim();
                if (!string.Equals(cs, Checksum(body), StringComparison.OrdinalIgnoreCase))
                    return SaveDecodeStatus.Corrupt;

                var lines = body.Split('\n');
                if (lines.Length < 2 || lines[0].Trim() != Header) return SaveDecodeStatus.Corrupt;

                var values = new Dictionary<string, string>();
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.Length == 0) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) return SaveDecodeStatus.Corrupt;
                    values[line.Substring(0, eq)] = line.Substring(eq + 1);
                }

                if (!values.TryGetValue("version", out var vs) ||
                    !int.TryParse(vs, NumberStyles.Integer, CultureInfo.InvariantCulture, out int version))
                    return SaveDecodeStatus.Corrupt;
                if (version > CurrentVersion || version < 1) return SaveDecodeStatus.Incompatible;

                var s = new EpisodeState();
                if (values.TryGetValue("items", out var items))
                    foreach (var it in items.Split(','))
                        if (it.Length > 0) s.AddItem(it);
                if (values.TryGetValue("flags", out var flags))
                    foreach (var f in flags.Split(','))
                        if (f.Length > 0) s.SetFlag(f);

                // Согласованность: ключ, полученный и не использованный, должен быть в инвентаре.
                if (s.HasFlag(FlagIds.KeyTaken) && !s.HasFlag(FlagIds.DoorOpened))
                    s.AddItem(ItemIds.LabKey);

                state = s;
                return SaveDecodeStatus.Ok;
            }
            catch (Exception)
            {
                return SaveDecodeStatus.Corrupt;
            }
        }

        public static string Checksum(string body)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (byte b in Encoding.UTF8.GetBytes(body))
                {
                    hash ^= b;
                    hash *= 16777619;
                }
                return hash.ToString("x8", CultureInfo.InvariantCulture);
            }
        }
    }
}
