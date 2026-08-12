"""납품 배치 검수 — 방향 5 × 프레임 7을 idle 기준으로 잰다.

절대값이 아니라 **idle 대비 편차**로 재는 것이 핵심이다. 공격·피격은 발을 붙인 채
상체만 움직이는 동작이라, 발밑·발 중심이 idle 과 어긋나면 쏘는 순간 몸이 미끄러진다.

숫자는 보조다. 마지막에 굽는 대조 시트를 **반드시 눈으로 본다** — 지금까지 팔이 세 프레임
내내 안 움직인 것도, 팔레트가 한 색으로 뭉친 것도 숫자는 전부 통과시켰다.

사용: python _verify_batch.py <키> [키 ...]
      키의 idle 5장은 `Assets/BaseResource/Unit/<키>/` 또는 `_exchange/in/` 에 있어야 한다.
"""
import io
import os
import sys
from collections import deque

from PIL import Image, ImageDraw

import _orig_map

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IN = os.path.join(HERE, '_exchange', 'in')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
OUT = os.path.join(HERE, '_exchange', 'out', '_verify')
ORIG = os.path.join(HERE, 'Reference', 'Original')

DIRS = ['s', 'se', 'e', 'ne', 'n']
FRAMES = ['atk1', 'atk2', 'hit', 'walk1', 'walk2', 'die1', 'die2']

# 엘리트는 한 등급 크다. 규격이 통째로 1.33배다.
SIZE = 96
ANCHOR = 48        # 발 중심. 좌우 반전축이라 캔버스 중심이어야 한다
FOOT_Y = 87        # 발밑
HEAD_Y = 12        # 머리끝
ELITE = (128, 64, 118, 16)


def spec(key):
    """(캔버스, 발중심x, 발밑y, 머리끝y)"""
    return ELITE if key.endswith('_elite') else (SIZE, ANCHOR, FOOT_Y, HEAD_Y)
FOOT_BAND = 6      # 발 판정 높이. 14줄로 보면 꼬리·망토가 발로 잡힌다

# 허용 편차 — 넘으면 반려
TOL_FOOT_X = 2
TOL_FOOT_Y = 1
TOL_HEAD = 3
TOL_HEAD_WALK = 3  # 걷기는 상하 흔들림을 허용한다
TOL_HEAD_X = 3     # 걷기 몸통 흔들림. 머리 중심으로 잰다
HOLE_MAX = 3.0     # 몸 안 구멍 비율(%). idle 실측이 0.1~0.6% 다
PALETTE_MAX = 0.06 # 원작에서 먼 색의 면적 비율. 총구 화염 정도만 허용한다

# 원작 색에서 이만큼 안이면 같은 색으로 본다(RGB 거리).
#
# 해상도가 2배가 되면 음영이 더 필요하다. 원작 색 사이의 중간 톤을 못 쓰게 하면
# "퀄만 더 좋게" 라는 요구와 정면으로 충돌한다. 실제로 전 캐릭터가 공통 외곽선
# #181820 하나를 쓰는데, 원작 최암부와 거리 28~39 로 사실상 같은 색이다.
# 새 **색상**을 막는 것이 목적이지 새 **음영**을 막는 것이 아니다.
PALETTE_NEAR = 56.0


def metrics(im):
    """(발중심x, 발밑y, 머리끝y, 불투명픽셀수, 반투명픽셀수, 머리중심x)"""
    px = im.load()
    w, h = im.size
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 8]
    if not pts:
        return None
    y1 = max(p[1] for p in pts)
    y0 = min(p[1] for p in pts)
    feet = [x for x, y in pts if y >= y1 - FOOT_BAND]
    head = [x for x, y in pts if y <= y0 + 20]
    semi = sum(1 for x, y in pts if 8 < px[x, y][3] < 248)
    return ((min(feet) + max(feet)) / 2, y1, y0, len(pts), semi,
            (min(head) + max(head)) / 2)


def holes(im):
    """몸 안에 뚫린 투명 구멍의 비율(%).

    반투명 검사만으로는 못 잡는다. 알파가 0과 255뿐이어도 몸 한가운데가
    송송 뚫려 있으면 화면에서 배경이 비쳐 그림이 삭은 것처럼 보인다.
    실제로 샐러맨더 동작 35장이 몸의 8~45%가 구멍인 채로 검수를 통과했다.

    테두리에서 이어지는 투명은 바깥이다. 거기서 못 닿는 투명만 구멍으로 센다.
    """
    px = im.load()
    w, h = im.size
    seen = [[False] * h for _ in range(w)]
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if px[x, y][3] <= 8 and not seen[x][y]:
                seen[x][y] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if px[x, y][3] <= 8 and not seen[x][y]:
                seen[x][y] = True
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and px[nx, ny][3] <= 8:
                seen[nx][ny] = True
                q.append((nx, ny))

    inner = sum(1 for y in range(h) for x in range(w)
                if px[x, y][3] <= 8 and not seen[x][y])
    opaque = sum(1 for y in range(h) for x in range(w) if px[x, y][3] > 8)
    return inner / max(1, opaque) * 100


def palette(im):
    px = im.load()
    w, h = im.size
    s = set()
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if c[3] > 8:
                s.add(c[:3])
    return s


def origin_palette(key):
    """
    원작 시트의 색 전부. **이것이 그 캐릭터가 쓸 수 있는 색의 전부다.**

    지금까지는 우리 idle 을 기준으로 삼았는데, 그 idle 자체가 원작을 안 보고
    그린 것이라 기준이 될 수 없었다(폭력배 21색·갱스터 74색·샐러맨더 84색).
    원작에 충실하다는 것은 사실상 색이 같다는 뜻이므로 원작을 기준으로 삼는다.

    시트가 없는 캐릭터(정본 신규)는 None — 그때는 idle 기준으로 물러선다.
    """
    sheet = _orig_map.SHEET.get(key)
    if not sheet:
        return None
    p = os.path.join(ORIG, sheet + '.png')
    if not os.path.exists(p):
        return None

    im = Image.open(p).convert('RGB')
    px = im.load()
    w, h = im.size
    # 네 모서리에서 가장 흔한 색 = 배경 키
    from collections import Counter
    c = Counter()
    for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        c[px[x, y]] += 1
    key_rgb = c.most_common(1)[0][0]

    # 오른쪽 아래 "Ripped by" 표는 캐릭터 색이 아니다
    y0, x0 = int(h * 0.78), int(w * 0.60)
    used = Counter()
    for y in range(h):
        for x in range(w):
            if y >= y0 and x >= x0:
                continue
            p2 = px[x, y]
            if p2 != key_rgb:
                used[p2] += 1
    return {rgb for rgb, n in used.items() if n >= 8}


_near_cache = {}


def far_from(rgb, palette):
    """원작 팔레트의 어느 색과도 멀면 True — 그때만 새 색으로 친다."""
    hit = _near_cache.get(rgb)
    if hit is None:
        best = min((rgb[0] - p[0]) ** 2 + (rgb[1] - p[1]) ** 2 + (rgb[2] - p[2]) ** 2
                   for p in palette)
        hit = best > PALETTE_NEAR * PALETTE_NEAR
        _near_cache[rgb] = hit
    return hit


def load(key, d, frame=None):
    n = f'unit_{key}_{d}' + (f'_{frame}' if frame else '') + '.png'
    for base in (IN, os.path.join(UNIT, key)):
        p = os.path.join(base, n)
        if os.path.exists(p):
            return Image.open(p).convert('RGBA'), p
    return None, None


def check(key):
    print(f'\n{"="*74}\n{key}\n{"="*74}')
    canvas, anchor, foot_y, head_y = spec(key)
    _near_cache.clear()
    fails = []
    idle = {}
    for d in DIRS:
        im, p = load(key, d)
        if im is None:
            fails.append(f'{d} idle 없음')
            continue
        idle[d] = (im, metrics(im), palette(im))

    if len(idle) < len(DIRS):
        print('  idle 이 모자라 비교 기준을 못 세운다')
        return fails

    # 원작 팔레트가 있으면 그것이 기준이다. 없으면 우리 idle 로 물러선다.
    origin = origin_palette(key)
    if origin:
        base_pal = origin
        print(f'  원작 팔레트 {len(base_pal)}색 기준')
    else:
        base_pal = set()
        for d in idle:
            base_pal |= idle[d][2]
        print(f'  원작 시트 없음 — idle 팔레트 {len(base_pal)}색 기준')

    for d in DIRS:
        im0, m0, ipal = idle[d]
        # idle 자체도 원작 팔레트를 지켜야 한다. 지금까지는 idle 이 기준이라 검사에서 빠져 있었다.
        if origin:
            px0 = im0.load()
            n0 = sum(1 for y in range(canvas) for x in range(canvas) if px0[x, y][3] > 8)
            bad0 = sum(1 for y in range(canvas) for x in range(canvas)
                       if px0[x, y][3] > 8 and far_from(px0[x, y][:3], origin))
            if bad0 / max(1, n0) > PALETTE_MAX:
                fails.append(f'{d}_idle 팔레트 밖 색 {bad0/n0*100:.1f}%')
        for f in FRAMES:
            im, p = load(key, d, f)
            tag = f'{d}_{f}'
            if im is None:
                fails.append(f'{tag} 없음')
                continue
            if im.size != (canvas, canvas):
                fails.append(f'{tag} 크기 {im.size} (기준 {canvas})')
                continue
            m = metrics(im)
            if m is None:
                fails.append(f'{tag} 빈 그림')
                continue
            fx, fy, hy, n, semi, hx = m

            if semi:
                fails.append(f'{tag} 반투명 {semi}px (이진 알파여야 한다)')

            # 몸에 구멍이 뚫렸는가. idle 은 0.1~0.6% 가 정상 범위다(눈·입 같은 실제 구멍).
            # 그보다 크면 그림이 삭은 것이라 화면에서 배경이 비친다.
            hole = holes(im)
            if hole > HOLE_MAX:
                fails.append(f'{tag} 몸에 구멍 {hole:.1f}% — 배경이 비친다')

            # 가로 기준은 동작마다 다르다. 하나로 재면 멀쩡한 것을 반려한다.
            #   공격·피격 — 발을 붙인 채 상체만 움직이니 **발 중심**
            #   걷기       — 발이 번갈아 벌어지는 게 정상. 흔들리면 안 되는 건 **몸통** → 머리 중심
            #   사망       — 무릎에서 옆으로 무너지므로 가로는 재지 않는다
            if f.startswith('walk'):
                if abs(hx - m0[5]) > TOL_HEAD_X:
                    fails.append(f'{tag} 머리중심 {hx:.1f} (idle {m0[5]:.1f}) — 걸을 때 몸이 흔들린다')
            elif not f.startswith('die'):
                if abs(fx - anchor) > TOL_FOOT_X:
                    fails.append(f'{tag} 발중심 {fx:.1f} (기준 {anchor})')

            if f.startswith('die'):
                if fy != foot_y:
                    fails.append(f'{tag} 발밑 {fy} (기준 {foot_y}) — 시체가 바닥에서 뜬다')
            elif abs(fy - m0[1]) > TOL_FOOT_Y:
                fails.append(f'{tag} 발밑 {fy} (idle {m0[1]})')

            tolh = TOL_HEAD_WALK if f.startswith('walk') else TOL_HEAD
            if not f.startswith('die') and abs(hy - m0[2]) > tolh:
                fails.append(f'{tag} 머리끝 {hy} (idle {m0[2]}, 허용 ±{tolh})')

            # 새 색은 면적으로 잰다. 색 가짓수로 재면 5px짜리 총구 화염이 56% 로 나온다.
            px = im.load()
            novel = sum(1 for y in range(canvas) for x in range(canvas)
                        if px[x, y][3] > 8 and far_from(px[x, y][:3], base_pal))
            if novel / max(1, n) > PALETTE_MAX:
                fails.append(f'{tag} 팔레트 밖 색 {novel/n*100:.1f}%')

        # 공격 2프레임이 실제로 다른가 — 팔만 까딱하는 것을 잡는다
        a1, _ = load(key, d, 'atk1')
        a2, _ = load(key, d, 'atk2')
        if a1 and a2:
            b1, b2 = a1.tobytes(), a2.tobytes()
            diff = sum(1 for i in range(0, len(b1), 4) if b1[i:i+4] != b2[i:i+4])
            if diff < 120:
                fails.append(f'{d} atk1↔atk2 차이 {diff}px — 거의 같은 그림이다')
        w1, _ = load(key, d, 'walk1')
        w2, _ = load(key, d, 'walk2')
        if w1 and w2:
            b1, b2 = w1.tobytes(), w2.tobytes()
            diff = sum(1 for i in range(0, len(b1), 4) if b1[i:i+4] != b2[i:i+4])
            if diff < 120:
                fails.append(f'{d} walk1↔walk2 차이 {diff}px — 걸음이 안 보인다')

    print(f'  {"반려 " + str(len(fails)) + "건" if fails else "수치 통과"}')
    for m in fails:
        print(f'    · {m}')
    return fails


def sheet(key):
    """방향 5행 × 프레임 8열 대조 시트. 눈으로 보는 것이 마지막 관문이다."""
    os.makedirs(OUT, exist_ok=True)
    z, cw, ch = 3, SIZE * 3 + 6, SIZE * 3 + 22
    cols = ['idle'] + FRAMES
    img = Image.new('RGB', (cw * len(cols) + 8, ch * len(DIRS) + 26), (22, 19, 34))
    d = ImageDraw.Draw(img)
    for ci, c in enumerate(cols):
        d.text((ci * cw + 10, 8), c, fill=(200, 190, 230))
    for ri, dr in enumerate(DIRS):
        y = 26 + ri * ch
        d.text((2, y + 4), dr, fill=(150, 230, 190))
        for ci, c in enumerate(cols):
            im, _ = load(key, dr, None if c == 'idle' else c)
            if im is None:
                continue
            big = im.resize((SIZE * z, SIZE * z), Image.NEAREST)
            img.paste(big, (ci * cw + 6, y + 16), big)
            # 발밑·중심선 — 어긋나면 여기서 바로 보인다
            d.line([(ci * cw + 6 + ANCHOR * z, y + 16),
                    (ci * cw + 6 + ANCHOR * z, y + 16 + SIZE * z)], fill=(90, 70, 130))
            d.line([(ci * cw + 6, y + 16 + FOOT_Y * z),
                    (ci * cw + 6 + SIZE * z, y + 16 + FOOT_Y * z)], fill=(90, 70, 130))
    p = os.path.join(OUT, f'{key}_sheet.png')
    img.save(p)
    print(f'  대조 시트: {p}')
    return p


def main():
    keys = sys.argv[1:]
    if not keys:
        print('키를 지정할 것: python _verify_batch.py thug')
        return
    total = 0
    for k in keys:
        total += len(check(k))
        sheet(k)
    print(f'\n총 반려 {total}건')


if __name__ == '__main__':
    main()
