# -*- coding: utf-8 -*-
"""유령 연출 납품 그림을 자르고 투명을 뽑는다 (그리지 않는다 — 자르기 · 투명 처리만).

납품은 **검정 바탕**이다. 빛 이펙트라 밝기가 곧 불투명도다 — 알파 = 가장 밝은 채널,
색은 그만큼 되돌려(언프리멀티플라이) 검정 테가 남지 않게 한다.

  _fx_soulout_sheet.png   768x128 -> fx_soulout_{1..6}.png   (128x128)
  _fx_revive_sheet.png    768x256 -> fx_revive_{1..6}.png    (128x256)
  _fx_soulflame_sheet.png 512x128 -> fx_soulflame_{1..4}.png (128x128)
  fx_soulcord.png         64x256  -> fx_soulcord_1.png       (256x64, 위쪽 = 오른쪽 끝)
  fx_spotbeam.png         256x512 -> fx_spotbeam.png + fx_spotdark.png (빛줄기 모양대로 구멍 난 어둠)
  possessbutton_glow.png  192x192 -> 그대로 투명만

결과는 Assets/BaseResource/InGameMainUI/ 에 쓴다(아틀라스 폴더).
"""
import os, sys
from PIL import Image, ImageFilter, ImageChops

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '..', '_exchange', 'in')
OUT = os.path.join(HERE, '..', '..', '..', 'Assets', 'BaseResource', 'InGameMainUI')
FLOOR = 10      # 이 밝기 아래는 바탕(검정)으로 본다 — 생성 그림의 잡티를 턴다


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


def report(name, im):
    a = im.getchannel('A')
    n = sum(1 for v in a.getdata() if v > 0)
    edge = 0
    w, h = im.size
    ap = a.load()
    for x in range(w):
        edge = max(edge, ap[x, 0], ap[x, h - 1])
    for y in range(h):
        edge = max(edge, ap[0, y], ap[w - 1, y])
    print(f'{name}: {im.size} · 채움 {n / (w * h):.0%} · 가장자리 최대 알파 {edge}')


def save(name, im):
    im.save(os.path.join(OUT, name + '.png'))
    report(name, im)


def cut(sheet, stem, count, cell):
    src = os.path.join(IN, sheet)
    if not os.path.exists(src):
        print('없음', sheet); return
    im = Image.open(src)
    w, h = cell
    assert im.size == (w * count, h), f'{sheet} 크기 {im.size}'
    for i in range(count):
        save(f'{stem}_{i + 1}', glow_alpha(im.crop((i * w, 0, (i + 1) * w, h))))


def cord():
    src = os.path.join(IN, 'fx_soulcord.png')
    if not os.path.exists(src):
        print('없음 fx_soulcord.png'); return
    im = glow_alpha(Image.open(src))
    # 게임의 줄기 그림은 가로다. 위쪽(유령 쪽)이 오른쪽 끝으로 가게 돌린다.
    save('fx_soulcord_1', im.rotate(-90, expand=True))


def spot():
    src = os.path.join(IN, 'fx_spotbeam.png')
    if not os.path.exists(src):
        print('없음 fx_spotbeam.png'); return
    beam = glow_alpha(Image.open(src))
    save('fx_spotbeam', beam)

    # 어둠 — 빛줄기가 덮는 자리만 뚫는다. 줄마다 빛의 왼끝~오른끝 사이가 «안»이다.
    w, h = beam.size
    ap = beam.getchannel('A').load()
    mask = Image.new('L', (w, h), 0)
    mp = mask.load()
    for y in range(h):
        xs = [x for x in range(w) if ap[x, y] > 40]
        if len(xs) < 2:
            continue
        for x in range(xs[0], xs[-1] + 1):
            mp[x, y] = 255
    mask = mask.filter(ImageFilter.GaussianBlur(7))
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
    cut('_fx_soulout_sheet.png', 'fx_soulout', 6, (128, 128))
    cut('_fx_revive_sheet.png', 'fx_revive', 6, (128, 256))
    cut('_fx_soulflame_sheet.png', 'fx_soulflame', 4, (128, 128))
    cord()
    spot()
    glow()
