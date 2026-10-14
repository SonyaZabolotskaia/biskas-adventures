using System;
using System.Collections.Generic;
using System.IO;
using Biskas.Core;
using Biskas.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Biskas.EditorTools
{
    /// <summary>
    /// Самопроверка: сценарии логики/сохранений (EpisodeSelfTest) на реальном JSON через JsonUtility
    /// и проверка, что в сцене все ссылки контроллера и представлений назначены.
    /// Не заменяет прохождение в Play Mode и проверку на телефоне.
    /// </summary>
    public static class SelfTestRunner
    {
        [MenuItem("Biskas/Самопроверка логики и сцены", priority = 10)]
        public static void RunMenu()
        {
            bool ok = Run(checkSceneByOpening: false, out string report);
            if (ok) Debug.Log(report); else Debug.LogError(report);
            EditorUtility.DisplayDialog("Самопроверка Biskas", ok ? "Все проверки пройдены. Подробности — в Console." :
                                        "Есть ошибки. Подробности — в Console.", "OK");
        }

        /// <summary>
        /// Unity.exe -batchmode -quit -projectPath . -executeMethod Biskas.EditorTools.SelfTestRunner.RunFromCommandLine -logFile Logs/selftest.log
        /// Код возврата 0 — успех.
        /// </summary>
        public static void RunFromCommandLine()
        {
            int code = 1;
            try
            {
                bool ok = Run(checkSceneByOpening: true, out string report);
                Debug.Log(report);
                code = ok ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[Biskas] Самопроверка упала: " + e);
            }
            EditorApplication.Exit(code);
        }

        public static bool Run(bool checkSceneByOpening, out string report)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ProjectSetup.ContentPath);
            if (asset == null)
            {
                report = "[FAIL] Не найден " + ProjectSetup.ContentPath;
                return false;
            }
            var content = JsonUtility.FromJson<EpisodeContent>(asset.text);
            string tmp = Path.Combine(Application.temporaryCachePath, "biskas_selftest");
            Directory.CreateDirectory(tmp);
            var r = EpisodeSelfTest.Run(content, tmp);

            var sceneLines = new List<string>();
            bool sceneOk = CheckScene(checkSceneByOpening, sceneLines);
            report = "[Biskas] Самопроверка (Unity " + Application.unityVersion + ")\n" + r + "\n" + string.Join("\n", sceneLines);
            return r.Ok && sceneOk;
        }

        private static bool CheckScene(bool openScene, List<string> lines)
        {
            if (!File.Exists(ProjectSetup.ScenePath))
            {
                lines.Add("[FAIL] Сцена не создана: " + ProjectSetup.ScenePath + " (Biskas → Создать или открыть сцену эпизода)");
                return false;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ProjectSetup.ScenePath)
            {
                if (!openScene)
                {
                    lines.Add("[SKIP] Проверка ссылок сцены: откройте " + ProjectSetup.ScenePath + " и повторите.");
                    return true;
                }
                EditorSceneManager.OpenScene(ProjectSetup.ScenePath, OpenSceneMode.Single);
            }

            bool ok = true;
            var behaviours = new List<MonoBehaviour>();
            behaviours.AddRange(UnityEngine.Object.FindObjectsByType<EpisodeController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            behaviours.AddRange(UnityEngine.Object.FindObjectsByType<OverlayPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            behaviours.AddRange(UnityEngine.Object.FindObjectsByType<RoomHotspot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            behaviours.AddRange(UnityEngine.Object.FindObjectsByType<InventoryView>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            behaviours.AddRange(UnityEngine.Object.FindObjectsByType<CharacterAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            behaviours.AddRange(UnityEngine.Object.FindObjectsByType<RewardAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            behaviours.AddRange(UnityEngine.Object.FindObjectsByType<InteractionFx>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            if (behaviours.Count < 1 + 4 + 3 + 1 + 5 + 2)
            {
                lines.Add("[FAIL] В сцене не хватает компонентов эпизода: найдено " + behaviours.Count);
                ok = false;
            }

            foreach (var b in behaviours)
            {
                var so = new SerializedObject(b);
                var it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null
                        && it.name != "m_Script")
                    {
                        lines.Add($"[FAIL] {b.GetType().Name} на «{b.name}»: не назначено поле {it.propertyPath}");
                        ok = false;
                    }
                }
            }

            var ids = new HashSet<string>();
            foreach (var h in UnityEngine.Object.FindObjectsByType<RoomHotspot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ids.Add(h.ObjectId);
            foreach (var need in new[] { ObjectIds.Pass, ObjectIds.Cache, ObjectIds.Door })
                if (!ids.Contains(need)) { lines.Add("[FAIL] Нет предмета " + need); ok = false; }

            var memory = UnityEngine.Object.FindFirstObjectByType<MemoryView>(FindObjectsInactive.Include);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ProjectSetup.ContentPath);
            if (memory != null && asset != null)
            {
                var missing = memory.MissingIcons(JsonUtility.FromJson<EpisodeContent>(asset.text).memory);
                if (missing.Count > 0) { lines.Add("[FAIL] Нет спрайтов иконок воспоминания: " + string.Join(", ", missing)); ok = false; }
            }

            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                lines.Add("[FAIL] Нет EventSystem");
                ok = false;
            }
            lines.Add(ok ? "[OK]   Сцена: ссылки назначены, предметы и EventSystem на месте" : "[FAIL] Сцена содержит ошибки");
            return ok;
        }
    }
}
