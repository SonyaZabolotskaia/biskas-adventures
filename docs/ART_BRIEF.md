# Арт первого эпизода

Стиль: рисованная 2D-графика, чёткий контур, простые тени, ограниченная палитра, хорошая читаемость на телефоне. Нужен слегка тревожный исследовательский тон. Фон тёмный с холодными акцентами; интерактивные объекты различимы по форме и контрасту, не только по цвету.

Дизайн героев необходимо утвердить по референсу выбранного периода. Начальная рабочая база: Бискас — рыжий кот, Врискас — зелёный; остальные детали не считать подтверждёнными.

## Набор

- laboratory_background — пустая лаборатория, 1920×1080.
- biskas_portrait_neutral / concerned / relieved — одинаковое кадрирование.
- vriskas_portrait_neutral / annoyed / pleased — одинаковое кадрирование.
- pass_damaged, key — отдельные предметы.
- cache_closed / cache_open — тайник.
- door_closed / door_open — дверь.

Текст, кодовые замки и UI рисуются средствами Unity, не внутри генерации. Нужен отдельный подбор шрифта с кириллицей и понятной лицензией.

## Шаблоны промптов для внешнего генератора

Фон:
> Background for a hand-drawn 2D mobile mystery adventure, empty enclosed laboratory room, side-facing fixed camera, clear readable composition, bold clean outlines, simple cel shading, muted blue-gray palette with restrained green accents. Clear floor and wall space for separately placed interactive props. No characters, no lettering, no interface, no collectible objects. Landscape 16:9.

Портрет с утверждённым референсом:
> Use the supplied approved character reference. Preserve the exact silhouette, facial markings, proportions, eye colors and line style. Waist-up portrait, three-quarter view, [emotion], simple cel shading, consistent framing, isolated character, transparent background, no lettering, no interface.

Предмет:
> Single [item] for the same hand-drawn 2D mystery game, bold clean outline, simple cel shading, readable silhouette at small size, [view matching the room], isolated, transparent background, no text, no extra objects.

Если генератор не выдаёт настоящий alpha, запросить однотонный фон и удалить его в редакторе. Для вариаций использовать утверждённый референс. Сначала проверить один фон, портрет и предмет в Unity; затем расширять набор.

## Доступные варианты

Локальный ComfyUI + FLUX.1-schnell для фонов и предметов: нет платы сервису за каждую локальную генерацию, но нужны ресурсы компьютера. Конкретные настройки выбираются по видеокарте, заранее скорость не обещать. Модель: https://huggingface.co/black-forest-labs/FLUX.1-schnell .

Облачный генератор с редактированием по референсу — удобнее при слабом компьютере; тариф и права нужно проверить у выбранного сервиса. Ручная чистка и слои — в Krita: https://krita.org/ .
