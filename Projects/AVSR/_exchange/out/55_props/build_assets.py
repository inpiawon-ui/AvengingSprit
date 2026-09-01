from pathlib import Path
from collections import deque
from PIL import Image, ImageDraw

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
OUT=ROOT/'Projects/AVSR/_exchange/in'; OUT.mkdir(parents=True,exist_ok=True)
GEN=Path(r'C:\Users\inpia\.codex\generated_images\01a03196-5880-70f2-82bb-e4a9f3a7276d')
T=(0,0,0,0); K=(0,0,0,255)

def save(im,name):
    # Binary alpha is mandatory.
    a=im.getchannel('A').point(lambda v:255 if v>=128 else 0)
    im.putalpha(a); im.save(OUT/name)

def clear_light_border(im):
    im=im.convert('RGBA'); w,h=im.size; p=im.load(); seen=set(); q=deque()
    for x in range(w): q.extend([(x,0),(x,h-1)])
    for y in range(h): q.extend([(0,y),(w-1,y)])
    while q:
        x,y=q.popleft()
        if (x,y) in seen: continue
        seen.add((x,y)); r,g,b,a=p[x,y]
        light=min(r,g,b)>220 and max(r,g,b)-min(r,g,b)<18
        if a<20 or light:
            p[x,y]=(0,0,0,0)
            if x:q.append((x-1,y))
            if x+1<w:q.append((x+1,y))
            if y:q.append((x,y-1))
            if y+1<h:q.append((x,y+1))
    return im

def generated(src,name,size,content_size):
    im=clear_light_border(Image.open(GEN/src))
    bbox=im.getchannel('A').getbbox(); im=im.crop(bbox)
    im=im.resize(content_size,Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',size,T)
    canvas.alpha_composite(im,((size[0]-content_size[0])//2,size[1]-content_size[1]))
    save(canvas,name)

# Three priority assets use the generated concepts, fitted to exact footprints.
generated('exec-f21f12df-2901-4272-8c4c-51a078e091ac.png','obj_refinery_crate_1.png',(144,132),(142,120))
generated('exec-9e2643e6-cad8-4c60-b061-33c6f6149543.png','obj_rooftop_pillar.png',(72,166),(70,166))
generated('exec-a83c35db-b8e7-4b11-8cd6-b38bbc5a116b.png','obj_street_bulk.png',(144,238),(140,238))

PAL={
'junk':((95,95,54,255),(49,54,35,255),(112,77,35,255),(174,143,70,255)),
'miss':((58,60,25,255),(35,40,22,255),(103,104,45,255),(172,158,75,255)),
'street':((19,31,54,255),(25,40,66,255),(33,52,76,255),(76,113,137,255)),
'roof':((12,38,68,255),(11,37,67,255),(24,72,104,255),(57,143,170,255)),
'ref':((31,34,34,255),(73,69,59,255),(128,65,31,255),(210,196,155,255))}

def barricade(kind,name,mode):
    a,b,c,h=PAL[kind]; im=Image.new('RGBA',(216,102),T); d=ImageDraw.Draw(im)
    # continuous grounded feet
    d.rectangle((5,96,210,101),fill=K)
    if mode=='scrap':
        for pts in [[(7,38),(57,30),(63,87),(12,93)],[(69,26),(133,34),(128,91),(64,88)],[(139,36),(207,27),(211,91),(136,94)]]:
            d.polygon(pts,fill=K); inner=[(x+(2 if x<108 else -2),y+3) for x,y in pts]; d.polygon(inner,fill=a)
        for x in (18,87,158): d.line((x,35,x+8,99),fill=c,width=4)
        d.line((8,65,208,59),fill=h,width=3)
    elif mode=='concrete':
        for x0 in (3,72,141):
            d.polygon([(x0,44),(x0+8,34),(x0+64,34),(x0+70,44),(x0+66,97),(x0+4,97)],fill=K)
            d.polygon([(x0+5,45),(x0+12,39),(x0+59,39),(x0+65,45),(x0+61,91),(x0+9,91)],fill=a)
            d.line((x0+12,42,x0+58,42),fill=h,width=2)
        d.rectangle((8,73,208,80),fill=c)
    elif mode=='street':
        for x in (12,101,190):
            d.rectangle((x,24,x+13,98),fill=K); d.rectangle((x+3,28,x+10,94),fill=b)
        for y in (43,68):
            d.rectangle((18,y,197,y+13),fill=K); d.rectangle((22,y+3,193,y+9),fill=c)
            for x in range(24,188,36): d.polygon([(x,y+3),(x+12,y+3),(x+22,y+9),(x+10,y+9)],fill=h)
    else: # rooftop safety fence
        for x in (8,69,138,199):
            d.rectangle((x,18,x+8,99),fill=K); d.rectangle((x+2,22,x+6,95),fill=c)
        d.rectangle((8,24,207,33),fill=K); d.rectangle((12,27,203,30),fill=h)
        d.rectangle((8,75,207,84),fill=K); d.rectangle((12,78,203,81),fill=c)
        for x in range(17,195,14): d.line((x,34,x+34,74),fill=b,width=2)
        for x in range(17,195,14): d.line((x+34,34,x,74),fill=b,width=2)
    save(im,name)

barricade('miss','obj_missile_barricade.png','concrete')
barricade('roof','obj_rooftop_barricade.png','roof')
barricade('junk','obj_junkyard_barricade.png','scrap')
barricade('street','obj_street_barricade.png','street')

def lowcover(kind,name,mode):
    a,b,c,h=PAL[kind]; im=Image.new('RGBA',(216,102),T); d=ImageDraw.Draw(im)
    if mode=='scrap':
        pieces=[(4,61,54,100),(43,48,99,100),(91,59,143,100),(132,44,190,100),(176,63,213,100)]
        for i,box in enumerate(pieces):
            d.rectangle(box,fill=K); x0,y0,x1,y1=box; d.rectangle((x0+3,y0+3,x1-2,y1-1),fill=(a,c,b,h)[i%4]); d.line((x0+5,y0+7,x1-5,y1-6),fill=h,width=2)
        d.ellipse((17,67,55,101),fill=K); d.ellipse((23,72,49,98),fill=b); d.ellipse((31,79,42,93),fill=K)
    elif mode=='ammo':
        for x0 in (3,55,107,159):
            d.polygon([(x0,47),(x0+44,47),(x0+51,55),(x0+51,100),(x0,100)],fill=K)
            d.rectangle((x0+4,52,x0+46,95),fill=a)
            d.line((x0+6,57,x0+43,57),fill=h,width=2); d.line((x0+8,91,x0+42,58),fill=c,width=3)
    elif mode=='curb':
        d.polygon([(4,54),(211,54),(215,65),(215,101),(0,101),(0,65)],fill=K)
        d.polygon([(7,58),(208,58),(211,66),(207,75),(8,75),(4,66)],fill=h)
        d.rectangle((5,76,210,97),fill=a)
        for x in range(8,210,36): d.line((x,77,x,97),fill=b,width=2)
    else: # rooftop duct bank
        for x0,w in [(3,70),(73,68),(141,72)]:
            d.polygon([(x0,51),(x0+8,42),(x0+w-8,42),(x0+w,51),(x0+w,99),(x0,99)],fill=K)
            d.polygon([(x0+4,54),(x0+11,47),(x0+w-11,47),(x0+w-4,54),(x0+w-4,94),(x0+4,94)],fill=a)
            d.line((x0+10,49,x0+w-10,49),fill=h,width=2)
            for y in (62,70,78): d.line((x0+12,y,x0+w-12,y),fill=c,width=2)
    # grounded contact
    d.rectangle((4,98,211,101),fill=K)
    save(im,name)

lowcover('junk','obj_junkyard_low_cover.png','scrap')
lowcover('miss','obj_missile_low_cover.png','ammo')
lowcover('roof','obj_rooftop_low_cover.png','duct')
lowcover('street','obj_street_low_cover.png','curb')

def blade(name,kind,mode):
    a,b,c,h=PAL[kind]; im=Image.new('RGBA',(144,144),T); d=ImageDraw.Draw(im)
    cx=cy=72
    if mode=='saw':
        pts=[]
        import math
        for i in range(32):
            ang=-math.pi/2+i*math.pi/16; rad=62 if i%2==0 else 51
            pts.append((round(cx+math.cos(ang)*rad),round(cy+math.sin(ang)*rad)))
        d.polygon(pts,fill=K); d.ellipse((22,22,122,122),fill=b,outline=c,width=4)
        for i in range(0,360,45):
            ang=math.radians(i); d.line((72,72,round(72+44*math.cos(ang)),round(72+44*math.sin(ang))),fill=h,width=5)
    elif mode=='fan':
        d.ellipse((9,9,135,135),fill=K); d.ellipse((14,14,130,130),fill=a,outline=c,width=4)
        for pts in [[(68,68),(61,18),(78,15),(81,60)],[(76,68),(126,61),(129,78),(84,81)],[(76,76),(83,126),(66,129),(63,84)],[(68,76),(18,83),(15,66),(60,63)]]:
            d.polygon(pts,fill=K); d.line(pts+[pts[0]],fill=h,width=3)
        for x in (30,114): d.ellipse((x-3,69,x+3,75),fill=h)
    else: # refinery valve wheel
        d.ellipse((12,12,132,132),outline=K,width=12); d.ellipse((18,18,126,126),outline=c,width=7)
        import math
        for i in range(8):
            ang=math.radians(i*45); x=round(72+48*math.cos(ang)); y=round(72+48*math.sin(ang))
            d.line((72,72,x,y),fill=K,width=9); d.line((72,72,x,y),fill=h,width=4)
        d.ellipse((56,56,88,88),fill=K); d.ellipse((62,62,82,82),fill=a,outline=h,width=3)
    save(im,name)

blade('obj_street_blade_1.png','street','saw')
blade('obj_rooftop_blade_1.png','roof','fan')
blade('obj_refinery_blade_1.png','ref','valve')

# Floor hazards use the full 2x2 footprint while keeping the axis at (72,72).
for n in ('obj_street_blade_1.png','obj_rooftop_blade_1.png','obj_refinery_blade_1.png'):
    im=Image.open(OUT/n).convert('RGBA'); bbox=im.getchannel('A').getbbox(); crop=im.crop(bbox)
    crop=crop.resize((144,144),Image.Resampling.NEAREST)
    save(crop,n)
