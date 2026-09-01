from pathlib import Path
from PIL import Image, ImageDraw
import math, random

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
OUT=ROOT/'Projects/AVSR/_exchange/in'; OUT.mkdir(parents=True,exist_ok=True)
T=(0,0,0,0); K=(2,5,12,255); W=(239,242,225,255); G=(143,151,146,255)
OR=(244,91,20,255); Y=(255,201,38,255); R=(177,34,28,255)
CY=(91,205,224,255); BL=(38,109,189,255); ICE=(188,236,244,255)
PU=(134,54,180,255); PK=(229,74,176,255); GR=(64,179,87,255)

def frame(size): return Image.new('RGBA',size,T)
def save(im,name):
    im.putalpha(im.getchannel('A').point(lambda a:255 if a>=128 else 0)); im.save(OUT/name)
def star(d,cx,cy,r1,r2,n,fill):
    pts=[]
    for i in range(n*2):
        a=-math.pi/2+i*math.pi/n; r=r1 if i%2==0 else r2
        pts.append((round(cx+math.cos(a)*r),round(cy+math.sin(a)*r)))
    d.polygon(pts,fill=fill)
def ring(d,box,color,width=2): d.ellipse(box,outline=color,width=width)

# Priority shared effects -------------------------------------------------
for i in range(4):
    im=frame((96,96)); d=ImageDraw.Draw(im); rad=(12,25,39,53)[i]
    star(d,48,48,rad,max(5,rad//2),10,K); star(d,48,48,max(5,rad-3),max(3,rad//2-2),10,(Y,OR,R,G)[i])
    for a in range(0,360,45):
        rr=rad+8; x=round(48+math.cos(math.radians(a+i*11))*rr); y=round(48+math.sin(math.radians(a+i*11))*rr)
        d.rectangle((x-2,y-2,x+2,y+2),fill=(W,Y,OR,G)[i])
    save(im,f'fx_burst_{i+1}.png')

for i,(ln,spread) in enumerate([(20,8),(32,14)],1):
    im=frame((48,48)); d=ImageDraw.Draw(im)
    d.polygon([(6,24),(6+ln,24-spread),(13+ln,22),(20+ln,24),(13+ln,26),(6+ln,24+spread)],fill=K)
    d.polygon([(8,24),(5+ln,24-spread+3),(12+ln,23),(17+ln,24),(12+ln,25),(5+ln,24+spread-3)],fill=OR)
    d.polygon([(10,24),(3+ln,22),(13+ln,24),(3+ln,26)],fill=Y)
    save(im,f'fx_muzzle_{i}.png')

for i in range(2):
    im=frame((48,48)); d=ImageDraw.Draw(im); ang=i*45
    ring(d,(13,2,35,24),K,4); ring(d,(14,3,34,23),Y,2)
    for a in range(ang,360+ang,90):
        x=round(24+math.cos(math.radians(a))*15); y=round(13+math.sin(math.radians(a))*15)
        d.rectangle((x-2,y-2,x+2,y+2),fill=OR)
    d.polygon([(24,7),(28,13),(24,19),(20,13)],fill=R)
    save(im,f'fx_mark_{i+1}.png')

for i in range(3):
    im=frame((64,64)); d=ImageDraw.Draw(im); start=195+i*25
    d.arc((8,8,56,56),start,start+125,fill=K,width=7); d.arc((9,9,55,55),start,start+125,fill=(W,CY,Y)[i],width=4)
    for j in range(4):
        a=math.radians(start+15+j*30); x=round(32+30*math.cos(a)); y=round(32+30*math.sin(a)); d.rectangle((x-2,y-2,x+2,y+2),fill=(CY,W,Y)[i])
    save(im,f'fx_reflect_{i+1}.png')

# State effects ----------------------------------------------------------
flames=[[(20,42),(15,29),(23,34),(27,19),(32,35),(37,27),(35,43)],[(17,43),(20,26),(25,34),(29,16),(33,31),(39,24),(36,43)],[(19,43),(14,31),(22,35),(26,20),(31,33),(36,29),(34,43)]]
for i,pts in enumerate(flames,1):
    im=frame((48,48)); d=ImageDraw.Draw(im); d.polygon(pts,fill=K); d.polygon([(x,y+2 if y<40 else y) for x,y in pts],fill=OR); d.polygon([(24,40),(25,31),(29,23),(31,40)],fill=Y); save(im,f'fx_burn_{i}.png')

for i in range(3):
    im=frame((144,144)); d=ImageDraw.Draw(im); r=(42,57,66)[i]
    ring(d,(72-r,72-r,72+r,72+r),K,7); ring(d,(74-r,74-r,70+r,70+r),ICE,4)
    for a in range(0,360,45):
        aa=math.radians(a+i*8); x=round(72+math.cos(aa)*r); y=round(72+math.sin(aa)*r)
        d.polygon([(x,y),(x+round(math.cos(aa)*14),y+round(math.sin(aa)*14)),(x+round(math.cos(aa+.5)*7),y+round(math.sin(aa+.5)*7))],fill=BL)
    if i>0: d.rectangle((59,54,85,111),outline=CY,width=3)
    save(im,f'fx_freeze_{i+1}.png')

for i in range(3):
    im=frame((64,64)); d=ImageDraw.Draw(im); rr=(27,20,13)[i]
    for a in range(i*20,360+i*20,60):
        aa=math.radians(a); x=round(32+math.cos(aa)*rr); y=round(32+math.sin(aa)*rr)
        d.polygon([(x-3,y),(x+2,y-5),(x+5,y),(x+1,y+6)],fill=(R,PK,PU)[i])
        d.line((x,y,32,32),fill=PU,width=2)
    d.ellipse((27,27,37,37),fill=R)
    save(im,f'fx_drain_{i+1}.png')

for i in range(3):
    im=frame((96,96)); d=ImageDraw.Draw(im); random.seed(560+i); count=(7,11,8)[i]
    for j in range(count):
        x=48+random.randint(-30,30); y=57+random.randint(-20,16); r=random.randint(7,16)+(i*3)
        d.ellipse((x-r,y-r,x+r,y+r),fill=K); d.ellipse((x-r+3,y-r+3,x+r-3,y+r-3),fill=(G,(94,102,104,255),(55,63,67,255))[i])
    save(im,f'fx_smoke_{i+1}.png')

for i in range(2):
    im=frame((24,24)); d=ImageDraw.Draw(im)
    d.polygon([(2,12),(8,7+i*2),(18,10),(22,12),(18,14),(8,17-i*2)],fill=K)
    d.polygon([(4,12),(10,9+i),(19,12),(10,15-i)],fill=(OR,Y)[i])
    save(im,f'fx_missile_trail_{i+1}.png')

# Character-specific effects --------------------------------------------
for i in range(5):
    im=frame((216,216)); d=ImageDraw.Draw(im); start=205+i*23
    d.arc((18,18,198,198),start,start+64,fill=K,width=15); d.arc((20,20,196,196),start,start+64,fill=(PU,PK,W,G,G)[i],width=9)
    a=math.radians(start+64); x=round(108+92*math.cos(a)); y=round(108+92*math.sin(a)); d.polygon([(x,y),(x+18,y-7),(x+9,y+13)],fill=(W,PK,PU,G,G)[i])
    save(im,f'fx_scythe_{i+1}.png')

for i in range(3):
    im=frame((216,216)); d=ImageDraw.Draw(im)
    for r,col in [(92,PU),(70,CY),(48,W)]: d.arc((108-r,108-r,108+r,108+r),i*20,300+i*20,fill=K,width=7); d.arc((108-r,108-r,108+r,108+r),i*20,300+i*20,fill=col,width=3)
    for a in range(i*30,360+i*30,60):
        aa=math.radians(a); x=round(108+82*math.cos(aa)); y=round(108+82*math.sin(aa)); d.polygon([(x,y-8),(x+7,y),(x,y+8),(x-7,y)],fill=CY)
    save(im,f'fx_ward_{i+1}.png')

for i in range(3):
    im=frame((72,24)); d=ImageDraw.Draw(im); y=12+(i-1)
    d.line((0,y,71,y),fill=K,width=7); d.line((0,y,71,y),fill=(G,CY,W)[i],width=3)
    for x in range(-8+i*4,80,16): d.ellipse((x,y-6,x+12,y+6),outline=(W,CY,G)[i],width=2)
    save(im,f'fx_chain_{i+1}.png')

def breath_mask(idx):
    im=frame((360,360)); d=ImageDraw.Draw(im); random.seed(900+idx)
    origin=(28,180); length=(160,250,330,280,190)[idx]; spread=(30,55,90,70,38)[idx]
    d.polygon([origin,(origin[0]+length,180-spread),(origin[0]+length+8,180),(origin[0]+length,180+spread)],fill=(255,255,255,255))
    for _ in range(20+idx*5):
        x=random.randint(45,min(350,origin[0]+length)); frac=(x-origin[0])/max(1,length); sy=max(4,int(spread*frac)); y=180+random.randint(-sy,sy); r=random.randint(3,9)
        d.rectangle((x-r,y-r,x+r,y+r),fill=(255,255,255,255))
    return im
def color_mask(mask,outer,mid,inner):
    im=frame(mask.size); d=ImageDraw.Draw(im); a=mask.getchannel('A')
    bbox=a.getbbox(); d.bitmap((0,0),a,fill=outer)
    if bbox:
        x0,y0,x1,y1=bbox; d.polygon([(x0,180),(x1-10,y0+12),(x1-4,180),(x1-10,y1-12)],fill=mid)
        d.polygon([(x0+8,180),(x1-35,160),(x1-12,180),(x1-35,200)],fill=inner)
    im.putalpha(a); return im
for i in range(5):
    m=breath_mask(i); save(color_mask(m,R,OR,Y),f'fx_breath_fire_{i+1}.png'); save(color_mask(m,BL,CY,ICE),f'fx_breath_ice_{i+1}.png')

for i in range(4):
    im=frame((144,144)); d=ImageDraw.Draw(im); r=(40,57,64,48)[i]
    d.ellipse((72-r,72-r,72+r,72+r),fill=K); d.ellipse((76-r,76-r,68+r,68+r),fill=(R,OR,Y,OR)[i])
    for a in range(i*17,360+i*17,60):
        aa=math.radians(a); x=round(72+math.cos(aa)*(r-8)); y=round(72+math.sin(aa)*(r-8)); d.rectangle((x-5,y-3,x+5,y+3),fill=Y)
    save(im,f'fx_lava_{i+1}.png')

for i in range(4):
    im=frame((144,144)); d=ImageDraw.Draw(im); rad=(16,37,63,71)[i]
    star(d,72,72,rad,max(6,rad//2),12,K); star(d,72,72,max(5,rad-4),max(4,rad//2-3),12,(Y,OR,R,G)[i])
    save(im,f'fx_grenade_burst_{i+1}.png')

for i in range(4):
    im=frame((216,216)); d=ImageDraw.Draw(im); random.seed(700+i)
    count=(6,12,18,10)[i]; dist=(18,48,82,100)[i]
    for j in range(count):
        a=random.random()*math.tau; rr=random.randint(max(4,dist-20),dist); x=round(108+math.cos(a)*rr); y=round(108+math.sin(a)*rr); s=random.randint(5,15)
        d.polygon([(x,y-s),(x+s//2,y),(x,y+s),(x-s//2,y+2)],fill=K); d.polygon([(x,y-s+3),(x+s//2-2,y),(x,y+s-3)],fill=(ICE,CY,BL,G)[i])
    save(im,f'fx_shatter_{i+1}.png')

for i in range(4):
    im=frame((216,216)); d=ImageDraw.Draw(im); r=(28,55,83,104)[i]
    d.arc((108-r,108-r,108+r,108+r),190,350,fill=K,width=13); d.arc((108-r,108-r,108+r,108+r),190,350,fill=(W,Y,OR,G)[i],width=7)
    for x in range(108-r,109+r,max(12,r//4)): d.polygon([(x,108),(x+5,95-i*4),(x+10,108)],fill=(G,W,Y,G)[i])
    save(im,f'fx_slam_{i+1}.png')

for i in range(3):
    im=frame((72,24)); d=ImageDraw.Draw(im); y=12
    d.rectangle((0,y-5,71,y+5),fill=K); d.rectangle((0,y-2,71,y+2),fill=(PU,PK,W)[i])
    for x in range(i*8-16,80,24):
        d.polygon([(x,y),(x+8,y-7),(x+16,y),(x+8,y+7)],outline=(PK,PU,W)[i])
    save(im,f'fx_curse_beam_{i+1}.png')

for i in range(4):
    im=frame((144,576)); d=ImageDraw.Draw(im); widths=(18,42,64,30); w=widths[i]
    d.rectangle((72-w,0,72+w,575),fill=K); d.rectangle((76-w,0,68+w,575),fill=(Y,W,ICE,G)[i])
    d.rectangle((68,0,76,575),fill=(W,Y,W,G)[i])
    for y in range(i*37,576,72): d.rectangle((72-w-8,y,72+w+8,y+5),fill=(W,Y,CY,G)[i])
    save(im,f'fx_holy_beam_{i+1}.png')

for i in range(3):
    im=frame((216,576)); d=ImageDraw.Draw(im); w=(34,72,103)[i]
    d.rectangle((108-w,0,108+w,575),fill=K); d.rectangle((113-w,0,103+w,575),fill=(BL,CY,W)[i])
    for x in range(108-w+12,108+w,24): d.line((x,0,x,575),fill=(CY,W,CY)[i],width=4)
    save(im,f'fx_laser_wide_{i+1}.png')

for i in range(3):
    im=frame((96,96)); d=ImageDraw.Draw(im)
    d.polygon([(25,45),(71,45),(80,55),(76,85),(20,85),(16,55)],fill=K)
    d.polygon([(29,49),(67,49),(74,57),(71,79),(25,79),(22,57)],fill=(28,53,70,255))
    d.ellipse((38,57,58,77),fill=BL,outline=CY,width=3)
    ang=(-20,0,20)[i]; ex=round(48+35*math.cos(math.radians(ang))); ey=round(48+35*math.sin(math.radians(ang)))
    d.line((48,50,ex,ey),fill=K,width=9); d.line((48,50,ex,ey),fill=G,width=5)
    if i==2: d.polygon([(ex,ey),(ex+10,ey-5),(ex+15,ey),(ex+10,ey+5)],fill=Y)
    save(im,f'fx_turret_{i+1}.png')

# Force exact horizontal edge equality for tiled one-cell effects.
for prefix,count in [('fx_chain',3),('fx_curse_beam',3)]:
    for i in range(1,count+1):
        p=OUT/f'{prefix}_{i}.png'; im=Image.open(p).convert('RGBA')
        for y in range(im.height): im.putpixel((im.width-1,y),im.getpixel((0,y)))
        save(im,p.name)
