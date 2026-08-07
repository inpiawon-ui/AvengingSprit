"""프레임 에셋의 빈 내부를 목업 실제 색으로 채운다.

문제: 납품된 9-slice 프레임(`continuebutton`, `chaptercard` 등)이 **테두리만 있고
내부가 투명**하다. 목업은 꽉 찬 패널이라 화면에서 속이 비어 보인다.

방식: 테두리 아트는 손대지 않고, 중앙에서 flood fill 로 찾은 '내부 투명 영역'만
목업의 세로 그라디언트로 채운다. 테두리에 틈이 있어 바깥으로 샜다면 건너뛴다.

일부 에셋은 색 자체가 틀렸다(게이지 바탕이 골드). 그건 RECOLOR 로 통째 교체한다.
"""
import os
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
MOCK = os.path.join(HERE, 'Reference', 'Mockups', 'lobby_hub.jpeg')
DST = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'BaseResource', 'LobbyMainUI'))

# 에셋명 → 목업에서 채움색을 뽑을 세로 스트립 (x, y0, y1)
#   내용물(글자·아트)이 없는 깨끗한 열을 골랐다.
FILL = {
    'continuebutton':          (25, 500, 568),
    'chaptercard':             (205, 292, 574),
    'battlepasscard':          (462, 292, 380),
    'eventcard':               (462, 394, 483),
    'dailylogincard':          (655, 496, 584),
    'ghostwidget':             (165, 14, 70),
    'staminacounter':          (182, 24, 52),
    'goldcounter':             (295, 24, 52),
    'gemcounter':              (412, 24, 52),
    'featuretabbarbackground': (650, 622, 690),
    'hostbutton':              (16, 716, 896),
    'chapterbutton':           (231, 712, 898),
    'shopbutton':              (455, 716, 896),
    'tophudbackground':        (680, 2, 98),
}

# 색 자체가 틀린 에셋 — 불투명 픽셀 색을 목업 스트립으로 갈아끼운다.
RECOLOR = {
    'ghostexpbarbg':   (150, 56, 66),
    'ghostexpbarfill': (90, 56, 66),
    'progressbarbg':   (120, 470, 479),
    'progressbarfill': (22, 470, 479),
    'continuebutton':  (178, 502, 563),   # 목업은 꽉 찬 골드 — 어둡게 납품됨
}


def strip(src, x, y0, y1, h):
    """목업 세로 스트립을 대상 높이로 늘린 색 배열을 만든다."""
    col = [src.getpixel((x, y)) for y in range(y0, y1)]
    n = len(col)
    return [col[min(n - 1, int(i * n / h))] for i in range(h)]


def interior(im):
    """중앙에서 flood fill 로 '테두리 안쪽 투명 영역'을 찾는다. 밖으로 새면 None."""
    w, h = im.size
    a = im.split()[3].load()
    cx, cy = w // 2, h // 2
    if a[cx, cy] > 200:
        return None                      # 이미 채워져 있다
    seen = [[False] * h for _ in range(w)]
    q = deque([(cx, cy)])
    seen[cx][cy] = True
    out, edge = [], 0
    while q:
        x, y = q.popleft()
        out.append((x, y))
        if x <= 0 or y <= 0 or x >= w - 1 or y >= h - 1:
            edge += 1
            if edge > 4:
                return None              # 테두리에 틈 — 바깥으로 샘
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and a[nx, ny] < 40:
                seen[nx][ny] = True
                q.append((nx, ny))
    return out


def main():
    src = Image.open(MOCK).convert('RGB')
    filled = skipped = 0

    for name, (x, y0, y1) in FILL.items():
        p = os.path.join(DST, name + '.png')
        if not os.path.exists(p):
            print(f'  없음 {name}')
            continue
        im = Image.open(p).convert('RGBA')
        px = im.load()
        inner = interior(im)
        if inner is None:
            print(f'  건너뜀 {name} (이미 채워짐 또는 테두리 틈)')
            skipped += 1
            continue
        col = strip(src, x, y0, y1, im.height)
        for ix, iy in inner:
            r, g, b = col[iy]
            px[ix, iy] = (r, g, b, 255)
        im.save(p)
        filled += 1
        print(f'  채움 {name} {im.size} 픽셀 {len(inner)}')

    for name, (x, y0, y1) in RECOLOR.items():
        p = os.path.join(DST, name + '.png')
        if not os.path.exists(p):
            print(f'  없음 {name}')
            continue
        im = Image.open(p).convert('RGBA')
        px = im.load()
        col = strip(src, x, y0, y1, im.height)
        for iy in range(im.height):
            r, g, b = col[iy]
            for ix in range(im.width):
                if px[ix, iy][3] > 20:            # 불투명한 곳만 색을 갈아끼운다
                    px[ix, iy] = (r, g, b, px[ix, iy][3])
        im.save(p)
        print(f'  재색 {name} {im.size}')

    print(f'채움 {filled} / 건너뜀 {skipped} / 재색 {len(RECOLOR)}')


if __name__ == '__main__':
    main()
