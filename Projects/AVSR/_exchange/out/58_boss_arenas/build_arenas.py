from pathlib import Path
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter
import random

ROOT = Path(r"C:/won/UnityProject/AvengingSprit")
FLOORS = ROOT / "Assets/BundleResource/RoomFloor"
DELIVERY = ROOT / "Projects/AVSR/_exchange/in"
PREVIEW = ROOT / "Projects/AVSR/_exchange/out/58_boss_arenas"
OUT = PREVIEW / "delivery"
OUT.mkdir(parents=True, exist_ok=True)
PREVIEW.mkdir(parents=True, exist_ok=True)
random.seed(5801)

def base(name):
    return Image.open(FLOORS / name).convert("RGB")

def line(draw, xy, fill, width=1):
    draw.line(xy, fill=fill, width=width)

def grid(draw, box, dark, light=None, step=36):
    x0,y0,x1,y1=box
    for x in range(x0,x1+1,step):
        line(draw,(x,y0,x,y1),dark,2)
        if light: line(draw,(x+2,y0,x+2,y1),light,1)
    for y in range(y0,y1+1,step):
        line(draw,(x0,y,x1,y),dark,2)
        if light: line(draw,(x0,y+2,x1,y+2),light,1)

def metal_rivet(draw,x,y,c=(140,142,118)):
    draw.rectangle((x-2,y-2,x+2,y+2),fill=(9,12,18),outline=(0,0,0))
    draw.point((x-1,y-1),fill=c)

def save(im,name):
    assert im.size==(720,936)
    im.save(OUT/name,optimize=True)

# 1 crusher: junkyard shell + three readable conveyor lanes.
im=base("roomfloor_env_junkyard.png"); crusher_shell=im.copy(); d=ImageDraw.Draw(im)
floor=(72,144,648,864)
lane_cols=[((36,42,37),(52,61,49)),((31,45,42),(46,58,52)),((38,43,35),(55,59,45))]
for i,(x0,x1) in enumerate([(72,240),(240,480),(480,648)]):
    dark,mid=lane_cols[i]
    d.rectangle((x0,144,x1-1,864),fill=mid)
    # alternating blue conveyor stripes, clipped per lane
    slope=1 if i%2==0 else -1
    for k in range(-900,900,36):
        if slope>0: pts=[(x0,k+144),(x0+13,k+144),(x1,k+(x1-x0)+144+13),(x1,k+(x1-x0)+144)]
        else: pts=[(x0,k+(x1-x0)+144),(x0,k+(x1-x0)+157),(x1,k+157),(x1,k+144)]
        d.polygon(pts,fill=(22,74,87))
    grid(d,(x0,144,x1,864),(16,22,23),(75,79,62))
for x in (240,480):
    d.rectangle((x-6,144,x+6,864),fill=(10,13,15),outline=(99,88,55))
    for y in range(156,864,36): metal_rivet(d,x,y,(205,139,49))
# Restore the approved shell outside the exact active rectangle.
im.paste(crusher_shell.crop((0,0,720,144)),(0,0))
im.paste(crusher_shell.crop((0,864,720,936)),(0,864))
im.paste(crusher_shell.crop((0,144,72,864)),(0,144))
im.paste(crusher_shell.crop((648,144,720,864)),(648,144))
d=ImageDraw.Draw(im)
# compressed scrap at upper/lower side wall, no center obstruction
for side_x in (10,660):
    for y in range(190,840,78):
        d.rectangle((side_x,y,side_x+46,y+52),fill=(32,35,24),outline=(7,9,8),width=3)
        d.line((side_x+5,y+10,side_x+40,y+42),fill=(112,77,34),width=3)
save(im,"roomfloor_crusher.png")

# 2 guardian: missile shell, flat broad 36px metal grating and edge stores.
im=base("roomfloor_env_missile.png"); d=ImageDraw.Draw(im)
for gy in range(144,864,36):
    for gx in range(72,648,36):
        tone=(57,59,31) if ((gx//36+gy//36)&1)==0 else (63,64,34)
        d.rectangle((gx+2,gy+2,gx+34,gy+34),fill=tone)
        d.line((gx+5,gy+8,gx+31,gy+8),fill=(88,87,47),width=1)
        d.line((gx+5,gy+27,gx+31,gy+27),fill=(38,40,22),width=1)
grid(d,floor,(20,22,15),(89,90,48))
# yellow ammo lockers along upper wall, leaving door open
for x in (90,162,486,558):
    d.rectangle((x,92,x+54,140),fill=(69,66,19),outline=(8,9,6),width=3)
    d.rectangle((x+5,98,x+49,134),outline=(143,125,24),width=3)
    d.line((x+5,103,x+49,127),fill=(175,137,17),width=4)
save(im,"roomfloor_guardian.png")

# 3 python: street shell; all four walls read as the same breakable brick.
im=base("roomfloor_env_street.png"); d=ImageDraw.Draw(im)
brick_dark=(10,17,31); brick=(25,41,66); hi=(43,61,83); mortar=(5,10,19)
bands=[(0,72,0,936),(648,720,0,936),(0,720,72,144),(0,720,864,936)]
for band in bands:
    if len(band)==4 and band[0] in (0,648):
        x0,x1,y0,y1=band
        d.rectangle((x0,y0,x1-1,y1-1),fill=brick_dark)
        for y in range(y0,y1,24):
            off=0 if (y//24)%2==0 else 18
            d.line((x0,y,x1,y),fill=mortar,width=2)
            for x in range(x0-off,x1,36): d.line((x,y,x,y+24),fill=mortar,width=2)
    else:
        x0,x1,y0,y1=band
        d.rectangle((x0,y0,x1-1,y1-1),fill=brick_dark)
        for y in range(y0,y1,24):
            off=0 if (y//24)%2==0 else 18
            d.line((x0,y,x1,y),fill=mortar,width=2)
            for x in range(x0-off,x1,36): d.line((x,y,x,y+24),fill=mortar,width=2)
    # add blue face highlights
    for _ in range(30):
        x=random.randrange(band[0]+3,max(band[0]+4,band[1]-5)); y=random.randrange(band[2]+3,max(band[2]+4,band[3]-5))
        d.rectangle((x,y,x+random.randrange(3,10),y+2),fill=hi)
# damaged notches on two different walls and edge-only debris
for poly in [[(0,310),(25,296),(47,312),(72,300),(72,362),(45,350),(18,370),(0,358)],
             [(648,612),(674,594),(695,608),(720,596),(720,660),(688,648),(665,669),(648,650)]]:
    d.polygon(poly,fill=(5,9,16)); d.line(poly+[poly[0]],fill=(58,76,96),width=3)
for x,y in [(82,318),(94,346),(625,623),(611,650),(110,850),(600,150)]:
    d.polygon([(x,y),(x+12,y-5),(x+19,y+5),(x+9,y+13)],fill=brick,outline=mortar)
im=ImageEnhance.Brightness(im).enhance(1.09)
save(im,"roomfloor_python.png")

# 4 kingpin: rooftop shell + four low snag structures at the active perimeter.
im=base("roomfloor_env_rooftop.png"); d=ImageDraw.Draw(im)
def rooftop_unit(cx,cy,kind):
    # hard-edged cast shadow first
    d.polygon([(cx-42,cy+20),(cx+35,cy+20),(cx+58,cy+39),(cx-20,cy+39)],fill=(4,12,23))
    if kind==0: # vent
        d.rectangle((cx-35,cy-18,cx+35,cy+20),fill=(17,47,76),outline=(3,10,19),width=3)
        for yy in range(cy-11,cy+15,8): d.line((cx-27,yy,cx+27,yy),fill=(48,90,119),width=3)
    elif kind==1: # water tank
        d.ellipse((cx-33,cy-31,cx+33,cy-3),fill=(39,69,96),outline=(3,9,18),width=3)
        d.rectangle((cx-33,cy-17,cx+33,cy+18),fill=(24,53,82),outline=(3,9,18),width=3)
        d.arc((cx-33,cy+2,cx+33,cy+30),0,180,fill=(72,112,138),width=3)
    else: # antenna pedestal
        d.rectangle((cx-24,cy-8,cx+24,cy+20),fill=(20,51,80),outline=(2,8,16),width=3)
        d.line((cx,cy-48,cx,cy+5),fill=(95,158,184),width=5)
        d.line((cx-14,cy-37,cx+14,cy-37),fill=(62,122,157),width=3)
    metal_rivet(d,cx-25,cy+10,(91,185,209)); metal_rivet(d,cx+25,cy+10,(91,185,209))
for args in [(122,225,0),(598,238,1),(122,750,2),(598,722,0)]: rooftop_unit(*args)
save(im,"roomfloor_kingpin.png")

# 5 robot snakes: lab shell + six identical closed hatch rings at exact centers.
im=base("roomfloor_ch1_twin_platform.png"); d=ImageDraw.Draw(im)
centers=[(180,288),(360,252),(540,288),(180,576),(360,612),(540,576)]
def hatch(cx,cy):
    # 144 diameter; ring and segmented closed cover
    d.ellipse((cx-72,cy-72,cx+71,cy+71),fill=(3,8,20),outline=(0,0,0),width=3)
    d.ellipse((cx-66,cy-66,cx+65,cy+65),fill=(25,39,72),outline=(78,99,132),width=4)
    d.ellipse((cx-54,cy-54,cx+53,cy+53),fill=(9,27,52),outline=(2,7,16),width=4)
    for ang in range(0,360,45):
        import math
        a=math.radians(ang); x=int(cx+61*math.cos(a)); y=int(cy+61*math.sin(a)); metal_rivet(d,x,y,(115,157,181))
    # six identical radial cover seams
    for dx,dy in [(0,-50),(43,-25),(43,25),(0,50),(-43,25),(-43,-25)]:
        d.line((cx,cy,cx+dx,cy+dy),fill=(49,73,105),width=3)
    d.rectangle((cx-13,cy-8,cx+13,cy+8),fill=(18,48,75),outline=(83,126,151),width=2)
for c in centers: hatch(*c)
im=ImageEnhance.Brightness(im).enhance(1.08)
save(im,"roomfloor_robot_snakes.png")

# 6 sludge: refinery shell, deliberately brightest; wet metal with stepped reflections.
im=base("roomfloor_env_refinery.png")
im=ImageEnhance.Brightness(im).enhance(1.38)
d=ImageDraw.Draw(im)
for gy in range(144,864,36):
    for gx in range(72,648,36):
        if ((gx//36)*7+(gy//36)*3)%5==0:
            d.rectangle((gx+4,gy+5,gx+29,gy+8),fill=(106,108,98))
            d.rectangle((gx+8,gy+9,gx+24,gy+10),fill=(67,73,70))
        elif ((gx//36)+(gy//36))%4==0:
            d.line((gx+6,gy+28,gx+22,gy+25),fill=(82,88,82),width=2)
grid(d,floor,(25,27,25),(88,85,72))
# edge slime only
slime=(74,103,45); slime_hi=(118,137,63)
for x in range(78,648,72):
    d.line((x,145,x+25,145,x+33,158),fill=slime,width=4)
    d.line((x+2,145,x+18,145),fill=slime_hi,width=2)
save(im,"roomfloor_sludge.png")

# QA measurements and contact sheet.
names=["crusher","guardian","python","kingpin","robot_snakes","sludge"]
report=[]
thumbs=[]
for n in names:
    p=OUT/f"roomfloor_{n}.png"; q=Image.open(p).convert("RGB")
    px=list(q.getdata()); lum=sum(.299*r+.587*g+.114*b for r,g,b in px)/len(px)
    report.append(f"roomfloor_{n}.png\t{q.size[0]}x{q.size[1]}\tmean_luma={lum:.2f}")
    t=q.resize((240,312),Image.Resampling.NEAREST); thumbs.append(t)
(PREVIEW/"qa.txt").write_text("\n".join(report),encoding="utf-8")
sheet=Image.new("RGB",(720,664),(5,8,14)); sd=ImageDraw.Draw(sheet)
for i,(n,t) in enumerate(zip(names,thumbs)):
    x=(i%3)*240; y=(i//3)*332
    sheet.paste(t,(x,y+20)); sd.text((x+6,y+4),n,fill=(235,240,245))
sheet.save(PREVIEW/"boss_arenas_contact.png")
