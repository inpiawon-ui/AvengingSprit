"""인게임 유닛 스프라이트를 온전한 소스에서 다시 만든다.

기존 unit_*.png 는 캐릭터 **안쪽까지 뚫려 있었다.** 옷·그림자처럼 어두운 부분이
배경색과 가까워서, 색 유사도만 보는 전역 임계 방식이 실루엣 내부를 파먹었다.
게임에서는 그 구멍으로 바닥이 비쳐 보라색 얼룩처럼 보였다.

소스는 `hostportraitimage_*`(182×140, 전신)다. 슬롯용 `hostslotportrait_*` 는
카드에 꽉 채우려고 얼굴 위주로 잘려 있어 인게임 유닛으로 쓸 수 없다.

이 초상들은 목업을 2배로 키운 것이라 픽셀이 가로세로 정확히 2개씩 겹쳐 있다.
그래서 마지막에 1/2로 줄이면 원래 픽셀 격자가 손실 없이 복원된다.

핵심은 두 가지 —
  1) **가장자리 flood fill 만** 쓴다. 바깥에서 번져 들어가므로 실루엣 안쪽은 못 뚫는다.
  2) 그래도 틈으로 새면 **구멍 메우기**로 되돌린다. 바깥과 연결되지 않은 투명 영역은
     정의상 캐릭터 내부이므로 무조건 복원한다.

허용오차는 호스트마다 다르다. 배경과 캐릭터의 명도 차가 제각각이라
(뱀파이어는 어두운 배경에 어두운 캐릭터) 하나의 값으로는 맞출 수 없다.
"""
import os
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IG = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
HS = os.path.join(ROOT, 'Assets', 'BaseResource', 'HostSelectPanel')

HOSTS = ['amazoness', 'dragon', 'hitman', 'mafia', 'ninja', 'rambo', 'robot',
         'slugger', 'snowwoman', 'vampire', 'wizard', 'yogamaster']

# 카드 배경은 캐릭터 색조로 물든 **그라데이션**이다. 그래서 "모서리 색과 얼마나
# 다른가"만 보면 중간에서 멈춰 배경 덩어리가 남는다. 대신 **이웃 픽셀과의 차이**를
# 따라 번진다 — 그라데이션은 인접 차가 작아 끝까지 번지고, 캐릭터 외곽선은
# 급격한 차이라 거기서 멈춘다. GLOBAL 은 캐릭터 속으로 새는 것을 막는 상한이다.
STEP = 26        # 이웃 픽셀과 이만큼 이내면 같은 배경으로 본다
GLOBAL = {'amazoness': 300, 'vampire': 150, 'robot': 170, 'ninja': 190}
DEFAULT_GLOBAL = 170


def rebuild(host):
    limit = GLOBAL.get(host, DEFAULT_GLOBAL)
    src = os.path.join(HS, f'hostportraitimage_{host}.png')
    im = Image.open(src).convert('RGBA')
    w, h = im.size
    px = im.load()

    corners = [px[0, 0], px[w - 1, 0], px[0, h - 1], px[w - 1, h - 1]]
    bg = tuple(sorted(c[i] for c in corners)[1] for i in range(3))

    def d(a, b):
        return sum(abs(a[i] - b[i]) for i in range(3))

    # 1) 가장자리에서만, 이웃 차이를 따라 번져 들어간다
    outside = [[False] * h for _ in range(w)]
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if not outside[x][y]:
                outside[x][y] = True; q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if not outside[x][y]:
                outside[x][y] = True; q.append((x, y))
    while q:
        x, y = q.popleft()
        here = px[x, y]
        for nx, ny in ((x+1, y), (x-1, y), (x, y+1), (x, y-1)):
            if not (0 <= nx < w and 0 <= ny < h) or outside[nx][ny]:
                continue
            nb = px[nx, ny]
            if d(nb, here) <= STEP and d(nb, bg) <= limit:
                outside[nx][ny] = True
                q.append((nx, ny))

    for x in range(w):
        for y in range(h):
            if outside[x][y]:
                px[x, y] = (0, 0, 0, 0)

    # 2) 바깥과 연결되지 않은 투명 영역 = 캐릭터 내부의 구멍 → 되돌린다
    orig = Image.open(src).convert('RGBA').load()
    seen = [[False] * h for 	_ in range(w)]
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if px[x, y][3] == 0 and not seen[x][y]:
                seen[x][y] = True; q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if px[x, y][3] == 0 and not seen[x][y]:
                seen[x][y] = True; q.append((x, y))
    while q:
        x, y = q.popleft()
        for nx, ny in ((x+1, y), (x-1, y), (x, y+1), (x, y-1)):
            if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and px[nx, ny][3] == 0:
                seen[nx][ny] = True
                q.append((nx, ny))
    holes = 0
    for x in range(w):
        for y in range(h):
            if px[x, y][3] == 0 and not seen[x][y]:
                px[x, y] = orig[x, y]
                holes += 1

    box = im.getchannel('A').getbbox()
    if box:
        im = im.crop(box)
    # 2배로 부풀려진 소스이므로 정확히 절반으로 줄여 원래 픽셀 격자를 되찾는다
    im = im.resize((max(1, im.width // 2), max(1, im.height // 2)), Image.NEAREST)
    out = os.path.join(IG, f'unit_{host}.png')
    before = Image.open(out).size if os.path.exists(out) else None
    im.save(out)
    return before, im.size, holes


def main():
    import sys
    sys.path.insert(0, HERE)
    from _fix_assets2 import despeckle
    for h in HOSTS:
        before, after, holes = rebuild(h)
        # 배경에서 떨어져 나온 조각은 게임에서 캐릭터 주변 얼룩으로 보인다
        gone = despeckle(os.path.join(IG, f'unit_{h}.png'), min_ratio=0.012)
        print(f'  unit_{h:<12} {before} → {after}   구멍복원 {holes}px · 얼룩 {gone}px')


if __name__ == '__main__':
    main()
