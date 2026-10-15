# -*- coding: utf-8 -*-
"""시안(941 × 1672) 과 게임 캡처(720 × 1280)를 같은 배율로 구역별로 잘라 나란히 붙인다 — 요소 단위 대조(2026-10-15).

  python Projects/AVSR/Tools/mock_vs_game.py <시안.png> <게임.png> <출력.png> [구역 이름=y0:y1 ...]
구역은 게임 좌표(720 기준). 기본: 위 막대 · 챕터 · 출전 방식 · 호스트 줄 · 설명 · 도전하기 · 아래 바.
"""
import sys

from PIL import Image, ImageDraw, ImageFont

FONT = 'C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Fonts/NotoSansKR-Bold.ttf'
DEFAULT = [('위 막대', 0, 125), ('챕터 · 보스 · 보상', 100, 455), ('출전 방식', 450, 750), ('호스트 줄', 745, 970),
           ('설명 · 도전하기', 960, 1135), ('아래 바', 1125, 1280)]


def main():
    mock_p, game_p, out_p = sys.argv[1:4]
    regions = DEFAULT
    if len(sys.argv) > 4:
        regions = []
        for a in sys.argv[4:]:
            n, r = a.split('=')
            y0, y1 = r.split(':')
            regions.append((n, int(y0), int(y1)))
    mock = Image.open(mock_p).convert('RGB')
    mock = mock.crop((0, 0, 941, min(mock.height, 1672))).resize((720, 1280), Image.LANCZOS)
    game = Image.open(game_p).convert('RGB')
    if game.size != (720, 1280):
        game = game.resize((720, 1280), Image.LANCZOS)
    s = 2   # 2배로 키워 잔 차이가 보이게
    f = ImageFont.truetype(FONT, 22)
    rows = []
    for name, y0, y1 in regions:
        a = mock.crop((0, y0, 720, y1)).resize((720 * s // 2 * 2 // 2 * 1, (y1 - y0)), Image.NEAREST)
        b = game.crop((0, y0, 720, y1))
        row = Image.new('RGB', (720 * 2 + 30, (y1 - y0) + 34), (18, 20, 34))
        d = ImageDraw.Draw(row)
        d.text((4, 4), f'{name}  (y {y0}~{y1})   시안 | 게임', font=f, fill=(255, 220, 120))
        row.paste(a, (0, 34))
        row.paste(b, (750, 34))
        rows.append(row)
    W = max(r.width for r in rows)
    H = sum(r.height for r in rows)
    sheet = Image.new('RGB', (W, H), (18, 20, 34))
    y = 0
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    sheet.save(out_p)
    print(out_p, sheet.size)


if __name__ == '__main__':
    main()
