from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
IN=ROOT/'Projects/AVSR/_exchange/in'; OUT=Path(__file__).parent
spec={
'obj_refinery_crate_1.png':(144,132),'obj_rooftop_pillar.png':(72,166),'obj_street_bulk.png':(144,238),
'obj_missile_barricade.png':(216,102),'obj_rooftop_barricade.png':(216,102),'obj_junkyard_barricade.png':(216,102),'obj_street_barricade.png':(216,102),
'obj_junkyard_low_cover.png':(216,102),'obj_missile_low_cover.png':(216,102),'obj_rooftop_low_cover.png':(216,102),'obj_street_low_cover.png':(216,102),
'obj_street_blade_1.png':(144,144),'obj_rooftop_blade_1.png':(144,144),'obj_refinery_blade_1.png':(144,144)}
font=ImageFont.truetype(r'C:\Windows\Fonts\arial.ttf',14)
sheet=Image.new('RGBA',(920,920),(6,11,20,255)); d=ImageDraw.Draw(sheet)
x=y=15; rowh=0
for name,size in spec.items():
    im=Image.open(IN/name).convert('RGBA'); alpha={p[3] for p in im.getdata()}
    bottom=sum(im.getpixel((xx,im.height-1))[3]>0 for xx in range(im.width))
    ok=im.size==size and alpha<={0,255} and bottom>0
    print(f'{name:32} {im.size} alpha={sorted(alpha)} bottom={bottom} {"PASS" if ok else "FAIL"}')
    if x+max(216,im.width)+15>920: x=15; y+=rowh+42; rowh=0
    sheet.alpha_composite(im,(x,y)); d.text((x,y+im.height+4),name.replace('obj_',''),font=font,fill=(210,220,225,255))
    x+=max(216,im.width)+15; rowh=max(rowh,im.height)
sheet.convert('RGB').save(OUT/'props_contact.png')
