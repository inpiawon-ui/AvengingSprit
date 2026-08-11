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

from PIL import Image, ImageDraw

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IN = os.path.join(HERE, '_exchange', 'in')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
OUT = os.path.join(HERE, '_exchange', 'out', '_verify')

DIRS = ['s', 'se', 'e', 'ne', 'n']
FRAMES = ['atk1', 'atk2', 'hit', 'walk1', 'walk2', 'die1', 'die2']

SIZE = 96
ANCHOR = 48        # 발 중심. 좌우 반전축이라 캔버스 중심이어야 한다
FOOT_Y = 87        # 발밑
HEAD_Y = 12        # 머리끝
FOOT_BAND = 6      # 발 판정 높이. 14줄로 보면 꼬리·망토가 발로 잡힌다

# 허용 편차 — 넘으면 반려
TOL_FOOT_X = 2
TOL_FOOT_Y = 1
TOL_HEAD = 3
TOL_HEAD_WALK = 3  # 걷기는 상하 흔들림을 허용한다
TOL_HEAD_X = 3     # 걷기 몸통 흔들림. 머리 중심으로 잰다


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


def load(key, d, frame=None):
    n = f'unit_{key}_{d}' + (f'_{frame}' if frame else '') + '.png'
    for base in (IN, os.path.join(UNIT, key)):
        p = os.path.join(base, n)
        if os.path.exists(p):
            return Image.open(p).convert('RGBA'), p
    return None, None


def check(key):
    print(f'\n{"="*74}\n{key}\n{"="*74}')
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

    base_pal = set()
    for d in idle:
        base_pal |= idle[d][2]
    print(f'  idle 팔레트 {len(base_pal)}색')

    for d in DIRS:
        im0, m0, _ = idle[d]
        for f in FRAMES:
            im, p = load(key, d, f)
            tag = f'{d}_{f}'
            if im is None:
                fails.append(f'{tag} 없음')
                continue
            if im.size != (SIZE, SIZE):
                fails.append(f'{tag} 크기 {im.size}')
                continue
            m = metrics(im)
            if m is None:
                fails.append(f'{tag} 빈 그림')
                continue
            fx, fy, hy, n, semi, hx = m

            if semi:
                fails.append(f'{tag} 반투명 {semi}px (이진 알파여야 한다)')

            # 가로 기준은 동작마다 다르다. 하나로 재면 멀쩡한 것을 반려한다.
            #   공격·피격 — 발을 붙인 채 상체만 움직이니 **발 중심**
            #   걷기       — 발이 번갈아 벌어지는 게 정상. 흔들리면 안 되는 건 **몸통** → 머리 중심
            #   사망       — 무릎에서 옆으로 무너지므로 가로는 재지 않는다
            if f.startswith('walk'):
                if abs(hx - m0[5]) > TOL_HEAD_X:
                    fails.append(f'{tag} 머리중심 {hx:.1f} (idle {m0[5]:.1f}) — 걸을 때 몸이 흔들린다')
            elif not f.startswith('die'):
                if abs(fx - ANCHOR) > TOL_FOOT_X:
                    fails.append(f'{tag} 발중심 {fx:.1f} (기준 {ANCHOR})')

            if f.startswith('die'):
                if fy != FOOT_Y:
                    fails.append(f'{tag} 발밑 {fy} (기준 {FOOT_Y}) — 시체가 바닥에서 뜬다')
            elif abs(fy - m0[1]) > TOL_FOOT_Y:
                fails.append(f'{tag} 발밑 {fy} (idle {m0[1]})')

            tolh = TOL_HEAD_WALK if f.startswith('walk') else TOL_HEAD
            if not f.startswith('die') and abs(hy - m0[2]) > tolh:
                fails.append(f'{tag} 머리끝 {hy} (idle {m0[2]}, 허용 ±{tolh})')

            # 새 색은 면적으로 잰다. 색 가짓수로 재면 5px짜리 총구 화염이 56% 로 나온다.
            px = im.load()
            novel = sum(1 for y in range(SIZE) for x in range(SIZE)
                        if px[x, y][3] > 8 and px[x, y][:3] not in base_pal)
            if novel / max(1, n) > 0.06:
                fails.append(f'{tag} idle 밖 색 {novel/n*100:.1f}%')

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
