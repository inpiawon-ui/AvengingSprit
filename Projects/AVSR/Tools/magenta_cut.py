# -*- coding: utf-8 -*-
"""마젠타 바탕 시트에서 칸을 잘라 투명 PNG 로 — 납품 그림은 배경이 안 뚫려 온다(메모 delivered-art-needs-alpha-pass).

  python magenta_cut.py grid <시트> <열> <행> <출력 폴더> <이름틀 예: fx_burst_{n}> [--size 192] [--fit W H BOTTOM]
     칸을 왼쪽 위부터 차례로 잘라 {n}=1,2,… 로 저장한다.
     --size S       : 정사각 S×S 로 줄인다(칸 비율 그대로)
     --fit W H B    : 그림 덩어리(bbox)를 폭 W 에 맞춰 줄이고 W×H 캔버스에 가운데 · 바닥선 B 에 앉힌다(유닛 그림용)
  python magenta_cut.py boxes <시트> <출력 폴더>   덩어리를 찾아 box_{n}.png 로 저장하고 좌표를 찍는다(부품 시트용)
"""
import os, sys
from PIL import Image


def key(im):
    """마젠타와 그 언저리를 투명으로, 가장자리의 분홍 물듦을 걷는다."""
    im = im.convert('RGBA')
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            # 마젠타 거리 — 빨강 · 파랑이 높고 초록이 낮을수록 바탕
            mag = min(r, b) - g
            if mag > 90 and r > 150 and b > 150:
                px[x, y] = (0, 0, 0, 0)
            elif mag > 40 and r > 110 and b > 110:
                # 언저리 — 반투명으로 두고 분홍을 빼 준다
                k = (mag - 40) / 50.0
                na = int(255 * (1 - k))
                nr = min(r, g + (r - g) // 3)
                nb = min(b, g + (b - g) // 3)
                px[x, y] = (nr, g, nb, na)
    return im


def cells(path, cols, rows):
    im = Image.open(path)
    cw, ch = im.width // cols, im.height // rows
    for r in range(rows):
        for c in range(cols):
            yield im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))


def fit(im, W, H, bottom):
    bb = im.getbbox()
    if not bb:
        return Image.new('RGBA', (W, H))
    part = im.crop(bb)
    s = W / part.width
    if part.height * s > bottom:
        s = bottom / part.height
    part = part.resize((max(1, round(part.width * s)), max(1, round(part.height * s))), Image.LANCZOS)
    out = Image.new('RGBA', (W, H))
    out.paste(part, ((W - part.width) // 2, bottom - part.height), part)
    return out


def main():
    mode = sys.argv[1]
    if mode == 'grid':
        sheet, cols, rows, outdir, tmpl = sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5], sys.argv[6]
        size = int(sys.argv[sys.argv.index('--size') + 1]) if '--size' in sys.argv else None
        fitv = None
        if '--fit' in sys.argv:
            i = sys.argv.index('--fit')
            fitv = tuple(int(v) for v in sys.argv[i + 1:i + 4])
        os.makedirs(outdir, exist_ok=True)
        for n, c in enumerate(cells(sheet, cols, rows), 1):
            im = key(c)
            if fitv:
                im = fit(im, *fitv)
            elif size:
                im = im.resize((size, size), Image.LANCZOS)
            p = os.path.join(outdir, tmpl.format(n=n) + '.png')
            im.save(p)
            print(p, im.size, im.getbbox())
    elif mode == 'boxes':
        sheet, outdir = sys.argv[2], sys.argv[3]
        im = key(Image.open(sheet))
        a = im.split()[3]
        w, h = im.size
        seen = bytearray(w * h)
        apx = a.load()
        os.makedirs(outdir, exist_ok=True)
        n = 0
        for y in range(0, h, 2):
            for x in range(0, w, 2):
                if apx[x, y] < 40 or seen[y * w + x]:
                    continue
                # 덩어리 넓게 잡기(8px 틈은 이어 본다)
                stack = [(x, y)]
                x0 = x1 = x; y0 = y1 = y
                seen[y * w + x] = 1
                while stack:
                    cx, cy = stack.pop()
                    for nx in range(max(0, cx - 8), min(w, cx + 9), 4):
                        for ny in range(max(0, cy - 8), min(h, cy + 9), 4):
                            if not seen[ny * w + nx] and apx[nx, ny] >= 40:
                                seen[ny * w + nx] = 1
                                stack.append((nx, ny))
                                x0, x1, y0, y1 = min(x0, nx), max(x1, nx), min(y0, ny), max(y1, ny)
                if (x1 - x0) * (y1 - y0) < 300:
                    continue
                n += 1
                box = (max(0, x0 - 4), max(0, y0 - 4), min(w, x1 + 5), min(h, y1 + 5))
                part = im.crop(box)
                part = part.crop(part.getbbox())
                p = os.path.join(outdir, f'box_{n}.png')
                part.save(p)
                print(p, box, part.size)


if __name__ == '__main__':
    main()
