from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

SRC=Path(r'C:\Users\inpia\AppData\Local\Temp\codex-clipboard-20d83937-054f-4757-8f03-c97f05e262c0.png')
OUT=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\out\51_stage_ui'); OUT.mkdir(parents=True,exist_ok=True)
im=Image.open(SRC).convert('RGBA'); d=ImageDraw.Draw(im)
ARB=Path(r'C:\Windows\Fonts\arialbd.ttf'); AR=Path(r'C:\Windows\Fonts\arial.ttf'); MKB=Path(r'C:\Windows\Fonts\malgunbd.ttf'); MK=Path(r'C:\Windows\Fonts\malgun.ttf')
def f(p,s): return ImageFont.truetype(str(p),s)

# Palette sampled from the current HUD.
navy=(5,10,21,255); panel=(8,15,29,255); panel2=(10,22,38,255); edge=(38,71,88,255)
cyan=(56,219,229,255); blue=(44,112,183,255); purple=(184,70,238,255); gold=(243,179,38,255); white=(229,237,240,255); dim=(111,136,149,255)

# Top-right: replace floating CH1 / 1/12 text with one stage identity plaque.
d.rectangle((294,67,485,161),fill=navy)
d.polygon([(302,72),(477,72),(484,79),(484,153),(477,160),(302,160),(296,153),(296,79)],fill=panel,outline=edge)
d.rectangle((301,77,479,79),fill=(83,45,116,255))
d.text((307,84),'CHAPTER 1',font=f(ARB,12),fill=cyan)
d.text((307,102),'유령 연구소',font=f(MKB,10),fill=dim)
d.text((307,121),'ROOM',font=f(ARB,9),fill=(126,150,160,255))
d.text((346,115),'01',font=f(ARB,24),fill=white)
d.text((383,123),'/ 12',font=f(ARB,13),fill=(190,200,207,255))
d.rectangle((307,149,470,153),fill=(19,37,48,255)); d.rectangle((307,149,320,153),fill=cyan)

# Replace the empty black lower slab with a stage-navigation console.
d.rectangle((0,718,485,898),fill=(7,11,20,255))
for y,c in [(718,(39,70,85,255)),(720,(17,39,52,255)),(897,(47,28,67,255))]: d.rectangle((0,y,485,y+1),fill=c)
# restrained vertical texture
for x in range(8,486,24): d.line((x,724,x,895),fill=(10,19,31,255),width=1)

# Stage rail above the controls.
d.polygon([(102,728),(330,728),(338,736),(338,784),(330,792),(102,792),(94,784),(94,736)],fill=panel2,outline=edge)
d.text((108,735),'CHAPTER 1 · 유령 연구소',font=f(MKB,9),fill=(173,198,205,255))
d.text((328,735),'01/12',font=f(ARB,9),fill=white,anchor='ra')
y=766; x0=108; x1=324
d.line((x0,y,x1,y),fill=(41,65,76,255),width=3)
for i in range(12):
    x=round(x0+(x1-x0)*i/11)
    if i==0:
        d.ellipse((x-7,y-7,x+7,y+7),fill=(15,51,63,255),outline=cyan,width=2); d.ellipse((x-2,y-2,x+2,y+2),fill=white)
    elif i in (3,7):
        d.polygon([(x,y-5),(x+5,y),(x,y+5),(x-5,y)],fill=(92,58,24,255),outline=gold)
    elif i==11:
        d.ellipse((x-6,y-6,x+6,y+6),fill=(59,23,66,255),outline=purple,width=2); d.text((x,y-5),'X',font=f(ARB,7),fill=white,anchor='ma')
    else:
        d.ellipse((x-3,y-3,x+3,y+3),fill=(31,49,60,255),outline=(74,94,103,255))

# Objective occupies the former dead center without becoming another large button.
d.polygon([(111,802),(333,802),(340,809),(333,831),(111,831),(104,824),(104,809)],fill=(9,20,34,255),outline=(34,67,84,255))
d.text((119,808),'현재 목표',font=f(MKB,9),fill=cyan)
d.text((214,806),'적 전멸',font=f(MKB,13),fill=white)
d.text((329,810),'0 / 4',font=f(ARB,10),fill=gold,anchor='ra')

# Re-paste the original controls so the new console remains directly usable.
src=Image.open(SRC).convert('RGBA')
im.alpha_composite(src.crop((0,770,101,872)),(0,770))
im.alpha_composite(src.crop((339,777,486,875)),(339,777))
d=ImageDraw.Draw(im)
d.text((49,870),'MOVE',font=f(ARB,8),fill=(75,104,118,255),anchor='ma')
d.text((410,873),'SKILLS',font=f(ARB,8),fill=(75,104,118,255),anchor='ma')

# Exact-size concept plus enlarged review image.
im.convert('RGB').save(OUT/'AVSR_ingame_stage_hud_mockup_486x899.png')
preview=im.resize((720,1332),Image.Resampling.NEAREST)
preview.convert('RGB').save(OUT/'AVSR_ingame_stage_hud_mockup.png')
