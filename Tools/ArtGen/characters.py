"""Оригинальные персонажи-заглушки проекта: кот-герой (слот «Бискас») и кот-компаньон (слот «Врискас»).
Это НЕ копии дизайна из сериала — временные собственные рисунки до утверждения референса.

Все части рисуются на общем холсте 512x512 в итоговых позициях: в Unity их накладывают друг на друга
и вращают вокруг суставов (хвост 318,440; тело 256,486; шея 256,290 — координаты холста, y вниз).
Запуск из корня проекта:  python Tools/ArtGen/characters.py   (нужен pip install cairosvg)
"""
import os
import cairosvg

ROOT = "Assets/_Game/Art/Characters"
SRC = "Tools/ArtGen/svg"
W = 6  # толщина контура

PALETTES = {
    "vriskas": dict(INK="#18261c", BASE="#5aa863", SHADE="#3c7a47", LIGHT="#8fd08f", CHEST="#b6e6ad",
                    EAR_IN="#2f5e3a", EYE="#e3f25c", EYE_RIM="#9fb52c", PUPIL="#0f1a12", MARK="#3c7a47",
                    squint=True, round_pupil=False, tail_rings=False, white_paws=False, folder="Vriskas"),
    "biskas": dict(INK="#2b1a10", BASE="#e58a3a", SHADE="#b8622a", LIGHT="#f7bd7c", CHEST="#fff0dc",
                   EAR_IN="#b8664a", EYE="#9ad46e", EYE_RIM="#5a9c40", PUPIL="#1a120c", MARK="#b8622a",
                   squint=False, round_pupil=True, tail_rings=True, white_paws=True, folder="Biskas"),
}


def svg(body, defs=""):
    return ('<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512">'
            f'<defs>{defs}</defs>{body}</svg>')


TAIL_PATH = "M318 440 C 400 450, 455 400, 440 320 C 432 275, 455 240, 478 228"
BODY_SHAPE = ("M190 300 C 150 340, 140 430, 170 478 L 342 478 C 372 430, 362 340, 322 300 "
              "C 290 278, 222 278, 190 300 Z")
HEAD_SHAPE = ("M150 205 C 150 140, 200 112, 256 112 C 312 112, 362 140, 362 205 "
              "C 362 262, 314 296, 256 296 C 198 296, 150 262, 150 205 Z")


def tail(p):
    rings = ""
    if p["tail_rings"]:
        # поперечные полосы вдоль хвоста
        for (x, y, a) in [(370, 448, 80), (420, 420, 50), (443, 372, 15), (440, 318, -5), (446, 278, -30)]:
            rings += (f'<rect x="{x-6}" y="{y-20}" width="12" height="40" rx="5" fill="{p["MARK"]}" '
                      f'transform="rotate({a} {x} {y})"/>')
    tip = p["CHEST"] if p["tail_rings"] else p["SHADE"]
    return f'''
<path d="{TAIL_PATH}" fill="none" stroke="{p["INK"]}" stroke-width="{36+2*W}" stroke-linecap="round"/>
<path d="{TAIL_PATH}" fill="none" stroke="{p["BASE"]}" stroke-width="36" stroke-linecap="round"/>
<path d="M330 452 C 405 458, 452 410, 446 340" fill="none" stroke="{p["SHADE"]}" stroke-width="12" stroke-linecap="round" opacity="0.9"/>
{rings}
<path d="M455 252 C 462 240, 470 233, 478 228" fill="none" stroke="{tip}" stroke-width="28" stroke-linecap="round"/>'''


def body(p):
    paw_front_l = p["CHEST"] if p["white_paws"] else p["LIGHT"]
    paw_front_r = p["CHEST"] if p["white_paws"] else p["BASE"]
    stripes = ""
    if p["tail_rings"]:
        stripes = (f'<path d="M168 350 C 182 356, 192 368, 196 380 M162 392 C 176 396, 186 406, 190 418 '
                   f'M340 352 C 330 358, 322 368, 318 380" fill="none" stroke="{p["MARK"]}" stroke-width="9" stroke-linecap="round"/>')
    return f'''
<path d="{BODY_SHAPE}" fill="{p["BASE"]}" stroke="{p["INK"]}" stroke-width="{W}" stroke-linejoin="round"/>
<clipPath id="bc"><path d="{BODY_SHAPE}"/></clipPath>
<g clip-path="url(#bc)">
  <path d="M300 280 C 360 330, 372 420, 350 490 L 420 490 L 420 280 Z" fill="{p["SHADE"]}"/>
  <path d="M220 300 C 200 350, 205 420, 230 478 L 282 478 C 306 420, 310 350, 292 300 Z" fill="{p["CHEST"]}"/>
  <path d="M168 360 C 180 330, 196 315, 214 306" fill="none" stroke="{p["LIGHT"]}" stroke-width="8" stroke-linecap="round"/>
  {stripes}
</g>
<ellipse cx="172" cy="458" rx="40" ry="30" fill="{p["BASE"]}" stroke="{p["INK"]}" stroke-width="{W}"/>
<ellipse cx="340" cy="458" rx="40" ry="30" fill="{p["SHADE"]}" stroke="{p["INK"]}" stroke-width="{W}"/>
<path d="M196 372 C 192 410, 192 452, 194 484 L 232 484 C 234 452, 236 410, 236 372 C 232 352, 200 352, 196 372 Z" fill="{p["LIGHT"]}" stroke="{p["INK"]}" stroke-width="{W}" stroke-linejoin="round"/>
<path d="M276 372 C 276 410, 278 452, 280 484 L 318 484 C 320 452, 320 410, 316 372 C 312 352, 280 352, 276 372 Z" fill="{p["BASE"]}" stroke="{p["INK"]}" stroke-width="{W}" stroke-linejoin="round"/>
<ellipse cx="212" cy="488" rx="27" ry="14" fill="{paw_front_l}" stroke="{p["INK"]}" stroke-width="{W}"/>
<ellipse cx="300" cy="488" rx="27" ry="14" fill="{paw_front_r}" stroke="{p["INK"]}" stroke-width="{W}"/>
<path d="M203 486 L203 497 M221 486 L221 497 M291 486 L291 497 M309 486 L309 497" stroke="{p["INK"]}" stroke-width="3" stroke-linecap="round"/>'''


def head(p):
    if p["tail_rings"]:
        marks = (f'<path d="M226 120 C 232 132, 236 142, 238 152 M256 116 L 256 152 M286 120 C 280 132, 276 142, 274 152 '
                 f'M156 196 C 168 196, 178 200, 186 206 M356 196 C 344 196, 334 200, 326 206" '
                 f'fill="none" stroke="{p["MARK"]}" stroke-width="8" stroke-linecap="round"/>')
        muzzle = f'<path d="M188 244 C 214 300, 298 300, 324 244 C 304 226, 208 226, 188 244 Z" fill="{p["CHEST"]}"/>'
    else:
        marks = (f'<path d="M256 118 L 256 150 M232 122 L 238 146 M280 122 L 274 146" '
                 f'stroke="{p["MARK"]}" stroke-width="8" stroke-linecap="round"/>')
        muzzle = f'<path d="M196 248 C 220 290, 292 290, 316 248 C 300 236, 212 236, 196 248 Z" fill="{p["CHEST"]}"/>'
    return f'''
<path d="M168 168 L 160 62 L 238 122 Z" fill="{p["BASE"]}" stroke="{p["INK"]}" stroke-width="{W}" stroke-linejoin="round"/>
<path d="M180 150 L 175 90 L 222 126 Z" fill="{p["EAR_IN"]}"/>
<path d="M344 168 L 352 62 L 274 122 Z" fill="{p["SHADE"]}" stroke="{p["INK"]}" stroke-width="{W}" stroke-linejoin="round"/>
<path d="M332 150 L 337 90 L 290 126 Z" fill="{p["EAR_IN"]}"/>
<path d="{HEAD_SHAPE}" fill="{p["BASE"]}" stroke="{p["INK"]}" stroke-width="{W}"/>
<clipPath id="hc"><path d="{HEAD_SHAPE}"/></clipPath>
<g clip-path="url(#hc)">
  <path d="M300 100 C 360 150, 370 240, 320 300 L 380 300 L 380 100 Z" fill="{p["SHADE"]}"/>
  {muzzle}
  {marks}
</g>
<path d="M170 180 C 176 150, 196 132, 218 124" fill="none" stroke="{p["LIGHT"]}" stroke-width="7" stroke-linecap="round"/>
<path d="M246 238 L 266 238 L 256 250 Z" fill="#3a2a2a" stroke="{p["INK"]}" stroke-width="3" stroke-linejoin="round"/>
<path d="M256 250 C 252 262, 240 264, 234 258 M256 250 C 260 262, 272 264, 278 258" fill="none" stroke="{p["INK"]}" stroke-width="4" stroke-linecap="round"/>
<path d="M196 240 L 130 230 M198 252 L 134 258 M316 240 L 382 230 M314 252 L 378 258" stroke="{p["INK"]}" stroke-width="3" stroke-linecap="round" opacity="0.8"/>'''


def eye(p, cx, cy):
    if p["round_pupil"]:
        pupil = f'<circle cx="{cx+2}" cy="{cy+1}" r="12" fill="{p["PUPIL"]}"/>'
        shape = (f'M{cx-28} {cy} C {cx-24} {cy-26}, {cx+24} {cy-26}, {cx+28} {cy} '
                 f'C {cx+24} {cy+24}, {cx-24} {cy+24}, {cx-28} {cy} Z')
    else:
        pupil = f'<ellipse cx="{cx+2}" cy="{cy}" rx="6" ry="16" fill="{p["PUPIL"]}"/>'
        shape = (f'M{cx-30} {cy} C {cx-18} {cy-24}, {cx+18} {cy-24}, {cx+30} {cy} '
                 f'C {cx+18} {cy+22}, {cx-18} {cy+22}, {cx-30} {cy} Z')
    s = f'''
<path d="{shape}" fill="{p["EYE"]}" stroke="{p["INK"]}" stroke-width="5"/>
<path d="M{cx-24} {cy+6} C {cx-12} {cy+18}, {cx+12} {cy+18}, {cx+24} {cy+6}" fill="none" stroke="{p["EYE_RIM"]}" stroke-width="5" opacity="0.8"/>
{pupil}
<circle cx="{cx-7}" cy="{cy-8}" r="5" fill="#ffffff"/>'''
    if p["squint"]:
        s += f'''
<path d="M{cx-33} {cy-4} C {cx-18} {cy-22}, {cx+18} {cy-26}, {cx+33} {cy-8} L {cx+34} {cy-26} L {cx-34} {cy-26} Z" fill="{p["BASE"]}"/>
<path d="M{cx-31} {cy-4} C {cx-16} {cy-20}, {cx+16} {cy-24}, {cx+31} {cy-8}" fill="none" stroke="{p["INK"]}" stroke-width="5" stroke-linecap="round"/>'''
    else:
        # приподнятые «брови» — открытый, внимательный взгляд
        s += f'<path d="M{cx-22} {cy-34} C {cx-10} {cy-40}, {cx+10} {cy-40}, {cx+22} {cy-34}" fill="none" stroke="{p["INK"]}" stroke-width="4" stroke-linecap="round" opacity="0.7"/>'
    return s


def lids(p):
    out = ""
    for cx in (214, 298):
        cy = 198
        out += f'''
<path d="M{cx-33} {cy} C {cx-18} {cy-29}, {cx+18} {cy-29}, {cx+33} {cy} C {cx+18} {cy+27}, {cx-18} {cy+27}, {cx-33} {cy} Z" fill="{p["BASE"]}"/>
<path d="M{cx-30} {cy+2} C {cx-14} {cy+14}, {cx+14} {cy+14}, {cx+30} {cy+2}" fill="none" stroke="{p["INK"]}" stroke-width="5" stroke-linecap="round"/>'''
    return out


def mouth(p):
    return f'''
<path d="M238 254 C 240 280, 272 280, 274 254 C 264 260, 248 260, 238 254 Z" fill="#3a1d22" stroke="{p["INK"]}" stroke-width="4" stroke-linejoin="round"/>
<path d="M246 268 C 252 274, 262 274, 266 268" fill="none" stroke="#c96a78" stroke-width="5" stroke-linecap="round"/>
<path d="M246 238 L 266 238 L 256 250 Z" fill="#3a2a2a" stroke="{p["INK"]}" stroke-width="3" stroke-linejoin="round"/>'''


def glow(color_mid):
    defs = (f'<radialGradient id="g" cx="50%" cy="50%" r="50%"><stop offset="0" stop-color="#ffffff" stop-opacity="1"/>'
            f'<stop offset="0.35" stop-color="{color_mid}" stop-opacity="0.85"/>'
            f'<stop offset="1" stop-color="{color_mid}" stop-opacity="0"/></radialGradient>')
    body = '<ellipse cx="214" cy="198" rx="62" ry="46" fill="url(#g)"/><ellipse cx="298" cy="198" rx="62" ry="46" fill="url(#g)"/>'
    return body, defs


def build(name):
    p = PALETTES[name]
    out = os.path.join(ROOT, p["folder"])
    os.makedirs(out, exist_ok=True)
    os.makedirs(SRC, exist_ok=True)
    gbody, gdefs = glow("#5cffa8" if name == "vriskas" else "#ffd36b")
    layers = {
        "tail": (tail(p), ""),
        "body": (body(p), ""),
        "head": (head(p), ""),
        "eyes_open": (eye(p, 214, 198) + eye(p, 298, 198), ""),
        "eyes_closed": (lids(p), ""),
        "mouth_talk": (mouth(p), ""),
        "eyes_glow": (gbody, gdefs),
    }
    for part, (b, d) in layers.items():
        doc = svg(b, d)
        fn = f"{name}_{part}"
        with open(os.path.join(SRC, fn + ".svg"), "w") as f:
            f.write(doc)
        cairosvg.svg2png(bytestring=doc.encode(), write_to=os.path.join(out, fn + ".png"),
                         output_width=512, output_height=512)
    print(name, "→", out)


if __name__ == "__main__":
    for n in PALETTES:
        build(n)
