"""방향 5장의 발 중심을 캔버스 중심(48)으로 맞춘다.

왼쪽 절반(← ↖ ↙)은 코드가 `localScale.x` 부호를 뒤집어 만드는데, 그 반전축이
**캔버스 중심**이다. 발이 48 에 없으면 왼쪽을 보는 순간 몸이 옆으로 튄다.
게다가 방향마다 발 중심이 다르면 방향을 바꿀 때마다 미끄러진다.

그림은 건드리지 않고 가로로 평행이동만 한다. 잘림이 생기면 그 장은 건너뛰고
알린다 — 잘라 내면서까지 맞추면 그림이 상한다.

발 판정은 **바닥 6줄**만 본다. 14줄로 보면 드래곤 꼬리·흡혈귀 망토가 발로
잡혀 실제보다 크게 어긋난 것처럼 나온다.

사용: python _fix_anchor.py <키> [키 ...]
"""
import io
import os
import shutil
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '_exchange', 'in')
BAK = os.path.join(IN, '_raw_anchor')

DIRS = ['s', 'se', 'e', 'ne', 'n']
ANCHOR = 48       # 반드시 캔버스 중심 — 좌우 반전축이다
FOOT_BAND = 6


def measure(im):
    px = im.load()
    w, h = im.size
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 8]
    if not pts:
        return None
    y1 = max(p[1] for p in pts)
    feet = [x for x, y in pts if y >= y1 - FOOT_BAND]
    return ((min(feet) + max(feet)) / 2,
            min(p[0] for p in pts), max(p[0] for p in pts))


def main():
    keys = sys.argv[1:]
    if not keys:
        print('키를 지정할 것: python _fix_anchor.py dragon vampire')
        return
    os.makedirs(BAK, exist_ok=True)

    for key in keys:
        print(f'\n=== {key} ===')
        moved = skipped = 0
        centers = []
        for d in DIRS:
            p = os.path.join(IN, f'unit_{key}_{d}.png')
            if not os.path.exists(p):
                print(f'  {d}: 파일 없음')
                continue
            raw = os.path.join(BAK, f'unit_{key}_{d}.png')
            if not os.path.exists(raw):
                shutil.copy2(p, raw)      # 원본 1회 보관 — 재실행해도 누적 이동 없음

            im = Image.open(raw).convert('RGBA')
            m = measure(im)
            if m is None:
                continue
            fc, x0, x1 = m
            dx = int(round(ANCHOR - fc))

            if x0 + dx < 0 or x1 + dx > im.size[0] - 1:
                print(f'  {d}: 발중심 {fc:.1f} → 이동 {dx:+d} 하면 잘린다. 건너뜀')
                shutil.copy2(raw, p)
                centers.append(fc)
                skipped += 1
                continue

            out = Image.new('RGBA', im.size, (0, 0, 0, 0))
            out.paste(im, (dx, 0), im)
            out.save(p)
            centers.append(measure(out)[0])
            if dx:
                moved += 1
            print(f'  {d}: 발중심 {fc:5.1f} → {measure(out)[0]:.1f}  이동 {dx:+d}px')

        if centers:
            print(f'  편차 {max(centers) - min(centers):.1f}px  '
                  f'(이동 {moved}장, 건너뜀 {skipped}장)')


if __name__ == '__main__':
    main()
