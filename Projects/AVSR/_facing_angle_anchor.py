"""8방향 스프라이트용 카메라 각도 앵커.

워크오더에 "탑뷰"라고 잘못 적어 뒀다. 실제 인게임은 **쿼터뷰**다 —
바닥이 원근으로 눕고, 캐릭터는 그 위에 선 전신이다. 탑뷰로 오해하면
후면(n)을 정수리 그림으로 그리게 되어 다섯 장을 통째로 못 쓴다.

말로 설명하는 대신 실제 화면을 그대로 합성해서 보여준다.
바닥은 인게임과 같은 크기(720×800)로 늘리고, 캐릭터도 인게임과 같은
표시 크기(96×92)로 올린다. 바닥에 그린 타원은 "바닥에 놓인 원이 화면에서
이렇게 눌려 보인다" = 카메라 기울기를 눈으로 재는 자다.
"""
import io
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
OUT = os.path.join(HERE, '_exchange', 'out', '07_facing')

FIELD = (720, 800)      # 인게임 RoomField 실치수
UNIT = (96, 92)         # 호스트 표시 크기
STRIP = 74              # 위쪽 설명 띠

# 바닥에 누운 원 — 원근을 눈으로 재는 자. 세로/가로 비가 곧 기울기다.
GUIDE = [(190, 640, 150), (300, 470, 96), (410, 330, 62)]  # (cx, cy, 반지름)


def _opaque(im, thresh=8):
    px = im.load()
    w, h = im.size
    return [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > thresh]


def main():
    floor = Image.open(os.path.join(SRC, 'roomfloor.png')).convert('RGBA').resize(FIELD, Image.NEAREST)
    unit = Image.open(os.path.join(SRC, 'unit_rambo.png')).convert('RGBA').resize(UNIT, Image.NEAREST)

    im = Image.new('RGBA', (FIELD[0], FIELD[1] + STRIP), (16, 12, 30, 255))
    im.paste(floor, (0, STRIP), floor)
    d = ImageDraw.Draw(im, 'RGBA')

    # 바닥 원 → 화면에서는 타원. 세 군데에 그려 깊이에 따른 변화까지 보이게 한다.
    for cx, cy, r in GUIDE:
        ry = r * 0.30                       # 실측 바닥 눌림비 (제단 타원 기준)
        d.ellipse((cx - r, STRIP + cy - ry, cx + r, STRIP + cy + ry),
                  outline=(90, 255, 160, 200), width=3)

    # 발밑 선은 캔버스 밑변이 아니라 **실제 가장 아래 불투명 픽셀**에 긋는다.
    # 이 그림 자체가 발 정렬 기준을 보여주는 자료라 여기서 어긋나면 안 된다.
    foot = max(y for _, y in _opaque(unit))

    # 캐릭터를 깊이별로 세 번 올린다. 멀어져도 서 있는 전신이라는 것이 핵심.
    for cx, cy, _ in GUIDE:
        top = STRIP + cy - foot - 1
        im.paste(unit, (cx - UNIT[0] // 2, top), unit)
        d.line((cx - 26, STRIP + cy, cx + 26, STRIP + cy), fill=(255, 210, 60, 220), width=2)

    d.text((14, 10), 'QUARTER VIEW  (NOT top-down)', fill=(255, 255, 255, 255))
    d.text((14, 28), 'green ellipse = a CIRCLE lying on the ground', fill=(150, 255, 190, 255))
    d.text((14, 46), 'yellow line = ground contact.  figure stays UPRIGHT at any depth',
           fill=(255, 210, 60, 255))

    im.convert('RGB').save(os.path.join(OUT, '04_각도_쿼터뷰_바닥합성.png'))
    print('saved 04_각도_쿼터뷰_바닥합성.png', im.size)


if __name__ == '__main__':
    main()
