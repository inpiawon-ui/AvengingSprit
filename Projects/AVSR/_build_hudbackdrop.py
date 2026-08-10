"""인게임 상단 HUD 배경판을 만든다.

HUD 를 1.5배로 키우면서 높이가 148 → 312px 이 됐다. 그 자리가 통짜 검정이라
화면 위쪽에 빈 띠가 크게 남았다.

목업에서 기계실 텍스처를 떠다 타일링해봤지만 버렸다. 목업의 HUD 영역은 글자와
게이지가 이미 그려져 있어 쓸 수 있는 구간이 200x30 밖에 안 되고, 그걸 되풀이하면
같은 무늬가 격자로 드러난다. 흐리게 뭉개도 얼룩진 격자가 남았다.

**목업에는 250px 짜리 HUD 패널이 애초에 없다.** 그 높이는 1.5배 확대에서 나온
것이다. 그래서 목업을 억지로 늘리는 대신, 이 게임의 다른 UI 와 같은 언어로 짠다 —
호스트 슬롯·얼티밋 카드·팁 바가 모두 어두운 남색 판에 밝은 테두리다.
"""
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IG = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

W, H = 720, 312                   # _layout_ingame.py 의 HUD_H(250) × 1.25

TOP = (10, 13, 22)                # 위가 더 어둡다 — 화면 가장자리로 눈이 안 끌리게
BOT = (21, 27, 42)
SEAM = (30, 38, 57)               # 금속판 이음매
EDGE = (86, 104, 142)             # 아래 경계 — 전투 필드와 갈라준다


def main():
    im = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(im)

    for y in range(H):
        t = y / (H - 1)
        d.line([0, y, W, y],
               fill=(round(TOP[0] + (BOT[0] - TOP[0]) * t),
                     round(TOP[1] + (BOT[1] - TOP[1]) * t),
                     round(TOP[2] + (BOT[2] - TOP[2]) * t), 255))

    # 행 구분 이음매 — 고스트 / 호스트 / 보스 세 행의 경계에 맞춘다
    for y in (round(76 * 1.25), round(178 * 1.25)):
        d.line([0, y, W, y], fill=SEAM + (255,))

    # 좌우 끝을 살짝 눌러 가운데로 시선을 모은다
    px = im.load()
    for x in range(W):
        edge = min(x, W - 1 - x) / 90.0
        if edge >= 1.0:
            continue
        k = 0.72 + 0.28 * edge
        for y in range(H):
            r, g, b, a = px[x, y]
            px[x, y] = (round(r * k), round(g * k), round(b * k), a)

    d.line([0, H - 2, W, H - 2], fill=EDGE + (255,))
    d.line([0, H - 1, W, H - 1], fill=(12, 15, 24, 255))

    out = os.path.join(IG, 'hudbackdrop.png')
    im.save(out)
    print(f'  hudbackdrop {im.size} → {out}')


if __name__ == '__main__':
    main()
