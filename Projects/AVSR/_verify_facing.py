"""8방향 5장 검수 — 워크오더 4차에 미리 공개한 반려 기준 그대로.

발밑 y 와 가로 중심 x 가 장마다 어긋나면, 방향을 바꿀 때 캐릭터가
제자리에서 들썩이거나 옆으로 튄다. 이게 8방향에서 가장 흔한 실패라
숫자로 먼저 거른 뒤 눈으로 본다.

팔레트 비교는 정본(unit_rambo.png)과의 색 분포 거리다. 각도가 바뀌면
색 비중은 달라지지만 **쓰는 색 자체**는 같아야 한다 — 다른 사람이면
정본에 없는 색이 대량으로 나온다.
"""
import io
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IN = os.path.join(HERE, '_exchange', 'in')
REF = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI', 'unit_rambo.png')

DIRS = ['s', 'se', 'e', 'ne', 'n']
SIZE = (96, 96)
FOOT_TOL = 2      # 발밑 y 편차 허용
CENTER_TOL = 3    # 가로 중심 x 편차 허용


def bbox(im, thresh=8):
    px = im.load()
    w, h = im.size
    xs, ys = [], []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] > thresh:
                xs.append(x)
                ys.append(y)
    if not xs:
        return None
    return min(xs), min(ys), max(xs), max(ys)


def palette(im, thresh=8):
    """불투명 픽셀의 색 집합 — 8단계로 뭉쳐 비슷한 색은 같게 본다."""
    px = im.load()
    w, h = im.size
    out = set()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a > thresh:
                out.add((r >> 5, g >> 5, b >> 5))
    return out


def main():
    ref = Image.open(REF).convert('RGBA')
    refpal = palette(ref)

    rows, fail = [], []
    for d in DIRS:
        p = os.path.join(IN, f'unit_rambo_{d}.png')
        if not os.path.exists(p):
            fail.append(f'{d}: 파일 없음')
            continue
        im = Image.open(p).convert('RGBA')
        bb = bbox(im)
        if bb is None:
            fail.append(f'{d}: 불투명 픽셀 없음')
            continue
        x0, y0, x1, y1 = bb
        pal = palette(im)
        # 정본에 없는 색이 얼마나 되나 — 클수록 다른 사람일 가능성
        novel = len(pal - refpal) / max(1, len(pal))
        rows.append(dict(d=d, size=im.size, foot=y1, cx=(x0 + x1) / 2,
                         h=y1 - y0 + 1, w=x1 - x0 + 1, novel=novel,
                         opaque=all(im.getdata(3)) if im.mode == 'RGBA' else True,
                         bottom_gap=im.size[1] - 1 - y1))

    print(f'{"방향":<5}{"크기":<12}{"발밑y":>6}{"밑여백":>7}{"중심x":>7}'
          f'{"높이":>6}{"폭":>6}{"새색%":>7}')
    for r in rows:
        print(f'{r["d"]:<5}{str(r["size"]):<12}{r["foot"]:>6}{r["bottom_gap"]:>7}'
              f'{r["cx"]:>7.1f}{r["h"]:>6}{r["w"]:>6}{r["novel"]*100:>7.1f}')

    if rows:
        foots = [r['foot'] for r in rows]
        cxs = [r['cx'] for r in rows]
        hs = [r['h'] for r in rows]
        fd, cd = max(foots) - min(foots), max(cxs) - min(cxs)
        print(f'\n발밑 편차 {fd}px (허용 {FOOT_TOL})   '
              f'중심 편차 {cd:.1f}px (허용 {CENTER_TOL})   '
              f'높이 {min(hs)}~{max(hs)}px')

        for r in rows:
            if r['size'] != SIZE:
                fail.append(f'{r["d"]}: 크기 {r["size"]} — 96x96 아님')
            if not any(a < 250 for a in Image.open(
                    os.path.join(IN, f'unit_rambo_{r["d"]}.png')).convert('RGBA').getdata(3)):
                fail.append(f'{r["d"]}: 알파 없음(전부 불투명)')
            if r['novel'] > 0.35:
                fail.append(f'{r["d"]}: 정본에 없는 색 {r["novel"]*100:.0f}% — 다른 인물 의심')
        if fd > FOOT_TOL:
            fail.append(f'발밑 편차 {fd}px > {FOOT_TOL} — 방향 전환 시 위아래로 튄다')
        if cd > CENTER_TOL:
            fail.append(f'중심 편차 {cd:.1f}px > {CENTER_TOL} — 방향 전환 시 옆으로 튄다')

    print('\n반려' if fail else '\n수치 통과 — 눈으로 확인할 것')
    for f in fail:
        print('  ✗', f)


if __name__ == '__main__':
    main()
