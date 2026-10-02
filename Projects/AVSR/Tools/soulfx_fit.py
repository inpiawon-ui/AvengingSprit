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


def cut8(sheet, stem, align):
    """8장짜리(4열 x 2행, 384x512 칸) -> 192x256 여덟 장.

    생성 그림은 윗줄과 아랫줄의 기준점이 몇십 px 씩 어긋나 온다 — 넘기면 위아래로 튄다.
    그림을 고치지 않고 **칸 안에서 옮겨** 맞춘다.
      align='bottom' : 가장 아래 픽셀을 칸 아래에서 20 px 위에, 그 밑동의 가로 중심을 칸 가운데에
      align='center' : 그림 덩어리의 한가운데를 칸 한가운데에
    """
    for i, c in enumerate(cells(sheet, 4, 2)):
        im = glow_alpha(c)
        solid = im.getchannel('A').point(lambda v: 255 if v > 60 else 0)
        box = solid.getbbox()
        if box:
            if align == 'bottom':
                band = solid.crop((0, max(box[1], box[3] - 40), im.width, box[3])).getbbox()
                cx = (band[0] + band[2]) / 2 if band else (box[0] + box[2]) / 2
                dx, dy = round(im.width / 2 - cx), (im.height - 20) - box[3]
            else:
                dx = round(im.width / 2 - (box[0] + box[2]) / 2)
                dy = round(im.height / 2 - (box[1] + box[3]) / 2)
            moved = Image.new('RGBA', im.size)
            moved.paste(im, (dx, dy))
            im = moved
        save(f'{stem}_{i + 1}', shrink(im, (192, 256)))


def spot():
    src = os.path.join(IN, '_soul2_beam.png')
    if not os.path.exists(src):
        print('없음 _soul2_beam.png'); return
    save('fx_spotbeam', shrink(glow_alpha(Image.open(src)), (256, 384)))


def icons():
    """버튼 아이콘 — 투명 바탕으로 받는다. 크기만 확인한다."""
    for n in ('possessicon_enter', 'possessicon_leave'):
        src = os.path.join(IN, n + '.png')
        if not os.path.exists(src):
            print('없음', n); continue
        im = Image.open(src).convert('RGBA')
        assert im.size == (96, 108), f'{n} 크기 {im.size}'
        save(n, im)

def glow():
    src = os.path.join(IN, 'possessbutton_glow.png')
    if not os.path.exists(src):
        print('없음 possessbutton_glow.png'); return
    save('possessbutton_glow', glow_alpha(Image.open(src)))


if __name__ == '__main__':
    # 3차(2026-10-02) — 4장짜리는 넘길 때 뚝뚝 끊겨 8장짜리로 다시 받았다. 혼줄 · 빛줄기는 2차 그대로.
    cut8('_soul3_wisp_sheet.png', 'fx_soulwisp', 'bottom')
    cut8('_soul3_ripple_sheet.png', 'fx_soulripple', 'center')
    cut8('_soul3_pillar_sheet.png', 'fx_soulpillar', 'bottom')
    cut8('_soul3_flame_sheet.png', 'fx_soulflame', 'bottom')
    cut8('_soul3_aura_sheet.png', 'fx_soulaura', 'bottom')
    cord()
    spot()
    glow()
    icons()