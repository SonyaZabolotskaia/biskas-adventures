# Манифест ассетов

Все изображения — **оригинальные временные рисунки проекта**, сгенерированные кодом (SVG → PNG) бесплатными инструментами: Python + cairosvg (LGPL). Сторонних, покупных или скачанных ассетов нет. Канонические дизайны персонажей сериала не воспроизводятся: «Бискас» (рыжий кот) и «Врискас» (зелёный кот) — собственные заглушки до утверждения референса (docs/ART_BRIEF.md). Права на использование персонажей вселенной для публикации решаются отдельно.

Пересборка: из корня проекта `pip install cairosvg`, затем `python Tools/ArtGen/characters.py` и `python Tools/ArtGen/room.py`. Исходники SVG: `Tools/ArtGen/svg/`. Дата: 12.10.2026.

| Путь (Assets/_Game/Art/…) | Назначение | Генератор |
|---|---|---|
| `Characters/Biskas/biskas_{tail,body,head,eyes_open,eyes_closed,mouth_talk,eyes_glow}.png` | Части героя для процедурной анимации (холст 512×512) | characters.py |
| `Characters/Vriskas/vriskas_{…те же части…}.png` | Части компаньона | characters.py |
| `Room/laboratory_background.png` | Фон лаборатории 1920×1080, без предметов и текста | room.py |
| `Room/table.png`, `Room/vent.png` | Декор: стол под пропуском, вентиляция над тайником | room.py |
| `Room/pass_damaged.png` | Повреждённый пропуск | room.py |
| `Room/wall_panel.png`, `cache_xray.png`, `cache_closed.png`, `cache_open.png` | Тайник: скрыт / вид X-Ray / найден (створка) / открыт | room.py |
| `Room/key.png` | Ключ (в комнате, в полёте и в инвентаре) | room.py |
| `Room/door_closed.png`, `door_open.png` | Дверь сектора | room.py |
| `FX/glow_soft.png`, `FX/sparkle.png` | Подсветка предметов, вспышка при получении ключа | room.py |
| `FX/dust_puff.png`, `FX/question_bubble.png` | «Ничего нет»: облачко пыли и пузырь для «?» (знак — текст Unity) | room.py |
| `Memory/memory_cloud.png`, `memory_bubble.png` | Облако-мысль и пузырьки воспоминания | room.py |
| `Memory/icon_alarm.png`, `icon_hand_key.png`, `icon_pass_fall.png`, `icon_steps.png` | Картинки шагов воспоминания | room.py |

Замена на утверждённый арт: положить PNG с теми же именами (для персонажей — те же суставы на холсте: хвост 318,440; тело 256,486; шея 256,290) и выполнить «Biskas → Пересоздать сцену эпизода».

Шрифт: встроенный Unity `LegacyRuntime.ttf` (заглушка, заменить до публикации).
