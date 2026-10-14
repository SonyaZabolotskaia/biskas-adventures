using System;
using System.IO;
using Biskas.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Biskas.EditorTools
{
    /// <summary>
    /// Воспроизводимое создание тестовой сцены лаборатории из UI-заглушек.
    /// «Создать или открыть» не трогает существующую сцену (ручные правки сохраняются);
    /// перезапись — только через явную команду «Пересоздать».
    /// </summary>
    public static class SceneBuilder
    {
        private const float RefW = 1920f, RefH = 1080f;
        private static Font _font;

        // ---------------- команды ----------------

        [MenuItem("Biskas/Создать или открыть сцену эпизода", priority = 0)]
        public static void CreateOrOpenMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateOrOpen(false);
        }

        [MenuItem("Biskas/Пересоздать сцену эпизода (перезапишет ручные правки)", priority = 1)]
        public static void RecreateMenu()
        {
            if (File.Exists(ProjectSetup.ScenePath) &&
                !EditorUtility.DisplayDialog("Пересоздать сцену?",
                    "Сцена " + ProjectSetup.ScenePath + " будет перезаписана. Ручные правки в ней пропадут.",
                    "Пересоздать", "Отмена"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateOrOpen(true);
        }

        /// <summary>
        /// Командная строка:
        /// Unity.exe -batchmode -quit -projectPath . -executeMethod Biskas.EditorTools.SceneBuilder.CreateSceneFromCommandLine [-biskasRecreate]
        /// </summary>
        public static void CreateSceneFromCommandLine()
        {
            bool recreate = Array.IndexOf(Environment.GetCommandLineArgs(), "-biskasRecreate") >= 0;
            try
            {
                CreateOrOpen(recreate);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[Biskas] Ошибка создания сцены: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void CreateOrOpen(bool recreate)
        {
            ProjectSetup.Apply();
            if (File.Exists(ProjectSetup.ScenePath) && !recreate)
            {
                EditorSceneManager.OpenScene(ProjectSetup.ScenePath, OpenSceneMode.Single);
                Debug.Log("[Biskas] Сцена уже существует, открыта без изменений: " + ProjectSetup.ScenePath +
                          ". Для перезаписи используйте «Пересоздать сцену эпизода».");
                return;
            }
            Build();
            ProjectSetup.EnsureSceneInBuild();
            Debug.Log("[Biskas] Сцена создана: " + ProjectSetup.ScenePath);
        }

        // ---------------- построение ----------------

        private static void Build()
        {
            var content = AssetDatabase.LoadAssetAtPath<TextAsset>(ProjectSetup.ContentPath);
            if (content == null) throw new InvalidOperationException("Не найден " + ProjectSetup.ContentPath);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Directory.CreateDirectory(Path.GetDirectoryName(ProjectSetup.ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Камера (Canvas в режиме Overlay, камера нужна для фона и чтобы не было «No cameras rendering»).
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefW, RefH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // по высоте: на вытянутых экранах кнопки не мельчают
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            // --- Фон (на весь экран, под вырезом тоже; без искажений — EnvelopeParent 16:9) ---
            var bg = Panel(root, "Background", new Color(0.09f, 0.11f, 0.15f), false);
            Stretch(bg);
            var bgArt = SpriteImage(bg, "Art", "Room/laboratory_background", false);
            var bgFit = bgArt.gameObject.AddComponent<AspectRatioFitter>();
            bgFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bgFit.aspectRatio = 16f / 9f;

            // --- Комната (в безопасной области) ---
            var safeRoom = SafeArea(root, "SafeArea_Room");
            var room = Rect(safeRoom, "Room");
            Anchor(room, 0, 0.19f, 1, 0.86f);

            var table = SpriteImage(room, "Decor_Table", "Room/table", true);
            AnchorBox(table.rectTransform, 0.2f, 0.13f, 480, 203);
            var vent = SpriteImage(room, "Decor_Vent", "Room/vent", true);
            AnchorBox(vent.rectTransform, 0.55f, 0.93f, 270, 78);

            // Персонажи позади предметов, касания не перехватывают
            var roomBiskas = CharacterBuilder.CreateInRoom(room, CharacterBuilder.Who.Biskas, new Vector2(0.075f, 0.27f), 270);
            var roomVriskas = CharacterBuilder.CreateInRoom(room, CharacterBuilder.Who.Vriskas, new Vector2(0.37f, 0.27f), 280);

            var hsPass = Hotspot(room, "Hotspot_Pass", Biskas.Core.ObjectIds.Pass, "Повреждённый пропуск",
                                 0.2f, 0.32f, new Vector2(340, 260), new Vector2(260, 167),
                                 ("default", "Room/pass_damaged"));
            var hsCache = Hotspot(room, "Hotspot_Cache", Biskas.Core.ObjectIds.Cache, "Панель стены",
                                  0.55f, 0.6f, new Vector2(330, 300), new Vector2(260, 228),
                                  ("hidden", "Room/wall_panel"), ("xray", "Room/cache_xray"),
                                  ("closed", "Room/cache_closed"), ("open", "Room/cache_open"));
            var hsDoor = Hotspot(room, "Hotspot_Door", Biskas.Core.ObjectIds.Door, "Дверь сектора",
                                 0.86f, 0.47f, new Vector2(320, 640), new Vector2(270, 553),
                                 ("closed", "Room/door_closed"), ("open", "Room/door_open"));

            // Створка тайника для анимации открытия (петля слева: x = 40/400 холста)
            var cacheVisual = hsCache.VisualRect;
            var flap = SpriteImage(cacheVisual, "Flap", "Room/cache_closed", true);
            Stretch(flap.rectTransform);
            flap.rectTransform.pivot = new Vector2(0.1f, 0.5f);
            flap.rectTransform.offsetMin = flap.rectTransform.offsetMax = Vector2.zero;
            flap.gameObject.SetActive(false);

            // --- Тонировка режимов (не перехватывает касания) ---
            var memoryTint = Panel(root, "Tint_Memory", new Color(0.6f, 0.42f, 0.18f, 0.28f), false);
            Stretch(memoryTint);
            memoryTint.gameObject.SetActive(false);
            var xrayTint = Panel(root, "Tint_XRay", new Color(0.1f, 0.8f, 0.35f, 0.18f), false);
            Stretch(xrayTint);
            xrayTint.gameObject.SetActive(false);

            // --- HUD (в безопасной области) ---
            var safeHud = SafeArea(root, "SafeArea_HUD");
            var top = Rect(safeHud, "TopBar");
            Anchor(top, 0, 0.87f, 1, 1);
            var menuBtn = Button(top, "Button_Menu", "Меню", new Color(0.2f, 0.24f, 0.3f, 0.95f), out var menuLabel);
            AnchorPixels(menuBtn, 0, 0.5f, 24, 0, 220, 120);
            var memBtn = Button(top, "Button_Memory", "Память Бискаса", new Color(0.2f, 0.24f, 0.3f, 0.95f), out var memLabel);
            AnchorPixels(memBtn, 1, 0.5f, -24 - 380 - 24, 0, 380, 120);
            var xrayBtn = Button(top, "Button_XRay", "Зрение Врискаса", new Color(0.2f, 0.24f, 0.3f, 0.95f), out var xrayLabel);
            AnchorPixels(xrayBtn, 1, 0.5f, -24, 0, 380, 120);

            var bottom = Rect(safeHud, "BottomBar");
            Anchor(bottom, 0, 0, 1, 0.19f);
            var invPanel = Panel(bottom, "Inventory", new Color(0.08f, 0.09f, 0.12f, 0.85f), true);
            invPanel.anchorMin = new Vector2(0, 0); invPanel.anchorMax = new Vector2(0, 1);
            invPanel.pivot = new Vector2(0, 0.5f);
            invPanel.sizeDelta = new Vector2(560, -16);
            invPanel.anchoredPosition = new Vector2(16, 0);
            var invTitle = Label(invPanel, "Title", "Инвентарь", 26, TextAnchor.UpperLeft, new Color(1, 1, 1, 0.7f));
            Anchor(invTitle.rectTransform, 0.03f, 0.78f, 0.97f, 0.98f);
            var inventory = invPanel.gameObject.AddComponent<InventoryView>();
            var slotButtons = new Button[3];
            var slotImages = new Image[3];
            var slotLabels = new Text[3];
            var slotIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Button(invPanel, "Slot_" + (i + 1), "", new Color(0.16f, 0.18f, 0.22f, 0.9f), out var l);
                b.anchorMin = b.anchorMax = new Vector2(0, 0);
                b.pivot = new Vector2(0, 0);
                b.sizeDelta = new Vector2(160, 140);
                b.anchoredPosition = new Vector2(24 + i * 176, 14);
                l.color = Color.black;
                l.fontSize = 24;
                Anchor(l.rectTransform, 0.05f, 0.02f, 0.95f, 0.3f);
                var icon = SpriteImage(b, "Icon", "Room/key", true);
                Anchor(icon.rectTransform, 0.12f, 0.26f, 0.88f, 0.96f);
                icon.enabled = false;
                slotIcons[i] = icon;
                slotButtons[i] = b.GetComponent<Button>();
                slotImages[i] = b.GetComponent<Image>();
                slotLabels[i] = l;
            }

            var msgPanel = Panel(bottom, "MessageBox", new Color(0.08f, 0.09f, 0.12f, 0.85f), false);
            msgPanel.anchorMin = new Vector2(0, 0); msgPanel.anchorMax = new Vector2(1, 1);
            msgPanel.offsetMin = new Vector2(16 + 560 + 16, 8);
            msgPanel.offsetMax = new Vector2(-16, -8);
            var msgSpeaker = Label(msgPanel, "Speaker", "", 28, TextAnchor.UpperLeft, new Color(1f, 0.82f, 0.45f));
            msgSpeaker.fontStyle = FontStyle.Bold;
            Anchor(msgSpeaker.rectTransform, 0.02f, 0.72f, 0.98f, 0.97f);
            var msgText = Label(msgPanel, "Text", "", 30, TextAnchor.UpperLeft, Color.white);
            Anchor(msgText.rectTransform, 0.02f, 0.04f, 0.98f, 0.74f);

            // --- Слой эффектов (полёт ключа) — поверх HUD, под оверлеями, не перехватывает касания ---
            var fxLayer = Rect(root, "FX_Layer"); Stretch(fxLayer);
            var flyingKey = SpriteImage(fxLayer, "FlyingKey", "Room/key", true);
            AnchorBox(flyingKey.rectTransform, 0.5f, 0.5f, 150, 150);
            flyingKey.gameObject.SetActive(false);
            var sparkle = SpriteImage(fxLayer, "Sparkle", "FX/sparkle", true);
            AnchorBox(sparkle.rectTransform, 0.5f, 0.5f, 220, 220);
            sparkle.gameObject.SetActive(false);
            var puff = SpriteImage(fxLayer, "Puff", "FX/dust_puff", true);
            AnchorBox(puff.rectTransform, 0.5f, 0.5f, 230, 200);
            puff.gameObject.SetActive(false);
            var question = SpriteImage(fxLayer, "Question", "FX/question_bubble", true);
            AnchorBox(question.rectTransform, 0.5f, 0.5f, 130, 130);
            var qMark = Label(question.rectTransform, "Mark", "?", 84, TextAnchor.MiddleCenter, new Color(0.17f, 0.1f, 0.06f));
            qMark.fontStyle = FontStyle.Bold;
            Anchor(qMark.rectTransform, 0f, 0.12f, 1f, 1f);
            question.gameObject.SetActive(false);
            var revealGlow = SpriteImage(fxLayer, "RevealGlow", "FX/glow_soft", false);
            AnchorBox(revealGlow.rectTransform, 0.5f, 0.5f, 380, 380);
            revealGlow.gameObject.SetActive(false);
            var revealSparkle = SpriteImage(fxLayer, "RevealSparkle", "FX/sparkle", true);
            AnchorBox(revealSparkle.rectTransform, 0.5f, 0.5f, 240, 240);
            revealSparkle.gameObject.SetActive(false);
            var interactionFx = fxLayer.gameObject.AddComponent<InteractionFx>();
            var iso = new SerializedObject(interactionFx);
            Set(iso, "puff", puff.rectTransform);
            Set(iso, "question", question.rectTransform);
            Set(iso, "revealGlow", revealGlow.rectTransform);
            Set(iso, "revealSparkle", revealSparkle.rectTransform);
            iso.ApplyModifiedPropertiesWithoutUndo();

            var reward = fxLayer.gameObject.AddComponent<RewardAnimator>();
            var rso = new SerializedObject(reward);
            Set(rso, "cacheFlap", flap.rectTransform);
            Set(rso, "cacheCenter", cacheVisual);
            Set(rso, "flyingKey", flyingKey.rectTransform);
            Set(rso, "sparkle", sparkle.rectTransform);
            rso.ApplyModifiedPropertiesWithoutUndo();

            // --- Оверлеи ---
            var memoryView = BuildMemoryView(root, out var memoryRefs);
            var dialogueView = BuildDialogueView(root, out var dlgRefs);
            var endView = BuildEndView(root, out var endRefs);
            var menuView = BuildMenuView(root, out var menuRefs);

            // --- Контроллер ---
            var controllerGo = new GameObject("EpisodeController");
            var controller = controllerGo.AddComponent<EpisodeController>();
            var so = new SerializedObject(controller);
            Set(so, "contentJson", content);
            SetArray(so, "hotspots", new UnityEngine.Object[] { hsPass, hsCache, hsDoor });
            Set(so, "memoryTint", memoryTint.gameObject);
            Set(so, "xrayTint", xrayTint.gameObject);
            Set(so, "roomVriskas", roomVriskas);
            Set(so, "roomBiskas", roomBiskas);
            Set(so, "rewardAnimator", reward);
            Set(so, "interactionFx", interactionFx);
            Set(so, "memoryButton", memBtn.GetComponent<Button>());
            Set(so, "memoryButtonImage", memBtn.GetComponent<Image>());
            Set(so, "memoryButtonLabel", memLabel);
            Set(so, "xrayButton", xrayBtn.GetComponent<Button>());
            Set(so, "xrayButtonImage", xrayBtn.GetComponent<Image>());
            Set(so, "xrayButtonLabel", xrayLabel);
            Set(so, "menuButton", menuBtn.GetComponent<Button>());
            Set(so, "menuButtonLabel", menuLabel);
            Set(so, "inventoryTitle", invTitle);
            Set(so, "inventory", inventory);
            Set(so, "messageSpeaker", msgSpeaker);
            Set(so, "messageText", msgText);
            Set(so, "memoryView", memoryView);
            Set(so, "dialogueView", dialogueView);
            Set(so, "menuView", menuView);
            Set(so, "endView", endView);
            so.ApplyModifiedPropertiesWithoutUndo();

            var inv = new SerializedObject(inventory);
            var slotsProp = inv.FindProperty("slots");
            slotsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var e = slotsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("button").objectReferenceValue = slotButtons[i];
                e.FindPropertyRelative("background").objectReferenceValue = slotImages[i];
                e.FindPropertyRelative("label").objectReferenceValue = slotLabels[i];
                e.FindPropertyRelative("icon").objectReferenceValue = slotIcons[i];
            }
            var itemSprites = inv.FindProperty("itemSprites");
            itemSprites.arraySize = 1;
            itemSprites.GetArrayElementAtIndex(0).FindPropertyRelative("itemId").stringValue = Biskas.Core.ItemIds.LabKey;
            itemSprites.GetArrayElementAtIndex(0).FindPropertyRelative("sprite").objectReferenceValue = Spr("Room/key");
            inv.ApplyModifiedPropertiesWithoutUndo();

            ApplyRefs(memoryView, memoryRefs);
            ApplyRefs(dialogueView, dlgRefs);
            ApplyRefs(endView, endRefs);
            ApplyRefs(menuView, menuRefs);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ProjectSetup.ScenePath))
                throw new IOException("Не удалось сохранить сцену " + ProjectSetup.ScenePath);
            AssetDatabase.Refresh();
        }

        // ---------------- оверлеи ----------------

        private static MemoryView BuildMemoryView(Transform parent, out (string, UnityEngine.Object)[] refs)
        {
            var host = Rect(parent, "MemoryView"); Stretch(host);
            var view = host.gameObject.AddComponent<MemoryView>();
            var panel = Panel(host, "Panel", new Color(0.16f, 0.1f, 0.05f, 0.9f), true); Stretch(panel);
            var safe = SafeArea(panel, "SafeArea");

            var title = Label(safe, "Title", "Воспоминание", 36, TextAnchor.UpperLeft, new Color(1f, 0.85f, 0.55f));
            title.fontStyle = FontStyle.Bold;
            Anchor(title.rectTransform, 0.02f, 0.9f, 0.3f, 0.98f);

            // Бискас слева внизу, над ним — пузырьки мысли к облаку
            var narratorBox = Rect(safe, "Narrator");
            Anchor(narratorBox, 0.01f, 0.02f, 0.25f, 0.52f);
            var narrator = CharacterBuilder.CreateFitted(narratorBox, "Biskas_Memory", CharacterBuilder.Who.Biskas, false);

            var b1 = SpriteImage(safe, "Bubble_1", "Memory/memory_bubble", true);
            AnchorBox(b1.rectTransform, 0.17f, 0.53f, 40, 40);
            var b2 = SpriteImage(safe, "Bubble_2", "Memory/memory_bubble", true);
            AnchorBox(b2.rectTransform, 0.215f, 0.59f, 62, 62);
            var b3 = SpriteImage(safe, "Bubble_3", "Memory/memory_bubble", true);
            AnchorBox(b3.rectTransform, 0.27f, 0.655f, 88, 88);

            var cloud = SpriteImage(safe, "Cloud", "Memory/memory_cloud", true);
            AnchorBox(cloud.rectTransform, 0.6f, 0.655f, 1000, 633);
            cloud.rectTransform.pivot = new Vector2(0.15f, 0.2f); // растёт от пузырьков
            cloud.rectTransform.anchoredPosition = new Vector2((0.15f - 0.5f) * 1000, (0.2f - 0.5f) * 633);
            var icon = SpriteImage(cloud.rectTransform, "Icon", "Memory/icon_alarm", true);
            Anchor(icon.rectTransform, 0.1f, 0.2f, 0.42f, 0.8f);
            var caption = Label(cloud.rectTransform, "Caption", "", 38, TextAnchor.MiddleLeft, new Color(0.3f, 0.2f, 0.1f));
            caption.fontStyle = FontStyle.Italic;
            Anchor(caption.rectTransform, 0.45f, 0.22f, 0.9f, 0.78f);

            // Объяснение Бискаса
            var box = Panel(safe, "ExplainBox", new Color(0.08f, 0.06f, 0.04f, 0.92f), false);
            Anchor(box, 0.26f, 0.03f, 0.98f, 0.33f);
            var speaker = Label(box, "Speaker", "Бискас", 32, TextAnchor.UpperLeft, new Color(1f, 0.7f, 0.35f));
            speaker.fontStyle = FontStyle.Bold;
            Anchor(speaker.rectTransform, 0.03f, 0.74f, 0.7f, 0.96f);
            var explain = Label(box, "Text", "", 34, TextAnchor.UpperLeft, Color.white);
            Anchor(explain.rectTransform, 0.03f, 0.08f, 1f, 0.74f);
            explain.rectTransform.offsetMax = new Vector2(-350f, 0f); // не заходить под кнопку «Далее»
            var next = Button(box, "Button_Next", "Далее", new Color(0.55f, 0.38f, 0.16f), out var nextLabel);
            AnchorPixels(next, 1f, 0.5f, -24, 0, 300, 120);

            panel.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            SetArray(so, "bubbles", new UnityEngine.Object[] { b1.rectTransform, b2.rectTransform, b3.rectTransform });
            SetArray(so, "iconSprites", new UnityEngine.Object[]
            {
                Spr("Memory/icon_alarm"), Spr("Memory/icon_hand_key"), Spr("Memory/icon_pass_fall"), Spr("Memory/icon_steps")
            });
            so.ApplyModifiedPropertiesWithoutUndo();

            refs = new (string, UnityEngine.Object)[]
            {
                ("root", panel.gameObject), ("cloud", cloud.rectTransform), ("icon", icon), ("caption", caption),
                ("title", title), ("narrator", narrator), ("speakerName", speaker), ("explainText", explain),
                ("nextButton", next.GetComponent<Button>()), ("nextLabel", nextLabel)
            };
            return view;
        }

        private static DialogueView BuildDialogueView(Transform parent, out (string, UnityEngine.Object)[] refs)
        {
            var host = Rect(parent, "DialogueView"); Stretch(host);
            var view = host.gameObject.AddComponent<DialogueView>();
            var panel = Panel(host, "Panel", new Color(0f, 0f, 0f, 0.78f), true); Stretch(panel);
            var safe = SafeArea(panel, "SafeArea");

            var left = Panel(safe, "Portrait_Biskas", new Color(0.3f, 0.2f, 0.12f), false);
            Anchor(left, 0.02f, 0.3f, 0.2f, 0.95f);
            var leftBox = Rect(left, "Character"); Anchor(leftBox, 0f, 0.12f, 1f, 1f);
            var leftCharacter = CharacterBuilder.CreateFitted(leftBox, "Biskas_Portrait", CharacterBuilder.Who.Biskas, false);
            var leftLabel = Label(left, "Label", "Бискас", 30, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.7f));
            Anchor(leftLabel.rectTransform, 0f, 0f, 1f, 0.12f);
            var right = Panel(safe, "Portrait_Vriskas", new Color(0.16f, 0.24f, 0.2f), false);
            Anchor(right, 0.8f, 0.3f, 0.98f, 0.95f);
            var rightBox = Rect(right, "Character"); Anchor(rightBox, 0f, 0.12f, 1f, 1f);
            var rightCharacter = CharacterBuilder.CreateFitted(rightBox, "Vriskas_Portrait", CharacterBuilder.Who.Vriskas, true);
            var rightLabel = Label(right, "Label", "Врискас", 30, TextAnchor.MiddleCenter, new Color(0.75f, 1f, 0.8f));
            Anchor(rightLabel.rectTransform, 0f, 0f, 1f, 0.12f);

            var box = Panel(safe, "TextBox", new Color(0.1f, 0.12f, 0.16f, 0.95f), false);
            Anchor(box, 0.22f, 0.3f, 0.78f, 0.95f);
            var speaker = Label(box, "Speaker", "", 36, TextAnchor.UpperLeft, new Color(1f, 0.82f, 0.45f));
            speaker.fontStyle = FontStyle.Bold;
            Anchor(speaker.rectTransform, 0.04f, 0.8f, 0.96f, 0.96f);
            var line = Label(box, "Line", "", 36, TextAnchor.UpperLeft, Color.white);
            Anchor(line.rectTransform, 0.04f, 0.06f, 0.96f, 0.78f);

            var next = Button(safe, "Button_Next", "Далее", new Color(0.25f, 0.35f, 0.5f), out var nextLabel);
            AnchorPixels(next, 0.5f, 0.13f, 0, 0, 360, 120);
            var c1 = Button(safe, "Button_Choice1", "", new Color(0.3f, 0.45f, 0.3f), out var c1Label);
            Anchor(c1, 0.04f, 0.05f, 0.48f, 0.24f);
            var c2 = Button(safe, "Button_Choice2", "", new Color(0.45f, 0.35f, 0.25f), out var c2Label);
            Anchor(c2, 0.52f, 0.05f, 0.96f, 0.24f);
            c1.gameObject.SetActive(false);
            c2.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            SetArray(so, "choiceButtons", new UnityEngine.Object[] { c1.GetComponent<Button>(), c2.GetComponent<Button>() });
            SetArray(so, "choiceLabels", new UnityEngine.Object[] { c1Label, c2Label });
            so.ApplyModifiedPropertiesWithoutUndo();

            refs = new (string, UnityEngine.Object)[]
            {
                ("root", panel.gameObject), ("speakerName", speaker), ("lineText", line),
                ("leftPortrait", left.GetComponent<Image>()), ("rightPortrait", right.GetComponent<Image>()),
                ("leftPortraitLabel", leftLabel), ("rightPortraitLabel", rightLabel),
                ("leftCharacter", leftCharacter), ("rightCharacter", rightCharacter),
                ("nextButton", next.GetComponent<Button>()), ("nextLabel", nextLabel)
            };
            return view;
        }

        private static EndView BuildEndView(Transform parent, out (string, UnityEngine.Object)[] refs)
        {
            var host = Rect(parent, "EndView"); Stretch(host);
            var view = host.gameObject.AddComponent<EndView>();
            var panel = Panel(host, "Panel", new Color(0.05f, 0.12f, 0.08f, 0.94f), true); Stretch(panel);
            var safe = SafeArea(panel, "SafeArea");
            var title = Label(safe, "Title", "Эпизод завершён", 56, TextAnchor.MiddleCenter, new Color(0.6f, 1f, 0.7f));
            title.fontStyle = FontStyle.Bold;
            Anchor(title.rectTransform, 0.1f, 0.72f, 0.9f, 0.9f);
            var body = Label(safe, "Body", "", 36, TextAnchor.MiddleCenter, Color.white);
            Anchor(body.rectTransform, 0.12f, 0.32f, 0.88f, 0.7f);
            var btn = Button(safe, "Button_NewGame", "Новая игра", new Color(0.25f, 0.45f, 0.3f), out var btnLabel);
            AnchorPixels(btn, 0.5f, 0.15f, 0, 0, 400, 120);
            panel.gameObject.SetActive(false);
            refs = new (string, UnityEngine.Object)[]
            {
                ("root", panel.gameObject), ("title", title), ("body", body),
                ("newGameButton", btn.GetComponent<Button>()), ("newGameLabel", btnLabel)
            };
            return view;
        }

        private static MenuView BuildMenuView(Transform parent, out (string, UnityEngine.Object)[] refs)
        {
            var host = Rect(parent, "MenuView"); Stretch(host);
            var view = host.gameObject.AddComponent<MenuView>();
            var panel = Panel(host, "Panel", new Color(0.05f, 0.06f, 0.09f, 0.97f), true); Stretch(panel);
            var safe = SafeArea(panel, "SafeArea");
            var title = Label(safe, "Title", "Бискас: Эхо памяти", 64, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.45f));
            title.fontStyle = FontStyle.Bold;
            Anchor(title.rectTransform, 0.1f, 0.74f, 0.9f, 0.92f);
            var sub = Label(safe, "Subtitle", "", 30, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
            Anchor(sub.rectTransform, 0.1f, 0.64f, 0.9f, 0.74f);
            var status = Label(safe, "Status", "", 30, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.45f));
            Anchor(status.rectTransform, 0.1f, 0.54f, 0.9f, 0.64f);
            var cont = Button(safe, "Button_Continue", "Продолжить", new Color(0.25f, 0.38f, 0.55f), out var contLabel);
            AnchorPixels(cont, 0.5f, 0.4f, 0, 0, 480, 120);
            var ng = Button(safe, "Button_NewGame", "Новая игра", new Color(0.35f, 0.3f, 0.25f), out var ngLabel);
            AnchorPixels(ng, 0.5f, 0.22f, 0, 0, 480, 120);
            // Меню видно при запуске (стартовый экран).
            refs = new (string, UnityEngine.Object)[]
            {
                ("root", panel.gameObject), ("title", title), ("subtitle", sub), ("status", status),
                ("continueButton", cont.GetComponent<Button>()), ("continueLabel", contLabel),
                ("newGameButton", ng.GetComponent<Button>()), ("newGameLabel", ngLabel)
            };
            return view;
        }

        // ---------------- помощники UI ----------------

        private static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static RectTransform SafeArea(Transform parent, string name)
        {
            var rt = Rect(parent, name);
            Stretch(rt);
            rt.gameObject.AddComponent<SafeAreaFitter>();
            return rt;
        }

        private static RectTransform Panel(Transform parent, string name, Color color, bool raycast)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return rt;
        }

        private static Text Label(Transform parent, string name, string text, int size, TextAnchor align, Color color)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(16, size / 2);
            t.resizeTextMaxSize = size;
            return t;
        }

        private static RectTransform Button(Transform parent, string name, string text, Color color, out Text label)
        {
            var rt = Panel(parent, name, color, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = rt.GetComponent<Image>();
            label = Label(rt, "Label", text, 34, TextAnchor.MiddleCenter, Color.white);
            Anchor(label.rectTransform, 0.05f, 0.08f, 0.95f, 0.92f);
            return rt;
        }

        /// <summary>
        /// Предмет комнаты: прозрачная область касания крупнее визуала + визуал-заглушка + подпись + подсветка.
        /// </summary>
        private static RoomHotspot Hotspot(Transform parent, string name, string objectId, string labelText,
                                           float ax, float ay, Vector2 hitSize, Vector2 visualSize,
                                           params (string key, string sprite)[] states)
        {
            var rt = Rect(parent, name);
            AnchorBox(rt, ax, ay, hitSize.x, hitSize.y);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0f); // невидимая, но принимает касания
            hit.raycastTarget = true;

            var hl = SpriteImage(rt, "Highlight", "FX/glow_soft", false);
            hl.color = new Color(1f, 0.9f, 0.4f, 0.85f);
            AnchorBox(hl.rectTransform, 0.5f, 0.5f, visualSize.x * 1.5f, visualSize.y * 1.5f);
            hl.gameObject.SetActive(false);
            var vis = SpriteImage(rt, "Visual", states[0].sprite, true);
            AnchorBox(vis.rectTransform, 0.5f, 0.5f, visualSize.x, visualSize.y);

            // Подпись под предметом (подсказка, куда касаться)
            var lbl = Label(rt, "Label", labelText, 24, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.85f));
            lbl.rectTransform.anchorMin = new Vector2(0f, 0f);
            lbl.rectTransform.anchorMax = new Vector2(1f, 0f);
            lbl.rectTransform.pivot = new Vector2(0.5f, 0f);
            lbl.rectTransform.sizeDelta = new Vector2(80f, 56f);
            lbl.rectTransform.anchoredPosition = Vector2.zero;
            var shadow = lbl.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -2f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = vis;

            var hs = rt.gameObject.AddComponent<RoomHotspot>();
            var so = new SerializedObject(hs);
            so.FindProperty("objectId").stringValue = objectId;
            Set(so, "button", btn);
            Set(so, "visual", vis);
            Set(so, "label", lbl);
            Set(so, "highlight", hl.gameObject);
            var arr = so.FindProperty("stateSprites");
            arr.arraySize = states.Length;
            for (int i = 0; i < states.Length; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("key").stringValue = states[i].key;
                e.FindPropertyRelative("sprite").objectReferenceValue = Spr(states[i].sprite);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return hs;
        }

        // ---------------- спрайты ----------------

        private static Sprite Spr(string artPath)
        {
            string path = ArtImportSettings.ArtRoot + artPath + ".png";
            var sp = ArtImportSettings.LoadSprite(path);
            if (sp == null) throw new FileNotFoundException("Нет спрайта " + path);
            return sp;
        }

        private static Image SpriteImage(Transform parent, string name, string artPath, bool preserveAspect)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Spr(artPath);
            img.preserveAspect = preserveAspect;
            img.raycastTarget = false;
            Stretch(rt);
            return img;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Центр в долях родителя, размер в единицах эталонного разрешения.</summary>
        private static void AnchorBox(RectTransform rt, float ax, float ay, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>Якорь в долях, пивот у той же стороны, смещение и размер в единицах.</summary>
        private static void AnchorPixels(RectTransform rt, float ax, float ay, float dx, float dy, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(ax, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(dx, dy);
        }

        // ---------------- привязка сериализованных полей ----------------

        private static void Set(SerializedObject so, string field, UnityEngine.Object value)
        {
            var p = so.FindProperty(field);
            if (p == null) throw new MissingFieldException(so.targetObject.GetType().Name, field);
            p.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject so, string field, UnityEngine.Object[] values)
        {
            var p = so.FindProperty(field);
            if (p == null || !p.isArray) throw new MissingFieldException(so.targetObject.GetType().Name, field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void ApplyRefs(UnityEngine.Object target, (string, UnityEngine.Object)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs) Set(so, field, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
