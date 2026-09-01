from pathlib import Path
from PIL import Image, ImageDraw

OUT=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in'); OUT.mkdir(parents=True,exist_ok=True)
PRE=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\out\48_icons\card_icons_preview.png')
T=(0,0,0,0); O=(5,8,18,255); D=(17,25,42,255); S=(43,66,82,255); W=(209,231,224,255)

def new():
    im=Image.new('RGBA',(24,24),T); return im,ImageDraw.Draw(im)
def poly(d,c,p): d.polygon(p,fill=c)
def rect(d,c,b): d.rectangle(b,fill=c)
# compact 3x5 bitmap letters, drawn at 2x scale
FONT={'S':['111','100','111','001','111'],'A':['010','101','111','101','101'],'B':['110','101','110','101','110']}
def letter(d,ch,c,x=9,y=7):
    for yy,row in enumerate(FONT[ch]):
        for xx,v in enumerate(row):
            if v=='1': rect(d,c,(x+xx*2,y+yy*2,x+xx*2+1,y+yy*2+1))

im,d=new()
# Level: compact engraved plate with double ascension chevron.
poly(d,O,[(3,4),(20,4),(23,8),(21,19),(18,22),(5,22),(2,19),(0,8)])
poly(d,(76,119,132,255),[(4,6),(19,6),(21,9),(19,18),(17,20),(6,20),(4,18),(2,9)])
poly(d,(24,38,58,255),[(5,8),(18,8),(19,10),(18,17),(16,18),(7,18),(5,16)])
poly(d,(211,93,255,255),[(6,14),(11,9),(16,14),(14,16),(11,13),(8,16)])
poly(d,(246,183,50,255),[(8,10),(11,7),(14,10),(13,11),(11,9),(9,11)])
rect(d,W,(7,18,15,19)); im.save(OUT/'icon_lv.png')

# S: crowned gold medal — widest, most prestigious silhouette.
im,d=new(); poly(d,O,[(3,6),(6,6),(8,2),(12,6),(16,2),(18,6),(21,6),(20,19),(16,23),(7,23),(3,19)])
poly(d,(246,183,50,255),[(5,7),(7,7),(8,5),(12,8),(16,5),(17,7),(19,7),(18,18),(15,21),(8,21),(5,18)])
poly(d,(111,63,18,255),[(6,9),(18,9),(17,18),(14,20),(9,20),(6,17)])
rect(d,(255,228,121,255),(7,9,16,10)); letter(d,'S',(255,228,121,255)); rect(d,D,(8,20,15,21)); im.save(OUT/'icon_grade_s.png')

# A: tall amethyst crest.
im,d=new(); poly(d,O,[(12,0),(22,7),(20,18),(12,24),(4,18),(2,7)])
poly(d,(124,61,196,255),[(12,2),(20,8),(18,17),(12,22),(6,17),(4,8)])
poly(d,(48,30,75,255),[(12,5),(18,9),(16,17),(12,20),(8,17),(6,9)])
poly(d,(229,173,255,255),[(8,7),(12,4),(16,7),(15,9),(12,7),(9,9)]); letter(d,'A',(229,173,255,255),9,10)
rect(d,W,(11,4,12,5)); im.save(OUT/'icon_grade_a.png')

# B: sturdy cyan shield.
im,d=new(); poly(d,O,[(3,3),(20,3),(23,6),(21,17),(16,22),(12,24),(7,22),(2,17),(0,6)])
poly(d,(55,208,222,255),[(4,5),(19,5),(21,7),(19,16),(15,20),(12,22),(8,20),(4,16),(2,7)])
poly(d,(15,61,73,255),[(6,7),(17,7),(19,9),(17,16),(14,19),(10,19),(6,16),(4,9)])
rect(d,(174,239,240,255),(6,7,17,8)); letter(d,'B',(174,239,240,255),9,10); rect(d,D,(10,20,13,21)); im.save(OUT/'icon_grade_b.png')

im,d=new()
# Shard: asymmetrical faceted possession crystal with two orbiting chips.
poly(d,O,[(12,0),(22,7),(19,17),(13,24),(5,20),(1,11),(6,4)])
poly(d,(124,61,196,255),[(12,2),(20,8),(17,16),(12,22),(7,19),(3,11),(7,6)])
poly(d,(211,93,255,255),[(12,3),(18,9),(14,17),(8,18),(5,11)])
poly(d,W,[(11,4),(15,8),(12,13),(8,10)]); poly(d,(55,208,222,255),[(14,17),(17,15),(16,19),(12,21)])
rect(d,(211,93,255,255),(21,3,22,5)); rect(d,(55,208,222,255),(1,19,2,20)); im.save(OUT/'icon_shard.png')

im,d=new()
# Ghost HP: cracked spectral heart/core dissolving toward the upper right.
poly(d,O,[(3,8),(5,4),(9,3),(12,6),(15,3),(19,5),(20,10),(17,15),(12,21),(7,16),(3,12)])
poly(d,(55,208,222,255),[(5,8),(6,6),(9,5),(12,8),(15,5),(18,7),(18,10),(15,14),(12,18),(9,15),(5,11)])
poly(d,W,[(6,7),(9,6),(12,9),(10,12),(7,10)]); poly(d,D,[(12,8),(15,10),(13,12),(16,14),(12,18),(11,13)])
rect(d,(124,61,196,255),(19,2,21,4)); rect(d,(124,61,196,255),(21,7,22,8)); rect(d,(124,61,196,255),(19,11,20,12)); im.save(OUT/'icon_ghosthp.png')

# 6-up inspection sheet, nearest-neighbor at 6x.
names=['icon_lv.png','icon_grade_s.png','icon_grade_a.png','icon_grade_b.png','icon_shard.png','icon_ghosthp.png']
sheet=Image.new('RGBA',(6*156,204),(8,12,22,255))
for i,n in enumerate(names):
    im=Image.open(OUT/n).resize((144,144),Image.Resampling.NEAREST); sheet.alpha_composite(im,(i*156+6,6))
sheet.convert('RGB').save(PRE)
