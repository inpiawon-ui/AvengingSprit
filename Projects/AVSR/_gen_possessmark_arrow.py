"""화면 밖 빙의 후보를 가리키는 가장자리 화살표 (기획서 1-5 B).

납품된 표식 4종과 같은 언어로 그린다 — 32x32 도트, 같은 파랑(#5EC8FF),
같은 어두운 외곽선(#080A10). 표식은 링, 이것은 방향이므로 화살표다.

위를 향한 한 장만 만든다. 아래쪽은 코드에서 180° 돌려 쓴다 —
같은 그림을 두 장 들고 있으면 한쪽만 고치는 사고가 난다.
"""
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IG = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

N = 32
BLUE = (94, 200, 255, 255)
DARK = (8, 10, 16, 255)


def main():
    im = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # 촉 — 위로 벌어지는 삼각형. 바깥을 어둡게 한 겹 두르고 안을 채운다.
    d.polygon([(16, 3), (30, 17), (2, 17)], fill=DARK)
    d.polygon([(16, 7), (26, 17), (6, 17)], fill=BLUE)

    # 자루 — 짧고 굵게. 길면 32px 안에서 촉이 작아져 방향이 안 읽힌다.
    d.rectangle([10, 17, 21, 28], fill=DARK)
    d.rectangle([12, 17, 19, 26], fill=BLUE)

    out = os.path.join(IG, 'possessmark_arrow.png')
    im.save(out)
    print(f'  possessmark_arrow {im.size} → {out}')


if __name__ == '__main__':
    main()
