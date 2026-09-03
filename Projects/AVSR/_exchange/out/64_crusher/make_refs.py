from pathlib import Path
from PIL import Image, ImageDraw

root = Path(r"C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/crusher")
out = Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/out/64_crusher")
out.mkdir(parents=True, exist_ok=True)
actions = ["atk1", "atk2", "move1", "move2", "hit"]
sheet = Image.new("RGBA", (5*256, 3*256), (24,24,30,255))
for c, action in enumerate(actions):
    for r, direction in enumerate(("s", "se", "e")):
        p = root / f"unit_crusher_{direction}_{action}.png"
        sheet.alpha_composite(Image.open(p).convert("RGBA"), (c*256, r*256))
sheet.save(out / "existing_actions.png")
angles = Image.new("RGBA", (5*256,256), (24,24,30,255))
for c,direction in enumerate(("s","se","e","ne","n")):
    angles.alpha_composite(Image.open(root/f"unit_crusher_{direction}.png").convert("RGBA"),(c*256,0))
angles.save(out / "accepted_angles.png")
