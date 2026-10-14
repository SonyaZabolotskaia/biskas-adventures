using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Biskas.EditorTools
{
    /// <summary>Настройки проекта для прототипа: идентификатор, горизонтальная ориентация, сцена сборки.</summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/_Game/Scenes/Lab_Episode1.unity";
        public const string ContentPath = "Assets/_Game/Data/episode1_ru.json";
        public const string AndroidPackageId = "com.biskasfan.echoofmemory";
        public const string Version = "0.1.0";
        public const int AndroidVersionCode = 1;

        [MenuItem("Biskas/Применить настройки проекта", priority = 20)]
        public static void ApplyMenu()
        {
            Apply();
            Debug.Log("[Biskas] Настройки проекта применены.");
        }

        public static void Apply()
        {
            PlayerSettings.companyName = "BiskasFan";
            PlayerSettings.productName = "Бискас: Эхо памяти";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageId);
            PlayerSettings.Android.bundleVersionCode = AndroidVersionCode;

            // Только горизонтальная ориентация (поворот между двумя landscape разрешён).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.runInBackground = false;

            // Android: IL2CPP + ARM64 (современные телефоны). Требуется модуль Android Build Support.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            EnsureLegacyInputEnabled();
            EnsureSceneInBuild();
            AssetDatabase.SaveAssets();
        }

        public static void EnsureSceneInBuild()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// UI прототипа использует StandaloneInputModule (касания и мышь через один путь событий).
        /// Пакет Input System не подключён, поэтому старый Input Manager должен быть включён.
        /// 0 = Input Manager, 1 = Input System, 2 = оба.
        /// </summary>
        private static void EnsureLegacyInputEnabled()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue != 1) return;
            prop.intValue = 2;
            so.ApplyModifiedProperties();
            Debug.LogWarning("[Biskas] Включён старый Input Manager (Active Input Handling = Both). Перезапустите редактор Unity.");
        }
    }
}
