from pathlib import Path
from PIL import Image, ImageDraw

root = Path(r"C:/won/UnityProject/AvengingSprit")
boss_root = root / "Projects/AVSR/Reference/Original"
floor_root = root / "Assets/BundleResource/RoomFloor"
out = root / "Projects/AVSR/_exchange/out/58_boss_arenas"
out.mkdir(parents=True, exist_ok=True)

bosses = ["Crusher", "Guardian", "Python", "Kingpin", "Robot Snakes", "Sludge"]
floors = ["junkyard", "missile", "street", "rooftop", "lab", "refinery"]
floor_files = {
    "junkyard": floor_root / "roomfloor_env_junkyard.png",
    "missile": floor_root / "roomfloor_env_missile.png",
    "street": floor_root / "roomfloor_env_street.png",
    "rooftop": floor_root / "roomfloor_env_rooftop.png",
    "lab": floor_root / "roomfloor_ch1_twin_platform.png",
    "refinery": floor_root / "roomfloor_env_refinery.png",
}

canvas = Image.new("RGB", (1200, 1290), (5, 8, 15))
d = ImageDraw.Draw(canvas)
for i, (boss, floor) in enumerate(zip(bosses, floors)):
    col, row = i % 3, i // 3
    x, y = col * 400, row * 645
    b = Image.open(boss_root / f"Bosses - {boss}.png").convert("RGB")
    b.thumbnail((380, 250), Image.Resampling.NEAREST)
    f = Image.open(floor_files[floor]).convert("RGB")
    f.thumbnail((270, 351), Image.Resampling.NEAREST)
    d.text((x + 10, y + 8), f"{boss} / {floor}", fill=(230, 235, 245))
    canvas.paste(b, (x + 10, y + 35))
    canvas.paste(f, (x + 65, y + 285))
canvas.save(out / "reference_contact.png")
