"""전수 육안 확인(_contact_sheet.py)에서 걸린 에셋을 목업에서 다시 만든다.

JPEG 목업에서 자르면 배경이 그대로 딸려온다. 그대로 쓰면 어두운 사각형이
화면에 얹힌다. 그래서 **가장자리에서 배경색을 따라 flood fill** 해서 알파를 판다 —
안쪽의 어두운 부분(메카 그림자 등)은 남는다.
"""
import os
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
MOCK = os.path.join(HERE, 'Reference', 'Mockups')
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')

# 에셋 → (목업, rect, 배율, 배경 허용오차, 지울 로컬 rect 목록)
#
# `clear` 는 크롭 안에 딸려온 남의 요소를 지운다. 목업에는 디자인 툴 잔재나
# 별도 요소(알림 배지)가 겹쳐 있어, 그대로 구우면 게임에 그게 박힌다.
JOBS = [
    # 보스가 얼굴만 65x81 이라 160x160 박스에서 2.4배 확대돼 뭉갰다.
    # 목업에는 매드닥터가 탄 메카 전체가 있다. 좌상단 흰 "80" 은 목업 잔재다.
    ('InGameMainUI', 'unit_boss', 'ingame_hd_scene.jpeg',
     (193, 80, 200, 198), 720 / 576, 46, [(0, 0, 17, 22)]),
    # 시상대가 "3" 을 잘라먹고 있었다.
    ('LobbyMainUI', 'rankingtabicon', 'lobby_hub.jpeg',
     (309, 626, 56, 44), 720 / 683, 40, []),
    # 봉투 우상단에 알림 배지가 겹친다 — 배지는 NotifyBadge 로 따로 그린다.
    ('LobbyMainUI', 'mailbutton', 'lobby_hub.jpeg',
     (567, 27, 36, 27), 720 / 683, 40, [(25, 0, 11, 10)]),
    # 셰브론이 갈색 덩어리로 뭉개져 있었다. 골드 버튼 쪽에서 따야 배경이 깔끔히 빠진다.
    ('HostSelectPanel', 'arrowicon', 'host_select.jpeg',
     (626, 884, 20, 26), 720 / 683, 70, []),
]


def cutout(im, tol):
    """가장자리에서 배경색을 따라 번져 들어가며 알파를 판다."""
    im = im.convert('RGBA')
    w, h = im.size
    px = im.load()

    # 네 모서리의 중앙값을 배경으로 본다
    corners = [px[0, 0], px[w - 1, 0], px[0, h - 1], px[w - 1, h - 1]]
    bg = tuple(sorted(c[i] for c in corners)[1] for i in range(3))

    def near(p):
        return abs(p[0] - bg[0]) + abs(p[1] - bg[1]) + abs(p[2] - bg[2]) <= tol

    seen = [[False] * h for _ in range(w)]
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if not seen[x][y] and near(px[x, y]):
                seen[x][y] = True; q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if not seen[x][y] and near(px[x, y]):
                seen[x][y] = True; q.append((x, y))

    while q:
        x, y = q.popleft()
        px[x, y] = (px[x, y][0], px[x, y][1], px[x, y][2], 0)
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and near(px[nx, ny]):
                seen[nx][ny] = True
                q.append((nx, ny))
    return im


def main():
    for folder, name, mock, (x, y, w, h), scale, tol, clears in JOBS:
        src = Image.open(os.path.join(MOCK, mock)).convert('RGB')
        c = cutout(src.crop((x, y, x + w, y + h)), tol)

        px = c.load()
        for cx, cy, cw, ch in clears:
            for yy in range(cy, min(cy + ch, c.height)):
                for xx in range(cx, min(cx + cw, c.width)):
                    px[xx, yy] = (0, 0, 0, 0)

        box = c.getchannel('A').getbbox()
        if box:
            c = c.crop(box)                      # 판 뒤 남은 여백 제거
        c = c.resize((round(c.width * scale), round(c.height * scale)), Image.LANCZOS)

        p = os.path.join(BASE, folder, name + '.png')
        before = Image.open(p).size if os.path.exists(p) else None
        c.save(p)
        opaque = c.getchannel('A').getextrema()[0] > 250
        print(f'  {folder}/{name:<16} {before} → {c.size}' + ('   ⚠ 알파 없음' if opaque else ''))


if __name__ == '__main__':
    main()
