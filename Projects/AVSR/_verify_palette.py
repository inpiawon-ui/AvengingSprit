"""납품 스프라이트를 **목업 기준**으로 검수한다.

이전 검수기는 `unit_*.png` 를 정본으로 삼아 "정본에 없는 색" 비율을 봤다.
그 정본이 이미 색이 뭉개진 상태였으므로, 검수기가 4차 내내 잘못된 팔레트를
통과시켰다. 정본은 승인 목업(`host_select.jpeg`)이다.

사용: python _verify_palette.py [키 ...]     (생략하면 12종 전부)
"""
import colorsys
import io
import os
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
MOCK = os.path.join(HERE, 'Reference', 'Mockups', 'host_select.jpeg')
IN = os.path.join(HERE, '_exchange', 'in')

# 목업 호스트 목록 격자 (실측). 열2·행1 = 람보(기관총) 기준으로 맞췄다.
X0, Y0, W, H = 122, 232, 96, 88
PX, PY = 105, 131
SLOTS = {
    'amazoness': (0, 0), 'rambo': (0, 1), 'wizard': (1, 0), 'ninja': (1, 2),
    'mafia': (2, 1), 'hitman': (2, 2), 'yogamaster': (3, 0), 'dragon': (3, 1),
    'robot': (3, 2), 'snowwoman': (4, 0), 'slugger': (4, 1), 'vampire': (4, 2),
}

DIRS = ['s', 'se', 'e', 'ne', 'n']
FAM = [(0, 15, '빨강'), (15, 45, '주황'), (45, 70, '노랑'), (70, 160, '초록'),
       (160, 250, '파랑'), (250, 330, '보라'), (330, 360, '빨강')]

# ⚠ 방향마다 보이는 재질이 다르다. 목업 크롭은 총을 몸 앞에 가로로 든 정면
#   자세라 회색이 가장 많이 보이는 프레임이다. 후면(n)은 총이 등 뒤로 가려져
#   회색이 1% 로 떨어지는 게 정상이다. 그래서 **방향마다** 목업 비율을 요구하면
#   멀쩡한 그림을 반려한다(실제로 그랬다).
#   판정은 "다섯 장을 통틀어 그 재질이 살아 있는가"로 본다.
GREY_FLOOR = 0.5      # 가장 잘 보이는 방향이 목업의 이 배수는 되어야 한다
GREY_WATCH = 10.0     # 목업 무채색이 이 값 이상인 캐릭터만 무채색을 본다
FAM_ALIVE = 3.0       # 목업의 주요 색상군이 어느 방향에서든 이 % 는 되어야 한다
FOOT_Y, CENTER_X = 87, 48


def spectrum(im, alpha):
    px = im.load()
    w, h = im.size
    f = {}
    grey = tot = 0
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if alpha and c[3] <= 8:
                continue
            hh, s, v = colorsys.rgb_to_hsv(c[0] / 255, c[1] / 255, c[2] / 255)
            if v < 0.18:
                continue                       # 배경·외곽선
            tot += 1
            if s < 0.18:
                grey += 1
                continue
            d = hh * 360
            for lo, hi, n in FAM:
                if lo <= d < hi:
                    f[n] = f.get(n, 0) + 1
                    break
    if not tot:
        return {}, 0.0
    return ({k: v / tot * 100 for k, v in sorted(f.items(), key=lambda kv: -kv[1])},
            grey / tot * 100)


def geometry(im):
    px = im.load()
    w, h = im.size
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 8]
    if not pts:
        return None
    ys = [p[1] for p in pts]
    xs = [p[0] for p in pts]
    y1 = max(ys)
    feet = [x for x, y in pts if y >= y1 - 14]
    semi = sum(1 for x, y in pts if px[x, y][3] < 248)
    return dict(size=im.size, top=min(ys), foot=y1, fc=(min(feet) + max(feet)) / 2,
                x0=min(xs), x1=max(xs), semi=semi)


def check(key):
    r, c = SLOTS[key]
    mock = Image.open(MOCK).convert('RGB').crop(
        (X0 + (c - 1) * PX, Y0 + r * PY, X0 + (c - 1) * PX + W, Y0 + r * PY + H))
    mf, mg = spectrum(mock, alpha=False)
    mmain = [k for k, v in mf.items() if v >= 5]

    print(f'\n=== {key} ===')
    print(f'  목업: ' + ' · '.join(f'{k} {v:.0f}%' for k, v in mf.items() if v >= 5)
          + f'  무채 {mg:.0f}%')

    fails = []
    tops, semis = [], []
    greys, seen = [], {}
    for d in DIRS:
        p = os.path.join(IN, f'unit_{key}_{d}.png')
        if not os.path.exists(p):
            fails.append(f'{d}: 파일 없음')
            continue
        im = Image.open(p).convert('RGBA')
        uf, ug = spectrum(im, alpha=True)
        g = geometry(im)
        umain = [k for k, v in uf.items() if v >= 5]
        tops.append(g['top'])
        semis.append(g['semi'])
        greys.append(ug)
        for k, v in uf.items():
            seen[k] = max(seen.get(k, 0.0), v)

        print(f'  {d:<3} ' + ' · '.join(f'{k} {v:.0f}%' for k, v in uf.items() if v >= 5)
              + f'  무채 {ug:.0f}%   발밑{g["foot"]} 중심{g["fc"]:.1f} 머리{g["top"]}')

        if g['size'] != (96, 96):
            fails.append(f'{d}: 크기 {g["size"]}')
        if g['foot'] != FOOT_Y:
            fails.append(f'{d}: 발밑 y={g["foot"]} (기준 {FOOT_Y})')
        if abs(g['fc'] - CENTER_X) > 1:
            fails.append(f'{d}: 발 중심 {g["fc"]:.1f} (기준 {CENTER_X})')
        if g['semi'] > 0:
            fails.append(f'{d}: 반투명 픽셀 {g["semi"]}개')
        if g['x0'] <= 0 or g['x1'] >= 95 or g['top'] <= 0 or g['foot'] >= 95:
            fails.append(f'{d}: 캔버스 가장자리에 닿음')

    if tops and max(tops) - min(tops) > 2:
        fails.append(f'머리끝이 방향마다 다름 {min(tops)}~{max(tops)} — 방향 바꿀 때 튄다')

    # 색은 다섯 장을 통틀어 본다 (방향마다 보이는 재질이 다르므로)
    if greys and mg >= GREY_WATCH and max(greys) < mg * GREY_FLOOR:
        fails.append(f'무채색 최대 {max(greys):.0f}% — 목업 {mg:.0f}% 의 절반 미만. '
                     f'회색·검정·흰색이 물들었다')
    for k in mmain:
        if seen.get(k, 0.0) < FAM_ALIVE:
            fails.append(f'색상군 {k} 이 어느 방향에도 없다 (목업 {mf[k]:.0f}%)')
    return fails


def main():
    keys = sys.argv[1:] or list(SLOTS)
    allfail = []
    for k in keys:
        if k not in SLOTS:
            print(f'알 수 없는 키: {k}')
            continue
        f = check(k)
        allfail += [f'{k} {x}' for x in f]
    print('\n반려' if allfail else '\n전부 통과 — 눈으로 확인할 것')
    for x in allfail:
        print('  ✗', x)


if __name__ == '__main__':
    main()
