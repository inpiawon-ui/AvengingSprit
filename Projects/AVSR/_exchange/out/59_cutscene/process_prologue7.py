from pathlib import Path
from PIL import Image, ImageDraw

src = Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d/exec-52243dc2-9e44-442f-b7e0-d03f593736e3.png")
out_dir = Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/out/59_cutscene")
delivery = Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/cut_prologue_7.png")
out_dir.mkdir(parents=True, exist_ok=True)

# Nearest-neighbour keeps the generated square pixel clusters intact.
im = Image.open(src).convert("RGB").resize((640, 640), Image.Resampling.NEAREST)
pix = im.load()
navy = (7, 16, 78)

# Flood only the connected blue backdrop from the canvas edges. Color-based
# replacement across the whole image would damage the lavender coat shadows.
def is_backdrop(rgb):
    r, g, b = rgb
    return b >= 72 and b-r >= 35 and b-g >= 28 and r < 80 and g < 105

stack=[]; seen=set()
for x in range(640):
    stack.extend(((x,0),(x,639)))
for y in range(640):
    stack.extend(((0,y),(639,y)))
while stack:
    x,y=stack.pop()
    if (x,y) in seen or not (0 <= x < 640 and 0 <= y < 640):
        continue
    if not is_backdrop(pix[x,y]):
        continue
    seen.add((x,y)); pix[x,y]=navy
    stack.extend(((x-1,y),(x+1,y),(x,y-1),(x,y+1)))

# Fixed 32-colour base, no-dither. Explicit navy/red/skin corrections below
# bring the finished asset into the requested 30-40 colour range.
q = im.quantize(colors=32, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")

# Lock both lenses to one solid black, as requested.
qd=ImageDraw.Draw(q)
lens_black=(3,3,5)
qd.polygon([(108,218),(153,213),(163,231),(154,254),(132,263),(111,255),(102,236)],fill=lens_black)
qd.polygon([(161,211),(199,207),(209,219),(205,239),(191,253),(168,247),(160,230)],fill=lens_black)

# Repaint the exposed firing hand with four flat skin tones, retaining only its
# dark outline. This prevents gray/lavender palette contamination.
qp=q.load(); hand_mask=Image.new("1",q.size,0)
ImageDraw.Draw(hand_mask).polygon([(347,374),(374,364),(444,370),(459,391),(439,438),(361,443),(339,414)],fill=1)
hm=hand_mask.load(); skin=[(91,39,25),(145,61,31),(190,91,45),(232,139,85)]
for y in range(360,446):
    for x in range(336,462):
        if not hm[x,y]: continue
        r,g,b=qp[x,y]; lum=(r+g+b)//3
        if lum < 35: continue
        qp[x,y]=skin[0 if lum<70 else 1 if lum<115 else 2 if lum<170 else 3]

# Replace the generated long trail with the measured 58 px pure-red afterimage.
# The gun muzzle ends at x=511 in this locked composition.
for y in range(326,355):
    for x in range(512,640):
        qp[x,y]=navy
red=(239,8,8)
for x0,x1 in [(516,523),(530,538),(546,554),(563,574)]:
    for y in range(338,342):
        for x in range(x0,x1+1):
            qp[x,y]=red

# Fourth-review correction: replace only the cool gray facial highlight with
# the next warm skin-ramp step. No positions or other palette entries change.
old_highlight=(195,169,163); warm_highlight=(232,139,85); replaced=0
for y in range(180,326):
    for x in range(80,216):
        if qp[x,y] == old_highlight:
            qp[x,y] = warm_highlight
            replaced += 1
q.save(out_dir / "cut_prologue_7_preview.png", optimize=True)
q.save(delivery, optimize=True)

colors = q.getcolors(maxcolors=1_000_000)
print(f"size={q.size[0]}x{q.size[1]} colors={len(colors)} mode={q.mode} tracer_span=58 highlight_replaced={replaced}")
