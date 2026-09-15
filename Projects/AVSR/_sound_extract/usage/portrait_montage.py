"""호스트 강제 실행 스냅샷에서 HUD 초상화를 잘라 번호별 몽타주를 만든다."""
import os, re, sys
from PIL import Image, ImageDraw

SP = sys.argv[1]
HUD_SHEET = sys.argv[2]
CROP = (6, 196, 40, 224)          # 256x224 화면 하단 왼쪽 초상화 영역
SCALE = 4
cells = []
for k in range(1, 27):
    log = os.path.join(SP, f"fh_k{k}.txt")
    shots = []
    for line in open(log):
        m = re.match(r"^P ([\d.]+) shot=(\d+)", line)
        if m and float(m.group(1)) in (15.0, 30.0, 60.0):
            shots.append(int(m.group(2)))
    imgs = []
    for s in shots:
        p = os.path.join(SP, f"snap_fh_k{k}", "avspirit", f"{s:04d}.png")
        if os.path.exists(p):
            imgs.append(Image.open(p).convert("RGB").crop(CROP).resize(((CROP[2]-CROP[0]) * SCALE, (CROP[3]-CROP[1]) * SCALE), Image.NEAREST))
    cells.append((k, imgs))
cw, ch = (CROP[2]-CROP[0]) * SCALE, (CROP[3]-CROP[1]) * SCALE
cols = 3
label_w = 60
rows_per_col = 9
W = cols * (label_w + 3 * cw + 20)
H = rows_per_col * (ch + 6)
sheet = Image.new("RGB", (W, H + 300), (40, 40, 40))
d = ImageDraw.Draw(sheet)
for i, (k, imgs) in enumerate(cells):
    col, row = divmod(i, rows_per_col)
    x0 = col * (label_w + 3 * cw + 20)
    y0 = row * (ch + 6)
    d.text((x0 + 8, y0 + ch // 2 - 6), f"#{k}", fill=(255, 255, 0))
    for j, im in enumerate(imgs[:3]):
        sheet.paste(im, (x0 + label_w + j * cw, y0))
hud = Image.open(HUD_SHEET).convert("RGB").crop((12, 150, 175, 245))
hud = hud.resize((hud.width * 3, hud.height * 3), Image.NEAREST)
sheet.paste(hud, (10, H + 10))
out = os.path.join(SP, "host_portraits.png")
sheet.save(out)
print(out, sheet.size)