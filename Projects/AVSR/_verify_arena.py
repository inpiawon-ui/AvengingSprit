"""보스 아레나 10장 검수 — 20차 워크오더 기준.

캐릭터 검수와 보는 것이 다르다.
  장판   원형인가 · 알파가 단계로 끊겨 있는가 · 안쪽이 옅고 테두리가 진한가
  바닥   전면 불투명인가 · **이음매 없이 반복되는가** · 기존 바닥보다 어두운가
  레이저 세로로 이어 붙일 수 있는가

이음매 검사는 눈으로 못 잡는다. 타일을 2×2 로 깔아 보고 "좀 어색한데" 로
넘어가면 방 전체에 격자가 생긴 뒤에야 안다. 그래서 수치로 잰다 —
**안쪽 이웃 열끼리의 차이**를 기준으로 삼고, **맨 끝 열과 첫 열의 차이**가
그보다 훨씬 크면 이음매가 있는 것이다.

사용: python _verify_arena.py
"""
import io
import os
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IN = os.path.join(HERE, '_exchange', 'in')
HUD = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

FIELDS = ['field_burn', 'field_freeze', 'field_curse', 'field_slow', 'field_damage']
FLOORS = ['roomfloor_robot_snakes', 'roomfloor_demolisher', 'roomfloor_python']
LASERS = ['obj_laser_crack', 'obj_laser_beam']

ALPHA_STEPS_MAX = 10   # 4~6 단계로 지시했다. 10 을 넘으면 그라데이션을 쪼갠 것이다
SEAM_RATIO = 2.5       # 이음매 차이가 안쪽 평균의 이 배를 넘으면 반려


def cols(im, x):
    px = im.load()
    return [px[x, y][:3] for y in range(im.height)]


def rows(im, y):
    px = im.load()
    return [px[x, y][:3] for x in range(im.width)]


def diff(a, b):
    return sum(abs(p[0] - q[0]) + abs(p[1] - q[1]) + abs(p[2] - q[2])
               for p, q in zip(a, b)) / (len(a) * 3.0)


def seam(im, horizontal=True):
    """(이음매 차이, 안쪽 평균 차이). 앞이 뒤보다 훨씬 크면 이음매가 보인다."""
    n = im.width if horizontal else im.height
    get = (lambda i: cols(im, i)) if horizontal else (lambda i: rows(im, i))
    inner = [diff(get(i), get(i + 1)) for i in range(1, min(n - 2, 24))]
    wrap = diff(get(n - 1), get(0))
    return wrap, sum(inner) / max(len(inner), 1)


def alpha_steps(im):
    return len({p[3] for p in im.convert('RGBA').getdata() if p[3] > 0})


def ring_profile(im):
    """(안쪽 평균 알파, 테두리 평균 알파). 테두리가 진해야 영역으로 읽힌다."""
    px = im.load()
    c = (im.width - 1) / 2.0
    inner = []
    edge = []
    for y in range(im.height):
        for x in range(im.width):
            a = px[x, y][3]
            if a == 0:
                continue
            r = ((x - c) ** 2 + (y - c) ** 2) ** 0.5 / c
            (inner if r < 0.45 else edge if r > 0.8 else []).append(a)
    return (sum(inner) / max(len(inner), 1)), (sum(edge) / max(len(edge), 1))


def mean_luma(im):
    px = im.convert('RGB').load()
    t = 0
    for y in range(0, im.height, 2):
        for x in range(0, im.width, 2):
            r, g, b = px[x, y]
            t += (r * 299 + g * 587 + b * 114) / 1000
    return t / ((im.height // 2) * (im.width // 2))


def main():
    fails = []

    print('── 장판 5종 ' + '─' * 46)
    for n in FIELDS:
        p = os.path.join(IN, n + '.png')
        if not os.path.exists(p):
            print(f'{n:<26} — 없음'); fails.append(n); continue
        im = Image.open(p).convert('RGBA')
        steps = alpha_steps(im)
        inner, edge = ring_profile(im)
        bad = []
        if im.size != (128, 128):
            bad.append(f'크기{im.size}')
        if steps > ALPHA_STEPS_MAX:
            bad.append(f'알파{steps}단계')
        if edge <= inner:
            bad.append('테두리가 안쪽보다 옅다')
        print(f'{n:<26} {im.width}x{im.height} 알파{steps:>3}단계 '
              f'안쪽{inner:>5.0f} 테두리{edge:>5.0f}  '
              + ('반려 ' + '·'.join(bad) if bad else 'OK'))
        if bad:
            fails.append(n)

    print('\n── 보스방 바닥 3종 ' + '─' * 40)
    base = os.path.join(HUD, 'roomfloor.png')
    base_luma = mean_luma(Image.open(base)) if os.path.exists(base) else 999
    print(f'(기준 roomfloor 밝기 {base_luma:.0f})')
    for n in FLOORS:
        p = os.path.join(IN, n + '.png')
        if not os.path.exists(p):
            print(f'{n:<26} — 없음'); fails.append(n); continue
        im = Image.open(p).convert('RGBA')
        clear = sum(1 for p2 in im.getdata() if p2[3] < 250)
        wx, ix = seam(im, True)
        wy, iy = seam(im, False)
        luma = mean_luma(im)
        bad = []
        if im.size != (256, 256):
            bad.append(f'크기{im.size}')
        if clear > 0:
            bad.append(f'투명{clear}px')
        if wx > ix * SEAM_RATIO:
            bad.append(f'좌우이음매({wx:.0f} vs {ix:.0f})')
        if wy > iy * SEAM_RATIO:
            bad.append(f'상하이음매({wy:.0f} vs {iy:.0f})')
        if luma > base_luma:
            bad.append(f'밝다({luma:.0f}>{base_luma:.0f})')
        print(f'{n:<26} {im.width}x{im.height} 밝기{luma:>5.0f} '
              f'좌우{wx:>5.1f}/{ix:<5.1f} 상하{wy:>5.1f}/{iy:<5.1f}  '
              + ('반려 ' + '·'.join(bad) if bad else 'OK'))
        if bad:
            fails.append(n)

    print('\n── 팝업 레이저 2종 ' + '─' * 40)
    for n in LASERS:
        p = os.path.join(IN, n + '.png')
        if not os.path.exists(p):
            print(f'{n:<26} — 없음'); fails.append(n); continue
        im = Image.open(p).convert('RGBA')
        wy, iy = seam(im, False)
        bad = []
        if im.size != (96, 512):
            bad.append(f'크기{im.size}')
        if wy > iy * SEAM_RATIO:
            bad.append(f'세로이음매({wy:.0f} vs {iy:.0f})')
        print(f'{n:<26} {im.width}x{im.height} 세로{wy:>5.1f}/{iy:<5.1f}  '
              + ('반려 ' + '·'.join(bad) if bad else 'OK'))
        if bad:
            fails.append(n)

    print()
    print(f'반려 {len(fails)}건: {", ".join(fails)}' if fails
          else '수치 검사 10장 전부 통과 — 이제 눈으로 본다')


if __name__ == '__main__':
    main()
