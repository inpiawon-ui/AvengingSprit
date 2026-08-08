"""전수 확대 검수(_zoomsheet.py)에서 걸린 결함을 고친다.

원인이 다섯 갈래다. 각각 다른 처방이 필요하다.

  A. HP바 bg 에 게이지가 구워져 있다
     목업에는 "가득 찬 바"만 있고 빈 트랙이 없다. 그걸 그대로 bg 로 잘라서,
     그 위에 fill 을 얹으니 항상 만땅으로 보인다. → 테두리는 남기고 속을 비운다.

  B. 초상 우측에 이웃 칸 경계선이 딸려왔다
     목업 그리드에서 자를 때 폭이 옆 칸으로 넘어갔다. → 경계선부터 오른쪽 끝까지
     바로 왼쪽 열로 덮어 지운다(크기를 바꾸면 슬롯 규격이 깨지므로).

  C. 유닛 스프라이트 주변에 얼룩이 남았다
     JPEG 노이즈 때문에 배경 flood fill 이 캐릭터 주변 점을 못 지웠다.
     → 본체에서 떨어져 나온 작은 덩어리를 지운다.

  D. 인게임 목업(576px)이 로비 목업(683px)보다 저해상도라 같은 아이콘이 뭉갰다
     → 같은 그림이 로비 쪽에 선명하게 있으므로 그걸 쓴다.

  E. 프레임 안쪽이 막혀 있다
     테두리여야 할 것이 통짜라 초상이 안 보인다. → 안쪽을 판다.
"""
import os
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')

IG = os.path.join(BASE, 'InGameMainUI')
HS = os.path.join(BASE, 'HostSelectPanel')
LB = os.path.join(BASE, 'LobbyMainUI')

STRIPED = ['hitman', 'ninja', 'robot', 'vampire']
UNITS = ['amazoness', 'boss', 'dragon', 'ghost', 'hitman', 'mafia', 'ninja',
         'rambo', 'robot', 'slugger', 'snowwoman', 'vampire', 'wizard', 'yogamaster']


# ── A. 바 배경 비우기 ─────────────────────────────────────────────
def hollow_bar(path, ring=2):
    """테두리 ring 픽셀만 남기고 속을 어두운 트랙으로 바꾼다."""
    im = Image.open(path).convert('RGBA')
    w, h = im.size
    px = im.load()
    for y in range(ring, h - ring):
        # 위에서 아래로 아주 옅은 그라데이션 — 평평한 단색은 싸구려로 보인다
        t = (y - ring) / max(1, h - 2 * ring - 1)
        v = int(20 - 8 * t)
        for x in range(ring, w - ring):
            px[x, y] = (v, v + 2, v + 6, 255)
    im.save(path)
    return im.size


# ── B. 우측 경계선 잔재 지우기 ─────────────────────────────────────
def wipe_right_stripe(path, scan=0.22):
    """오른쪽 구간에서 가장 밝은 세로선을 찾아, 그 왼쪽 열로 끝까지 덮는다."""
    im = Image.open(path).convert('RGBA')
    w, h = im.size
    px = im.load()

    start = int(w * (1 - scan))
    bright = [(sum(sum(px[x, y][:3]) for y in range(h)) / h, x) for x in range(start, w)]
    peak = max(bright)[1]
    cut = max(1, peak - 3)          # 선 바로 앞까지만 남긴다

    for y in range(h):
        src = px[cut - 1, y]
        for x in range(cut, w):
            px[x, y] = src
    im.save(path)
    return cut, w


# ── C. 얼룩(고립 덩어리) 제거 ──────────────────────────────────────
def despeckle(path, min_ratio=0.004, alpha_cut=24):
    """알파 연결 성분 중 가장 큰 것 대비 min_ratio 미만인 덩어리를 지운다."""
    im = Image.open(path).convert('RGBA')
    w, h = im.size
    px = im.load()
    if im.getchannel('A').getextrema()[0] > 250:
        return 0                                   # 통짜 이미지는 대상 아님

    seen = [[False] * h for _ in range(w)]
    blobs = []
    for sy in range(h):
        for sx in range(w):
            if seen[sx][sy] or px[sx, sy][3] <= alpha_cut:
                continue
            q, cells = deque([(sx, sy)]), []
            seen[sx][sy] = True
            while q:
                x, y = q.popleft()
                cells.append((x, y))
                for nx, ny in ((x+1, y), (x-1, y), (x, y+1), (x, y-1),
                               (x+1, y+1), (x-1, y-1), (x+1, y-1), (x-1, y+1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] \
                            and px[nx, ny][3] > alpha_cut:
                        seen[nx][ny] = True
                        q.append((nx, ny))
            blobs.append(cells)

    if not blobs:
        return 0
    biggest = max(len(b) for b in blobs)
    removed = 0
    for b in blobs:
        if len(b) < biggest * min_ratio:
            for x, y in b:
                px[x, y] = (0, 0, 0, 0)
            removed += len(b)
    if removed:
        im.save(path)
    return removed


# ── E. 프레임 속 파내기 ───────────────────────────────────────────
def hollow_frame(path, inset=9):
    im = Image.open(path).convert('RGBA')
    w, h = im.size
    px = im.load()
    for y in range(inset, h - inset):
        for x in range(inset, w - inset):
            px[x, y] = (0, 0, 0, 0)
    im.save(path)


def main():
    print('A. HP바 배경 비우기 — 게이지가 구워져 있었다')
    for n in ('bosshpbarbg', 'ghosthpbarbg', 'hosthpbarbg'):
        print(f'   {n:<16} {hollow_bar(os.path.join(IG, n + ".png"))}')

    print('B. 초상 우측 이웃 경계선 지우기')
    for n in STRIPED:
        for pre, d in (('hostportraitimage_', HS), ('hostslotportrait_', HS)):
            cut, w = wipe_right_stripe(os.path.join(d, pre + n + '.png'))
            print(f'   {pre + n:<30} {cut}~{w} 열 덮음')
        cut, w = wipe_right_stripe(os.path.join(IG, 'unit_' + n + '.png'))
        print(f'   {"unit_" + n:<30} {cut}~{w} 열 덮음')

    print('C. 유닛 얼룩 제거')
    for n in UNITS:
        p = os.path.join(IG, f'unit_{n}.png')
        r = despeckle(p)
        if r:
            print(f'   unit_{n:<18} {r}px 제거')

    print('D. 저해상도 아이콘을 로비의 선명본으로 교체')
    for dst, src in (('gemicon', 'gemicon'),
                     ('goldicon', 'goldicon'),
                     ('ghosthudicon', 'ghostportraiticon')):
        a = Image.open(os.path.join(LB, src + '.png')).convert('RGBA')
        a.save(os.path.join(IG, dst + '.png'))
        print(f'   {dst:<16} ← LobbyMainUI/{src}  {a.size}')
    # 셰브론도 같은 이유 — 21x27 뭉갠 것보다 38x32 선명본이 낫다
    a = Image.open(os.path.join(HS, 'ghostarrowchevron.png')).convert('RGBA')
    a.save(os.path.join(HS, 'arrowicon.png'))
    print(f'   arrowicon        ← ghostarrowchevron  {a.size}')

    print('E. 프레임 속 파내기 — 초상이 가려져 있었다')
    hollow_frame(os.path.join(IG, 'hostportraitframe.png'))
    print('   hostportraitframe')


if __name__ == '__main__':
    main()
