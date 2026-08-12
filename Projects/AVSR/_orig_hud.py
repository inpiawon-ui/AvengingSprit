"""원작 HUD 시트를 조각으로 자른다.

시트 한 장에 게이지·열쇠·호스트 초상이 함께 들어 있다. 우리 UI 는 조각마다
따로 스프라이트를 받으므로 여기서 나눈다.

조각 찾기는 **연결 성분**으로 한다. 배경(단색 키)이 아닌 픽셀 덩어리를 훑어
각각의 경계 상자를 얻는다. 격자 위치를 손으로 적으면 시트가 조금만 달라져도
전부 어긋나므로 그렇게 하지 않는다.

초상은 크기가 고르고 아래쪽에 격자로 모여 있어 크기·정렬로 골라낸다.

사용: python _orig_hud.py
산출: _exchange/out/16_hud/  (조각 PNG + 번호 붙인 대조 시트)
"""
import io
import os
import sys
from collections import Counter, deque

from PIL import Image, ImageDraw

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
SHEET = os.path.join(HERE, 'Reference', 'Original',
                     'Miscellaneous - HUD.png')
OUT = os.path.join(HERE, '_exchange', 'out', '16_hud')

WATERMARK_BOTTOM = 0.22
WATERMARK_RIGHT = 0.45
MIN_AREA = 24
PORTRAIT = (16, 16)     # 원작 초상 한 칸
PORTRAIT_TOP = 140      # 이 아래에 초상 격자가 있다


def bg_key(im):
    px = im.load()
    w, h = im.size
    c = Counter()
    for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        c[px[x, y]] += 1
    return c.most_common(1)[0][0]


def components(im, key, skip_box):
    """배경이 아닌 픽셀의 연결 성분 경계 상자 목록."""
    px = im.load()
    w, h = im.size
    seen = [[False] * h for _ in range(w)]
    boxes = []
    sx0, sy0 = skip_box
    for y in range(h):
        for x in range(w):
            if seen[x][y] or px[x, y] == key:
                continue
            if y >= sy0 and x >= sx0:
                continue
            q = deque([(x, y)])
            seen[x][y] = True
            x0 = x1 = x
            y0 = y1 = y
            n = 0
            while q:
                cx, cy = q.popleft()
                n += 1
                x0 = min(x0, cx); x1 = max(x1, cx)
                y0 = min(y0, cy); y1 = max(y1, cy)
                # 대각선까지 이어 붙인다 — 도트 그림은 모서리로만 붙은 부분이 많다
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        nx, ny = cx + dx, cy + dy
                        if (0 <= nx < w and 0 <= ny < h and not seen[nx][ny]
                                and px[nx, ny] != key
                                and not (ny >= sy0 and nx >= sx0)):
                            seen[nx][ny] = True
                            q.append((nx, ny))
            if n >= MIN_AREA:
                boxes.append((x0, y0, x1 + 1, y1 + 1, n))
    return boxes


def main():
    os.makedirs(OUT, exist_ok=True)
    im = Image.open(SHEET).convert('RGB')
    key = bg_key(im)
    w, h = im.size
    skip = (int(w * WATERMARK_RIGHT), int(h * (1 - WATERMARK_BOTTOM)))

    boxes = components(im, key, skip)
    boxes.sort(key=lambda b: (b[1], b[0]))     # 위에서 아래, 왼쪽에서 오른쪽

    # 초상은 16x16 이고 시트 아래쪽에 격자로 모여 있다.
    # "가장 흔한 크기" 로 고르면 게이지의 8x8 칸이 잡힌다 — 위치까지 함께 본다.
    portraits = [b for b in boxes
                 if PORTRAIT[0] - 2 <= b[2] - b[0] <= PORTRAIT[0] + 2
                 and PORTRAIT[1] - 2 <= b[3] - b[1] <= PORTRAIT[1] + 2
                 and b[1] >= PORTRAIT_TOP]
    portrait_size = PORTRAIT

    print(f'조각 {len(boxes)}개 · 초상 후보 {len(portraits)}개 (크기 {portrait_size})')

    rgba = im.convert('RGBA')
    px = rgba.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][:3] == key:
                px[x, y] = (0, 0, 0, 0)

    for i, (x0, y0, x1, y1, _) in enumerate(portraits):
        rgba.crop((x0, y0, x1, y1)).save(os.path.join(OUT, f'portrait_{i:02d}.png'))

    # 번호 붙인 대조 시트 — 어느 초상이 누구인지 눈으로 짝지어야 한다
    z = 3
    per = 8
    pw, ph = portrait_size
    rows = (len(portraits) + per - 1) // per
    sheet = Image.new('RGB', (per * (pw * z + 8), rows * (ph * z + 22)), (28, 28, 36))
    d = ImageDraw.Draw(sheet)
    for i, (x0, y0, x1, y1, _) in enumerate(portraits):
        c = rgba.crop((x0, y0, x1, y1))
        big = c.resize((c.width * z, c.height * z), Image.NEAREST)
        cx = (i % per) * (pw * z + 8) + 4
        cy = (i // per) * (ph * z + 22) + 18
        sheet.paste(big, (cx, cy), big)
        d.text((cx, cy - 14), f'{i:02d}', fill=(220, 220, 235))
    sheet.save(os.path.join(OUT, '00_portraits.png'))

    # 초상이 아닌 조각(게이지·열쇠)도 남긴다
    others = [b for b in boxes if b not in portraits]
    for i, (x0, y0, x1, y1, _) in enumerate(others):
        rgba.crop((x0, y0, x1, y1)).save(os.path.join(OUT, f'part_{i:02d}.png'))
    print(f'초상 {len(portraits)} · 그 밖 {len(others)} → {OUT}')


if __name__ == '__main__':
    main()
