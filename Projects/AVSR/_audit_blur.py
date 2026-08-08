"""뭉갠 에셋을 수치로 골라낸다 — 눈대중으로 몇 개만 집지 않기 위해.

목업이 JPEG 라, 작게 잡힌 요소를 잘라 확대하면 픽셀아트가 아니라 흐릿한 죽이 된다.
그런데 "흐릿하다"는 눈으로만 판단하면 놓치는 게 생긴다. 그래서 두 가지를 잰다.

  보간율  — 어떤 픽셀이 좌우 이웃의 **중간값**에 가까운 비율.
            픽셀아트는 인접 픽셀이 같거나 확 다르다(계단). 중간값이 잘 안 나온다.
            흐릿한 그림은 확대 보간 때문에 중간값 픽셀이 잔뜩 생긴다.

  색밀도  — 넓이 대비 고유색 수. 뭉갠 그림은 같은 면적에 색이 과하게 많다.

배경·목업 통짜 컷(방바닥·배경판)은 원래 사진 같은 그림이라 제외한다.
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')

# 원래 회화적인 것들 — 픽셀 계단을 기대하면 안 된다
SKIP = ('background', 'roomfloor', 'art', 'lockup', 'card', 'bar', 'tipbar',
        'counter', 'button' if False else 'zzz')


def interp_ratio(im, tol=10):
    """좌우 이웃의 중간값에 가까운 픽셀 비율."""
    rgb = im.convert('RGB')
    w, h = rgb.size
    px = rgb.load()
    a = im.getchannel('A').load()
    hit = tot = 0
    for y in range(h):
        for x in range(1, w - 1):
            if a[x, y] < 200:
                continue
            l, c, r = px[x - 1, y], px[x, y], px[x + 1, y]
            if l == r:
                continue                      # 평탄면 — 판단 대상 아님
            tot += 1
            if all(abs(c[i] - (l[i] + r[i]) / 2) <= tol for i in range(3)) \
               and any(abs(l[i] - r[i]) > 2 * tol for i in range(3)):
                hit += 1
    return hit / tot if tot else 0.0


def main():
    rows = []
    for folder in sorted(os.listdir(BASE)):
        d = os.path.join(BASE, folder)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith('.png'):
                continue
            name = f[:-4]
            if any(s in name for s in SKIP):
                continue
            im = Image.open(os.path.join(d, f)).convert('RGBA')
            if im.width * im.height > 60000:      # 큰 배경판 제외
                continue
            ir = interp_ratio(im)
            colors = len(im.convert('RGB').getcolors(maxcolors=1 << 20))
            density = colors / (im.width * im.height)
            rows.append((ir, density, colors, im.size, folder, name))

    rows.sort(reverse=True)
    print(f'{"에셋":<34}{"보간율":>8}{"색밀도":>8}{"색":>6}{"크기":>12}')
    for ir, den, c, size, folder, name in rows[:26]:
        flag = '  ← 뭉갬' if ir >= 0.16 else ''
        print(f'{folder[:4]+"/"+name:<34}{ir*100:>7.1f}%{den:>8.2f}{c:>6}'
              f'{f"{size[0]}x{size[1]}":>12}{flag}')


if __name__ == '__main__':
    main()
