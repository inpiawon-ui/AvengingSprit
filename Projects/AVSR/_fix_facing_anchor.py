"""8방향 5장을 **발 중심** 기준으로 다시 정렬한다.

납품본은 바운딩 박스 중심이 맞춰져 있다. 그런데 총이 방향마다 다른 쪽으로
다른 길이만큼 뻗어 있어서, 박스를 맞추면 몸통이 반대로 밀린다.
납품본 발 중심은 36.5 ~ 57.5 로 21px 흔들린다 — 방향을 바꾸는 순간
캐릭터가 옆으로 순간이동한다.

그림은 건드리지 않고 가로로 평행이동만 한다.

앵커 x 를 48(정중앙)이 아니라 42 로 잡는 이유:
정중앙으로 맞추면 `s` 의 소총 총구가 캔버스 오른쪽으로 8px 튀어나가 잘린다.
42 로 내리면 잘림이 2px 안쪽(총구 끝 점 하나)으로 줄고, 다섯 장이 모두
같은 기준을 쓰므로 튀는 현상은 똑같이 사라진다. 전체가 6px 왼쪽에 서지만
다섯 장이 함께 움직이므로 화면에서는 보이지 않는다.
"""
import os
import shutil

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '_exchange', 'in')
BAK = os.path.join(HERE, '_exchange', 'in', '_raw_facing')

DIRS = ['s', 'se', 'e', 'ne', 'n']
ANCHOR = 42          # 발 중심을 놓을 x
FOOT_BAND = 14       # 아래에서 이만큼이 발·다리 — 총이 없는 구간


def foot_center(im):
    px = im.load()
    w, h = im.size
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 8]
    y1 = max(p[1] for p in pts)
    feet = [x for x, y in pts if y >= y1 - FOOT_BAND]
    return (min(feet) + max(feet)) / 2, min(p[0] for p in pts), max(p[0] for p in pts)


def main():
    os.makedirs(BAK, exist_ok=True)
    for d in DIRS:
        p = os.path.join(IN, f'unit_rambo_{d}.png')
        raw = os.path.join(BAK, f'unit_rambo_{d}.png')
        if not os.path.exists(raw):
            shutil.copy2(p, raw)          # 원본은 한 번만 보관 — 재실행해도 누적 이동 없음

        im = Image.open(raw).convert('RGBA')
        fc, x0, x1 = foot_center(im)
        dx = int(round(ANCHOR - fc))

        out = Image.new('RGBA', im.size, (0, 0, 0, 0))
        out.paste(im, (dx, 0), im)
        out.save(p)

        clip = max(0, -(x0 + dx)) + max(0, (x1 + dx) - (im.size[0] - 1))
        print(f'{d:<4} 발중심 {fc:5.1f} → {ANCHOR}   이동 {dx:+3d}px   '
              f'잘림 {clip}px')


if __name__ == '__main__':
    main()
