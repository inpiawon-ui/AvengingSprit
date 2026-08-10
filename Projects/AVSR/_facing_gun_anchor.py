"""총구 방향 도해 — `ne` 가 하늘을 겨누고 `n` 은 옆을 겨누는 문제 때문에.

몸은 돌렸는데 총은 안 돌린 것이 원인이다. 말로 "완만하게"라고 적으면 또
어긋나므로, 바닥 원근에서 계산한 **화면상 각도를 숫자로** 박아 넣는다.

바닥은 카메라 기울기 때문에 세로가 SQUASH 배로 눌려 보인다(제단 타원 실측).
그래서 바닥 위에서 45° 인 방향도 화면에서는 atan(SQUASH) ≈ 17° 로 보인다.
`n`·`s` 는 총구가 화면 안쪽/바깥쪽을 향하므로 길이가 줄어드는(단축) 그림이다.
"""
import math
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
IN = os.path.join(HERE, '_exchange', 'in')
OUT = os.path.join(HERE, '_exchange', 'out', '07_facing')

SQUASH = 0.30          # 바닥 눌림비 — roomfloor 제단 타원 실측
CELL = 96 * 3
DIRS = [('s', -90), ('se', -45), ('e', 0), ('ne', 45), ('n', 90)]


def screen_angle(world_deg):
    """바닥 위 방향 → 화면에서 보이는 각도(도)."""
    r = math.radians(world_deg)
    return math.degrees(math.atan2(math.sin(r) * SQUASH, math.cos(r)))


def main():
    sheet = Image.new('RGBA', (CELL * 5 + 20 * 6, CELL + 150), (24, 20, 38, 255))
    d = ImageDraw.Draw(sheet)
    d.text((20, 12), 'GUN BARREL DIRECTION - must follow the body, on the GROUND plane',
           fill=(255, 255, 255, 255))
    d.text((20, 30), f'ground is squashed {SQUASH:.2f} by the camera, so 45 deg on the '
                     f'ground looks like {screen_angle(45):.0f} deg on screen',
           fill=(180, 180, 210, 255))

    for i, (k, world) in enumerate(DIRS):
        p = os.path.join(IN, f'unit_rambo_{k}.png')
        x = 20 + i * (CELL + 20)
        if os.path.exists(p):
            im = Image.open(p).convert('RGBA').resize((CELL, CELL), Image.NEAREST)
            sheet.paste(im, (x, 60), im)

        sa = screen_angle(world)
        cx, cy = x + CELL // 2, 60 + int(52 * 3)      # 총을 든 높이쯤

        if abs(world) == 90:
            # 화면 안/바깥으로 향하는 방향 — 길이가 줄어든다. 짧은 화살로 표시.
            L, note = 46, 'FORESHORTENED'
            ex, ey = cx, cy - L if world > 0 else cy + L
        else:
            L, note = 150, f'{sa:+.0f} deg'
            ex, ey = cx + L * math.cos(math.radians(-sa)), cy + L * math.sin(math.radians(-sa))

        d.line((cx, cy, ex, ey), fill=(255, 90, 120, 230), width=5)
        d.ellipse((ex - 7, ey - 7, ex + 7, ey + 7), fill=(255, 90, 120, 230))
        d.text((x + 8, 62), k.upper(), fill=(255, 255, 255, 255))
        d.text((x + 8, CELL + 74), f'ground {world:+d} deg', fill=(150, 255, 190, 255))
        d.text((x + 8, CELL + 92), f'screen {note}', fill=(255, 210, 60, 255))

    sheet.convert('RGB').save(os.path.join(OUT, '05_총구각도_도해.png'))
    print('saved 05_총구각도_도해.png')
    for k, w in DIRS:
        print(f'  {k:<3} 바닥 {w:+4d}도 → 화면 {screen_angle(w):+6.1f}도')


if __name__ == '__main__':
    main()
