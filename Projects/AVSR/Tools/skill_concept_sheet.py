"""캐릭터 스킬 시트 — 한 장에 컨셉 · 패시브 · 액티브 시안(3컷) · 필살기 시안(3컷).
쓰기: python skill_concept_sheet.py [호스트키 ...]   (없으면 전부)
결과: Projects/AVSR/_exchange/ref/skillmock/concept/sheet_<NN>_<키>.png + 모아 보기 concept_all_<n>.png
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
from skill_concepts import HOSTS, GROUPS  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'skillmock', 'concept')
FB = 'C:/Windows/Fonts/malgunbd.ttf'
FR = 'C:/Windows/Fonts/malgun.ttf'
F_TITLE = ImageFont.truetype(FB, 40)
F_HEAD = ImageFont.truetype(FB, 26)
F_BODY = ImageFont.truetype(FR, 22)
F_TAG = ImageFont.truetype(FB, 22)

BG = (18, 20, 28)
PANEL = (30, 33, 44)
GOLD = (255, 214, 102)
SKY = (140, 200, 255)
RED = (255, 120, 120)
WHITE = (236, 236, 240)
GREY = (160, 165, 180)

W = 2200
MOCK_H = 620          # 시안 한 줄 높이 — 세 컷(2160x1280 → 1046x620)


def wrap(d, text, font, width):
    lines, cur = [], ''
    for ch in text:
        t = cur + ch
        if d.textlength(t, font=font) > width and cur:
            lines.append(cur)
            cur = ch
        else:
            cur = t
    if cur:
        lines.append(cur)
    return lines


def mock_strip(name):
    p = os.path.join(IN, name + '.png')
    if not os.path.exists(p):
        return None
    m = Image.open(p).convert('RGB')
    m = m.crop((0, 0, m.width, int(m.height * 0.94)))   # 아래 메모 칸 일부 — 글은 오른쪽에 따로 적는다
    return m.resize((int(m.width * MOCK_H / m.height), MOCK_H))


def block(d, x, y, tag, color, title, desc, width):
    d.text((x, y), tag, fill=color, font=F_TAG)
    y += 34
    d.text((x, y), title, fill=WHITE, font=F_HEAD)
    y += 40
    for ln in wrap(d, desc, F_BODY, width):
        d.text((x, y), ln, fill=GREY, font=F_BODY)
        y += 30
    return y + 16


def sheet(idx, key):
    who, grp, concept, (pn, pd), (an, ad, _cuts), (un, ud), ult, mark = HOSTS[key]
    act = mock_strip(f'mock_active_{key}_v2') or mock_strip(f'mock_active_{key}')   # 검수 반려로 다시 받은 판이 있으면 그쪽
    ul = mock_strip(f'mock_skill_{ult}')
    H = 150 + (MOCK_H + 70) * 2 + 20
    img = Image.new('RGB', (W, H), BG)
    d = ImageDraw.Draw(img)
    d.text((30, 24), f'{idx:02d}. {who}', fill=WHITE, font=F_TITLE)
    d.text((30, 84), f'{GROUPS[grp]}   ·   컨셉: {concept}', fill=GOLD, font=F_HEAD)
    if mark:
        tw = d.textlength(mark, font=F_HEAD)
        d.rounded_rectangle((W - tw - 60, 28, W - 30, 72), 10, fill=(90, 40, 40))
        d.text((W - tw - 45, 34), mark, fill=(255, 200, 200), font=F_HEAD)

    y0 = 150
    for row, (strip, tag, color, title, desc, extra) in enumerate([
        (act, '액티브 · 일반 스킬 (평타로 충전)', SKY, an, ad, ('패시브', GOLD, pn, pd)),
        (ul, '필살기 (빙의할 때 1회)', RED, un, ud, None),
    ]):
        y = y0 + row * (MOCK_H + 70)
        d.rounded_rectangle((20, y - 10, W - 20, y + MOCK_H + 40), 14, fill=PANEL)
        if strip is not None:
            img.paste(strip, (30, y + 15))
        else:
            d.text((60, y + MOCK_H // 2), '(시안 대기 중)', fill=GREY, font=F_HEAD)
        sw = strip.width if strip is not None else 1120
        tx = 30 + sw + 40
        side = W - tx - 50   # 오른쪽 글 칸 폭 — 시안 폭이 시안마다 달라 그때그때 잰다
        ty = block(d, tx, y + 15, tag, color, title, desc, side)
        if extra:
            block(d, tx, ty + 10, extra[0], extra[1], extra[2], extra[3], side)
        if row == 0:
            d.text((tx, y + MOCK_H - 10), '세 컷: 액티브 발동 → 결과 → 패시브 순간', fill=GREY, font=F_BODY)
        else:
            d.text((tx, y + MOCK_H - 10), '세 컷: 기 → 승 · 전 → 결', fill=GREY, font=F_BODY)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f'sheet_{idx:02d}_{key}.png')
    img.save(path)
    return path


if __name__ == '__main__':
    keys = list(HOSTS)
    want = sys.argv[1:] or keys
    paths = []
    for i, k in enumerate(keys, 1):
        if k in want:
            paths.append(sheet(i, k))
            print(paths[-1])
    if not sys.argv[1:]:
        # 모아 보기 — 4장씩 2열, 장마다 컨셉군 순서대로
        allp = [os.path.join(OUT, f'sheet_{i:02d}_{k}.png') for i, k in enumerate(keys, 1)]
        for part in range((len(allp) + 3) // 4):
            ims = [Image.open(p).convert('RGB') for p in allp[part * 4:(part + 1) * 4]]
            w, h = ims[0].size
            out = Image.new('RGB', (w * 2, h * ((len(ims) + 1) // 2)), BG)
            for j, im in enumerate(ims):
                out.paste(im, ((j % 2) * w, (j // 2) * h))
            out = out.resize((out.width // 2, out.height // 2), Image.LANCZOS)
            out.save(os.path.join(OUT, f'concept_all_{part + 1}.png'))
