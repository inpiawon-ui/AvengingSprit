"""2차 납품(잘림 12 · 뭉갬 5) 검수 — 워크오더에 공표한 기준 그대로.

`_audit_crop.py`·`_audit_blur.py` 와 같은 잣대를 납품본에 그대로 들이댄다.
납품자가 자체 PASS 를 보내와도 그건 참고만 한다. 1차 때 "정상"이라던 것이
실제로는 코드 도형이었다.

수치를 통과해도 마지막은 눈이다. 내용이 맞는지는 재서 알 수 없다.
"""
import os
import sys
from collections import deque
from PIL import Image, ImageChops, ImageStat

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '_exchange', 'in')

CLIPPED = ['missiontabicon', 'friendstabicon', 'friendstablock', 'progressrewardchest',
           'staminaicon', 'bossportrait', 'hostslotlockicon', 'ownedhostchesticon',
           'hostupgradeicon', 'possessghosticon', 'staticon_dash', 'headerportaldeco']
BLURRED = ['unit_boss', 'pausebutton', 'rankingtabicon', 'mailbutton', 'shot']


def edge_touch(im):
    w, h = im.size
    a = im.getchannel('A').load()
    border = [(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)] \
        + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]
    return sum(1 for x, y in border if a[x, y] > 200) / len(border)


def detached(im):
    w, h = im.size
    a = im.getchannel('A').load()
    seen = [[False] * h for _ in range(w)]
    blobs = []
    for sy in range(h):
        for sx in range(w):
            if seen[sx][sy] or a[sx, sy] <= 24:
                continue
            q, n = deque([(sx, sy)]), 0
            seen[sx][sy] = True
            while q:
                x, y = q.popleft(); n += 1
                for nx, ny in ((x+1, y), (x-1, y), (x, y+1), (x, y-1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and a[nx, ny] > 24:
                        seen[nx][ny] = True; q.append((nx, ny))
            blobs.append(n)
    if not blobs:
        return 1.0
    blobs.sort(reverse=True)
    return sum(blobs[1:]) / sum(blobs)


def interp(im, tol=10):
    rgb = im.convert('RGB')
    w, h = rgb.size
    px = rgb.load()
    a = im.getchannel('A').load()
    hit = tot = 0
    for y in range(h):
        for x in range(1, w - 1):
            if a[x, y] < 200:
                continue
            l, c, r = px[x - 1, y], px[x, y], px[x + 1, y]
            if l == r:
                continue
            tot += 1
            if all(abs(c[i] - (l[i] + r[i]) / 2) <= tol for i in range(3)) \
               and any(abs(l[i] - r[i]) > 2 * tol for i in range(3)):
                hit += 1
    return hit / tot if tot else 0.0


def symmetry(im):
    a = im.convert('RGB')
    diff = ImageChops.difference(a, a.transpose(Image.FLIP_LEFT_RIGHT))
    return 1.0 - ImageStat.Stat(diff).mean[0] / 255.0


def main():
    fails = []
    print(f'{"파일":<26}{"크기":>10}{"잘림":>8}{"뭉갬":>8}{"덧붙음":>9}{"대칭":>8}{"색":>6}  판정')
    for group, names in (('잘림 복원', CLIPPED), ('뭉갬 재제작', BLURRED)):
        print(f'\n── {group} ──')
        for n in names:
            p = os.path.join(IN, n + '.png')
            if not os.path.exists(p):
                print(f'{n:<26}{"— 미납품":>10}')
                fails.append((n, '미납품'))
                continue
            im = Image.open(p).convert('RGBA')
            if im.getchannel('A').getextrema()[0] > 250:
                print(f'{n:<26}{f"{im.width}x{im.height}":>10}{"":>8}{"":>8}{"":>9}'
                      f'{"":>8}{"":>6}  반려 알파없음')
                fails.append((n, '알파없음'))
                continue

            t, b, s = edge_touch(im), interp(im), detached(im)
            sym = symmetry(im)
            ncol = len(im.convert('RGB').getcolors(maxcolors=1 << 20))

            bad = []
            if t >= 0.05:
                bad.append('잘림')
            if b >= 0.16:
                bad.append('뭉갬')
            if s >= 0.03:
                bad.append('덧붙음')
            if sym >= 0.98 and ncol <= 6:
                bad.append('코드도형')

            print(f'{n:<26}{f"{im.width}x{im.height}":>10}{t*100:>7.1f}%{b*100:>7.1f}%'
                  f'{s*100:>8.1f}%{sym*100:>7.1f}%{ncol:>6}  '
                  + ('반려 ' + '·'.join(bad) if bad else 'OK'))
            if bad:
                fails.append((n, '·'.join(bad)))

    print()
    if fails:
        print(f'반려 {len(fails)}건 — ' + ', '.join(f'{n}({w})' for n, w in fails))
        sys.exit(1)
    print('수치 검사 17종 전부 통과. 이제 눈으로 본다')


if __name__ == '__main__':
    main()
