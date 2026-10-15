"""2차 부품 시트 자르기 — 분홍 바탕은 거리 키잉 + 덩어리 라벨, 검은 바탕은 프레임 등분(가산용).
사용: python cut_parts2.py [시트키 ...]   (없으면 납품된 것 전부)
"""
import json, os, sys, codecs
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

ROOT = 'C:/won/UnityProject/AvengingSprit/'
EX = ROOT + 'Projects/AVSR/_exchange/'
OUT = EX + 'out/parts2/'
FONT = ROOT + 'Assets/BaseResource/Fonts/NotoSansKR-Bold.ttf'
FX_FRAMES = {'p2_fx_pillar_cyan': 6, 'p2_fx_pillar_purple': 6, 'p2_fx_pillar_gold': 6,
             'p2_fx_split': 6, 'p2_fx_flip': 6, 'p2_fx_jackpot': 8, 'p2_fx_essence': 6}

DIL = {'p2_summon_ui': 2}  # 부품 간격이 15px 남짓으로 붙어 그려진 시트
man = json.load(codecs.open(EX + 'parts2_manifest.json', 'r', 'utf-8'))


def key_pink(im):
    r, g, b = im[..., 0], im[..., 1], im[..., 2]
    dist = np.sqrt((255 - r) ** 2 + g ** 2 + (255 - b) ** 2)
    a = np.clip((dist - 45) / 55.0, 0, 1)
    k = 1.0 - a
    rgb = im.copy()
    for c, m in enumerate([255, 0, 255]):
        rgb[..., c] = np.where(a > 0.02, (im[..., c] - k * m) / np.maximum(a, 0.02), 0)
    return np.dstack([np.clip(rgb, 0, 255), a * 255]).astype(np.uint8), a


def rows_then_cols(boxes):
    # 세로로 겹치는 상자끼리 한 줄로 묶고, 줄 안에서는 왼쪽부터
    boxes = sorted(boxes, key=lambda t: (t[0] + t[2]) / 2)
    rows = []
    for bx in boxes:
        cy = (bx[0] + bx[2]) / 2
        for row in rows:
            ry0 = min(t[0] for t in row); ry1 = max(t[2] for t in row)
            if ry0 <= cy <= ry1:
                row.append(bx); break
        else:
            rows.append([bx])
    rows.sort(key=lambda row: min(t[0] for t in row))
    return [bx for row in rows for bx in sorted(row, key=lambda t: t[1])]


def cut_pink(k, names):
    im = np.array(Image.open(EX + f'in/{k}.png').convert('RGB')).astype(np.float32)
    rgba, a = key_pink(im)
    mask = ndimage.binary_dilation(a > 0.15, iterations=DIL.get(k, 8))
    lab, n = ndimage.label(mask)
    boxes = []
    for sl in ndimage.find_objects(lab):
        y0, y1, x0, x1 = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop
        if (y1 - y0) * (x1 - x0) >= 500:
            boxes.append((y0, x0, y1, x1))
    boxes = rows_then_cols(boxes)
    od = OUT + k.replace('p2_', '') + '/'
    os.makedirs(od, exist_ok=True)
    for f in os.listdir(od):
        os.remove(od + f)
    ok = len(boxes) == len(names)
    res = []
    for i, (y0, x0, y1, x1) in enumerate(boxes):
        crop = Image.fromarray(rgba[y0:y1, x0:x1])
        crop = crop.crop(crop.getbbox())
        nm = names[i] if ok else f'_part{i + 1:02d}'
        crop.save(od + nm + '.png')
        res.append((nm, crop))
    return ok, len(boxes), len(names), res


def cut_black(k, nframes):
    im = np.array(Image.open(EX + f'in/{k}.png').convert('RGB')).astype(np.float32)
    lum = im.max(axis=2)
    on = lum > 18
    ys, xs = np.where(on)
    od = OUT + k.replace('p2_', '') + '/'
    os.makedirs(od, exist_ok=True)
    for f in os.listdir(od):
        os.remove(od + f)
    # 줄 나누기: 빈 가로줄 띠로 자른다
    # 두 줄 사이에 흩어진 점 몇 개가 다리를 놓아 한 줄로 읽혔다(대박 시트) — 거의 빈 줄도 경계로 본다
    rowhit = on.sum(axis=1) > on.shape[1] * 0.015
    bands, y = [], 0
    H = on.shape[0]
    while y < H:
        if rowhit[y]:
            y0 = y
            while y < H and (rowhit[y:y + 12].any()):
                y += 1
            bands.append((y0, y))
        y += 1
    bands = [b for b in bands if b[1] - b[0] > 40]
    per = [nframes // len(bands) + (1 if i < nframes % len(bands) else 0) for i in range(len(bands))] if bands else []
    res, idx, saved = [], 0, []
    for (y0, y1), cnt in zip(bands, per):
        cols = on[y0:y1].any(axis=0)
        # 프레임 폭이 제각각이라(작게 → 크게 → 작게) 등분하면 이웃 프레임이 섞인다 — 빈 세로줄로 나눈다
        runs, x = [], 0
        W = cols.shape[0]
        while x < W:
            if cols[x]:
                s = x
                while x < W and cols[x:x + 20].any():
                    x += 1
                runs.append((s, x))
            x += 1
        if len(runs) != cnt:
            cx = np.where(cols)[0]
            x0, x1 = cx.min(), cx.max() + 1
            w = (x1 - x0) / cnt
            runs = [(int(x0 + j * w), int(x0 + (j + 1) * w)) for j in range(cnt)]
        # 모든 프레임을 같은 칸 폭 · 같은 줄 높이로(가운데 맞춤, 바닥 공통) — 재생 때 흔들리지 않게
        cw = max(b - a for a, b in runs)
        pad = np.zeros((y1 - y0, W + cw * 2, 3), np.float32)
        pad[:, cw:cw + W] = im[y0:y1]
        for fa, fb in runs:
            c = (fa + fb) // 2 + cw
            fr = pad[:, c - cw // 2:c - cw // 2 + cw]
            alpha = fr.max(axis=2, keepdims=True)
            # 가산용 — 검정은 투명, 색은 알파로 나눠 펴 둔다(가산 재질은 RGB 만 써도 같다)
            rgb = np.where(alpha > 0, fr * 255.0 / np.maximum(alpha, 1), 0)
            out = np.dstack([np.clip(rgb, 0, 255), alpha]).astype(np.uint8)
            idx += 1
            nm = f'{k.replace("p2_", "")}_{idx:02d}'
            saved.append((nm, Image.fromarray(out)))
            res.append((nm, Image.fromarray(fr.astype(np.uint8))))
    # 줄마다 칸 크기가 달라도 재생 때 흔들리지 않게 가장 큰 칸으로 맞춘다(가로 가운데 · 바닥 맞춤)
    if saved:
        mw = max(im.width for _, im in saved); mh = max(im.height for _, im in saved)
        res = []
        for nm, im in saved:
            c = Image.new('RGBA', (mw, mh), (0, 0, 0, 0))
            # 솟는 것(기둥 · 갈래)은 바닥, 터지는 것(대박 · 뒤집기 · 정수)은 가운데
            yo = mh - im.height if ('pillar' in k or 'split' in k) else (mh - im.height) // 2
            c.paste(im, ((mw - im.width) // 2, yo))
            c.save(od + nm + '.png')
            bg = Image.new('RGB', (mw, mh), (0, 0, 0)); bg.paste(c, (0, 0), c)
            res.append((nm, bg))
    return len(bands) > 0, idx, nframes, res


def contact(k, res, black):
    f = ImageFont.truetype(FONT, 14)
    cw = max(r[1].width for r in res) + 16
    ch = max(r[1].height for r in res) + 30
    cols = min(len(res), max(1, 1600 // cw))
    rows = (len(res) + cols - 1) // cols
    sh = Image.new('RGB', (cols * cw, rows * ch), (0, 0, 0) if black else (40, 44, 60))
    d = ImageDraw.Draw(sh)
    for i, (nm, im) in enumerate(res):
        x, y = (i % cols) * cw, (i // cols) * ch
        if not black:
            # 체크무늬 위에 얹어 뚫림 확인
            bg = Image.new('RGB', im.size, (70, 74, 90))
            dd = ImageDraw.Draw(bg)
            for yy in range(0, im.height, 8):
                for xx in range(0, im.width, 8):
                    if (xx // 8 + yy // 8) % 2:
                        dd.rectangle([xx, yy, xx + 7, yy + 7], fill=(95, 99, 118))
            bg.paste(im, (0, 0), im)
            sh.paste(bg, (x + 8, y + 24))
        else:
            sh.paste(im.convert('RGB'), (x + 8, y + 24))
        d.text((x + 8, y + 4), f'{nm} {im.width}x{im.height}', font=f, fill=(255, 220, 120))
    sh.save(EX + f'ref/store_mock/check_{k}.png')


keys = sys.argv[1:] or list(man)
report = []
for k in keys:
    if not os.path.exists(EX + f'in/{k}.png'):
        report.append(f'{k}\t미납'); continue
    if man[k]['bg'] == 'pink':
        ok, got, want, res = cut_pink(k, man[k]['parts'])
        black = False
    else:
        ok, got, want, res = cut_black(k, FX_FRAMES[k])
        black = True
    if res:
        contact(k, res, black)
    report.append(f'{k}\t{"맞음" if ok else "개수 다름"}\t{got}/{want}')
print('\n'.join(report))
