"""통일 카드판 4등급 → Assets/BaseResource/Card/cardpanel_{등급}.png + cardcrown_legendary.png (2026-10-12).

PD 「카드 프레임들이 다 다른데 통일하는 게 낫지 않아?」 — 시안 in/mock_cardframe_unified.png, 납품 in/tiers_part_cardpanels.png(분홍 바탕).
- 네 장의 몸통(분홍 아닌 영역에서 폭이 넓은 줄부터)을 같은 상자로 잘라 190x350 으로 맞춘다(가장 가까운 점).
- 전설 왕관은 카드 위로 튀어나와 몸통과 따로 뗀다 — 카드 크기를 네 장 똑같이 두고, 게임이 왕관을 카드 위에 얹는다.
  왕관 그림에는 몸통 윗부분 몇 줄이 같이 들어 있다(같은 점이라 겹쳐도 이어진다).
- 옛 카드판은 지우지 않고 Projects/AVSR/_archive/cardpanel_20261012/ 로 옮겨 둔다(처음 한 번).
python cut_cardpanels.py → 몸통 상자 · 왕관 자리(카드 칸 162x320 기준)를 출력
"""
import os
import shutil

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in', 'tiers_part_cardpanels.png')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'Card')
ARCHIVE = os.path.join(ROOT, 'Projects', 'AVSR', '_archive', 'cardpanel_20261012')
NAMES = ['common', 'rare', 'epic', 'legendary']
OUT = (190, 350)
CARD = (162, 320)   # 게임 카드 칸


def key_magenta(rgb):
    a = np.asarray(rgb).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mag = (r > 170) & (b > 170) & (g < 110)
    out = np.dstack([a, np.where(mag, 0, 255)]).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')


def main():
    sheet = Image.open(SRC).convert('RGB')
    a = np.asarray(sheet).astype(int)
    solid = ~((a[..., 0] > 170) & (a[..., 2] > 170) & (a[..., 1] < 110))
    boxes = []
    for i in range(4):
        sub = solid[:, i * 400:(i + 1) * 400]
        widths = sub.sum(axis=1)
        top = int(np.argmax(widths > 200))
        bottom = int(len(widths) - np.argmax(widths[::-1] > 200))
        xs = np.where(sub[top:bottom].any(axis=0))[0]
        boxes.append((i * 400 + int(xs[0]), top, i * 400 + int(xs[-1]) + 1, bottom))
    # 네 장 같은 크기로 — 가장 큰 폭 · 높이로 가운데 맞춤
    w = max(b[2] - b[0] for b in boxes)
    h = max(b[3] - b[1] for b in boxes)
    if not os.path.exists(ARCHIVE):
        os.makedirs(ARCHIVE)
        for n in NAMES:
            for ext in ('.png', '.png.meta'):
                p = os.path.join(DST, 'cardpanel_%s%s' % (n, ext))
                if os.path.exists(p):
                    shutil.copyfile(p, os.path.join(ARCHIVE, 'cardpanel_%s%s' % (n, ext)))
    for n, (x0, y0, x1, y1) in zip(NAMES, boxes):
        cx = (x0 + x1) // 2
        box = (cx - w // 2, y0, cx - w // 2 + w, y0 + h)
        key_magenta(sheet.crop(box)).resize(OUT, Image.NEAREST).save(os.path.join(DST, 'cardpanel_%s.png' % n))
        print(n, 'body', box)
    # 왕관 — 전설 칸에서 몸통 윗변 위로 튀어나온 부분의 가로 범위 + 몸통 윗줄 조금
    x0, y0, x1, y1 = boxes[3]
    cx = (x0 + x1) // 2
    body_left = cx - w // 2
    above = solid[:y0, x0:x1]
    ys = np.where(above.any(axis=1))[0]
    xs = np.where(above.any(axis=0))[0]
    overlap = 40   # 왕관 받침이 몸통 윗테에 앉은 깊이(납품 그림 실측)
    crown = (x0 + int(xs[0]), int(ys[0]), x0 + int(xs[-1]) + 1, y0 + overlap)
    sx, sy = OUT[0] / w, OUT[1] / h
    cw, ch = round((crown[2] - crown[0]) * sx), round((crown[3] - crown[1]) * sy)
    key_magenta(sheet.crop(crown)).resize((cw, ch), Image.NEAREST).save(os.path.join(DST, 'cardcrown_legendary.png'))
    kx, ky = CARD[0] / w, CARD[1] / h
    print('crown', crown, 'sprite', (cw, ch),
          'card-pos x %.1f y %.1f w %.1f h %.1f (카드 왼쪽 위 기준, y 아래로 +)' % (
              (crown[0] - body_left) * kx, (crown[1] - y0) * ky, (crown[2] - crown[0]) * kx, (crown[3] - crown[1]) * ky))


if __name__ == '__main__':
    main()
