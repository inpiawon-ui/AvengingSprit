from collections import deque
from pathlib import Path
from PIL import Image

root = Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR")
target = root / "_exchange/in/cut_start_11.png"
original = Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d/exec-df3c6984-109e-4cdf-b9fe-1247688cbbb0.png")
generated = Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d/exec-30710d3f-32f0-441b-ac3a-2d6320dbcb58.png")

base = Image.open(original).convert("RGB").resize((640,640), Image.Resampling.NEAREST)
base = base.quantize(colors=38, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
floor = Image.open(generated).convert("RGB").resize((640, 640), Image.Resampling.NEAREST)
floor = floor.quantize(colors=16, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")

bp = base.load()
mask = bytearray(640 * 640)
queue = deque()

def chroma(c):
    r, g, b = c
    return g >= 65 and g > r * 2.2 and g > b * 1.65

# Only the large chroma field connected to the bottom edge is replaced.
for x in range(640):
    if chroma(bp[x, 639]):
        queue.append((x, 639))
        mask[639 * 640 + x] = 1

while queue:
    x, y = queue.popleft()
    for nx, ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
        if 0 <= nx < 640 and 0 <= ny < 640:
            i = ny * 640 + nx
            if not mask[i] and chroma(bp[nx, ny]):
                mask[i] = 1
                queue.append((nx, ny))

out = base.copy()
op, fp = out.load(), floor.load()
for y in range(640):
    for x in range(640):
        if mask[y * 640 + x]:
            op[x, y] = fp[x, y]

# Quantize only the inserted pixels. This preserves the generated tile seams and
# shadows while leaving the original console area byte-identical.
coords=[(x,y) for y in range(640) for x in range(640) if mask[y*640+x]]
strip=Image.new("RGB",(len(coords),1))
sp=strip.load()
for i,(x,y) in enumerate(coords): sp[i,0]=op[x,y]
strip=strip.quantize(colors=16,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert("RGB")
sp=strip.load()
for i,(x,y) in enumerate(coords): op[x,y]=sp[i,0]

out.save(target, optimize=True)
colors = len(out.getcolors(1_000_000))
green = sum(1 for c in out.getdata() if chroma(c))
changed = sum(mask)
print("size", out.size, "mode", out.mode, "colors", colors, "replaced", changed, "green_like_remaining", green)
