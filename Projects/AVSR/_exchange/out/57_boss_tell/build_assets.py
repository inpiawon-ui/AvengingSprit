from pathlib import Path
from PIL import Image, ImageDraw

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
OUT=ROOT/'Projects/AVSR/_exchange/in'; OUT.mkdir(parents=True,exist_ok=True)
T=(0,0,0,0)

def hatch(name,color,width,reverse=False):
    im=Image.new('RGBA',(64,64),T); p=im.load()
    for y in range(64):
        for x in range(64):
            phase=((x+y) if reverse else (x-y))%16
            if phase<width: p[x,y]=color
    im.save(OUT/name)

# Opposite slopes; periods divide 64 exactly, so all four tile boundaries loop.
hatch('fx_danger_hatch.png',(205,45,37,255),6,False)
hatch('fx_safe_hatch.png',(50,155,78,255),4,True)

# Thick right-facing dodge arrow, centered on (24,24). Its tail differentiates
# it from the small triangular blue possess marker.
im=Image.new('RGBA',(48,48),T); d=ImageDraw.Draw(im)
outline=(45,35,9,255); yellow=(255,218,63,255); white=(255,250,205,255)
outer=[(3,18),(22,18),(22,10),(45,24),(22,38),(22,30),(3,30)]
inner=[(6,21),(26,21),(26,16),(40,24),(26,32),(26,27),(6,27)]
d.polygon(outer,fill=outline)
d.polygon(inner,fill=yellow)
d.line((7,22,29,22),fill=white,width=2)
d.rectangle((7,23,23,24),fill=white)
im.save(OUT/'ui_dodge_arrow.png')
