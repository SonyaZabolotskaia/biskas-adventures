using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Biskas.EditorTools
{
    /// <summary>
    /// Сборка development APK для локальной проверки на телефоне. Не release, не AAB, без подписи магазина.
    /// </summary>
    public static class AndroidBuild
    {
        public const string OutputPath = "Builds/Android/BiskasEcho-dev.apk";

        [MenuItem("Biskas/Собрать Android APK (development)", priority = 30)]
        public static void BuildMenu()
        {
            var result = Build(out string message);
            if (result) EditorUtility.RevealInFinder(OutputPath);
            EditorUtility.DisplayDialog("Android-сборка", message, "OK");
        }

        /// <summary>
        /// Unity.exe -batchmode -quit -projectPath . -buildTarget Android
        ///   -executeMethod Biskas.EditorTools.AndroidBuild.BuildFromCommandLine -logFile Logs/build-android.log
        /// Код возврата: 0 — APK собран и файл существует; 2 — нет Android Build Support; 1 — ошибка сборки.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            int code;
            try
            {
                if (!IsAndroidSupportInstalled())
                {
                    Debug.LogError("[Biskas] " + MissingModuleMessage);
                    code = 2;
                }
                else
                {
                    bool ok = Build(out string message);
                    if (ok) Debug.Log("[Biskas] " + message); else Debug.LogError("[Biskas] " + message);
                    code = ok ? 0 : 1;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Biskas] Сборка упала: " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        private const string MissingModuleMessage =
            "Модуль Android Build Support не установлен для этой версии Unity. " +
            "Unity Hub → Installs → 6000.3.10f1 → Add modules → Android Build Support (+ OpenJDK, Android SDK & NDK Tools).";

        public static bool IsAndroidSupportInstalled() =>
            BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);

        public static bool Build(out string message)
        {
            if (!IsAndroidSupportInstalled())
            {
                message = MissingModuleMessage;
                return false;
            }
            if (!File.Exists(ProjectSetup.ScenePath))
            {
                message = "Нет сцены " + ProjectSetup.ScenePath + ". Сначала: Biskas → Создать или открыть сцену эпизода.";
                return false;
            }

            ProjectSetup.Apply();
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            if (File.Exists(OutputPath)) File.Delete(OutputPath);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            bool ok = s.result == BuildResult.Succeeded && File.Exists(OutputPath);
            message = ok
                ? $"APK собран: {Path.GetFullPath(OutputPath)} ({s.totalSize / (1024 * 1024)} МБ, {s.totalTime.TotalSeconds:F0} с)"
                : $"Сборка не удалась: result={s.result}, ошибок={s.totalErrors}. Подробности — в Console/журнале.";
            return ok;
        }
    }
}
