using System.Collections;
using System.Collections.Generic;
using Biskas.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.Runtime
{
    /// <summary>
    /// Корневой объект эпизода: связывает чистую логику (Biskas.Core) с UI-представлением,
    /// сохранениями и жизненным циклом приложения. Все ссылки задаются в сцене
    /// (их проставляет команда Biskas → «Создать сцену эпизода»).
    /// </summary>
    public sealed class EpisodeController : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private TextAsset contentJson;

        [Header("Комната")]
        [SerializeField] private RoomHotspot[] hotspots = new RoomHotspot[0];
        [SerializeField] private GameObject memoryTint;
        [SerializeField] private GameObject xrayTint;
        [SerializeField] private CharacterAnimator roomVriskas;   // анимированный компаньон в комнате
        [SerializeField] private CharacterAnimator roomBiskas;    // анимированный герой в комнате
        [SerializeField] private RewardAnimator rewardAnimator;   // открытие тайника и полёт ключа
        [SerializeField] private InteractionFx interactionFx;     // «появление» / «ничего нет»
        [SerializeField] private float walkSpeed = 1100f;         // скорость Бискаса, ед. Canvas в секунду
        [SerializeField] private float reachTime = 0.3f;          // жест «тянется» перед результатом
        [SerializeField] private float endScreenDelay = 1.1f;     // пауза, чтобы увидеть открытую дверь

        [Header("HUD")]
        [SerializeField] private Button memoryButton;
        [SerializeField] private Image memoryButtonImage;
        [SerializeField] private Text memoryButtonLabel;
        [SerializeField] private Button xrayButton;
        [SerializeField] private Image xrayButtonImage;
        [SerializeField] private Text xrayButtonLabel;
        [SerializeField] private Button menuButton;
        [SerializeField] private Text menuButtonLabel;
        [SerializeField] private Text inventoryTitle;
        [SerializeField] private InventoryView inventory;
        [SerializeField] private Text messageSpeaker;
        [SerializeField] private Text messageText;

        [Header("Оверлеи")]
        [SerializeField] private MemoryView memoryView;
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private MenuView menuView;
        [SerializeField] private EndView endView;

        [Header("Цвета заглушек")]
        [SerializeField] private Color passColor = new Color(0.86f, 0.76f, 0.55f);
        [SerializeField] private Color wallColor = new Color(0.33f, 0.37f, 0.42f);
        [SerializeField] private Color cacheXrayColor = new Color(0.3f, 0.85f, 0.45f);
        [SerializeField] private Color cacheRevealedColor = new Color(0.78f, 0.56f, 0.2f);
        [SerializeField] private Color cacheEmptyColor = new Color(0.22f, 0.19f, 0.16f);
        [SerializeField] private Color doorClosedColor = new Color(0.5f, 0.22f, 0.22f);
        [SerializeField] private Color doorOpenColor = new Color(0.2f, 0.55f, 0.32f);
        [SerializeField] private Color modeOffColor = new Color(0.2f, 0.24f, 0.3f, 0.95f);
        [SerializeField] private Color memoryOnColor = new Color(0.85f, 0.6f, 0.25f, 1f);
        [SerializeField] private Color xrayOnColor = new Color(0.25f, 0.75f, 0.4f, 1f);

        private EpisodeContent _content;
        private EpisodeLogic _logic;
        private SaveStore _store;
        private readonly DialogueRunner _dialogue = new DialogueRunner();
        private bool _sessionStarted;       // игровое состояние загружено и активно
        private bool _saveBlocked;          // повреждённый файл не перезаписываем до «Новой игры»
        private bool _newGameArmed;         // подтверждение сброса в меню
        private int _lastActionFrame = -1;  // одно действие с комнатой за кадр
        private bool _animating;            // ввод в комнате заблокирован на время анимации
        private string _hiddenItem;         // предмет «в полёте» ещё не показан в инвентаре

        public EpisodeLogic Logic => _logic;
        public string SavePath => _store?.MainPath;

        // ---------------- запуск ----------------

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            _content = LoadContent(contentJson);
            _store = new SaveStore(Application.persistentDataPath);
            _logic = new EpisodeLogic(new EpisodeState());

            foreach (var h in hotspots) if (h != null) h.Tapped += OnHotspotTapped;
            if (memoryButton != null) memoryButton.onClick.AddListener(() => OnModeButton(AbilityMode.Memory));
            if (xrayButton != null) xrayButton.onClick.AddListener(() => OnModeButton(AbilityMode.XRay));
            if (menuButton != null) menuButton.onClick.AddListener(OpenPauseMenu);
            if (inventory != null) inventory.ItemTapped += OnItemTapped;
            if (dialogueView != null)
            {
                dialogueView.NextPressed += OnDialogueNext;
                dialogueView.ChoicePressed += OnDialogueChoice;
            }
            if (menuView != null)
            {
                menuView.ContinuePressed += OnMenuContinue;
                menuView.NewGamePressed += OnMenuNewGame;
            }
            if (endView != null) endView.NewGamePressed += StartNewGame;
        }

        private void Start()
        {
            ApplyStaticTexts();
            var load = _store.Load();
            string status = null;
            bool canContinue = false;

            switch (load.Status)
            {
                case LoadStatus.Ok:
                    _logic.Reset(load.State);
                    canContinue = true;
                    break;
                case LoadStatus.RestoredFromBackup:
                    _logic.Reset(load.State);
                    canContinue = true;
                    status = T("ui_save_restored");
                    break;
                case LoadStatus.Corrupt:
                    _saveBlocked = true;
                    status = T("ui_save_corrupt");
                    break;
                case LoadStatus.Incompatible:
                    _saveBlocked = true;
                    status = T("ui_save_incompatible");
                    break;
            }

            RefreshAll();
            ShowMessage(canContinue ? null : "hint_start");
            if (menuView != null)
                menuView.Show(T("ui_title"), T("ui_subtitle"), status, canContinue,
                              T("ui_continue"), T(canContinue ? "ui_new_game" : "ui_start"));
            else
                BeginSession();
        }

        private static EpisodeContent LoadContent(TextAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("[Biskas] Не назначен JSON с данными эпизода.");
                return new EpisodeContent();
            }
            var c = JsonUtility.FromJson<EpisodeContent>(asset.text);
            return c ?? new EpisodeContent();
        }

        private void BeginSession()
        {
            _sessionStarted = true;
            RefreshAll();
            if (_logic.State.IsCompleted)
            {
                ShowEnd();
                return;
            }
            if (_logic.IsDialoguePending) StartDialogue();
            else if (_logic.State.Stage == EpisodeStage.Start) ShowMessage("hint_start");
        }

        // ---------------- меню ----------------

        private void OpenPauseMenu()
        {
            if (!CanActOnRoom()) return;
            _newGameArmed = false;
            menuView.Show(T("ui_title"), T("ui_subtitle"), null, true, T("ui_continue"), T("ui_new_game"));
        }

        private void OnMenuContinue()
        {
            _newGameArmed = false;
            menuView.Hide();
            if (!_sessionStarted) BeginSession();
        }

        private void OnMenuNewGame()
        {
            bool hasProgress = _logic.State.Flags.Count > 0 && !_saveBlocked;
            if (hasProgress && !_newGameArmed)
            {
                _newGameArmed = true;
                menuView.SetStatus(T("ui_new_game_confirm"));
                return;
            }
            StartNewGame();
        }

        private void StartNewGame()
        {
            _newGameArmed = false;
            _saveBlocked = false;
            _dialogue.Stop();
            _store.DeleteAll();
            _logic.Reset();
            if (memoryView != null) memoryView.HideSilently();
            if (rewardAnimator != null) rewardAnimator.Stop();
            StopAllCoroutines();
            if (interactionFx != null) interactionFx.HideAll();
            if (roomBiskas != null) roomBiskas.StopMove(true);
            _animating = false;
            _hiddenItem = null;
            if (dialogueView != null) dialogueView.Hide();
            if (endView != null) endView.Hide();
            if (menuView != null) menuView.Hide();
            Save();
            _sessionStarted = false;
            BeginSession();
        }

        // ---------------- ввод в комнате ----------------

        private bool CanActOnRoom()
        {
            if (!_sessionStarted) return false;
            if (menuView != null && menuView.IsOpen) return false;
            if (memoryView != null && memoryView.IsOpen) return false;
            if (dialogueView != null && dialogueView.IsOpen) return false;
            if (endView != null && endView.IsOpen) return false;
            if (_animating) return false;                          // идёт анимация получения ключа
            if (_lastActionFrame == Time.frameCount) return false; // мультитач/двойная доставка
            _lastActionFrame = Time.frameCount;
            return true;
        }

        private void OnModeButton(AbilityMode mode)
        {
            if (!CanActOnRoom()) return;
            Handle(_logic.ToggleMode(mode));
        }

        private void OnItemTapped(string itemId)
        {
            if (!CanActOnRoom()) return;
            Handle(_logic.ToggleSelectItem(itemId));
        }

        /// <summary>
        /// Касание предмета: в обычном режиме и в режиме памяти Бискас подходит к предмету,
        /// тянется к нему, и только тогда применяется действие с анимацией результата.
        /// В X-Ray смотрит Врискас — подходить не нужно. Ввод в комнате заблокирован до конца.
        /// </summary>
        private void OnHotspotTapped(string objectId)
        {
            if (!CanActOnRoom()) return;
            var hs = FindHotspot(objectId);
            if (_logic.Mode == AbilityMode.XRay || roomBiskas == null || hs == null)
            {
                Resolve(objectId, hs, walked: false);
                return;
            }
            _animating = true;
            roomBiskas.MoveTo(StandPointFor(hs), walkSpeed, () => StartCoroutine(ReachAndResolve(objectId, hs)));
        }

        private IEnumerator ReachAndResolve(string objectId, RoomHotspot hs)
        {
            float objX = hs.VisualRect.position.x, catX = roomBiskas.transform.position.x;
            roomBiskas.SetFacing(objX < catX);
            roomBiskas.Reach();
            yield return new WaitForSecondsRealtime(reachTime);
            Resolve(objectId, hs, walked: true);
        }

        private void Resolve(string objectId, RoomHotspot hs, bool walked)
        {
            var r = _logic.TapObject(objectId);
            bool manages = (r.Event == EpisodeEvent.KeyObtained && rewardAnimator != null) || r.Event == EpisodeEvent.EpisodeCompleted;
            Handle(r);

            // Анимация результата: что-то появилось/изменилось — «выпрыг» со вспышкой, иначе — «ничего нет».
            if (interactionFx != null && hs != null && r.Event != EpisodeEvent.ShowMemory)
            {
                bool found = r.StateChanged || r.Event != EpisodeEvent.None;
                if (r.Event == EpisodeEvent.KeyObtained) { /* открытие тайника и полёт ключа — RewardAnimator */ }
                else if (found) interactionFx.PlayReveal(hs.VisualRect);
                else
                {
                    interactionFx.PlayNothing(hs.VisualRect);
                    if (walked && roomBiskas != null) roomBiskas.ShakeHead();
                }
            }

            if (!manages) StartCoroutine(UnlockAfter(walked ? 0.35f : 0f));
        }

        private IEnumerator UnlockAfter(float seconds)
        {
            if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
            if (rewardAnimator == null || !rewardAnimator.IsPlaying) _animating = false;
        }

        private RoomHotspot FindHotspot(string objectId)
        {
            foreach (var h in hotspots) if (h != null && h.ObjectId == objectId) return h;
            return null;
        }

        /// <summary>Точка на полу у предмета: под настенным предметом или сбоку от напольного/стоящего на столе.</summary>
        private Vector2 StandPointFor(RoomHotspot hs)
        {
            var cat = (RectTransform)roomBiskas.transform;
            var room = cat.parent as RectTransform;
            if (room == null) return roomBiskas.HomePosition;
            Rect rr = room.rect;
            float anchorX = rr.xMin + cat.anchorMin.x * rr.width;
            Vector3 objLocal = room.InverseTransformPoint(hs.VisualRect.position);
            float halfObj = hs.VisualRect.rect.width * 0.5f;
            float halfCat = cat.rect.width * 0.32f;

            // Предмет висит на стене выше головы (тайник) — встать прямо под ним.
            float anchorY = rr.yMin + cat.anchorMin.y * rr.height;
            float catTop = anchorY + roomBiskas.HomePosition.y + cat.rect.height * 0.3f;
            float objBottom = objLocal.y - hs.VisualRect.rect.height * 0.5f;
            if (objBottom > catTop)
                return new Vector2(Mathf.Clamp(objLocal.x, rr.xMin + halfCat, rr.xMax - halfCat) - anchorX, roomBiskas.HomePosition.y);

            // Иначе — сбоку от предмета (слева, если хватает места).
            float left = objLocal.x - halfObj - halfCat;
            float x = left >= rr.xMin + halfCat ? left : Mathf.Min(objLocal.x + halfObj + halfCat, rr.xMax - halfCat);
            return new Vector2(x - anchorX, roomBiskas.HomePosition.y);
        }

        private void Handle(ActionResult r)
        {
            if (!string.IsNullOrEmpty(r.MessageId)) ShowMessage(r.MessageId);
            if (r.StateChanged) Save();
            if (r.Event == EpisodeEvent.KeyObtained && rewardAnimator != null) _hiddenItem = ItemIds.LabKey;
            RefreshAll();

            switch (r.Event)
            {
                case EpisodeEvent.ShowMemory:
                    var m = _content.memory;
                    memoryView.Show(m, _content.SpeakerName("biskas"), T("ui_next"), T("ui_memory_close"), OnMemoryClosed);
                    break;
                case EpisodeEvent.CacheRevealed:
                    if (roomVriskas != null) roomVriskas.Bounce();
                    break;
                case EpisodeEvent.KeyObtained:
                    PlayKeyPickup();
                    break;
                case EpisodeEvent.EpisodeCompleted:
                    StartCoroutine(EndAfterDelay());
                    break;
            }
        }

        private IEnumerator EndAfterDelay()
        {
            _animating = true;
            if (roomBiskas != null) roomBiskas.Bounce();
            yield return new WaitForSecondsRealtime(endScreenDelay);
            _animating = false;
            ShowEnd();
        }

        private void PlayKeyPickup()
        {
            if (rewardAnimator == null) return;
            _animating = true;
            int slot = 0;
            foreach (var it in _logic.State.Items) if (it != _hiddenItem) slot++;
            rewardAnimator.PlayKeyPickup(inventory != null ? inventory.SlotRect(slot) : null, () =>
            {
                _animating = false;
                _hiddenItem = null;
                RefreshAll();
                if (roomBiskas != null) roomBiskas.Bounce();
            });
        }

        private void OnMemoryClosed()
        {
            if (_logic.IsDialoguePending) StartDialogue();
        }

        // ---------------- диалог ----------------

        private void StartDialogue()
        {
            var data = _content.FindDialogue(DialogueIds.AfterMemory);
            if (data == null || !_dialogue.Start(data, _logic.State))
            {
                Debug.LogError("[Biskas] Диалог не найден: " + DialogueIds.AfterMemory);
                return;
            }
            SaveIfDialogueChanged();
            ShowDialogueNode();
        }

        private void OnDialogueNext()
        {
            if (!_dialogue.IsActive) return;
            _dialogue.Advance();
            SaveIfDialogueChanged();
            ShowDialogueNode();
        }

        private void OnDialogueChoice(int index)
        {
            if (!_dialogue.IsActive) return;
            _dialogue.Choose(index);
            SaveIfDialogueChanged();
            ShowDialogueNode();
        }

        private void ShowDialogueNode()
        {
            var node = _dialogue.Current;
            if (node == null)
            {
                dialogueView.Hide();
                ShowMessage("hint_after_dialogue");
                RefreshAll();
                return;
            }
            var choices = new List<string>();
            if (node.choices != null) foreach (var c in node.choices) choices.Add(c.text);
            dialogueView.ShowLine(node.speaker, _content.SpeakerName(node.speaker), node.text, choices, T("ui_next"),
                                  _content.SpeakerName("biskas"), _content.SpeakerName("vriskas"));
        }

        private void SaveIfDialogueChanged()
        {
            if (_dialogue.ConsumeStateChanged()) Save();
        }

        // ---------------- представление ----------------

        private void ShowEnd()
        {
            if (endView == null) return;
            string line = T("door_open");
            endView.Show(T("ui_end_title"), line + "\n\n" + T("ui_end_body"), T("ui_new_game"));
        }

        private void ShowMessage(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                if (messageSpeaker != null) messageSpeaker.text = "";
                if (messageText != null) messageText.text = "";
                return;
            }
            var m = _content.ResolveMessage(id, _logic.State);
            if (messageSpeaker != null) messageSpeaker.text = m != null ? _content.SpeakerName(m.speaker) : "";
            if (m != null && !string.IsNullOrEmpty(m.text))
            {
                float talk = Mathf.Clamp(m.text.Length * 0.045f, 0.8f, 3f);
                if (roomVriskas != null && m.speaker == "vriskas") roomVriskas.Talk(talk);
                if (roomBiskas != null && m.speaker == "biskas") roomBiskas.Talk(talk);
            }
            if (messageText != null) messageText.text = m != null ? m.text : "[" + id + "]";
        }

        private string T(string id) => _content.Text(id, _logic.State);

        private void ApplyStaticTexts()
        {
            if (memoryButtonLabel != null) memoryButtonLabel.text = T("ui_memory");
            if (xrayButtonLabel != null) xrayButtonLabel.text = T("ui_xray");
            if (menuButtonLabel != null) menuButtonLabel.text = T("ui_menu");
            if (inventoryTitle != null) inventoryTitle.text = T("ui_inventory");
        }

        private void RefreshAll()
        {
            var s = _logic.State;
            var mode = _logic.Mode;
            bool memorySeen = s.HasFlag(FlagIds.MemorySeen);
            bool revealed = s.HasFlag(FlagIds.CacheRevealed);
            bool taken = s.HasFlag(FlagIds.KeyTaken);
            bool open = s.HasFlag(FlagIds.DoorOpened);

            foreach (var h in hotspots)
            {
                if (h == null) continue;
                switch (h.ObjectId)
                {
                    case ObjectIds.Pass:
                        h.ApplyState("default", T("obj_pass"), passColor,
                                mode == AbilityMode.Memory && s.HasFlag(FlagIds.PassInspected));
                        break;
                    case ObjectIds.Cache:
                        if (taken) h.ApplyState("open", T("obj_cache_empty"), cacheEmptyColor, false);
                        else if (revealed) h.ApplyState("closed", T("obj_cache_revealed"), cacheRevealedColor, mode == AbilityMode.XRay);
                        else if (mode == AbilityMode.XRay && memorySeen) h.ApplyState("xray", T("obj_cache_xray"), cacheXrayColor, true);
                        else h.ApplyState("hidden", T("obj_cache_hidden"), wallColor, false);
                        break;
                    case ObjectIds.Door:
                        h.ApplyState(open ? "open" : "closed", T(open ? "obj_door_open" : "obj_door_closed"), open ? doorOpenColor : doorClosedColor,
                                !open && _logic.SelectedItem == ItemIds.LabKey);
                        break;
                }
            }

            if (memoryTint != null) memoryTint.SetActive(mode == AbilityMode.Memory);
            if (xrayTint != null) xrayTint.SetActive(mode == AbilityMode.XRay);
            if (roomVriskas != null) roomVriskas.SetGlow(mode == AbilityMode.XRay);
            if (roomBiskas != null) roomBiskas.SetGlow(mode == AbilityMode.Memory);
            if (memoryButtonImage != null) memoryButtonImage.color = mode == AbilityMode.Memory ? memoryOnColor : modeOffColor;
            if (xrayButtonImage != null) xrayButtonImage.color = mode == AbilityMode.XRay ? xrayOnColor : modeOffColor;
            if (inventory != null)
            {
                var shown = new List<string>();
                foreach (var it in s.Items) if (it != _hiddenItem) shown.Add(it);
                inventory.Refresh(shown, _logic.SelectedItem, id => T("item_" + id));
            }
        }

        // ---------------- сохранение и жизненный цикл ----------------

        private void Save()
        {
            if (_saveBlocked || _store == null) return;
            if (!_store.Save(_logic.State))
            {
                Debug.LogWarning("[Biskas] Ошибка сохранения: " + _store.LastError);
                if (messageText != null) messageText.text = T("ui_save_failed");
            }
        }

        private void OnApplicationPause(bool paused)
        {
            AudioListener.pause = paused;
            if (paused && _sessionStarted) Save();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _sessionStarted) Save();
            if (hasFocus) _lastActionFrame = -1; // восстановить ввод после возврата
        }

        private void OnApplicationQuit()
        {
            if (_sessionStarted) Save();
        }
    }
}
