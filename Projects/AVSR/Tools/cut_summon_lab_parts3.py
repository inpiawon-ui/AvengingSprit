# -*- coding: utf-8 -*-
"""소환 깨짐 부품 3차 자르기 — 검은 바탕 가산용 파티클 시트로 만든다.
- lab_glass_shards2(4×2): 둘레 옅은 회색 얼룩(알파 1~31)을 떼고 검은 바탕에 미리 곱한다.
  큰 판 · 중간 5개 → lab_shards_plate_sheet(5×1), 부스러기 3개 → lab_shards_chip_sheet(3×1)
- lab_splash(3×2): 「후광 금지」인데 어두운 청록 번짐이 붙어 왔다 → 물 테두리 안쪽만 남기고 검게.
  → lab_splash_sheet(3×2)
칸은 정사각 128, 그림은 칸 가운데.
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
# 유니티를 다른 세션이 쓰는 동안은 Assets 에 바로 넣지 않는다(넣으면 그쪽 에디터가 임포트를 돈다) — 인자로 넣을 곳을 준다
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'Assets', 'BaseResource', 'SummonReveal')
CELL = 128


def cells(img, cols, rows):
    w, h = img.width // cols, img.height // rows
    return [img.crop((c * w, r * h, (c + 1) * w, (r + 1) * h)) for r in range(rows) for c in range(cols)]


def fit(rgb, margin=6):
    """검은 바탕 그림을 밝은 부분 테두리로 잘라 정사각 칸 가운데(가장 긴 변 = 칸 - 여백)."""
    a = np.asarray(rgb)
    ys, xs = np.nonzero(a.max(-1) > 8)
    box = rgb.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    s = (CELL - margin * 2) / max(box.size)
    box = box.resize((max(1, round(box.width * s)), max(1, round(box.height * s))), Image.LANCZOS)
    out = Image.new('RGB', (CELL, CELL))
    out.paste(box, ((CELL - box.width) // 2, (CELL - box.height) // 2))
    return out


def shard_clean(c):
    a = np.asarray(c.convert('RGBA')).astype(float)
    al = a[..., 3] / 255
    al[al < 0.5] = 0   # 회색 얼룩 테(알파 1~31)
    return Image.fromarray((a[..., :3] * al[..., None]).round().astype(np.uint8))


def splash_clean(c):
    a = np.asarray(c.convert('RGB')).astype(float)
    v = a.max(-1)
    core = v >= 80
    keep = ndimage.binary_fill_holes(ndimage.binary_dilation(core, iterations=2))
    a[~keep] = 0
    return Image.fromarray(a.round().astype(np.uint8))


def gain(img, k):
    """밝기를 그림에 굽는다 — 파티클 색은 1 을 못 넘는다. 선형 색 공간에서 k 배(시안 합성으로 정한 값)."""
    c = np.asarray(img).astype(float) / 255
    lin = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4) * k
    lin = np.clip(lin, 0, 1)
    s = np.where(lin <= 0.0031308, lin * 12.92, 1.055 * np.power(lin, 1 / 2.4) - 0.055)
    return Image.fromarray((s * 255).round().astype(np.uint8))


# 시안(mock_summon_lab_free_peak) 대조로 정한 밝기 — 유리는 뒤 금빛을 받아 더 밝게 빛난다(2026-10-22)
SHARD_GAIN, SPLASH_GAIN = 1.7, 1.4


def sheet(imgs, cols, rows, name):
    s = Image.new('RGB', (CELL * cols, CELL * rows))
    for i, im in enumerate(imgs):
        s.paste(im, ((i % cols) * CELL, (i // cols) * CELL))
    s.save(os.path.join(OUT, name))
    print(name, s.size)


def pieces(rgb, n, rows):
    """칸이 아니라 덩어리로 자른다 — 납품 그림은 조각이 칸 경계를 넘는다. 큰 덩어리 n 개를 위 줄부터 왼→오 순으로."""
    a = np.asarray(rgb)
    lab, _ = ndimage.label(ndimage.binary_dilation(a.max(-1) > 8, iterations=3))
    objs = ndimage.find_objects(lab)
    area = [(o[0].stop - o[0].start) * (o[1].stop - o[1].start) for o in objs]
    top = sorted(range(len(objs)), key=lambda i: -area[i])[:n]
    row_h = a.shape[0] / rows
    top.sort(key=lambda i: (int(((objs[i][0].start + objs[i][0].stop) / 2) // row_h), objs[i][1].start))
    out = []
    for i in top:
        o = objs[i]
        m = (lab[o] == i + 1)[..., None]
        out.append(Image.fromarray((a[o] * m).astype(np.uint8)))
    return out


def main():
    sh = pieces(shard_clean(Image.open(os.path.join(IN, 'lab_glass_shards2.png'))), 8, 2)
    sheet([gain(fit(c), SHARD_GAIN) for c in sh[:5]], 5, 1, 'lab_shards_plate_sheet.png')
    sheet([gain(fit(c), SHARD_GAIN) for c in sh[5:]], 3, 1, 'lab_shards_chip_sheet.png')
    sp = [splash_clean(c) for c in cells(Image.open(os.path.join(IN, 'lab_splash.png')), 3, 2)]
    sheet([gain(fit(c), SPLASH_GAIN) for c in sp], 3, 2, 'lab_splash_sheet.png')


if __name__ == '__main__':
    main()
