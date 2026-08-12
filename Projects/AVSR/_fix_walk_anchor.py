"""동작 프레임의 가로 어긋남을 잡는다. 그림은 건드리지 않고 평행이동만 한다.

기준이 동작마다 다르다.
  걷기        발이 번갈아 벌어지는 게 정상이다. 흔들리면 안 되는 건 몸통 →
              **머리 밴드(위 20줄)의 가로 중심**을 그 방향 idle 과 맞춘다
  공격·피격   발을 붙인 채 상체만 움직이는 동작이다 → **발 중심**을 48 로 맞춘다

하나의 기준으로 전부 재면 멀쩡한 것을 반려하거나 어긋난 것을 놓친다.

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
PLANTED = ['atk1', 'atk2', 'hit']     # 발을 붙이고 하는 동작
HEAD_BAND = 20
FOOT_BAND = 6
ANCHOR = 48
ELITE_ANCHOR = 64      # 엘리트는 캔버스 128 이라 반전축도 64 다


def anchor_of(key):
    return ELITE_ANCHOR if key.endswith('_elite') else ANCHOR
DEADZONE = 1      # 1px 이하는 그냥 둔다. 눈에 안 보이는 것까지 옮기면 그림만 상한다


def find(key, name):
    for base in (IN, os.path.join(UNIT, key)):
        p = os.path.join(base, name)
        if os.path.exists(p):
            return p
    return None


def foot_center(im):
    px = im.load()
    w, h = im.size
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 8]
    if not pts:
        return None, None, None
    y1 = max(p[1] for p in pts)
    feet = [x for x, y in pts if y >= y1 - FOOT_BAND]
    return ((min(feet) + max(feet)) / 2,
            min(p[0] for p in pts), max(p[0] for p in pts))


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

            for f in WALK + PLANTED:
                name = f'unit_{key}_{d}_{f}.png'
                p = find(key, name)
                if p is None:
                    continue
                # 원본 보관 — 재실행해도 이동이 누적되지 않게 하려는 것이다.
                #
                # ⚠ 납품이 갱신되면 **옛 백업을 버려야 한다.** 그러지 않으면 새 그림 위에
                #    옛 그림을 덮어써서 납품을 통째로 되돌린다. 실제로 샐러맨더 25장이
                #    구멍 있는 옛 파일로 되돌아갔다. 현재 파일이 백업보다 새로우면
                #    새 납품이므로 백업을 다시 만든다.
                raw = os.path.join(bak, name)
                if not os.path.exists(raw) or os.path.getmtime(p) > os.path.getmtime(raw) + 1:
                    shutil.copy2(p, raw)

                im = Image.open(raw).convert('RGBA')
                walk = f in WALK
                want = ref if walk else anchor_of(key)
                cur, x0, x1 = (head_center if walk else foot_center)(im)
                dx = int(round(want - cur))
                label = '머리중심' if walk else '발중심'

                if abs(dx) <= DEADZONE:
                    shutil.copy2(raw, p)
                    print(f'  {d}_{f}: {label} {cur:5.1f} (기준 {want:.1f})  그대로')
                    continue
                if x0 + dx < 0 or x1 + dx > im.size[0] - 1:
                    shutil.copy2(raw, p)
                    print(f'  {d}_{f}: {dx:+d} 하면 잘린다 — 건너뜀')
                    continue

                out = Image.new('RGBA', im.size, (0, 0, 0, 0))
                out.paste(im, (dx, 0), im)
                out.save(p)
                after = (head_center if walk else foot_center)(out)[0]
                print(f'  {d}_{f}: {label} {cur:5.1f} → {after:.1f}  이동 {dx:+d}px')


if __name__ == '__main__':
    main()
