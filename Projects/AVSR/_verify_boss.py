"""보스 3체 납품 검수 — 워크오더 17차 기준.

캐릭터 검수(`_verify_batch.py`)와 기준이 다르다.
  캔버스 256×256 · 닿는 선 y=232 · 방향 s·se·e · 프레임 6 + die/tell

가장 흔한 사고가 **닿는 선 흔들림**이라 그것을 제일 먼저 본다.
프레임마다 바닥 위치가 달라지면 보스가 떠 보이거나 파묻힌다.
`tell`(예고 표시)만은 바닥에 까는 그림이라 이 검사에서 뺀다.

사용: python _verify_boss.py
"""
import io
import os
import sys
from collections import deque

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '_exchange', 'in')
PACK = os.path.join(HERE, '_exchange', 'out', '17_boss')

SIZE = 256
FOOT_Y = 232
FOOT_TOL = 3        # 닿는 선 허용 오차
HOLE_MAX = 3.0      # 몸 안 구멍 비율(%)
PALETTE_NEAR = 56.0  # 원작 색에서 이만큼 안이면 같은 색으로 본다

BOSSES = ['robot_snakes', 'demolisher', 'python']
DIRS = ['s', 'se', 'e']
FRAMES = [None, 'atk1', 'atk2', 'hit', 'move1', 'move2']
EXTRA = ['die1', 'die2', 'tell']     # 정면(s)만


def files_of(boss):
    out = []
    for d in DIRS:
        for f in FRAMES:
            out.append(f'unit_{boss}_{d}.png' if f is None else f'unit_{boss}_{d}_{f}.png')
    for f in EXTRA:
        out.append(f'unit_{boss}_s_{f}.png')
    return out


def holes(im):
    """테두리에서 못 닿는 투명 = 몸 안의 구멍."""
    a = im.getchannel('A')
    w, h = im.size
    px = a.load()
    seen = [[False] * h for _ in range(w)]
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if px[x, y] <= 8 and not seen[x][y]:
                seen[x][y] = True; q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if px[x, y] <= 8 and not seen[x][y]:
                seen[x][y] = True; q.append((x, y))
    while q:
        cx, cy = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and px[nx, ny] <= 8:
                seen[nx][ny] = True; q.append((nx, ny))
    inner = sum(1 for y in range(h) for x in range(w) if px[x, y] <= 8 and not seen[x][y])
    body = sum(1 for y in range(h) for x in range(w) if px[x, y] > 8) + inner
    return inner * 100.0 / max(body, 1)


def palette_of(boss):
    p = os.path.join(PACK, boss, '00_palette.png')
    if not os.path.exists(p):
        return None
    im = Image.open(p).convert('RGB')
    return {im.getpixel((x, y)) for y in range(im.height) for x in range(im.width)}


def measure(im, pal):
    """(반투명 px, 바닥 y, 팔레트 밖 비율%)"""
    px = im.load()
    w, h = im.size
    semi = 0
    bottom = -1
    out = 0
    solid = 0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a <= 8:
                continue
            if a < 250:
                semi += 1
                continue
            solid += 1
            if y > bottom:
                bottom = y
            if pal and not any((r - pr) ** 2 + (g - pg) ** 2 + (b - pb) ** 2
                               <= PALETTE_NEAR ** 2 for pr, pg, pb in pal):
                out += 1
    return semi, bottom, out * 100.0 / max(solid, 1)


def main():
    total_fail = 0
    for boss in BOSSES:
        pal = palette_of(boss)
        print(f'\n=== {boss} === (팔레트 {len(pal) if pal else "없음"}색)')
        print(f'{"파일":<34}{"크기":>10}{"바닥":>7}{"반투명":>8}{"구멍":>8}{"팔레트밖":>9}  판정')
        bottoms = []
        for name in files_of(boss):
            p = os.path.join(IN, name)
            if not os.path.exists(p):
                print(f'{name:<34}{"— 없음":>10}')
                total_fail += 1
                continue
            im = Image.open(p).convert('RGBA')
            semi, bottom, outp = measure(im, pal)
            hp = holes(im)

            bad = []
            if im.size != (SIZE, SIZE):
                bad.append('크기')
            if semi > 0:
                bad.append('반투명')
            if hp > HOLE_MAX:
                bad.append('구멍')
            if outp > 1.0:
                bad.append('팔레트')
            # tell 은 바닥에 까는 표시라 닿는 선 검사에서 뺀다
            if not name.endswith('_tell.png'):
                bottoms.append((name, bottom))
                if abs(bottom - FOOT_Y) > FOOT_TOL:
                    bad.append(f'바닥{bottom}')

            print(f'{name:<34}{f"{im.width}x{im.height}":>10}{bottom:>7}{semi:>8}'
                  f'{hp:>7.1f}%{outp:>8.1f}%  ' + ('반려 ' + '·'.join(bad) if bad else 'OK'))
            if bad:
                total_fail += 1

        if bottoms:
            lo = min(b for _, b in bottoms)
            hi = max(b for _, b in bottoms)
            print(f'  → 닿는 선 {lo}~{hi} (흔들림 {hi-lo}px)')

    print()
    print(f'반려 {total_fail}건' if total_fail else '수치 검사 63장 전부 통과 — 이제 눈으로 본다')


if __name__ == '__main__':
    main()
