"""아이콘 납품 검수 · 게임 크기 맞춤 (규칙 `.claude/rules/project/art_resource_format.md` 2-1절).

  python Tools/harness/icon_tool.py review --in <납품 폴더> --names a,b,c [--out sheet.png]
      분홍(#FF00FF)을 뚫어 <이름>_alpha.png 로 두고, 큰 그림 · 게임 크기를 늘어놓은 시트를 만든다.
      그림 덩어리 가로세로비 1.3 초과 · 분홍 찌꺼기를 빨갛게 띄운다(긴 변은 fit 이 칸의 94% 로 맞춘다).
      같은 이름에 _sq / _v2 판이 있으면 그것을 본다(_sq > _v2 > 첫 판).
  python Tools/harness/icon_tool.py fit --in <납품 폴더> --names a,b,c --size 128 [--prefix card_]
      뚫고 → 그림 영역을 잘라 → 정사각 가운데 · 94% 로 맞춘 게임 파일 <prefix><이름>.png 를 같은 폴더에 둔다. **자르지 않는다.**
출처: AVSR 2026-10-22 카드 아이콘(icon_review_sheet.py · fit_step1_card_icons.py 를 게임과 상관없게 합쳤다).
"""
import argparse
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont


def cut(path):
    """분홍 바탕을 뚫는다. 남은 분홍 찌꺼기 수도 돌려준다(0 이 정상)."""
    a = np.array(Image.open(path).convert('RGBA'))
    r, g, b = (a[..., i].astype(int) for i in range(3))
    a[(r > 190) & (b > 190) & (g < 90), 3] = 0
    # 분홍은 빨강 = 파랑 · 초록 0 근처 — 보라(파랑이 큼) · 빨강(파랑이 낮음)은 세지 않는다
    left = int(((r > 200) & (b > 200) & (g < 70) & (np.abs(r - b) < 20) & (a[..., 3] > 0)).sum())
    return Image.fromarray(a), left


def shape(img):
    al = np.asarray(img.getchannel('A')) > 24
    ys, xs = np.nonzero(al)
    if not len(xs):
        return 0.0, 0.0
    w, h = xs.max() - xs.min() + 1, ys.max() - ys.min() + 1
    return w / h, min(w, h) / max(w, h)


def square(rgba, size, fill=0.94):
    box = rgba.getchannel('A').point(lambda v: 255 if v > 24 else 0).getbbox()
    art = rgba.crop(box)
    side = max(art.width, art.height)
    pad = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    pad.alpha_composite(art, ((side - art.width) // 2, (side - art.height) // 2))
    inner = round(size * fill)
    out = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    out.alpha_composite(pad.resize((inner, inner), Image.LANCZOS), ((size - inner) // 2, (size - inner) // 2))
    return out


def best(folder, name):
    for v in ('_sq', '_v2', ''):
        p = os.path.join(folder, name + v + '.png')
        if os.path.exists(p):
            return p, v
    return None, ''


def font(size):
    for f in (r'C:\Windows\Fonts\malgun.ttf', r'C:\Windows\Fonts\gulim.ttc'):
        if os.path.exists(f):
            return ImageFont.truetype(f, size)
    return ImageFont.load_default()


def review(args):
    names = [n for n in args.names.split(',') if n]
    cols, cw, ch = 5, 420, 300
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cw, max(1, rows) * ch), (20, 24, 36, 255))
    d, f, fs = ImageDraw.Draw(sheet), font(20), font(15)
    bad = 0
    for i, n in enumerate(names):
        p, v = best(args.input, n)
        x, y = (i % cols) * cw, (i // cols) * ch
        if p is None:
            d.text((x + 10, y + 8), f'{n}  없음', font=f, fill=(255, 110, 90))
            continue
        img, left = cut(p)
        img.save(os.path.join(args.input, n + '_alpha.png'))
        ratio, short = shape(img)
        sheet.alpha_composite(square(img, 200, 1.0), (x + 10, y + 40))
        sheet.alpha_composite(square(img, args.size), (x + 230, y + 40))
        warn = []
        if left > 30: warn.append(f'분홍 {left}')
        if ratio > 1.3 or (ratio and ratio < 1 / 1.3): warn.append(f'비율 {ratio:.2f}')
        bad += bool(warn)
        d.text((x + 10, y + 8), f'{n}{v}', font=f, fill=(230, 236, 245))
        d.text((x + 230, y + 40 + args.size + 8), f'비율 {ratio:.2f} · 짧은 변 {short:.0%}', font=fs, fill=(150, 160, 180))
        if warn:
            d.text((x + 230, y + 40 + args.size + 30), ' · '.join(warn), font=fs, fill=(255, 110, 90))
        print(f'{n}{v}\t비율 {ratio:.2f}\t짧은 변 {short:.0%}\t분홍 {left}' + ('\t⚠ ' + ' · '.join(warn) if warn else ''))
    out = args.out or os.path.join(args.input, '_icon_review_sheet.png')
    sheet.save(out)
    print(f'걸린 것 {bad} / {len(names)} → {out}')


def fit(args):
    for n in [n for n in args.names.split(',') if n]:
        p, v = best(args.input, n)
        if p is None:
            print('없음', n)
            continue
        icon = square(cut(p)[0], args.size)
        dst = os.path.join(args.input, f'{args.prefix}{n}.png')
        icon.save(dst)
        print(f'{n}{v} → {os.path.basename(dst)} ({args.size})')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('mode', choices=['review', 'fit'])
    ap.add_argument('--in', dest='input', required=True)
    ap.add_argument('--names', required=True)
    ap.add_argument('--size', type=int, default=128)
    ap.add_argument('--prefix', default='')
    ap.add_argument('--out', default='')
    args = ap.parse_args()
    (review if args.mode == 'review' else fit)(args)


if __name__ == '__main__':
    main()
