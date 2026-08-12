"""원작 타이틀 로고 → 우리 타이틀 화면 스프라이트.

원작 시트에는 US 로고가 **6장** 들어 있다. 서로 다른 그림이 아니라 **같은 로고의
색만 바뀐 것** — 아케이드 타이틀에서 로고가 무지개색으로 순환하던 그 연출이다.
그래서 6장을 따로 잘라 순서대로 갈아 끼우면 원작의 색 순환이 그대로 재현된다.

자를 때 각 프레임을 제 경계 상자로 자르면 **프레임마다 크기가 달라져 로고가 떤다**
(색이 바뀌면서 외곽 한 픽셀이 생겼다 없어진다). 6장의 경계 상자를 **합집합**으로
묶어 모두 같은 크기로 자른다.

사용: python _orig_title.py
산출: Assets/BaseResource/TitleMainUI/titlelogo_0..5.png
"""
import io
import os
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHEET = os.path.join(HERE, 'Reference', 'Original',
                     'Miscellaneous - Title Screen (USA_JPN).png')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'TitleMainUI')

US_RIGHT = 0.50      # US 로고는 시트 왼쪽 절반
HEAD_SKIP = 31       # 맨 위 "US"/"JP" 글자 줄이 y 9~30 을 쓴다
FRAMES = 6
ZOOM = 3             # 도트를 살린 정수 확대. 720 폭 화면을 거의 채운다
PAD = 2              # 잘라낸 상자 둘레 여백


def bg_color(im):
    px = im.load()
    w, h = im.size
    return px[0, 0]


def bands(im, key, right, top):
    """세로로 빈 줄을 경계 삼아 덩어리 구간을 나눈다."""
    px = im.load()
    w, h = im.size
    rows = []
    for y in range(top, h):
        rows.append(any(px[x, y] != key for x in range(right)))

    out = []
    y = 0
    while y < len(rows):
        if not rows[y]:
            y += 1
            continue
        s = y
        while y < len(rows) and rows[y]:
            y += 1
        out.append((s + top, y + top))
    return out


def box_of(im, key, right, y0, y1):
    px = im.load()
    x0, x1 = right, -1
    for y in range(y0, y1):
        for x in range(right):
            if px[x, y] != key:
                if x < x0:
                    x0 = x
                if x > x1:
                    x1 = x
    return x0, x1 + 1


def main():
    os.makedirs(OUT, exist_ok=True)
    im = Image.open(SHEET).convert('RGB')
    key = bg_color(im)
    right = int(im.width * US_RIGHT)

    bs = bands(im, key, right, HEAD_SKIP)
    print(f'배경 {key} · US 폭 {right} · 덩어리 {len(bs)}개')
    for i, (a, b) in enumerate(bs):
        print(f'  {i}: y {a}~{b} (높이 {b-a})')

    # 로고는 위에서부터 6개. 그 아래는 INSERT COIN / PUSH START BUTTON 문구다.
    logo = bs[:FRAMES]
    if len(logo) < FRAMES:
        print(f'⚠ 로고 프레임을 {len(logo)}개만 찾았다 — 시트를 확인하라')
        return

    # 프레임마다 상자가 다르면 로고가 떤다. 폭·높이를 합집합으로 통일한다.
    xs = [box_of(im, key, right, a, b) for a, b in logo]
    x0 = min(a for a, _ in xs) - PAD
    x1 = max(b for _, b in xs) + PAD
    hh = max(b - a for a, b in logo) + PAD * 2

    rgba = im.convert('RGBA')
    px = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            if px[x, y][:3] == key:
                px[x, y] = (0, 0, 0, 0)

    # 여백(PAD)을 두면 첫 프레임의 자르기 범위가 위쪽 "US" 글자에 닿는다. 지운다.
    for y in range(logo[0][0]):
        for x in range(rgba.width):
            px[x, y] = (0, 0, 0, 0)

    # ⚠ 위쪽 기준으로 맞추면 안 된다. 첫 프레임만 잉크가 한 줄 더 높아
    #   (그 색만 배경과 구분되는 화소가 있다) 로고가 1px 튄다.
    #   아래쪽 끝은 6장 모두 정확히 134px 간격이므로 **바닥을 기준**으로 맞춘다.
    for i, (a, b) in enumerate(logo):
        top = b + PAD - hh
        cut = rgba.crop((x0, top, x1, top + hh))
        big = cut.resize((cut.width * ZOOM, cut.height * ZOOM), Image.NEAREST)
        big.save(os.path.join(OUT, f'titlelogo_{i}.png'))

    print(f'\n로고 {FRAMES}장 · 원본 {x1-x0}x{hh} → {ZOOM}배 {(x1-x0)*ZOOM}x{hh*ZOOM}')
    print(f'→ {OUT}')


if __name__ == '__main__':
    main()
