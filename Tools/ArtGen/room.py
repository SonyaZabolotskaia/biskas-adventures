"""Оригинальная графика лаборатории, предметов и воспоминания (SVG → PNG через cairosvg).
Запуск из корня проекта:  python Tools/ArtGen/room.py
Стиль по docs/ART_BRIEF.md: чёткий контур, простые тени, тёмная холодная палитра, без текста."""
import os
import cairosvg

OUT_ROOM = "Assets/_Game/Art/Room"
OUT_FX = "Assets/_Game/Art/FX"
OUT_MEM = "Assets/_Game/Art/Memory"
SRC = "Tools/ArtGen/svg"
INK = "#10161d"


def render(folder, name, w, h, body, defs=""):
    os.makedirs(folder, exist_ok=True)
    os.makedirs(SRC, exist_ok=True)
    doc = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">'
           f'<defs>{defs}</defs>{body}</svg>')
    with open(os.path.join(SRC, name + ".svg"), "w") as f:
        f.write(doc)
    cairosvg.svg2png(bytestring=doc.encode(), write_to=os.path.join(folder, name + ".png"),
                     output_width=w, output_height=h)


# ---------------- фон ----------------
def background():
    w, h = 1920, 1080
    floor_y = 760
    defs = '''
<linearGradient id="wall" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1b232d"/><stop offset="1" stop-color="#28323f"/></linearGradient>
<linearGradient id="floor" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#232b34"/><stop offset="1" stop-color="#12171d"/></linearGradient>
<radialGradient id="vig" cx="50%" cy="45%" r="75%"><stop offset="0.55" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.55"/></radialGradient>
<radialGradient id="lamp" cx="50%" cy="0%" r="100%"><stop offset="0" stop-color="#7dffc0" stop-opacity="0.35"/><stop offset="1" stop-color="#7dffc0" stop-opacity="0"/></radialGradient>
<linearGradient id="pipe" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#55626f"/><stop offset="0.5" stop-color="#3a4550"/><stop offset="1" stop-color="#232b33"/></linearGradient>'''
    b = [f'<rect width="{w}" height="{h}" fill="url(#wall)"/>']
    # стеновые панели
    for i, x in enumerate(range(-40, w, 320)):
        b.append(f'<rect x="{x+10}" y="150" width="300" height="{floor_y-190}" rx="6" fill="#26303c" stroke="#161d25" stroke-width="5"/>')
        b.append(f'<rect x="{x+22}" y="162" width="276" height="16" fill="#2f3b49"/>')
        b.append(f'<circle cx="{x+30}" cy="{floor_y-60}" r="5" fill="#3b4856"/><circle cx="{x+290}" cy="{floor_y-60}" r="5" fill="#3b4856"/>')
    # горизонтальная полоса и плинтус
    b.append(f'<rect x="0" y="470" width="{w}" height="22" fill="#1d252f"/><rect x="0" y="474" width="{w}" height="6" fill="#2fbf7a" opacity="0.35"/>')
    b.append(f'<rect x="0" y="{floor_y-30}" width="{w}" height="30" fill="#151b22"/>')
    # трубы под потолком
    b.append(f'<rect x="0" y="40" width="{w}" height="34" fill="url(#pipe)" stroke="{INK}" stroke-width="4"/>')
    b.append(f'<rect x="0" y="92" width="{w}" height="20" fill="url(#pipe)" stroke="{INK}" stroke-width="3"/>')
    for x in range(120, w, 360):
        b.append(f'<rect x="{x}" y="30" width="26" height="90" rx="4" fill="#2d3640" stroke="{INK}" stroke-width="3"/>')
    # потолочные лампы
    for x in (420, 1180):
        b.append(f'<path d="M{x-160} 128 L {x+160} 128 L {x+420} {floor_y} L {x-420} {floor_y} Z" fill="url(#lamp)"/>')
        b.append(f'<rect x="{x-90}" y="118" width="180" height="16" rx="6" fill="#bfffe0" stroke="{INK}" stroke-width="4"/>')
    # настенный монитор (без текста) и кабели
    b.append(f'<rect x="1340" y="220" width="220" height="140" rx="10" fill="#0d1a16" stroke="{INK}" stroke-width="6"/>')
    b.append('<path d="M1360 320 L 1400 290 L 1430 305 L 1470 255 L 1500 275 L 1540 240" fill="none" stroke="#45e39a" stroke-width="5" opacity="0.8"/>')
    b.append(f'<path d="M1450 360 C 1450 420, 1600 430, 1640 {floor_y-30}" fill="none" stroke="#0e1318" stroke-width="10"/>')
    b.append(f'<path d="M60 200 C 120 260, 90 420, 160 {floor_y-30}" fill="none" stroke="#0e1318" stroke-width="12"/>')
    # пол с плиткой в перспективе
    b.append(f'<rect x="0" y="{floor_y}" width="{w}" height="{h-floor_y}" fill="url(#floor)"/>')
    for i in range(-8, 18):
        x0 = 960 + i * 150
        b.append(f'<line x1="{960 + i*90}" y1="{floor_y}" x2="{x0 + (x0-960)*0.9}" y2="{h}" stroke="#1a2129" stroke-width="3"/>')
    for y in (800, 860, 950, 1060):
        b.append(f'<line x1="0" y1="{y}" x2="{w}" y2="{y}" stroke="#1a2129" stroke-width="3"/>')
    b.append(f'<rect width="{w}" height="{h}" fill="url(#vig)"/>')
    render(OUT_ROOM, "laboratory_background", w, h, "".join(b), defs)


# ---------------- декор и предметы ----------------
def table():
    w, h = 520, 220
    b = f'''
<rect x="40" y="70" width="22" height="140" fill="#2a323b" stroke="{INK}" stroke-width="5"/>
<rect x="458" y="70" width="22" height="140" fill="#20272e" stroke="{INK}" stroke-width="5"/>
<rect x="300" y="70" width="150" height="80" rx="4" fill="#38424d" stroke="{INK}" stroke-width="5"/>
<rect x="345" y="104" width="60" height="10" rx="5" fill="#9aa6b1"/>
<rect x="10" y="30" width="500" height="44" rx="6" fill="#6b7783" stroke="{INK}" stroke-width="6"/>
<rect x="18" y="36" width="484" height="10" rx="4" fill="#94a0ab"/>
<rect x="10" y="60" width="500" height="14" fill="#4b5560"/>'''
    render(OUT_ROOM, "table", w, h, b)


def vent():
    w, h = 520, 150
    slats = "".join(f'<rect x="50" y="{38+i*20}" width="420" height="10" rx="4" fill="#0f141a"/>' for i in range(4))
    b = f'''<rect x="20" y="20" width="480" height="110" rx="12" fill="#4d5966" stroke="{INK}" stroke-width="7"/>
{slats}<circle cx="38" cy="38" r="6" fill="#8b97a3"/><circle cx="482" cy="38" r="6" fill="#8b97a3"/>
<circle cx="38" cy="112" r="6" fill="#8b97a3"/><circle cx="482" cy="112" r="6" fill="#8b97a3"/>'''
    render(OUT_ROOM, "vent", w, h, b)


def pass_card():
    w, h = 420, 270
    b = f'''
<g transform="rotate(-6 210 135)">
<path d="M40 50 L 360 50 L 380 70 L 380 230 L 40 230 Z" fill="#d9d2bf" stroke="{INK}" stroke-width="7" stroke-linejoin="round"/>
<path d="M360 50 L 380 70 L 360 72 Z" fill="#a49c88" stroke="{INK}" stroke-width="4"/>
<rect x="40" y="50" width="340" height="40" fill="#3f8f6a"/>
<rect x="66" y="108" width="100" height="104" rx="6" fill="#8d8a80" stroke="{INK}" stroke-width="5"/>
<path d="M76 120 L 156 200 M156 120 L 84 196 M96 112 L 150 206" stroke="#5d5a52" stroke-width="5"/>
<rect x="196" y="112" width="150" height="14" rx="6" fill="#9b9483"/><rect x="196" y="140" width="110" height="14" rx="6" fill="#9b9483"/>
<rect x="290" y="168" width="58" height="44" rx="6" fill="#d8b04a" stroke="{INK}" stroke-width="4"/>
<path d="M300 180 L 338 180 M300 190 L 338 190 M300 200 L 338 200 M319 168 L 319 212" stroke="#9c7a24" stroke-width="3"/>
<path d="M230 50 L 214 118 L 246 150 L 222 230" fill="none" stroke="{INK}" stroke-width="4"/>
<path d="M232 56 L 218 116 L 250 148 L 228 226" fill="none" stroke="#f7f2e3" stroke-width="2"/>
<rect x="196" y="22" width="44" height="36" rx="8" fill="none" stroke="#7f8a94" stroke-width="7"/>
</g>'''
    render(OUT_ROOM, "pass_damaged", w, h, b)


PANEL_W, PANEL_H = 400, 350


def _panel(fill, stroke_seam, screws=True, scratch=True, handle=False, ajar=False):
    s = f'<rect x="40" y="35" width="320" height="280" rx="10" fill="{fill}" stroke="{INK}" stroke-width="7"/>'
    s += f'<rect x="58" y="53" width="284" height="244" rx="6" fill="none" stroke="{stroke_seam}" stroke-width="4"/>'
    if screws:
        for (x, y) in [(62, 57), (338, 57), (62, 293), (338, 293)]:
            s += f'<circle cx="{x}" cy="{y}" r="8" fill="#8995a1" stroke="{INK}" stroke-width="3"/><path d="M{x-5} {y} L {x+5} {y}" stroke="{INK}" stroke-width="3"/>'
    if scratch:
        s += '<path d="M300 260 L 330 290 M310 255 L 336 282" stroke="#9fb0bf" stroke-width="4" stroke-linecap="round"/>'
    if handle:
        s += f'<rect x="318" y="150" width="22" height="56" rx="8" fill="#c79b3b" stroke="{INK}" stroke-width="5"/>'
    if ajar:
        s += '<path d="M360 45 L 372 60 L 372 300 L 360 310 Z" fill="#e7b257" opacity="0.8"/>'
    return s


def cache_states():
    # скрытый: почти как стена, едва заметная царапина-подсказка
    render(OUT_ROOM, "wall_panel", PANEL_W, PANEL_H, _panel("#2e3946", "#26303b"))
    # X-Ray: каркас, пустота и силуэт ключа внутри
    xr = f'''<rect x="40" y="35" width="320" height="280" rx="10" fill="#0b2a1c" fill-opacity="0.75" stroke="#5cffa8" stroke-width="5" stroke-dasharray="14 8"/>
<rect x="90" y="85" width="220" height="180" rx="8" fill="#0f3d28" stroke="#9dffcf" stroke-width="4"/>
<g transform="rotate(-25 200 175)" fill="none" stroke="#d9ffe9" stroke-width="7" stroke-linecap="round">
<circle cx="150" cy="175" r="26"/><path d="M176 175 L 270 175 M240 175 L 240 196 M260 175 L 260 192"/></g>
<path d="M40 120 L 360 120 M40 230 L 360 230" stroke="#5cffa8" stroke-width="2" opacity="0.5"/>'''
    render(OUT_ROOM, "cache_xray", PANEL_W, PANEL_H, xr)
    # найден: видны щели и ручка, панель приоткрыта (тёплый отблеск)
    render(OUT_ROOM, "cache_closed", PANEL_W, PANEL_H,
           _panel("#5b6774", "#1a2129", screws=False, handle=True, ajar=True))
    # открыт: тёмная ниша, пусто
    op = f'''<rect x="40" y="35" width="320" height="280" rx="10" fill="#3a4450" stroke="{INK}" stroke-width="7"/>
<rect x="64" y="58" width="272" height="234" rx="6" fill="#0b0f14" stroke="{INK}" stroke-width="5"/>
<path d="M64 58 L 110 100 L 290 100 L 336 58 Z" fill="#141a21"/>
<rect x="110" y="200" width="180" height="12" rx="4" fill="#232b34"/>
<path d="M120 120 L 140 140 M260 130 L 280 120" stroke="#1f2730" stroke-width="4"/>'''
    render(OUT_ROOM, "cache_open", PANEL_W, PANEL_H, op)


def key():
    w = h = 256
    b = f'''<g transform="rotate(-35 128 128)">
<circle cx="70" cy="128" r="40" fill="#e1b84a" stroke="{INK}" stroke-width="7"/>
<circle cx="70" cy="128" r="17" fill="none" stroke="{INK}" stroke-width="6"/>
<rect x="104" y="116" width="120" height="24" rx="8" fill="#e1b84a" stroke="{INK}" stroke-width="7"/>
<rect x="180" y="134" width="16" height="30" rx="4" fill="#e1b84a" stroke="{INK}" stroke-width="6"/>
<rect x="204" y="134" width="16" height="22" rx="4" fill="#e1b84a" stroke="{INK}" stroke-width="6"/>
<path d="M50 104 C 60 96, 74 94, 86 98" fill="none" stroke="#fff3c4" stroke-width="7" stroke-linecap="round"/>
<path d="M112 122 L 210 122" stroke="#fff3c4" stroke-width="5" stroke-linecap="round"/></g>'''
    render(OUT_ROOM, "key", w, h, b)


DOOR_W, DOOR_H = 420, 860


def door(open_state):
    frame = f'<rect x="20" y="20" width="380" height="830" rx="10" fill="#3b4550" stroke="{INK}" stroke-width="8"/>'
    if not open_state:
        leaf = f'''<rect x="52" y="60" width="316" height="780" rx="6" fill="#5b6672" stroke="{INK}" stroke-width="7"/>
<rect x="76" y="90" width="268" height="300" rx="6" fill="#4d5762" stroke="#2e3640" stroke-width="5"/>
<rect x="76" y="420" width="268" height="300" rx="6" fill="#4d5762" stroke="#2e3640" stroke-width="5"/>
<rect x="290" y="420" width="50" height="110" rx="8" fill="#9aa5b0" stroke="{INK}" stroke-width="6"/>
<circle cx="315" cy="455" r="10" fill="{INK}"/><rect x="311" y="458" width="8" height="26" fill="{INK}"/>
<g>{"".join(f'<path d="M{52+i*40} 840 L {92+i*40} 760 L {112+i*40} 760 L {72+i*40} 840 Z" fill="#d6b03a"/>' for i in range(8))}</g>
<rect x="52" y="756" width="316" height="84" fill="none" stroke="{INK}" stroke-width="6"/>
<circle cx="210" cy="48" r="16" fill="#ff4b4b" stroke="{INK}" stroke-width="5"/><circle cx="205" cy="43" r="5" fill="#ffd0d0"/>'''
    else:
        leaf = f'''<defs><linearGradient id="cor" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0b1712"/><stop offset="1" stop-color="#173a2a"/></linearGradient></defs>
<rect x="52" y="60" width="316" height="780" fill="url(#cor)" stroke="{INK}" stroke-width="7"/>
<path d="M150 840 L 190 300 L 230 300 L 270 840 Z" fill="#1f5a3e" opacity="0.6"/>
<path d="M120 300 L 300 300" stroke="#7dffc0" stroke-width="6" opacity="0.7"/>
<path d="M52 60 L 120 110 L 120 800 L 52 840 Z" fill="#5b6672" stroke="{INK}" stroke-width="7"/>
<circle cx="210" cy="48" r="16" fill="#4bff8f" stroke="{INK}" stroke-width="5"/><circle cx="205" cy="43" r="5" fill="#d8ffe6"/>'''
    render(OUT_ROOM, "door_open" if open_state else "door_closed", DOOR_W, DOOR_H, frame + leaf)


# ---------------- эффекты ----------------
def fx():
    defs = '<radialGradient id="g" cx="50%" cy="50%" r="50%"><stop offset="0" stop-color="#fff" stop-opacity="1"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>'
    render(OUT_FX, "glow_soft", 256, 256, '<rect width="256" height="256" fill="url(#g)"/>', defs)
    star = ('<path d="M64 4 C 70 50, 78 58, 124 64 C 78 70, 70 78, 64 124 C 58 78, 50 70, 4 64 C 50 58, 58 50, 64 4 Z" '
            'fill="#fff6c8" stroke="#ffd34d" stroke-width="3"/><circle cx="64" cy="64" r="10" fill="#ffffff"/>')
    render(OUT_FX, "sparkle", 128, 128, star)
    # облачко пыли («ничего нет»)
    puff = "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#c9c2b6" stroke="#6f675c" stroke-width="5"/>'
                   for x, y, r in [(70, 150, 46), (130, 120, 58), (196, 150, 48), (110, 170, 40), (170, 176, 38)])
    puff += '<circle cx="118" cy="112" r="16" fill="#e9e4da"/><circle cx="182" cy="140" r="10" fill="#e9e4da"/>'
    render(OUT_FX, "dust_puff", 256, 220, puff)
    # пузырь для «?» (сам знак — текст Unity)
    bub = ('<path d="M128 14 C 196 14, 240 56, 240 110 C 240 164, 196 200, 140 202 L 112 238 L 116 200 '
           'C 58 194, 16 160, 16 110 C 16 56, 60 14, 128 14 Z" fill="#fdf8ec" stroke="#2b1a10" stroke-width="9" stroke-linejoin="round"/>')
    render(OUT_FX, "question_bubble", 256, 256, bub)


# ---------------- воспоминание ----------------
CLOUD_INK = "#5a3f22"


def memory():
    # облако-мысль
    w, h = 1200, 760
    bumps = [(230, 260, 160), (430, 195, 170), (650, 195, 170), (870, 230, 160), (1030, 370, 145),
             (940, 545, 155), (700, 590, 155), (460, 590, 155), (240, 530, 150), (165, 385, 130)]
    outline = "".join(f'<circle cx="{x}" cy="{y}" r="{r+8}" fill="{CLOUD_INK}"/>' for x, y, r in bumps)
    outline += f'<ellipse cx="600" cy="400" rx="470" ry="240" fill="{CLOUD_INK}"/>'
    fill = "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#fbefd6"/>' for x, y, r in bumps)
    fill += '<ellipse cx="600" cy="400" rx="462" ry="232" fill="#fbefd6"/>'
    shade = '<ellipse cx="620" cy="460" rx="420" ry="170" fill="#f1dcb4" opacity="0.6"/>'
    render(OUT_MEM, "memory_cloud", w, h, outline + fill + shade)
    render(OUT_MEM, "memory_bubble", 128, 128,
           f'<circle cx="64" cy="64" r="56" fill="#fbefd6" stroke="{CLOUD_INK}" stroke-width="8"/>')

    ink = CLOUD_INK
    icons = {
        "icon_alarm": f'''<rect x="78" y="180" width="100" height="36" rx="8" fill="#6b5a48" stroke="{ink}" stroke-width="7"/>
<path d="M88 180 C 88 110, 168 110, 168 180 Z" fill="#e2483d" stroke="{ink}" stroke-width="7"/>
<path d="M108 150 C 108 132, 120 124, 132 124" fill="none" stroke="#ffd1c8" stroke-width="7" stroke-linecap="round"/>
<path d="M40 110 L 70 128 M128 50 L 128 86 M216 110 L 186 128 M60 60 L 86 92 M196 60 L 170 92" stroke="#e2483d" stroke-width="9" stroke-linecap="round"/>''',
        "icon_hand_key": f'''<rect x="150" y="40" width="80" height="176" rx="6" fill="#c8b79a" stroke="{ink}" stroke-width="6"/>
<rect x="160" y="80" width="60" height="70" rx="4" fill="#3b2e22"/>
<path d="M30 190 C 30 150, 60 130, 100 128 L 150 120 C 164 118, 168 136, 154 140 L 120 146 L 160 150 C 172 152, 170 168, 158 168 L 110 170 C 90 200, 60 210, 30 190 Z" fill="#3a3a44" stroke="{ink}" stroke-width="6" stroke-linejoin="round"/>
<g transform="rotate(-10 170 112)"><circle cx="150" cy="112" r="14" fill="#e1b84a" stroke="{ink}" stroke-width="5"/><rect x="162" y="106" width="46" height="12" rx="4" fill="#e1b84a" stroke="{ink}" stroke-width="5"/></g>''',
        "icon_pass_fall": f'''<rect x="20" y="196" width="216" height="26" rx="6" fill="#8a7a66" stroke="{ink}" stroke-width="6"/>
<g transform="rotate(18 128 120)"><rect x="70" y="80" width="116" height="74" rx="8" fill="#e8dfc8" stroke="{ink}" stroke-width="6"/>
<rect x="70" y="80" width="116" height="16" fill="#3f8f6a"/><rect x="82" y="104" width="30" height="36" rx="3" fill="#8d8a80"/></g>
<path d="M60 40 L 70 80 M100 24 L 106 66 M150 30 L 146 70" stroke="{ink}" stroke-width="7" stroke-linecap="round" opacity="0.6"/>''',
        "icon_steps": "".join(
            f'<g transform="translate({x} {y}) rotate({a})"><ellipse cx="0" cy="0" rx="18" ry="28" fill="{ink}"/>'
            f'<ellipse cx="0" cy="40" rx="13" ry="14" fill="{ink}"/></g>'
            for x, y, a in [(60, 190, -10), (110, 140, 10), (150, 100, -10), (200, 50, 10)]),
    }
    for name, body in icons.items():
        render(OUT_MEM, name, 256, 256, body)


if __name__ == "__main__":
    background(); table(); vent(); pass_card(); cache_states(); key()
    door(False); door(True); fx(); memory()
    print("ok")
