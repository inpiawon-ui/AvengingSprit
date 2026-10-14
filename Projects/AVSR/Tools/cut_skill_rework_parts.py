"""호스트 스킬 개편(2026-10-14) 부품 시트 → InGameMainUI 프레임(fx_{이름}_{번호}.png).

검은 바탕 시트는 밝기를 알파로(빛은 더하기로 그리던 것), 분홍 바탕 시트는 분홍을 뚫는다.
쓰기: python cut_skill_rework_parts.py [이름 ...] [--preview]
  --preview 면 Assets 에 쓰지 않고 _exchange/ref/skillmock/parts_preview.png 만 만든다.
"""
import os
import sys
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
FLOOR = 18   # 이 밝기 아래는 바탕 — 칠한 판의 옅은 후광이 네모로 남지 않게

# 시트 이름: (게임 이름, 칸 폭, 칸 높이, 열, 행, 바탕)
PARTS = {
    'part_th_spin':   ('thspin',   192, 192, 6, 1, 'black'),
    'part_th_ring':   ('thring',   256, 256, 8, 1, 'black'),
    'part_th_shell':  ('thshell',   24,  24, 4, 1, 'pink'),
    'part_c4_disc':   ('c4disc',    40,  40, 4, 1, 'pink'),
    'part_c4_bomb':   ('c4bomb',    40,  40, 4, 1, 'pink'),
    'part_c4_trail':  ('c4trail',  256,  32, 1, 4, 'black'),
    'part_sl_vortex': ('slvortex', 256, 256, 6, 1, 'black'),
    'part_sl_swing':  ('slswing',  256, 160, 1, 6, 'black'),
    'part_sl_ball':   ('slball',    64,  32, 1, 4, 'black'),
    'part_md_wraith': ('mdwraith', 160, 192, 6, 1, 'black'),
    'part_md_pop':    ('mdpop',    192, 192, 6, 1, 'black'),
    'part_md_slash':  ('mdslash',  192, 128, 1, 4, 'black'),
    'part_md_stack':  ('mdstack',   32,  40, 4, 1, 'pink'),
    'part_sl_wind':   ('slwind',   512, 256, 1, 6, 'black'),
    'part_sl_tendril':('sltendril',384, 128, 1, 6, 'black'),
}


def black_to_alpha(rgb):
    a = np.asarray(rgb).astype(float)
    a = np.clip((a - FLOOR) / (255 - FLOOR), 0, 1)
    alpha = a.max(axis=2, keepdims=True)
    color = np.where(alpha > 0, a / np.maximum(alpha, 1e-6), 0)
    return Image.fromarray(np.concatenate([color * 255, alpha * 255], axis=2).astype(np.uint8), 'RGBA')


def pink_to_alpha(rgba):
    a = np.asarray(rgba.convert('RGBA')).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    key = (r > 170) & (b > 170) & (g < 120)
    a[..., 3] = np.where(key, 0, np.where(a[..., 3] > 0, 255, 0))
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def cut(sheet_name):
    name, w, h, cols, rows, bg = PARTS[sheet_name]
    src = os.path.join(EX, 'in', sheet_name + '.png')
    if not os.path.exists(src):
        return name, None
    sheet = Image.open(src)
    if sheet.size != (w * cols, h * rows):
        print('크기 맞춤', sheet_name, sheet.size, '→', (w * cols, h * rows))
        sheet = sheet.resize((w * cols, h * rows), Image.NEAREST)
    frames = []
    for i in range(cols * rows):
        c = sheet.crop(((i % cols) * w, (i // cols) * h, (i % cols + 1) * w, (i // cols + 1) * h))
        if bg == 'black':
            # ⚠ 이미 알파가 있는 시트(코덱스가 뚫어서 보냄)는 RGB 만 쓰면 투명한 칸의 색(하늘색 등)이 그대로 칠해진다 —
            #   회오리 · 원혼이 꽉 찬 덩어리로 보였던 원인(2026-10-14). 검정 위에 먼저 얹고 밝기를 알파로 바꾼다
            if c.mode in ('RGBA', 'LA', 'P'):
                k = Image.new('RGBA', c.size, (0, 0, 0, 255))
                k.alpha_composite(c.convert('RGBA'))
                c = k
            f = black_to_alpha(c.convert('RGB'))
        else:
            f = pink_to_alpha(c)
        frames.append(f)
    return name, frames


def main():
    only = [a for a in sys.argv[1:] if not a.startswith('--')]
    preview = '--preview' in sys.argv
    rows_img = []
    for sheet_name in PARTS:
        if only and sheet_name not in only and PARTS[sheet_name][0] not in only:
            continue
        name, frames = cut(sheet_name)
        if frames is None:
            print('없음', sheet_name)
            continue
        if preview:
            cell = 128
            strip = Image.new('RGBA', (cell * len(frames) + 160, cell), (34, 36, 46, 255))
            for i, f in enumerate(frames):
                t = f.copy()
                t.thumbnail((cell - 4, cell - 4), Image.NEAREST)
                strip.alpha_composite(t, (160 + i * cell + (cell - t.width) // 2, (cell - t.height) // 2))
            from PIL import ImageDraw
            ImageDraw.Draw(strip).text((6, 6), name, fill=(255, 255, 255, 255))
            rows_img.append(strip)
            continue
        for i, f in enumerate(frames):
            f.save(os.path.join(DST, 'fx_%s_%d.png' % (name, i + 1)))
        print(name, len(frames))
    if preview and rows_img:
        W = max(r.width for r in rows_img)
        out = Image.new('RGBA', (W, sum(r.height for r in rows_img)), (34, 36, 46, 255))
        y = 0
        for r in rows_img:
            out.alpha_composite(r, (0, y))
            y += r.height
        out.save(os.path.join(EX, 'ref', 'skillmock', 'parts_preview.png'))
        print('preview', out.size)


if __name__ == '__main__':
    main()
