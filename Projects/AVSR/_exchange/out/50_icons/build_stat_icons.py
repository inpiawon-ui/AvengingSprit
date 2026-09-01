from pathlib import Path
from PIL import Image, ImageDraw

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
OUT=ROOT/'Projects'/'AVSR'/'_exchange'/'in'; OUT.mkdir(parents=True,exist_ok=True)
PRE=ROOT/'Projects'/'AVSR'/'_exchange'/'out'/'50_icons'/'stat_icons_set_preview.png'; PRE.parent.mkdir(parents=True,exist_ok=True)
T=(0,0,0,0); NAV=(0,1,44,255); DEEP=(1,16,51,255); WHITE=(247,247,249,255)
RED=(234,0,1,255); HOT=(252,12,18,255); ORANGE=(195,96,10,255); GOLD=(246,194,70,255); PALE=(255,226,116,255)
TEAL0=(0,0,77,255); TEAL1=(4,88,208,255); TEAL2=(10,205,253,255); TEAL3=(41,243,254,255); TEAL4=(143,248,252,255)

def icon():
    im=Image.new('RGBA',(28,28),T); return im,ImageDraw.Draw(im)
def rect(d,c,b): d.rectangle(b,fill=c)
def poly(d,c,p): d.polygon(p,fill=c)
def line(d,c,p,w=1): d.line(p,fill=c,width=w)

# CRIT — shattered bullseye hit. No blade silhouette.
im,d=icon()
# dark 1 px silhouette
poly(d,NAV,[(12,2),(16,2),(17,6),(22,4),(24,7),(21,11),(26,13),(25,17),(20,17),(21,23),(17,25),(14,21),(10,26),(7,24),(8,19),(3,20),(1,16),(5,13),(2,9),(5,6),(10,7)])
poly(d,RED,[(12,4),(16,4),(16,8),(21,6),(22,8),(19,12),(24,14),(23,16),(18,15),(19,21),(17,23),(14,19),(10,24),(9,22),(10,17),(5,18),(3,16),(7,13),(4,9),(6,8),(11,10)])
poly(d,HOT,[(11,8),(17,8),(20,12),(19,17),(15,20),(10,18),(7,14)])
poly(d,ORANGE,[(13,10),(17,11),(18,14),(16,17),(12,17),(9,14),(10,11)])
rect(d,GOLD,(12,11,15,15)); rect(d,PALE,(13,11,14,12));
# fracture lines punch through the target
line(d,NAV,[(14,3),(14,9)],1); line(d,NAV,[(14,16),(18,22)],1); line(d,NAV,[(9,14),(3,14)],1); line(d,NAV,[(17,12),(22,8)],1)
im.save(OUT/'staticon_crit.png')

# RNG — long-range reticle with a beam that reaches a distant target.
im,d=icon()
poly(d,TEAL0,[(4,3),(12,3),(12,6),(18,6),(18,3),(24,3),(24,9),(21,9),(21,13),(27,13),(27,17),(21,17),(21,21),(24,21),(24,25),(18,25),(18,22),(11,22),(11,25),(4,25),(4,20),(7,20),(7,16),(1,16),(1,12),(7,12),(7,8),(4,8)])
# open crosshair arcs
rect(d,TEAL2,(5,5,10,7)); rect(d,TEAL3,(5,6,6,11)); rect(d,TEAL1,(5,19,10,21)); rect(d,TEAL2,(5,16,6,20))
rect(d,TEAL2,(19,5,22,7)); rect(d,TEAL3,(21,6,22,11)); rect(d,TEAL1,(19,19,22,21)); rect(d,TEAL2,(21,16,22,20))
# extending sight line and far point
rect(d,TEAL4,(8,13,20,15)); rect(d,WHITE,(10,13,16,14)); poly(d,TEAL3,[(18,10),(24,14),(18,18)]); rect(d,WHITE,(22,13,24,15))
im.save(OUT/'staticon_rng.png')

# RATE — three staggered fire pulses / chevrons. Distinct from SPD's wing.
im,d=icon()
poly(d,NAV,[(2,4),(9,4),(15,10),(15,5),(19,5),(26,12),(26,16),(19,23),(15,23),(15,18),(9,24),(2,24),(12,14)])
poly(d,ORANGE,[(3,6),(8,6),(16,14),(8,22),(3,22),(11,14)])
poly(d,GOLD,[(10,6),(14,6),(22,14),(14,22),(10,22),(18,14)])
poly(d,(244,145,25,255),[(17,7),(20,7),(26,13),(26,15),(20,21),(17,21),(24,14)])
poly(d,PALE,[(5,8),(8,8),(14,14),(8,20),(5,20),(11,14)])
rect(d,WHITE,(4,10,5,12)); rect(d,HOT,(22,12,25,15))
im.save(OUT/'staticon_rate.png')

# Existing four + new three, enlarged only for inspection.
names=['staticon_hp.png','staticon_atk.png','staticon_spd.png','staticon_dash.png','staticon_crit.png','staticon_rng.png','staticon_rate.png']
sheet=Image.new('RGBA',(7*120,170),(7,11,21,255))
for i,n in enumerate(names):
    p=(OUT/n) if (OUT/n).exists() else (ROOT/'Assets'/'BaseResource'/'HostSelectPanel'/n)
    src=Image.open(p).convert('RGBA'); src.thumbnail((28,28),Image.Resampling.NEAREST)
    tile=Image.new('RGBA',(28,28),T); tile.alpha_composite(src,((28-src.width)//2,(28-src.height)//2))
    sheet.alpha_composite(tile.resize((112,112),Image.Resampling.NEAREST),(i*120+4,4))
sheet.convert('RGB').save(PRE)
