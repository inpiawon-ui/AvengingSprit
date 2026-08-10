"""시트 분할이 어긋난 에셋을 골라낸다.

목업 한 장에서 요소를 잘라 쓰다 보니, 잘린 자리가 요소의 실제 경계와 안 맞는다.
증상은 두 가지고 방향이 정반대다.

  잘림    — 그림이 이미지 가장자리에 닿아 있다. 칼·팔·테두리가 잘려 나갔다는 뜻.
            아이콘은 사방에 여백이 있어야 정상이다.

  덧붙음  — 가장자리에 본체와 떨어진 이물이 붙어 있다. 옆 칸의 경계선이나
            겹쳐 있던 배지가 같이 딸려온 것.

둘 다 '불투명 픽셀이 테두리에 얼마나 닿아 있나'와 '본체에서 떨어진 덩어리가
있나'로 잡힌다. 배경판·바닥처럼 원래 화면을 꽉 채우는 것은 제외한다.
"""
import os
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')

# 화면을 꽉 채우거나 늘려 쓰는 것들 — 테두리에 닿는 게 정상
FULLBLEED = ('background', 'roomfloor', 'fill', 'bg', 'cooldown', 'tipbar',
             'lockup', 'art', 'frame', 'card', 'counter', 'widget', 'pedestal',
             'title', 'button')

# 상자를 꽉 채우는 도형이라 '잘림' 판정이 원래 높게 나오는 것들.
# 둥근 사각형·원은 상하좌우 중앙이 가장자리에 닿는 게 맞다.
#
# ⚠️ 여기에 넣기 전에 **목업과 대조해 크롭이 정확한지 눈으로 확인할 것.**
# dpadbase 는 1차 감사에서 61.2% 로 걸렸는데 "목업 충실"이라고 넘겼다가,
# 실제로는 좌우·아래가 잘리고 모서리에 얼룩이 남은 상태였다. 수치가
# 오탐인 것과 결함이 없는 것은 다르다.
SHAPE_FILLS_BOX = ('dpadbase', 'dpadknob')


def analyse(path):
    im = Image.open(path).convert('RGBA')
    w, h = im.size
    a = im.getchannel('A').load()
    if im.getchannel('A').getextrema()[0] > 250:
        return None                              # 통짜 — 판단 불가

    border = [(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)] \
        + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]
    touch = sum(1 for x, y in border if a[x, y] > 200) / len(border)

    # 본체에서 떨어진 덩어리
    seen = [[False] * h for _ in range(w)]
    blobs = []
    for sy in range(h):
        for sx in range(w):
            if seen[sx][sy] or a[sx, sy] <= 24:
                continue
            q, n = deque([(sx, sy)]), 0
            seen[sx][sy] = True
            while q:
                x, y = q.popleft(); n += 1
                for nx, ny in ((x+1, y), (x-1, y), (x, y+1), (x, y-1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and a[nx, ny] > 24:
                        seen[nx][ny] = True; q.append((nx, ny))
            blobs.append(n)
    blobs.sort(reverse=True)
    stray = sum(blobs[1:]) / max(1, sum(blobs))
    return touch, stray, len(blobs), im.size


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
            if any(s in name for s in FULLBLEED) or name in SHAPE_FILLS_BOX:
                continue
            r = analyse(os.path.join(d, f))
            if r is None:
                continue
            touch, stray, nb, size = r
            if touch >= 0.05 or stray >= 0.03:
                rows.append((max(touch, stray), touch, stray, nb, size, folder, name))

    rows.sort(reverse=True)
    print(f'{"에셋":<36}{"테두리닿음":>10}{"이물":>8}{"덩어리":>7}{"크기":>11}  진단')
    for _, touch, stray, nb, size, folder, name in rows:
        why = []
        if touch >= 0.05:
            why.append('잘림')
        if stray >= 0.03:
            why.append('덧붙음')
        print(f'{folder[:4]+"/"+name:<36}{touch*100:>9.1f}%{stray*100:>7.1f}%'
              f'{nb:>7}{f"{size[0]}x{size[1]}":>11}  {"·".join(why)}')
    print(f'\n{len(rows)}건')


if __name__ == '__main__':
    main()
