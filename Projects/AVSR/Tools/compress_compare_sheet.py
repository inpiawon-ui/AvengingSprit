import cv2, numpy as np, glob, os
from PIL import Image, ImageDraw, ImageFont
root = r'C:\won\UnityProject\AvengingSprit'
src_dir = os.path.join(root, r'Assets\BaseResource\InGameMainUI')
cdir = os.path.join(root, r'Projects\AVSR\_exchange\ref\resource_audit\compress')
names = ['obj_arc_emitter_on', 'buffcard_c001', 'affinity_strike', 'rps_tri_force', 'exitportal', 'levelupframe']
names = [n for n in names if os.path.exists(os.path.join(src_dir, n + '.png'))]


def load_rgba(p):
    im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
    if im.shape[2] == 3:
        im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
    return im


def on_black(im):
    a = im[:, :, 3:4].astype(np.float32) / 255
    return (im[:, :, :3].astype(np.float32) * a).astype(np.uint8)


pages = {m: [load_rgba(p) for p in sorted(glob.glob(os.path.join(cdir, m + '_p*.png')))] for m in ('a_now', 'b_astc4x4', 'c_astc6x6')}


def find(src, mode):
    t = on_black(src)
    best = None
    for pg in pages[mode]:
        # 아틀라스 미리보기는 위아래가 뒤집혀 있을 수 있다 — 둘 다 본다
        for flip in (False, True):
            P = on_black(cv2.flip(pg, 0) if flip else pg)
            if P.shape[0] < t.shape[0] or P.shape[1] < t.shape[1]:
                continue
            r = cv2.matchTemplate(P, t, cv2.TM_SQDIFF)
            mn, _, loc, _ = cv2.minMaxLoc(r)
            score = mn / t.size
            if best is None or score < best[0]:
                img = cv2.flip(pg, 0) if flip else pg
                best = (score, img[loc[1]:loc[1] + t.shape[0], loc[0]:loc[0] + t.shape[1]])
    return best


rows = []
for n in names:
    src = load_rgba(os.path.join(src_dir, n + '.png'))
    h, w = src.shape[:2]
    scale = max(1, min(4, 300 // max(h, w) if max(h, w) < 300 else 1))
    crops = [src]
    errs = []
    for m in ('a_now', 'b_astc4x4', 'c_astc6x6'):
        s, c = find(src, m)
        crops.append(c)
        diff = np.abs(c[:, :, :3].astype(int) - src[:, :, :3].astype(int)) * (src[:, :, 3:4] > 0)
        errs.append(diff.mean())
    # 큰 것은 가운데 일부만(디테일이 보이게)
    if max(h, w) > 300:
        cy, cx = h // 2, w // 2
        hh, ww = min(h, 160), min(w, 240)
        crops = [c[cy - hh // 2:cy + hh // 2, cx - ww // 2:cx + ww // 2] for c in crops]
        scale = 2
    tiles = []
    for c in crops:
        bg = np.zeros((c.shape[0], c.shape[1], 3), np.uint8); bg[:] = (40, 30, 30)
        a = c[:, :, 3:4].astype(np.float32) / 255
        comp = (c[:, :, :3] * a + bg * (1 - a)).astype(np.uint8)
        comp = cv2.resize(comp, None, fx=scale, fy=scale, interpolation=cv2.INTER_NEAREST)
        tiles.append(cv2.cvtColor(comp, cv2.COLOR_BGR2RGB))
    rows.append((n, tiles, errs))

colw = max(t.shape[1] for _, ts, _ in rows for t in ts) + 16
head = 44
H = head + sum(max(t.shape[0] for t in ts) + 34 for _, ts, _ in rows)
sheet = Image.new('RGB', (colw * 4, H), (24, 24, 28))
d = ImageDraw.Draw(sheet)
try:
    f = ImageFont.truetype(r'C:\Windows\Fonts\malgun.ttf', 18)
    fs = ImageFont.truetype(r'C:\Windows\Fonts\malgun.ttf', 14)
except OSError:
    f = fs = None
for i, t in enumerate(['원본 그림', '지금 (압축 없음 · 272MB)', 'ASTC 4x4 (약 32MB)', 'ASTC 6x6 (약 14MB)']):
    d.text((i * colw + 8, 10), t, fill=(240, 220, 160), font=f)
y = head
for n, ts, errs in rows:
    d.text((8, y), n, fill=(200, 200, 200), font=fs)
    for i, e in enumerate(errs):
        d.text(((i + 1) * colw + 8, y), f'원본과 차이 {e:.1f}', fill=(150, 200, 150), font=fs)
    y += 20
    for i, t in enumerate(ts):
        sheet.paste(Image.fromarray(t), (i * colw + 8, y))
    y += max(t.shape[0] for t in ts) + 14
out = os.path.join(cdir, 'compare_ingamemainui.png')
sheet.save(out)
print(out, sheet.size, [(n, [round(e, 1) for e in errs]) for n, _, errs in rows])
