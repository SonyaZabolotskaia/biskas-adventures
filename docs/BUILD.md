# Запуск и сборка

## Среда (проверено 12.10.2026)

| Компонент | Состояние |
|---|---|
| Unity Editor | 6000.3.10f1 — `C:\Program Files\Unity\Hub\Editor\6000.3.10f1` |
| Android Build Support (+ OpenJDK, SDK & NDK) | **не установлен** (в `PlaybackEngines` только `windowsstandalonesupport`) |
| Unity Hub | установлен, вход выполнен, проект добавлен |
| Пакеты проекта | только `com.unity.ugui 2.0.0` + встроенные модули (`Packages/manifest.json`) |
| Ввод | uGUI EventSystem + StandaloneInputModule (старый Input Manager; пакет Input System не подключён) |

## Первый запуск проекта

1. Unity Hub → войти в учётную запись → **Projects → Add → Add project from disk** → `C:\Projects\BiskasGame`. Версия редактора: 6000.3.10f1 (`ProjectSettings/ProjectVersion.txt`). Unity сгенерирует `Library/` и недостающие `ProjectSettings`.
2. Дождаться импорта и компиляции (Console без ошибок).
3. Меню **Biskas → Создать или открыть сцену эпизода**. Создаётся `Assets/_Game/Scenes/Lab_Episode1.unity`, сцена добавляется в Build Settings, применяются настройки игрока (горизонтальная ориентация, `com.biskasfan.echoofmemory`).
   - Если в Console появилось «Включён старый Input Manager… Перезапустите редактор» — перезапустите Unity.
4. Меню **Biskas → Самопроверка логики и сцены** (сцена должна быть открыта) — ожидается «Все проверки пройдены».
5. Game view → выбрать разрешение 1920×1080 (и для проверки 2400×1080, Simulator) → **Play**.

## Команды Editor (меню Biskas)

| Пункт меню | Действие |
|---|---|
| Создать или открыть сцену эпизода | Создаёт сцену, если её нет; существующую только открывает (ручные правки сохраняются) |
| Пересоздать сцену эпизода (перезапишет ручные правки) | Явная перезапись с подтверждением |
| Самопроверка логики и сцены | 10 сценариев логики/сохранений + проверка ссылок в сцене |
| Применить настройки проекта | Идентификатор, версия, ориентация, IL2CPP/ARM64, сцена в сборке |
| Собрать Android APK (development) | `Builds/Android/BiskasEcho-dev.apk` |

## Командная строка (PowerShell, из `C:\Projects\BiskasGame`)

```powershell
$U = "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe"

# Создать тестовую сцену (повторный запуск не трогает существующую; -biskasRecreate — перезаписать)
& $U -batchmode -quit -projectPath . -executeMethod Biskas.EditorTools.SceneBuilder.CreateSceneFromCommandLine -logFile Logs\scene.log
echo $LASTEXITCODE

# Самопроверка (код 0 — успех)
& $U -batchmode -quit -projectPath . -executeMethod Biskas.EditorTools.SelfTestRunner.RunFromCommandLine -logFile Logs\selftest.log
echo $LASTEXITCODE

# Development APK (код 0 — APK есть; 2 — нет Android Build Support; 1 — ошибка)
& $U -batchmode -quit -projectPath . -buildTarget Android -executeMethod Biskas.EditorTools.AndroidBuild.BuildFromCommandLine -logFile Logs\build-android.log
echo $LASTEXITCODE
```

`-batchmode` требует активированной лицензии (вход в Unity Hub). Проект не должен быть одновременно открыт в редакторе.

## Android

Нужно до первой сборки: Unity Hub → Installs → 6000.3.10f1 → **Add modules → Android Build Support** с подпунктами **OpenJDK** и **Android SDK & NDK Tools**.

Настройки (ставит `ProjectSetup.Apply`):

- Package: `com.biskasfan.echoofmemory`, версия 0.1.0 (versionCode 1)
- Ориентация: только Landscape Left/Right
- IL2CPP, ARM64; development-сборка, APK (не AAB), отладочная подпись Unity
- Сцены сборки: `Assets/_Game/Scenes/Lab_Episode1.unity`

Установка на телефон (включена отладка по USB): `adb install -r Builds\Android\BiskasEcho-dev.apk` (adb — в `...\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools` после установки модуля).

Сохранение на устройстве: `Application.persistentDataPath/episode1.save` (+ `.bak`, `.tmp`).

## Графика

Все PNG генерируются скриптами: `pip install cairosvg`, затем из корня `python Tools/ArtGen/characters.py` и `python Tools/ArtGen/room.py`. После замены картинок — «Biskas → Пересоздать сцену эпизода». Если Unity не подхватывает изменённые скрипты/картинки: Assets → Refresh (в крайнем случае Reimport All).

## Проверка ядра без Unity (необязательно)

`Tools/CoreCheck` — консольная программа, выполняющая те же сценарии `EpisodeSelfTest` на JSON из проекта:
`dotnet run --project Tools/CoreCheck` (нужен .NET 8 SDK и доступ к NuGet для restore).
