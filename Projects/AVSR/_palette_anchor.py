"""람보 팔레트 앵커 — 목업에서 다시 뽑는다.

지금까지 `unit_rambo.png` 를 정본으로 삼고 "정본 팔레트를 벗어나지 말 것"을
반려 기준으로 걸었다. 그런데 그 정본이 이미 색이 뭉개진 상태였다.
그래서 검수기가 "정본에 없는 색 0.0% — 통과"를 4차 내내 찍었다.
잘못된 팔레트를 강제하고 있었던 것이다.

승인된 것은 `Reference/Mockups/host_select.jpeg` 의 호스트 목록이다.
거기서 람보 칸을 잘라 **팔레트를 다시 뽑는다.**
"""
import colorsys
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MOCK = os.path.join(HERE, 'Reference', 'Mockups', 'host_select.jpeg')
UNIT = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'BaseResource', 'Unit', 'rambo'))
OUT = os.path.join(HERE, '_exchange', 'out', '10_palette')

SLOT = (122, 230, 218, 322)   # 목업의 `람보(기관총)` 칸 (그림만, 이름표 제외)
SWATCH = 14                   # 팔레트에 남길 색 수


FAMILIES = [(0, 15, '빨강'), (15, 45, '주황·살색'), (45, 70, '노랑'), (70, 160, '초록'),
            (160, 250, '파랑'), (250, 330, '보라'), (330, 360, '빨강2')]


def quantize(im, n):
    """색상군마다 대표색을 뽑는다.

    빈도순으로만 고르면 면적이 넓은 갈색이 전부 차지하고, 정작 구분에 필요한
    빨강(머리띠)·초록(조끼)·회색(총)이 빠진다. 실제로 그렇게 나와서
    6색이 전부 갈색이었다. 색상군별로 나눠 뽑아야 재질 구분이 남는다.
    """
    q = im.convert('RGB').quantize(colors=48, method=Image.MEDIANCUT).convert('RGB')
    px = q.load()
    w, h = q.size
    by = {}
    grey = {}
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            hh, s, v = colorsys.rgb_to_hsv(c[0] / 255, c[1] / 255, c[2] / 255)
            if v < 0.10:
                continue                      # 카드 배경
            if s < 0.18:                      # 무채색 — 총·금속이 여기 있다
                grey[c] = grey.get(c, 0) + 1
                continue
            d = hh * 360
            for lo, hi, name in FAMILIES:
                if lo <= d < hi:
                    by.setdefault(name.replace('2', ''), {})
                    by[name.replace('2', '')][c] = by[name.replace('2', '')].get(c, 0) + 1
                    break

    out = []
    for name, cnt in sorted(by.items(), key=lambda kv: -sum(kv[1].values())):
        top = sorted(cnt.items(), key=lambda kv: -kv[1])[:3]
        out += [c for c, _ in top]
    out += [c for c, _ in sorted(grey.items(), key=lambda kv: -kv[1])[:3]]
    return out[:n]


def main():
    os.makedirs(OUT, exist_ok=True)
    crop = Image.open(MOCK).convert('RGB').crop(SLOT)

    crop.resize((crop.width * 8, crop.height * 8), Image.NEAREST) \
        .save(os.path.join(OUT, '01_목업_람보_x8.png'))

    pal = quantize(crop, SWATCH)
    cell, pad = 96, 8
    sw = Image.new('RGB', (cell * len(pal) + pad * 2, cell + 46), (24, 20, 38))
    d = ImageDraw.Draw(sw)
    d.text((pad, 6), 'PALETTE from the approved mockup - use THESE, not the current sprite',
           fill=(255, 255, 255))
    for i, c in enumerate(pal):
        x = pad + i * cell
        d.rectangle((x, 24, x + cell - 4, 24 + cell - 24), fill=c)
        d.text((x + 4, 24 + cell - 20), '#%02X%02X%02X' % c, fill=(230, 230, 240))
    sw.save(os.path.join(OUT, '02_팔레트_목업에서추출.png'))

    # 지금 것과 나란히 — 무엇이 잘못됐는지 한눈에
    cur = Image.open(os.path.join(UNIT, 'unit_rambo_s.png')).convert('RGBA')
    z = 6
    cmp = Image.new('RGB', (crop.width * 8 + cur.width * z + 30, max(crop.height * 8, cur.height * z) + 30),
                    (24, 20, 38))
    cmp.paste(crop.resize((crop.width * 8, crop.height * 8), Image.NEAREST), (10, 24))
    big = cur.resize((cur.width * z, cur.height * z), Image.NEAREST)
    cmp.paste(big, (crop.width * 8 + 20, 24), big)
    d2 = ImageDraw.Draw(cmp)
    d2.text((10, 6), 'MOCKUP (approved)', fill=(150, 255, 190))
    d2.text((crop.width * 8 + 20, 6), 'CURRENT (wrong) - every material collapsed to one brown',
            fill=(255, 120, 120))
    cmp.save(os.path.join(OUT, '03_비교_목업_대_현재.png'))

    print('팔레트 %d색:' % len(pal), ' '.join('#%02X%02X%02X' % c for c in pal))


if __name__ == '__main__':
    main()
