# Следующие ассеты эпизода 1

Прототип работает на заглушках; изображения не блокируют разработку. Порядок: сначала **один** фон, **один** портрет Бискаса и **один** предмет → проверить в Unity на телефоне → только потом остальное по утверждённым референсам. Перед портретами нужен утверждённый референс дизайна героев выбранного периода (`docs/ART_BRIEF.md`).

| # | Файл | Размер исходника | Куда подключается | Заменяет заглушку |
|---|---|---|---|---|
| 1 | laboratory_background.png | 1920×1080 | `Canvas/Background` (Image.sprite) | тёмный прямоугольник + «Decor_*» |
| 2 | pass_damaged.png | 512×512, alpha | `Hotspot_Pass/Visual` | бежевый прямоугольник |
| 3 | cache_closed.png | 512×512, alpha | `Hotspot_Cache/Visual` (состояние revealed) | оранжевый прямоугольник |
| 4 | cache_open.png | 512×512, alpha | `Hotspot_Cache/Visual` (состояние empty) | тёмный прямоугольник |
| 5 | wall_panel.png | 512×512, alpha | `Hotspot_Cache/Visual` (hidden) | серый прямоугольник |
| 6 | door_closed.png | 512×1024, alpha | `Hotspot_Door/Visual` | красный прямоугольник |
| 7 | door_open.png | 512×1024, alpha | `Hotspot_Door/Visual` | зелёный прямоугольник |
| 8 | key.png | 256×256, alpha | слот инвентаря | жёлтая ячейка с подписью |
| 9 | biskas_portrait_neutral / concerned / relieved.png | 1024×1024, alpha | `DialogueView/.../Portrait_Biskas` | оранжевая плашка |
| 10 | Врискас: части персонажа (tail, body, head, eyes_open/closed/glow, mouth_talk) по референсу | общий холст 512–1024, alpha | `Vriskas_Room` и `Vriskas_Portrait` | **сделана оригинальная заглушка** (12.10.2026), заменить по утверждённому референсу |
| 11 | Шрифт с кириллицей (OFL или иная явная лицензия) | — | все Text | LegacyRuntime |

Эффекты памяти (сепия) и X-Ray (зелёная тонировка + подсветка тайника) остаются средствами Unity — отдельные картинки не нужны.

Техническая подготовка: Texture Type = Sprite (2D and UI), Filter = Bilinear, без mipmaps и Read/Write, Max Size по замеру (ориентир 2048 для фона, 512 для предметов), сжатие ASTC для Android. Для состояний тайника/двери потребуется добавить в `RoomHotspot.Apply` выбор спрайта (параметр `sprite` уже предусмотрен). Каждый итоговый файл записывать в `docs/ASSET_MANIFEST.md`.

## Промпты (внешний генератор)

**1. Фон**
> Background for a hand-drawn 2D mobile mystery adventure, empty enclosed laboratory room seen from a fixed side-facing camera, dim blue-gray walls, a metal ventilation grille high on the middle-right wall, a plain lab table on the left, an empty wall area in the center-right, a heavy sector door on the far right edge left unlit, clear readable composition, bold clean outlines, simple cel shading, muted blue-gray palette with restrained cold green accents, slightly tense investigative mood. No characters, no lettering, no interface, no keys, no cards, no collectible objects. Landscape 16:9, 1920x1080.

**2. Пропуск**
> Single damaged laboratory access card for the same hand-drawn 2D mystery game, cracked plastic, scratched-out photo area, bent corner, bold clean outline, simple cel shading, muted palette, readable silhouette at small size, slight top-down view, isolated, transparent background, no text, no logos, no extra objects.

**3–5. Тайник / панель**
> Single square metal wall panel for the same hand-drawn 2D mystery game, flush with a blue-gray lab wall, small scratch near one corner, bold clean outline, simple cel shading, front view, isolated, transparent background, no text. Variant A: closed (panel slightly ajar, warm glint behind it). Variant B: open, empty dark recess. Keep identical size, perspective and outline for all variants.

**6–7. Дверь**
> Heavy laboratory sector door for the same hand-drawn 2D mystery game, tall rectangular steel door with a mechanical key lock, front view, bold clean outline, simple cel shading, muted blue-gray with a red status lamp, isolated, transparent background, no text, no signs. Variant B: the same door open, dark corridor with faint cold green light, status lamp green. Identical framing for both variants.

**8. Ключ**
> Single old brass mechanical key for the same hand-drawn 2D mystery game, simple readable silhouette, bold clean outline, simple cel shading, slight angle, isolated, transparent background, no text, no extra objects.

**9–10. Портреты** (только с утверждённым референсом; окрас и отметины сверять с ним)
> Use the supplied approved character reference. Preserve the exact silhouette, facial markings, proportions, eye colors and line style. Waist-up portrait, three-quarter view, [neutral | concerned | relieved / neutral | annoyed | pleased], simple cel shading, consistent framing identical to the other portraits of this set, isolated character, transparent background, no lettering, no interface.

Если генератор не даёт настоящий alpha — просить однотонный фон и удалять его вручную (Krita), проверяя края на светлом и тёмном фоне.
