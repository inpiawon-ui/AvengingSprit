"""총구 화염이 캔버스 오른쪽 끝에서 잘린 두 장을 고친다.

`se_atk1`·`ne_atk1` 의 화염이 x=95 까지 닿아 평평하게 잘렸다. 재보니 **딱 1px
초과**라, 화염 덩어리만 왼쪽으로 1px 옮기면 모양을 그대로 두고 가장자리에서
뗄 수 있다. 다시 그릴 필요가 없다.

주의 — 화염 색(#F0B428 등)은 캐릭터 장비에도 쓰인다. 색만 보고 옮기면 몸의
호박색 부분까지 딸려 간다. 그래서 **x=95 에 닿은 화염에서 시작해 이웃으로
번지는 덩어리**만 골라낸다. 총구 밖 화염만 잡히고 몸은 건드리지 않는다.

옮긴 자리에 총구 끝 픽셀이 있으면 화염이 덮는다 — 화염은 총구에서 나오므로
겹치는 게 맞다.
"""
import os
import shutil
from collections import deque

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '_exchange', 'in')
BAK = os.path.join(IN, '_raw_flash')

TARGETS = ['unit_rambo_se_atk1', 'unit_rambo_ne_atk1']
FLASH = {(255, 248, 190), (255, 220, 48), (240, 180, 40), (232, 108, 24), (190, 118, 36)}
SHIFT = 1


def flash_blob(px, w, h):
    """x=95 에 닿은 화염에서 시작해 이웃 화염으로 번진 덩어리."""
    seen = set()
    q = deque((w - 1, y) for y in range(h)
              if px[w - 1, y][3] > 8 and px[w - 1, y][:3] in FLASH)
    seen.update(q)
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen \
               and px[nx, ny][3] > 8 and px[nx, ny][:3] in FLASH:
                seen.add((nx, ny))
                q.append((nx, ny))
    return seen


def main():
    os.makedirs(BAK, exist_ok=True)
    for name in TARGETS:
        p = os.path.join(IN, name + '.png')
        raw = os.path.join(BAK, name + '.png')
        if not os.path.exists(raw):
            shutil.copy2(p, raw)

        im = Image.open(raw).convert('RGBA')
        px = im.load()
        w, h = im.size
        blob = flash_blob(px, w, h)
        if not blob:
            print(f'{name}: 가장자리 화염 없음 — 건너뜀')
            continue

        out = im.copy()
        op = out.load()
        for x, y in blob:                       # 원래 자리를 비우고
            op[x, y] = (0, 0, 0, 0)
        for x, y in sorted(blob):               # 1px 왼쪽에 다시 찍는다
            op[x - SHIFT, y] = px[x, y]
        out.save(p)

        rp = out.load()
        edge = sum(1 for y in range(h) if rp[w - 1, y][3] > 8)
        xs = [x for x, y in blob]
        print(f'{name}: 화염 {len(blob)}px (x={min(xs)}~{max(xs)}) → '
              f'{SHIFT}px 왼쪽. 우측 끝 접촉 {edge}px')


if __name__ == '__main__':
    main()
