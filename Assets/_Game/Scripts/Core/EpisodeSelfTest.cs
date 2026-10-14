using System;
using System.Collections.Generic;
using System.IO;

namespace Biskas.Core
{
    /// <summary>
    /// Автоматическая проверка логики, данных и сохранений эпизода без Unity.
    /// Запускается из меню Biskas → «Самопроверка логики», через -executeMethod
    /// и из консольного Tools/CoreCheck. Не заменяет проверку в редакторе и на телефоне.
    /// </summary>
    public static class EpisodeSelfTest
    {
        public sealed class Report
        {
            public readonly List<string> Passed = new List<string>();
            public readonly List<string> Failed = new List<string>();
            public bool Ok => Failed.Count == 0;

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder();
                foreach (var p in Passed) sb.Append("[OK]   ").Append(p).Append('\n');
                foreach (var f in Failed) sb.Append("[FAIL] ").Append(f).Append('\n');
                sb.Append(Ok ? "ИТОГ: все проверки пройдены" : "ИТОГ: есть ошибки").Append($" ({Passed.Count} ok, {Failed.Count} fail)");
                return sb.ToString();
            }
        }

        public static Report Run(EpisodeContent content, string tempDirectory)
        {
            var r = new Report();
            Check(r, "Данные: все сообщения логики и диалог присутствуют", () => ValidateContent(content));
            Check(r, "1. Главный путь: пропуск → память → диалог → X-Ray → тайник → ключ → дверь", () => MainPath(content, 0));
            Check(r, "2. Неправильный порядок даёт реакцию и не ломает путь", () => WrongOrder(content));
            Check(r, "3. Повторные касания не дублируют ключ, флаги и завершение", () => RepeatedTaps(content));
            Check(r, "4. Переключение режимов не меняет сюжетное состояние", () => ModeSwitching());
            Check(r, "5. Выбор в диалоге меняет ответ и сохраняет флаг", () => ChoiceConsequence(content));
            Check(r, "6. Сохранение после подсказки и после ключа восстанавливается и проходимо", () => SaveRestore(content, tempDirectory));
            Check(r, "7. Завершённый эпизод восстанавливается; «Новая игра» сбрасывает", () => CompletedAndNewGame(content, tempDirectory));
            Check(r, "8. Повреждённое/несовместимое сохранение не ломает загрузку", () => CorruptSaves(tempDirectory));
            Check(r, "9. Повторный диалог после восстановления не оставляет два выбора", () => DialogueReplay(content));
            return r;
        }

        // ---------- сценарии ----------

        private static string ValidateContent(EpisodeContent c)
        {
            if (c == null) return "content == null";
            var missing = new List<string>();
            foreach (var id in EpisodeLogic.AllMessageIds)
            {
                bool hasDefault = false;
                foreach (var m in c.messages)
                    if (m != null && m.id == id && string.IsNullOrEmpty(m.requiresFlag)) hasDefault = true;
                if (!hasDefault) missing.Add(id);
            }
            if (missing.Count > 0) return "нет реплики по умолчанию для: " + string.Join(", ", missing);

            var d = c.FindDialogue(DialogueIds.AfterMemory);
            if (d == null) return "нет диалога " + DialogueIds.AfterMemory;
            if (d.Find(d.start) == null) return "нет стартового узла диалога";
            int choiceNodes = 0;
            foreach (var n in d.nodes)
            {
                if (!string.IsNullOrEmpty(n.next) && d.Find(n.next) == null) return $"узел {n.id}: next={n.next} не найден";
                if (n.choices != null && n.choices.Count > 0)
                {
                    choiceNodes++;
                    foreach (var ch in n.choices)
                        if (d.Find(ch.next) == null) return $"узел {n.id}: выбор ведёт в несуществующий {ch.next}";
                }
                if (string.IsNullOrEmpty(c.SpeakerName(n.speaker)) && n.speaker != "narrator")
                    return $"узел {n.id}: неизвестный говорящий {n.speaker}";
            }
            if (choiceNodes != 1) return "ожидался ровно один узел с выбором, найдено " + choiceNodes;
            if (c.memory == null || c.memory.steps == null || c.memory.steps.Count == 0) return "пустое воспоминание";
            foreach (var st in c.memory.steps)
                if (st == null || string.IsNullOrEmpty(st.icon) || string.IsNullOrEmpty(st.caption) || string.IsNullOrEmpty(st.explain))
                    return "шаг воспоминания без иконки, подписи или объяснения";
            return null;
        }

        private static string MainPath(EpisodeContent c, int choice)
        {
            var logic = new EpisodeLogic(new EpisodeState());
            Expect(logic.TapObject(ObjectIds.Pass), "pass_inspect_first", true);
            logic.ToggleMode(AbilityMode.Memory);
            var mem = logic.TapObject(ObjectIds.Pass);
            Expect(mem, "memory_pass_seen", true, EpisodeEvent.ShowMemory);
            Assert(logic.IsDialoguePending, "диалог должен ожидаться после воспоминания");
            RunDialogue(c, logic.State, choice);
            Assert(!logic.IsDialoguePending, "диалог должен быть завершён");
            logic.ToggleMode(AbilityMode.XRay);
            Assert(logic.Mode == AbilityMode.XRay, "режим X-Ray");
            Expect(logic.TapObject(ObjectIds.Cache), "xray_cache_found", true, EpisodeEvent.CacheRevealed);
            Expect(logic.TapObject(ObjectIds.Cache), "xray_cache_open_hint", false);
            logic.ToggleMode(AbilityMode.XRay); // выключить → обычный
            Assert(logic.Mode == AbilityMode.Normal, "повторное нажатие X-Ray возвращает обычный режим");
            Expect(logic.TapObject(ObjectIds.Cache), "cache_key_taken", true, EpisodeEvent.KeyObtained);
            Assert(logic.State.HasItem(ItemIds.LabKey), "ключ в инвентаре");
            Expect(logic.TapObject(ObjectIds.Door), "door_select_key", false);
            logic.ToggleSelectItem(ItemIds.LabKey);
            Expect(logic.TapObject(ObjectIds.Door), "door_open", true, EpisodeEvent.EpisodeCompleted);
            Assert(logic.State.IsCompleted, "эпизод завершён");
            Assert(logic.State.Stage == EpisodeStage.Completed, "этап Completed");
            return null;
        }

        private static string WrongOrder(EpisodeContent c)
        {
            var logic = new EpisodeLogic(new EpisodeState());
            Expect(logic.TapObject(ObjectIds.Door), "door_locked", false);
            Expect(logic.TapObject(ObjectIds.Cache), "cache_wall_plain", false);
            logic.ToggleMode(AbilityMode.XRay);
            Expect(logic.TapObject(ObjectIds.Cache), "xray_wall_no_hint", false);
            Expect(logic.TapObject(ObjectIds.Door), "xray_door", false);
            logic.ToggleMode(AbilityMode.Memory);
            Expect(logic.TapObject(ObjectIds.Pass), "memory_pass_need_inspect", false);
            Expect(logic.TapObject(ObjectIds.Door), "memory_door", false);
            Expect(logic.TapObject(ObjectIds.Cache), "memory_wall", false);
            Assert(logic.State.Flags.Count == 0, "неверный порядок не должен ставить флаги");
            // Выбор предмета, которого нет, игнорируется
            logic.ToggleSelectItem(ItemIds.LabKey);
            Assert(logic.SelectedItem == null, "нельзя выбрать отсутствующий ключ");
            // Главный путь остаётся доступен
            logic.ToggleMode(AbilityMode.Memory); // → Normal
            Expect(logic.TapObject(ObjectIds.Pass), "pass_inspect_first", true);
            logic.ToggleMode(AbilityMode.Memory);
            Expect(logic.TapObject(ObjectIds.Pass), "memory_pass_seen", true, EpisodeEvent.ShowMemory);
            // Тайник нельзя открыть в обычном режиме, пока он не найден X-Ray
            logic.ToggleMode(AbilityMode.Memory);
            Expect(logic.TapObject(ObjectIds.Cache), "cache_wall_plain", false);
            logic.ToggleMode(AbilityMode.XRay);
            Expect(logic.TapObject(ObjectIds.Cache), "xray_cache_found", true, EpisodeEvent.CacheRevealed);
            return null;
        }

        private static string RepeatedTaps(EpisodeContent c)
        {
            var logic = ReachKey(c, 0);
            for (int i = 0; i < 10; i++)
            {
                var res = logic.TapObject(ObjectIds.Cache);
                Assert(res.Event != EpisodeEvent.KeyObtained, "ключ выдан повторно на касании " + i);
            }
            Assert(logic.State.CountItem(ItemIds.LabKey) == 1, "ровно один ключ");
            logic.ToggleMode(AbilityMode.Memory);
            var replay = logic.TapObject(ObjectIds.Pass);
            Expect(replay, "memory_pass_replay", false, EpisodeEvent.ShowMemory);
            logic.ToggleMode(AbilityMode.Memory);
            logic.ToggleSelectItem(ItemIds.LabKey);
            int completions = 0;
            for (int i = 0; i < 5; i++)
                if (logic.TapObject(ObjectIds.Door).Event == EpisodeEvent.EpisodeCompleted) completions++;
            Assert(completions == 1, "завершение сработало " + completions + " раз");
            Assert(logic.State.CountItem(ItemIds.LabKey) == 0, "ключ израсходован");
            for (int i = 0; i < 5; i++) logic.TapObject(ObjectIds.Cache);
            Assert(logic.State.CountItem(ItemIds.LabKey) == 0, "после завершения ключ не появляется снова");

            // Диалог: лишние «Далее»/выборы игнорируются
            var st = new EpisodeState();
            var d = new DialogueRunner();
            d.Start(c.FindDialogue(DialogueIds.AfterMemory), st);
            while (d.IsActive && !d.HasChoices) d.Advance();
            Assert(!d.Advance(), "Далее не должно работать в узле выбора");
            Assert(d.Choose(0), "выбор 0");
            Assert(!d.Choose(1), "второй выбор после уже сделанного должен игнорироваться");
            Assert(!(st.HasFlag(FlagIds.TrustVriskas) && st.HasFlag(FlagIds.RelyOnSelf)), "оба флага выбора одновременно");
            return null;
        }

        private static string ModeSwitching()
        {
            var logic = new EpisodeLogic(new EpisodeState());
            logic.TapObject(ObjectIds.Pass);
            var before = logic.State.Clone();
            logic.ToggleMode(AbilityMode.Memory);
            Assert(logic.Mode == AbilityMode.Memory, "Memory");
            logic.ToggleMode(AbilityMode.XRay);
            Assert(logic.Mode == AbilityMode.XRay, "Memory → XRay переключает, а не складывает");
            logic.ToggleMode(AbilityMode.XRay);
            Assert(logic.Mode == AbilityMode.Normal, "XRay → Normal");
            for (int i = 0; i < 20; i++) logic.ToggleMode(i % 2 == 0 ? AbilityMode.Memory : AbilityMode.XRay);
            Assert(logic.State.ContentEquals(before), "переключение режимов изменило сюжетное состояние");
            return null;
        }

        private static string ChoiceConsequence(EpisodeContent c)
        {
            var a = ReachKey(c, 0);
            var b = ReachKey(c, 1);
            Assert(a.State.HasFlag(FlagIds.TrustVriskas) && !a.State.HasFlag(FlagIds.RelyOnSelf), "флаг выбора A");
            Assert(b.State.HasFlag(FlagIds.RelyOnSelf) && !b.State.HasFlag(FlagIds.TrustVriskas), "флаг выбора B");

            string ta = c.Text("door_open", a.State), tb = c.Text("door_open", b.State);
            Assert(ta != tb, "финальная реплика у двери должна отличаться для разных выборов");
            string xa = c.Text("xray_cache_found", a.State), xb = c.Text("xray_cache_found", b.State);
            Assert(xa != xb, "реплика Врискаса при находке тайника должна отличаться");

            // Ответ в самом диалоге отличается
            var d = c.FindDialogue(DialogueIds.AfterMemory);
            var r1 = new DialogueRunner(); r1.Start(d, new EpisodeState());
            var r2 = new DialogueRunner(); r2.Start(d, new EpisodeState());
            while (!r1.HasChoices) r1.Advance();
            while (!r2.HasChoices) r2.Advance();
            r1.Choose(0); r2.Choose(1);
            Assert(r1.Current.text != r2.Current.text, "ответ после выбора одинаков");
            return null;
        }

        private static string SaveRestore(EpisodeContent c, string tmp)
        {
            var store = NewStore(tmp, "restore");
            // После подсказки (диалог завершён)
            var logic = new EpisodeLogic(new EpisodeState());
            logic.TapObject(ObjectIds.Pass);
            logic.ToggleMode(AbilityMode.Memory);
            logic.TapObject(ObjectIds.Pass);
            RunDialogue(c, logic.State, 1);
            Assert(store.Save(logic.State), "save hint: " + store.LastError);
            var loaded = store.Load();
            Assert(loaded.Status == LoadStatus.Ok, "статус загрузки " + loaded.Status);
            Assert(loaded.State.ContentEquals(logic.State), "состояние после подсказки не совпало");
            Assert(loaded.State.Stage == EpisodeStage.HintKnown, "этап HintKnown, а не " + loaded.State.Stage);

            var resumed = new EpisodeLogic(loaded.State);
            Assert(resumed.Mode == AbilityMode.Normal, "после загрузки режим обычный");
            resumed.ToggleMode(AbilityMode.XRay);
            Expect(resumed.TapObject(ObjectIds.Cache), "xray_cache_found", true, EpisodeEvent.CacheRevealed);
            Expect(resumed.TapObject(ObjectIds.Cache), "xray_cache_open_hint", false);
            resumed.ToggleMode(AbilityMode.XRay);
            Expect(resumed.TapObject(ObjectIds.Cache), "cache_key_taken", true, EpisodeEvent.KeyObtained);
            Assert(store.Save(resumed.State), "save key");

            var loaded2 = store.Load();
            Assert(loaded2.Status == LoadStatus.Ok, "статус загрузки 2");
            Assert(loaded2.State.HasItem(ItemIds.LabKey) && loaded2.State.CountItem(ItemIds.LabKey) == 1, "ключ восстановлен один раз");
            var resumed2 = new EpisodeLogic(loaded2.State);
            Expect(resumed2.TapObject(ObjectIds.Cache), "cache_empty", false);
            resumed2.ToggleSelectItem(ItemIds.LabKey);
            Expect(resumed2.TapObject(ObjectIds.Door), "door_open", true, EpisodeEvent.EpisodeCompleted);

            // Восстановление при незавершённом диалоге
            var mid = new EpisodeLogic(new EpisodeState());
            mid.TapObject(ObjectIds.Pass);
            mid.ToggleMode(AbilityMode.Memory);
            mid.TapObject(ObjectIds.Pass);
            store.Save(mid.State);
            var l3 = store.Load();
            Assert(new EpisodeLogic(l3.State).IsDialoguePending, "после загрузки диалог должен быть снова доступен");
            return null;
        }

        private static string CompletedAndNewGame(EpisodeContent c, string tmp)
        {
            var store = NewStore(tmp, "complete");
            var logic = ReachKey(c, 0);
            logic.ToggleSelectItem(ItemIds.LabKey);
            logic.TapObject(ObjectIds.Door);
            Assert(store.Save(logic.State), "save");
            var l = store.Load();
            Assert(l.Status == LoadStatus.Ok && l.State.IsCompleted, "завершение восстановлено");
            Assert(l.State.CountItem(ItemIds.LabKey) == 0, "использованный ключ не возвращается после загрузки");
            Expect(new EpisodeLogic(l.State).TapObject(ObjectIds.Door), "door_already_open", false);

            // Новая игра
            store.DeleteAll();
            logic.Reset();
            Assert(store.Load().Status == LoadStatus.NoSave, "после сброса сохранения нет");
            Assert(logic.State.Flags.Count == 0 && logic.State.Items.Count == 0, "состояние сброшено");
            Assert(logic.Mode == AbilityMode.Normal && logic.SelectedItem == null, "режим и выбор сброшены");
            Assert(store.Save(logic.State), "save new");
            Assert(store.Load().State.Stage == EpisodeStage.Start, "новая игра начинается с начала");
            return null;
        }

        private static string CorruptSaves(string tmp)
        {
            var store = NewStore(tmp, "corrupt");
            File.WriteAllText(store.MainPath, "мусор\0\x01 не сохранение");
            Assert(store.Load().Status == LoadStatus.Corrupt, "мусор → Corrupt");

            File.WriteAllText(store.MainPath, "");
            Assert(store.Load().Status == LoadStatus.Corrupt, "пустой файл → Corrupt");

            var st = new EpisodeState(); st.SetFlag(FlagIds.PassInspected);
            string good = SaveCodec.Encode(st);
            File.WriteAllText(store.MainPath, good.Substring(0, good.Length / 2));
            Assert(store.Load().Status == LoadStatus.Corrupt, "обрезанный файл → Corrupt");

            File.WriteAllText(store.MainPath, good.Replace("pass_inspected", "pass_inspectex"));
            Assert(store.Load().Status == LoadStatus.Corrupt, "изменённый файл (контрольная сумма) → Corrupt");

            string future = SaveCodec.Header + "\nversion=99\nitems=\nflags=\n";
            future += "checksum=" + SaveCodec.Checksum(future) + "\n";
            File.WriteAllText(store.MainPath, future);
            Assert(store.Load().Status == LoadStatus.Incompatible, "будущая версия → Incompatible");

            // Резервная копия
            store.DeleteAll();
            Assert(store.Save(st), "save 1");
            var st2 = st.Clone(); st2.SetFlag(FlagIds.MemorySeen);
            Assert(store.Save(st2), "save 2");
            Assert(File.Exists(store.BackupPath), "есть .bak");
            File.WriteAllText(store.MainPath, "broken");
            var l = store.Load();
            Assert(l.Status == LoadStatus.RestoredFromBackup && l.State.HasFlag(FlagIds.PassInspected), "восстановление из .bak");

            // Прерванная запись: основной удалён, остался .tmp
            store.DeleteAll();
            File.WriteAllText(store.TempPath, SaveCodec.Encode(st2));
            var lt = store.Load();
            Assert(lt.Status == LoadStatus.RestoredFromBackup && lt.State.HasFlag(FlagIds.MemorySeen), "восстановление из .tmp");

            // CRLF (файл отредактирован в Windows) — читается
            store.DeleteAll();
            File.WriteAllText(store.MainPath, SaveCodec.Encode(st2).Replace("\n", "\r\n"));
            Assert(store.Load().Status == LoadStatus.Ok, "CRLF читается");
            store.DeleteAll();
            return null;
        }

        private static string DialogueReplay(EpisodeContent c)
        {
            var st = new EpisodeState();
            var d = c.FindDialogue(DialogueIds.AfterMemory);
            var r = new DialogueRunner();
            r.Start(d, st);
            while (!r.HasChoices) r.Advance();
            r.Choose(0); // выбрал, но приложение закрыли до конца диалога
            st.ClearFlag(FlagIds.DialogueDone);
            r.Start(d, st);
            while (!r.HasChoices) r.Advance();
            r.Choose(1);
            while (r.IsActive) r.Advance();
            Assert(st.HasFlag(FlagIds.RelyOnSelf) && !st.HasFlag(FlagIds.TrustVriskas), "должен остаться только последний выбор");
            Assert(st.HasFlag(FlagIds.DialogueDone), "диалог завершён");
            return null;
        }

        // ---------- помощники ----------

        private static EpisodeLogic ReachKey(EpisodeContent c, int choice)
        {
            var logic = new EpisodeLogic(new EpisodeState());
            logic.TapObject(ObjectIds.Pass);
            logic.ToggleMode(AbilityMode.Memory);
            logic.TapObject(ObjectIds.Pass);
            RunDialogue(c, logic.State, choice);
            logic.ToggleMode(AbilityMode.XRay);
            logic.TapObject(ObjectIds.Cache);
            logic.ToggleMode(AbilityMode.XRay);
            logic.TapObject(ObjectIds.Cache);
            Assert(logic.State.HasItem(ItemIds.LabKey), "ReachKey: ключ не получен");
            return logic;
        }

        private static void RunDialogue(EpisodeContent c, EpisodeState state, int choice)
        {
            var r = new DialogueRunner();
            Assert(r.Start(c.FindDialogue(DialogueIds.AfterMemory), state), "диалог не стартовал");
            int guard = 0;
            while (r.IsActive && guard++ < 50)
            {
                if (r.HasChoices) r.Choose(choice);
                else r.Advance();
            }
            Assert(!r.IsActive, "диалог зациклился");
        }

        private static SaveStore NewStore(string tmp, string sub)
        {
            string dir = Path.Combine(tmp, "biskas_selftest_" + sub);
            Directory.CreateDirectory(dir);
            var s = new SaveStore(dir);
            s.DeleteAll();
            return s;
        }

        private static void Expect(ActionResult r, string msg, bool changed, EpisodeEvent evt = EpisodeEvent.None)
        {
            if (r.MessageId != msg || r.StateChanged != changed || r.Event != evt)
                throw new CheckFailed($"ожидалось ({msg}, changed={changed}, {evt}), получено ({r})");
        }

        private static void Assert(bool cond, string what)
        {
            if (!cond) throw new CheckFailed(what);
        }

        private sealed class CheckFailed : Exception
        {
            public CheckFailed(string m) : base(m) { }
        }

        private static void Check(Report r, string name, Func<string> body)
        {
            try
            {
                string err = body();
                if (err == null) r.Passed.Add(name);
                else r.Failed.Add(name + " — " + err);
            }
            catch (CheckFailed e) { r.Failed.Add(name + " — " + e.Message); }
            catch (Exception e) { r.Failed.Add(name + " — исключение " + e.GetType().Name + ": " + e.Message); }
        }
    }
}
