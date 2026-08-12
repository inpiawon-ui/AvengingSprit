"""원작 시스템 폰트 → TMP 가 읽을 아틀라스 PNG.

원작 시트 아래쪽에 16칸 × 4줄짜리 ASCII 격자가 있다(0x20~0x5F).
글자는 회색 타일 위에 흰색(#FFFFF7)으로 그려져 있으므로, 흰 픽셀만 남기고
나머지를 투명으로 만들어 폰트 아틀라스를 만든다.

원작 폰트는 **영문 대문자·숫자·기호만** 있다. 한글은 없다.
그래서 이 폰트는 TMP 의 주 폰트로 쓰고 한글은 기존 폰트를 대체(fallback)로 둔다 —
HUD 의 GHOST·HOST·STAGE·숫자처럼 픽셀 느낌이 가장 중요한 곳이 전부 영문·숫자다.

사용: python _orig_font.py
산출: Assets/BaseResource/Fonts/orig_font_atlas.png (+ 셀 정보 JSON)
"""
import io
import json
import os
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHEET = os.path.join(HERE, 'Reference', 'Original', 'Miscellaneous - Fonts.png')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts')

# 시트 안의 ASCII 격자
ORIGIN = (15, 271 + 2 * 9)   # 기호 줄(0x20)이 시작되는 자리
CELL = (9, 9)
COLS = 16
ROWS = 4                     # 0x20 ~ 0x5F
FIRST = 0x20
GLYPH = (0xFF, 0xFF, 0xF7)   # 글자색
TOL = 40


def near(a, b):
    return (abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2])) <= TOL


def main():
    os.makedirs(OUT, exist_ok=True)
    im = Image.open(SHEET).convert('RGB')
    px = im.load()
    cw, ch = CELL

    atlas = Image.new('RGBA', (COLS * cw, ROWS * ch), (255, 255, 255, 0))
    ap = atlas.load()

    glyphs = []
    for r in range(ROWS):
        for c in range(COLS):
            sx = ORIGIN[0] + c * cw
            sy = ORIGIN[1] + r * ch
            ink = 0
            # 글자가 실제로 차지하는 가로 범위 — 폭이 좁은 글자(I, .)를 좁게 쓰기 위함
            xs = []
            for y in range(ch):
                for x in range(cw):
                    if near(px[sx + x, sy + y], GLYPH):
                        ap[c * cw + x, r * ch + y] = (255, 255, 255, 255)
                        ink += 1
                        xs.append(x)
            code = FIRST + r * COLS + c
            if ink == 0:
                # 빈 칸 = 공백. 폭만 준다.
                glyphs.append(dict(code=code, x=c * cw, y=r * ch, w=cw, h=ch,
                                   advance=cw, blank=True))
                continue
            x0, x1 = min(xs), max(xs)
            glyphs.append(dict(code=code, x=c * cw, y=r * ch, w=cw, h=ch,
                               left=x0, right=x1, advance=x1 - x0 + 2, blank=False))

    atlas.save(os.path.join(OUT, 'orig_font_atlas.png'))
    meta = dict(cell=[cw, ch], cols=COLS, rows=ROWS, first=FIRST,
                atlas=[atlas.width, atlas.height], glyphs=glyphs)
    io.open(os.path.join(OUT, 'orig_font_atlas.json'), 'w', encoding='utf-8').write(
        json.dumps(meta, ensure_ascii=False, indent=1))

    used = sum(1 for g in glyphs if not g['blank'])
    print(f'아틀라스 {atlas.width}x{atlas.height} · 글자 {used}자 (빈 칸 {len(glyphs)-used})')
    print(f'→ {OUT}')


if __name__ == '__main__':
    main()
