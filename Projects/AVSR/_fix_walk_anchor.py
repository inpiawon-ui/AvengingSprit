"""걷기 2프레임의 가로 흔들림을 잡는다. 그림은 건드리지 않고 평행이동만 한다.

걷기는 **발이 움직이고 몸통은 제자리**여야 한다. 실제 이동은 트랜스폼이 하므로
스프라이트 안에서 몸통이 좌우로 흔들리면 걸을 때마다 캐릭터가 비틀거린다.

기준을 발 중심으로 잡으면 안 된다 — 걸을 때 발은 원래 번갈아 벌어진다.
**머리 밴드(위 20줄)의 가로 중심**을 그 방향 idle 과 맞춘다. 쿼터뷰라 머리 중심은
방향마다 다르므로(람보 idle 기준 43~54) 절대값이 아니라 **같은 방향 idle 대비**로 잰다.

사망은 손대지 않는다. 무릎에서 옆으로 무너지며 몸이 쏠리는 것은 정상이다.

사용: python _fix_walk_anchor.py <키> [키 ...]
"""
import io
import os
import shutil
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IN = os.path.join(HERE, '_exchange', 'in')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')

DIRS = ['s', 'se', 'e', 'ne', 'n']
WALK = ['walk1', 'walk2']
HEAD_BAND = 20
DEADZONE = 3      # 3px 이하는 그냥 둔다. 눈에 안 보이는 것까지 옮기면 그림만 상한다


def find(key, name):
    for base in (IN, os.path.join(UNIT, key)):
        p = os.path.join(base, name)
        if os.path.exists(p):
            return p
    return None


def head_center(im):
    px = im.load()
    w, h = im.size
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 8]
    if not pts:
        return None, None, None
    y0 = min(p[1] for p in pts)
    head = [x for x, y in pts if y <= y0 + HEAD_BAND]
    return ((min(head) + max(head)) / 2,
            min(p[0] for p in pts), max(p[0] for p in pts))


def main():
    keys = sys.argv[1:]
    if not keys:
        print('키를 지정할 것: python _fix_walk_anchor.py thug')
        return

    for key in keys:
        print(f'\n=== {key} ===')
        bak = os.path.join(IN, '_raw_walk')
        os.makedirs(bak, exist_ok=True)
        for d in DIRS:
            base = find(key, f'unit_{key}_{d}.png')
            if base is None:
                print(f'  {d}: idle 없음 — 건너뜀')
                continue
            ref = head_center(Image.open(base).convert('RGBA'))[0]

            for f in WALK:
                name = f'unit_{key}_{d}_{f}.png'
                p = find(key, name)
                if p is None:
                    continue
                raw = os.path.join(bak, name)
                if not os.path.exists(raw):
                    shutil.copy2(p, raw)      # 원본 1회 보관 — 재실행해도 누적 이동 없음

                im = Image.open(raw).convert('RGBA')
                hc, x0, x1 = head_center(im)
                dx = int(round(ref - hc))
                if abs(dx) <= DEADZONE:
                    shutil.copy2(raw, p)
                    print(f'  {d}_{f}: 머리중심 {hc:5.1f} (idle {ref:.1f})  그대로')
                    continue
                if x0 + dx < 0 or x1 + dx > im.size[0] - 1:
                    shutil.copy2(raw, p)
                    print(f'  {d}_{f}: {dx:+d} 하면 잘린다 — 건너뜀')
                    continue

                out = Image.new('RGBA', im.size, (0, 0, 0, 0))
                out.paste(im, (dx, 0), im)
                out.save(p)
                print(f'  {d}_{f}: 머리중심 {hc:5.1f} → {head_center(out)[0]:.1f}  '
                      f'이동 {dx:+d}px')


if __name__ == '__main__':
    main()
