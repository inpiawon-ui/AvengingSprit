"""공격·피격 15장 검수 — 워크오더 6차에 미리 공개한 기준 그대로.

핵심은 idle 과의 **차이**다. 공격·피격은 발을 붙인 채 상체만 움직이는 것이라,
발밑·발 중심·머리끝이 idle 과 어긋나면 쏘는 순간 캐릭터가 미끄러지거나 튄다.
그래서 절대값이 아니라 idle 대비 편차로 잰다.
"""
import io
import os
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IN = os.path.join(HERE, '_exchange', 'in')
IDLE = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit', 'rambo')

DIRS = ['s', 'se', 'e', 'ne', 'n']
FRAMES = ['atk1', 'atk2', 'hit']
FOOT_BAND = 14
FOOT_TOL = 0        # 발밑 y 는 idle 과 정확히 같아야 한다
CENTER_TOL = 1
TOP_TOL = 2


def measure(path):
    im = Image.open(path).convert('RGBA')
    px = im.load()
    w, h = im.size
    pts = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] > 8]
    if not pts:
        return None
    ys = [p[1] for p in pts]
    xs = [p[0] for p in pts]
    y0, y1 = min(ys), max(ys)
    feet = [x for x, y in pts if y >= y1 - FOOT_BAND]
    semi = sum(1 for x, y in pts if px[x, y][3] < 248)
    return dict(size=im.size, top=y0, foot=y1, fc=(min(feet) + max(feet)) / 2,
                x0=min(xs), x1=max(xs), semi=semi, im=im)


def palette(im):
    px = im.load()
    w, h = im.size
    return {(px[x, y][0] >> 5, px[x, y][1] >> 5, px[x, y][2] >> 5)
            for y in range(h) for x in range(w) if px[x, y][3] > 8}


def main():
    fail = []
    print(f'{"파일":<24}{"크기":<10}{"발밑":>5}{"발중심":>7}{"머리":>5}'
          f'{"폭":>5}{"반투명":>7}{"새색%":>7}')

    for d in DIRS:
        base = measure(os.path.join(IDLE, f'unit_rambo_{d}.png'))
        bpal = palette(base['im'])
        print(f'  [idle {d}]{"":<14}{str(base["size"]):<10}{base["foot"]:>5}'
              f'{base["fc"]:>7.1f}{base["top"]:>5}{base["x1"]-base["x0"]+1:>5}')

        for f in FRAMES:
            name = f'unit_rambo_{d}_{f}.png'
            p = os.path.join(IN, name)
            if not os.path.exists(p):
                fail.append(f'{name}: 파일 없음')
                continue
            m = measure(p)
            if m is None:
                fail.append(f'{name}: 불투명 픽셀 없음')
                continue
            novel = len(palette(m['im']) - bpal) / max(1, len(palette(m['im'])))
            print(f'{name:<24}{str(m["size"]):<10}{m["foot"]:>5}{m["fc"]:>7.1f}'
                  f'{m["top"]:>5}{m["x1"]-m["x0"]+1:>5}{m["semi"]:>7}{novel*100:>7.1f}')

            if m['size'] != (96, 96):
                fail.append(f'{name}: 크기 {m["size"]}')
            if abs(m['foot'] - base['foot']) > FOOT_TOL:
                fail.append(f'{name}: 발밑 y {m["foot"]} ≠ idle {base["foot"]} '
                            f'— 쏠 때 위아래로 튄다')
            if abs(m['fc'] - base['fc']) > CENTER_TOL:
                fail.append(f'{name}: 발 중심 {m["fc"]:.1f} vs idle {base["fc"]:.1f} '
                            f'— 쏠 때 옆으로 미끄러진다')
            if abs(m['top'] - base['top']) > TOP_TOL:
                fail.append(f'{name}: 머리끝 {m["top"]} vs idle {base["top"]} '
                            f'({m["top"]-base["top"]:+d}px) — 키가 바뀐다')
            if m['x0'] <= 0 or m['x1'] >= 95 or m['top'] <= 0 or m['foot'] >= 95:
                fail.append(f'{name}: 캔버스 가장자리에 닿음 — 잘렸을 수 있다')
            if m['semi'] > 0:
                fail.append(f'{name}: 반투명 픽셀 {m["semi"]}개')
            if novel > 0.35:
                fail.append(f'{name}: 정본에 없는 색 {novel*100:.0f}%')

    print('\n반려' if fail else '\n수치 통과 — 눈으로 확인할 것')
    for x in fail:
        print('  ✗', x)


if __name__ == '__main__':
    main()
