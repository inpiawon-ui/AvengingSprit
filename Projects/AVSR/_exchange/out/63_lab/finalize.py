from pathlib import Path
from PIL import Image, ImageEnhance, ImageDraw, ImageStat

SRC = Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d/exec-c4498f81-a5e2-48f1-92d0-092d4828af24.png")
DEST = Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/roomfloor_env_lab.png")
PREVIEW = Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/out/63_lab/roomfloor_env_lab_preview.png")
DEST.parent.mkdir(parents=True, exist_ok=True)
PREVIEW.parent.mkdir(parents=True, exist_ok=True)

src = Image.open(SRC).convert("RGB")
# Fill target without letterboxing, retaining the authored top-down composition.
scale = max(720 / src.width, 936 / src.height)
resized = src.resize((round(src.width * scale), round(src.height * scale)), Image.Resampling.LANCZOS)
left = (resized.width - 720) // 2
top = (resized.height - 936) // 2
im = resized.crop((left, top, left + 720, top + 936))

# Rebuild only the gameplay floor as an exact 16 x 20 array of 36 px slabs.
# Source texture is sampled from the generated center; seams are deterministic.
source_floor = im.crop((72, 144, 648, 864))
tile_seed = source_floor.resize((36, 36), Image.Resampling.BOX)
floor = Image.new("RGB", (576, 720))
for row in range(20):
    for col in range(16):
        tile = tile_seed.copy()
        # Restrained per-tile variation, without changing geometry.
        factor = 0.91 + ((col * 7 + row * 11) % 9) * 0.012
        tile = ImageEnhance.Brightness(tile).enhance(factor)
        floor.paste(tile, (col * 36, row * 36))

draw = ImageDraw.Draw(floor)
seam = (8, 16, 25)
highlight = (30, 47, 59)
for x in range(0, 577, 36):
    if x < 576:
        draw.line((x, 0, x, 719), fill=seam, width=2)
        if x + 2 < 576:
            draw.line((x + 2, 0, x + 2, 719), fill=highlight, width=1)
for y in range(0, 721, 36):
    if y < 720:
        draw.line((0, y, 575, y), fill=seam, width=2)
        if y + 2 < 720:
            draw.line((0, y + 2, 575, y + 2), fill=highlight, width=1)
im.paste(floor, (72, 144))

# Match the approved room luminance band while preserving color relationships.
lum = ImageStat.Stat(im.convert("L")).mean[0]
if lum < 36:
    im = ImageEnhance.Brightness(im).enhance(36 / max(lum, 1))
elif lum > 46:
    im = ImageEnhance.Brightness(im).enhance(46 / lum)

im.save(DEST, optimize=True)
im.save(PREVIEW, optimize=True)
print("size", im.size, "mode", im.mode, "luminance", round(ImageStat.Stat(im.convert('L')).mean[0], 2))
