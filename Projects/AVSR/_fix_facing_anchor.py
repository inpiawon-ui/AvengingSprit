"""8방향 5장을 **발 중심** 기준으로 다시 정렬한다.

납품본은 바운딩 박스 중심이 맞춰져 있다. 그런데 총이 방향마다 다른 쪽으로
다른 길이만큼 뻗어 있어서, 박스를 맞추면 몸통이 반대로 밀린다.
납품본 발 중심은 36.5 ~ 57.5 로 21px 흔들린다 — 방향을 바꾸는 순간
캐릭터가 옆으로 순간이동한다.

그림은 건드리지 않고 가로로 평행이동만 한다.

⚠️ 재납품을 받은 뒤에는 이 스크립트를 그냥 돌리지 마라.
   `_raw_facing/` 에서 복원하므로, 백업이 옛 버전이면 **반려한 그림이
   되살아난다.** 새 납품본을 받으면 백업부터 갱신할 것.
   (5차 재납품은 Codex 쪽에서 이미 x=42 로 맞춰 왔다 — 이동량 0.)

앵커 x 는 반드시 **캔버스 정중앙(48)** 이어야 한다.
왼쪽 절반(← ↖ ↙)은 코드가 `localScale.x` 부호를 뒤집어 만드는데, 이 반전은
**캔버스 중심을 축으로** 일어난다. 발이 42 에 있으면 뒤집힌 순간 발이
96-42=54 로 가서, 오른쪽을 보다 왼쪽을 보는 찰나에 몸이 12px 옆으로 튄다.
발 정렬을 맞춰 놓고도 반전에서 도로 어긋나는 함정이다.

(4차에는 `s` 의 소총이 옆으로 길어 48 로 옮기면 총구가 잘렸다. 5차에서
 화면 앞쪽으로 단축되며 폭이 89 → 44 로 줄어 제약이 사라졌다.)
"""
import os
import shutil

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '_exchange', 'in')
BAK = os.path.join(HERE, '_exchange', 'in', '_raw_facing')

DIRS = ['s', 'se', 'e', 'ne', 'n']
ANCHOR = 48          # 발 중심을 놓을 x
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
