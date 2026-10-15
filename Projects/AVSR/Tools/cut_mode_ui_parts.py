"""추가 모드 UI 부품 자르기 (2026-10-15) — 납품 _exchange/in/modeui_*.png → _exchange/out/mode_ui/*.png + 시안 대조 그림.

발주: order_mode_ui_parts.py (같은 PARTS 표를 읽는다).
- 마젠타 바탕을 뚫는다(가장자리 번짐까지 · 그림 안쪽 갇힌 마젠타까지 전역으로 센다).
- 시트는 칸으로 나눠 _1 · _2 … 로 저장한다.
- 2배로 받은 그림을 게임 크기로 줄인다(BOX — 2x2 픽셀을 한 점으로).
- Assets 에는 아직 넣지 않는다 — 유니티 구현 때 화면 설계서 5절 이름으로 옮기고 임포트 설정(09_import_settings)을 한다.
- 대조: _exchange/out/mode_ui/_compare.png — 왼쪽 시안 앵커 · 오른쪽 납품 부품(같은 크기).
python cut_mode_ui_parts.py [이름 ...]
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from order_mode_ui_parts import PARTS  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
OUT = os.path.join(EX, 'out', 'mode_ui')


def key_magenta(img):
    a = np.asarray(img.convert('RGBA')).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mag = (r > 170) & (b > 170) & (g < 110)
    # 가장자리 번짐 — 분홍 기운(빨강 · 파랑이 초록보다 크게 높은 것)
    fringe = (r - g > 60) & (b - g > 60) & (np.abs(r - b) < 70) & ~mag
    a[..., 3] = np.where(mag, 0, a[..., 3])
    # 번짐 픽셀은 분홍을 빼고 반투명으로
    a[..., 3] = np.where(fringe, 140, a[..., 3])
    a[..., 0] = np.where(fringe, g + (r - g) // 3, a[..., 0])
    a[..., 2] = np.where(fringe, g + (b - g) // 3, a[..., 2])
    left = int(((a[..., 3] > 0) & (r > 200) & (b > 200) & (g < 60)).sum())
    return Image.fromarray(a.clip(0, 255).astype('uint8'), 'RGBA'), left


def cells(img, n, cols):
    rows = (n + cols - 1) // cols
    cw, ch = img.width / cols, img.height / rows
    for i in range(n):
        c, r = i % cols, i // cols
        yield img.crop((round(c * cw), round(r * ch), round((c + 1) * cw), round((r + 1) * ch)))


def fit(part, w, h, keyed):
    if keyed:
        bbox = part.getchannel('A').getbbox()
        if bbox:
            part = part.crop(bbox)
    return part.resize((w, h), Image.BOX)


def main(names):
    os.makedirs(OUT, exist_ok=True)
    rows = []
    for name in names:
        src = os.path.join(EX, 'in', f'modeui_{name}.png')
        if not os.path.exists(src):
            print('없음', name)
            continue
        w, h, n, cols, bg, _desc, anchor, _mock = PARTS[name]
        img = Image.open(src).convert('RGBA')
        keyed = bg == 'mag'
        left = 0
        if keyed:
            img, left = key_magenta(img)
        outs = []
        for i, part in enumerate(cells(img, n, cols)):
            p = fit(part, w, h, keyed)
            fn = f'{name}.png' if n == 1 else f'{name}_{i + 1}.png'
            p.save(os.path.join(OUT, fn))
            outs.append(p)
        print(f'{name}: {img.size} → {n}장 {w}x{h} · 남은 마젠타 {left}')
        rows.append((name, Image.open(os.path.join(EX, 'ref', 'mode_ui_parts', f'anchor_{anchor}' if not anchor.startswith('anchor_') else anchor)).convert('RGB'), outs))
    compare(rows)


def compare(rows):
    if not rows:
        return
    f = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 18)
    blocks = []
    for name, anc, outs in rows:
        s = min(1.0, 360 / anc.width, 360 / anc.height)
        anc = anc.resize((max(1, int(anc.width * s)), max(1, int(anc.height * s))))
        ps = []
        for p in outs:
            k = min(1.0, 360 / p.width, 360 / p.height) if p.width > 360 or p.height > 360 else 1.0
            q = p.resize((max(1, int(p.width * k)), max(1, int(p.height * k))), Image.NEAREST)
            bgq = Image.new('RGBA', q.size, (40, 44, 60, 255))
            bgq.alpha_composite(q)
            ps.append(bgq.convert('RGB'))
        bw = anc.width + 20 + sum(p.width + 8 for p in ps)
        bh = max([anc.height] + [p.height for p in ps]) + 30
        blk = Image.new('RGB', (bw, bh), (18, 20, 30))
        d = ImageDraw.Draw(blk)
        d.text((0, 0), name, font=f, fill=(255, 210, 63))
        blk.paste(anc, (0, 26))
        x = anc.width + 20
        for p in ps:
            blk.paste(p, (x, 26))
            x += p.width + 8
        blocks.append(blk)
    W = max(b.width for b in blocks) + 20
    H = sum(b.height + 14 for b in blocks) + 20
    sheet = Image.new('RGB', (W, H), (10, 12, 20))
    y = 10
    for b in blocks:
        sheet.paste(b, (10, y))
        y += b.height + 14
    sheet.save(os.path.join(OUT, '_compare.png'))
    print('대조', os.path.join(OUT, '_compare.png'), sheet.size)


if __name__ == '__main__':
    main(sys.argv[1:] or list(PARTS))
