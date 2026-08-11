"""승인 목업 vs 현재 유닛 스프라이트 전수 비교.

`unit_rambo.png` 를 정본으로 삼은 탓에 색이 한 갈색으로 뭉친 것이 4차 내내
통과했다. 정본은 목업이다. 12종 전부 목업과 대조해 어디까지 어긋났는지 본다.

목업 호스트 목록은 3열 5행 = 15칸인데 우리 테이블은 12종이다.
람보·마법사·닌자는 목업에 색 변형이 하나씩 더 있다(레이저·녹색·적색).
"""
import colorsys
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MOCK = os.path.join(HERE, 'Reference', 'Mockups', 'host_select.jpeg')
UNIT = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'BaseResource', 'Unit'))
OUT = os.path.join(HERE, '_exchange', 'out', '11_palette_all')

# 실측한 격자 (열2·행1 = 람보 기관총 기준으로 맞춤)
X0, Y0, W, H = 122, 232, 96, 88
PX, PY = 105, 131

# (행, 열) → 우리 키. 목업의 색 변형(람보 레이저 등)은 우리 테이블에 없다.
SLOTS = [
    (0, 0, 'amazoness', '아마조네스'), (0, 1, 'rambo', '람보(기관총)'),
    (1, 0, 'wizard', '마법사(분홍)'), (1, 2, 'ninja', '닌자(청색)'),
    (2, 1, 'mafia', '마피아'), (2, 2, 'hitman', '히트맨'),
    (3, 0, 'yogamaster', '요가마스터'), (3, 1, 'dragon', '드래곤'),
    (3, 2, 'robot', '로봇'), (4, 0, 'snowwoman', '설녀'),
    (4, 1, 'slugger', '슬러거'), (4, 2, 'vampire', '흡혈귀'),
]

FAMILIES = [(0, 15, '빨강'), (15, 45, '주황'), (45, 70, '노랑'), (70, 160, '초록'),
            (160, 250, '파랑'), (250, 330, '보라'), (330, 360, '빨강')]


def families(im, alpha):
    """색상군별 면적 비율 + 무채색 비율. 무채색은 금속·흰옷이라 따로 센다."""
    px = im.load()
    w, h = im.size
    f = {}
    grey = tot = 0
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if alpha and c[3] <= 8:
                continue
            hh, s, v = colorsys.rgb_to_hsv(c[0] / 255, c[1] / 255, c[2] / 255)
            if v < 0.18:
                continue                      # 배경·외곽선
            tot += 1
            if s < 0.18:
                grey += 1
                continue
            d = hh * 360
            for lo, hi, name in FAMILIES:
                if lo <= d < hi:
                    f[name] = f.get(name, 0) + 1
                    break
    if not tot:
        return {}, 0
    return ({k: v / tot * 100 for k, v in sorted(f.items(), key=lambda kv: -kv[1])},
            grey / tot * 100)


def main():
    os.makedirs(OUT, exist_ok=True)
    mock = Image.open(MOCK).convert('RGB')

    z = 3
    cellw, cellh = W * z + 96 * z + 24, max(H, 96) * z + 44
    sheet = Image.new('RGB', (cellw, cellh * len(SLOTS)), (24, 20, 38))
    d = ImageDraw.Draw(sheet)

    rows = []
    for i, (r, c, key, kr) in enumerate(SLOTS):
        box = (X0 + (c - 1) * PX, Y0 + r * PY, X0 + (c - 1) * PX + W, Y0 + r * PY + H)
        m = mock.crop(box)
        mf, mg = families(m, alpha=False)

        p = os.path.join(UNIT, key, f'unit_{key}.png')
        u = Image.open(p).convert('RGBA')
        uf, ug = families(u, alpha=True)

        m.resize((W * z, H * z), Image.NEAREST).save(os.path.join(OUT, f'{key}__mockup.png'))

        y = i * cellh
        sheet.paste(m.resize((W * z, H * z), Image.NEAREST), (8, y + 34))
        big = u.resize((u.width * z, u.height * z), Image.NEAREST)
        sheet.paste(big, (W * z + 16, y + 34), big)

        mmain = sum(1 for v in mf.values() if v >= 5)
        umain = sum(1 for v in uf.values() if v >= 5)
        bad = umain < mmain
        d.text((8, y + 6), f'{kr} ({key})   목업 색상군 {mmain} / 현재 {umain}'
                           + ('   ← 뭉침' if bad else ''),
               fill=(255, 110, 110) if bad else (150, 255, 190))
        d.text((8, y + 20), 'mockup: ' + ' '.join(f'{k}{v:.0f}%' for k, v in mf.items() if v >= 4)
                            + f' 무채{mg:.0f}%', fill=(190, 190, 205))
        d.text((W * z + 16, y + 20), 'ours: ' + ' '.join(f'{k}{v:.0f}%' for k, v in uf.items() if v >= 4)
                                     + f' 무채{ug:.0f}%', fill=(190, 190, 205))
        rows.append((kr, key, mf, mg, uf, ug, mmain, umain))

    sheet.save(os.path.join(OUT, '00_전수비교.png'))

    print(f'{"캐릭터":<14}{"목업 색상군":<26}{"현재 색상군":<26}{"판정"}')
    for kr, key, mf, mg, uf, ug, mm, um in rows:
        ms = ' '.join(f'{k}{v:.0f}' for k, v in mf.items() if v >= 5) + f' 무{mg:.0f}'
        us = ' '.join(f'{k}{v:.0f}' for k, v in uf.items() if v >= 5) + f' 무{ug:.0f}'
        print(f'{kr:<14}{ms:<26}{us:<26}{"뭉침" if um < mm else "ok"}')


if __name__ == '__main__':
    main()
