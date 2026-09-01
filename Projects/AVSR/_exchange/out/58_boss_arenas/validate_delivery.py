from pathlib import Path
from PIL import Image
import hashlib

root=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange")
names=["crusher","guardian","python","kingpin","robot_snakes","sludge"]
for n in names:
    a=root/"in"/f"roomfloor_{n}.png"
    b=root/"out/58_boss_arenas/delivery"/f"roomfloor_{n}.png"
    im=Image.open(a).convert("RGB")
    data=list(im.getdata())
    lum=sum(.299*r+.587*g+.114*bb for r,g,bb in data)/len(data)
    same=hashlib.sha256(a.read_bytes()).digest()==hashlib.sha256(b.read_bytes()).digest()
    print(f"{a.name}: {im.size[0]}x{im.size[1]}, mean={lum:.2f}, hash_match={same}")
