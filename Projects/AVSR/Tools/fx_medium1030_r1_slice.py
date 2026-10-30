# 영매 강신 의식 1번 시안 부품 시트 자르기(2026-10-30, fx_medium1030_slice 를 고쳐 씀) — 검정 바탕 시트 → 프레임마다 밝기를 투명도로(되곱셈 풀기) → 같은 틀로 잘라 게임 크기로.
# 쓰기: python Projects/AVSR/Tools/fx_medium1030_slice.py   (납품된 sheet_*.png 만 처리한다)
import os
import numpy as np
from PIL import Image

D = 'Projects/AVSR/_exchange/fx_medium1030_r1'
# 강신 의식 1번 시안 부품 — 게임 가로 = 시안에서 잰 가로 × 0.75(시안 영매 키 103 px → 게임 77 px)
SPEC = {
    'r1_rune':    ('fx_mdr1rune',    420, 'x', 5),
    'r1_moon':    ('fx_mdr1moon',    110,  'x', 0),
    'r1_ghost':   ('fx_mdr1ghost',   100,  'x', 5),
    'r1_chain':   ('fx_mdr1chain',   85,  'x', 0),
    'r1_aura':    ('fx_mdr1aura',    140, 'x', 0),
    'r1_pillar':  ('fx_mdr1pillar',  36,  'x', 0),
    'r1_subring': ('fx_mdr1subring', 105, 'x', 0),
    'r1_up':      ('fx_mdr1up',      52,  'x', 0),
    'r1_upself':  ('fx_mdr1upself',  52,  'x', 0),
}


def to_alpha(rgb):
    a = rgb.max(axis=2)
    out = np.zeros(rgb.shape[:2] + (4,), np.float32)
    nz = a > 0
    out[..., :3][nz] = rgb[nz] / a[nz, None] * 255.0
    out[..., 3] = a
    out[a < 10] = 0   # 검정 바탕의 잡티
    return out


def frames_of(alpha, axis):
    prof = (alpha > 10).any(axis=0 if axis == 'x' else 1)
    runs, start = [], None
    for i, on in enumerate(prof):
        if on and start is None: start = i
        if not on and start is not None:
            if i - start > 8: runs.append((start, i))
            start = None
    if start is not None: runs.append((start, len(prof)))
    return runs


def frames_by_count(alpha, n):
    prof = alpha.sum(axis=0)
    W = len(prof)
    cuts = [0]
    for k in range(1, n):
        c = k * W // n; r = W // (2 * n)
        cuts.append(c - r + int(np.argmin(prof[c - r:c + r])))
    cuts.append(W)
    return list(zip(cuts[:-1], cuts[1:]))


def main():
    os.makedirs(f'{D}/game', exist_ok=True)
    for key, (name, gw, axis, count) in SPEC.items():
        p = f'{D}/sheet_{key}.png'
        if not os.path.exists(p):
            print('없음', key); continue
        rgb = np.asarray(Image.open(p).convert('RGB'), np.float32)
        rgba = to_alpha(rgb)
        runs = frames_by_count(rgba[..., 3], count) if count else frames_of(rgba[..., 3], axis)
        # 프레임마다 같은 틀(합친 테두리) — 프레임끼리 흔들리지 않게
        cuts = [rgba[:, a:b] if axis == 'x' else rgba[a:b, :] for a, b in runs]
        boxes = []
        for c in cuts:
            ys, xs = np.nonzero(c[..., 3] > 10)
            boxes.append((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
        w = max(b[2] - b[0] for b in boxes); h = max(b[3] - b[1] for b in boxes)
        size = (gw, max(1, round(gw * h / w)))
        for i, (c, b) in enumerate(zip(cuts, boxes), 1):
            cx, cy = (b[0] + b[2]) // 2, (b[1] + b[3]) // 2
            frame = np.zeros((h, w, 4), np.float32)
            x0, y0 = cx - w // 2, cy - h // 2
            src = c[max(0, y0):y0 + h, max(0, x0):x0 + w]
            frame[max(0, -y0):max(0, -y0) + src.shape[0], max(0, -x0):max(0, -x0) + src.shape[1]] = src
            im = Image.fromarray(frame.clip(0, 255).astype(np.uint8), 'RGBA')
            im.save(f'{D}/full_{name}_{i}.png')
            im.resize(size, Image.LANCZOS).save(f'{D}/game/{name}_{i}.png')
        print(key, '프레임', len(cuts), '틀', (w, h), '→ 게임', size)


if __name__ == '__main__':
    main()
