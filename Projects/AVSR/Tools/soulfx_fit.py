# -*- coding: utf-8 -*-
"""유령 연출 납품 그림을 자르고 투명을 뽑는다 (그리지 않는다 — 자르기 · 줄이기 · 투명 처리만).

납품은 **검정 바탕**이다. 빛 이펙트라 밝기가 곧 불투명도다 — 알파 = 가장 밝은 채널,
색은 그만큼 되돌려(언프리멀티플라이) 검정 테가 남지 않게 한다.

2차(2026-10-02) — 1차는 128 px 칸으로 받아 뭉개져 반려됐다. 큰 칸으로 받아 절반으로 줄인다.

  _soul2_wisp_sheet.png   1536x1024 (384x1024 x4)   -> fx_soulwisp_{1..4}.png   (192x512)
  _soul2_ripple_sheet.png 1536x1024 (768x512 2x2)   -> fx_soulripple_{1..4}.png (384x256)
  _soul2_cord_sheet.png   1536x1024 (384x1024 x4)   -> fx_soulcord_{1..4}.png   (512x96 가로 — 왼쪽 = 몸, 오른쪽 = 유령)
  _soul2_pillar_sheet.png 1536x1024 (384x1024 x4)   -> fx_soulpillar_{1..4}.png (192x512)
  _soul2_flame_sheet.png  1536x1024 (384x1024 x4)   -> fx_soulflame_{1..4}.png  (192x512)
  _soul2_aura_sheet.png   1536x1024 (768x512 2x2)   -> fx_soulaura_{1..4}.png   (384x256)
  _soul2_beam.png         1024x1536                 -> fx_spotbeam.png + fx_spotdark.png (256x384)
  possessbutton_glow.png  192x192                   -> 그대로 투명만

결과는 Assets/BaseResource/InGameMainUI/ 에 쓴다(아틀라스 폴더).
"""
import os
from PIL import Image, ImageFilter, ImageChops

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '..', '_exchange', 'in')
OUT = os.path.join(HERE, '..', '..', '..', 'Assets', 'BaseResource', 'InGameMainUI')
FLOOR = 12      # 이 밝기 아래는 바탕(검정)으로 본다 — 생성 그림의 잡티를 턴다


def glow_alpha(im):
    """검정 바탕 빛 그림 -> 투명 그림."""
    im = im.convert('RGB')
    r, g, b = im.split()
    a = ImageChops.lighter(ImageChops.lighter(r, g), b)
    a = a.point(lambda v: 0 if v <= FLOOR else min(255, int((v - FLOOR) * 255 / (255 - FLOOR))))
    px, ap = im.load(), a.load()
    out = Image.new('RGBA', im.size)
    op = out.load()
    for y in range(im.height):
        for x in range(im.width):
            al = ap[x, y]
            if al == 0:
                continue
            cr, cg, cb = px[x, y]
            k = 255.0 / al
            op[x, y] = (min(255, int(cr * k)), min(255, int(cg * k)), min(255, int(cb * k)), al)
    return out


def shrink(im, size):
    """투명 그림을 줄인다. 알파를 곱한 채로 줄여야 가장자리에 검은 테가 안 생긴다."""
    pre = im.convert('RGBa').resize(size, Image.LANCZOS)
    return pre.convert('RGBA')


def report(name, im):
    a = im.getchannel('A')
    w, h = im.size
    ap = a.load()
    n = sum(1 for y in range(h) for x in range(w) if ap[x, y] > 0)
    edge = 0
    for x in range(w):
        edge = max(edge, ap[x, 0], ap[x, h - 1])
    for y in range(h):
        edge = max(edge, ap[0, y], ap[w - 1, y])
    print(f'{name}: {im.size} · 채움 {n / (w * h):.0%} · bbox {im.getbbox()} · 가장자리 최대 알파 {edge}')


def save(name, im):
    im.save(os.path.join(OUT, name + '.png'))
    report(name, im)


def cells(sheet, cols, rows):
    src = os.path.join(IN, sheet)
    if not os.path.exists(src):
        print('없음', sheet); return []
    im = Image.open(src)
    assert im.size == (1536, 1024), f'{sheet} 크기 {im.size}'
    w, h = im.width // cols, im.height // rows
    return [im.crop((c * w, r * h, (c + 1) * w, (r + 1) * h)) for r in range(rows) for c in range(cols)]


def cut(sheet, stem, cols, rows, size):
    for i, c in enumerate(cells(sheet, cols, rows)):
        save(f'{stem}_{i + 1}', shrink(glow_alpha(c), size))


def cord():
    for i, c in enumerate(cells('_soul2_cord_sheet.png', 4, 1)):
        im = glow_alpha(c).crop((64, 0, 320, 1024))            # 줄은 칸 가운데 2/3 폭 안에 있다
        im = shrink(im, (128, 512)).rotate(-90, expand=True)    # 위쪽(유령 쪽)이 오른쪽 끝으로
        save(f'fx_soulcord_{i + 1}', im)


def spot():
    src = os.path.join(IN, '_soul2_beam.png')
    if not os.path.exists(src):
        print('없음 _soul2_beam.png'); return
    beam = shrink(glow_alpha(Image.open(src)), (256, 384))
    save('fx_spotbeam', beam)

    # 어둠 — 빛줄기가 덮는 자리만 뚫는다. 줄마다 빛의 왼끝~오른끝 사이가 «안»이다.
    w, h = beam.size
    ap = beam.getchannel('A').load()
    mask = Image.new('L', (w, h), 0)
    mp = mask.load()
    for y in range(h):
        xs = [x for x in range(w) if ap[x, y] > 28]
        if len(xs) < 2:
            continue
        for x in range(xs[0], xs[-1] + 1):
            mp[x, y] = 255
    mask = mask.filter(ImageFilter.GaussianBlur(6))
    dark = Image.new('RGBA', (w, h), (255, 255, 255, 255))
    dark.putalpha(mask.point(lambda v: 255 - v))
    # 가장자리는 반드시 꽉 찬 어둠 — 둘레를 메우는 판과 이어져야 한다.
    dp = dark.load()
    for x in range(w):
        dp[x, 0] = dp[x, h - 1] = (255, 255, 255, 255)
    for y in range(h):
        dp[0, y] = dp[w - 1, y] = (255, 255, 255, 255)
    save('fx_spotdark', dark)


def glow():
    src = os.path.join(IN, 'possessbutton_glow.png')
    if not os.path.exists(src):
        print('없음 possessbutton_glow.png'); return
    save('possessbutton_glow', glow_alpha(Image.open(src)))


if __name__ == '__main__':
    cut('_soul2_wisp_sheet.png', 'fx_soulwisp', 4, 1, (192, 512))
    cut('_soul2_ripple_sheet.png', 'fx_soulripple', 2, 2, (384, 256))
    cord()
    cut('_soul2_pillar_sheet.png', 'fx_soulpillar', 4, 1, (192, 512))
    cut('_soul2_flame_sheet.png', 'fx_soulflame', 4, 1, (192, 512))
    cut('_soul2_aura_sheet.png', 'fx_soulaura', 2, 2, (384, 256))
    spot()
    glow()
