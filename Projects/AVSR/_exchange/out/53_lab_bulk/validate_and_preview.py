from pathlib import Path
from PIL import Image, ImageDraw

root=Path(r'C:\won\UnityProject\AvengingSprit')
asset=root/'Projects/AVSR/_exchange/in/obj_bulk.png'
im=Image.open(asset).convert('RGBA')
alpha={p[3] for p in im.getdata()}
bbox=im.getchannel('A').getbbox()
bottom=sum(1 for x in range(im.width) if im.getpixel((x,237))[3])
print('size',im.size,'alpha',sorted(alpha),'bbox',bbox,'opaque_bottom_pixels',bottom)

names=['obj_holding_bulk.png','obj_junkyard_bulk.png','obj_missile_bulk.png','obj_rooftop_bulk.png','obj_refinery_bulk.png','obj_bulk.png']
canvas=Image.new('RGBA',(6*174,278),(7,12,22,255))
for i,n in enumerate(names):
    p=asset if n=='obj_bulk.png' else root/'Assets/BaseResource/InGameMainUI'/n
    tile=Image.open(p).convert('RGBA')
    canvas.alpha_composite(tile,(i*174+15,20))
canvas.convert('RGB').save(Path(__file__).with_name('bulk_comparison.png'))
