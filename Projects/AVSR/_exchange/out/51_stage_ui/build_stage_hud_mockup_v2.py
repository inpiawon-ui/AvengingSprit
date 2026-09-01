from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

SRC=Path(r'C:\Users\inpia\AppData\Local\Temp\codex-clipboard-20d83937-054f-4757-8f03-c97f05e262c0.png')
OUT=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\out\51_stage_ui'); OUT.mkdir(parents=True,exist_ok=True)
base=Image.open(SRC).convert('RGBA'); im=base.copy(); d=ImageDraw.Draw(im)
ARB=Path(r'C:\Windows\Fonts\arialbd.ttf'); AR=Path(r'C:\Windows\Fonts\arial.ttf'); MKB=Path(r'C:\Windows\Fonts\malgunbd.ttf'); MK=Path(r'C:\Windows\Fonts\malgun.ttf')
def f(p,s): return ImageFont.truetype(str(p),s)
navy=(5,10,21,255); panel=(8,15,29,255); panel2=(10,22,38,255); edge=(38,71,88,255)
cyan=(56,219,229,255); purple=(184,70,238,255); gold=(243,179,38,255); white=(229,237,240,255); dim=(111,136,149,255)

# Persistent compact chapter/room plate.
d.rectangle((294,67,485,161),fill=navy)
d.polygon([(302,72),(477,72),(484,79),(484,153),(477,160),(302,160),(296,153),(296,79)],fill=panel,outline=edge)
d.rectangle((301,77,479,79),fill=(83,45,116,255))
d.text((307,84),'CHAPTER 1',font=f(ARB,12),fill=cyan); d.text((307,102),'유령 연구소',font=f(MKB,10),fill=dim)
d.text((307,121),'ROOM',font=f(ARB,9),fill=(126,150,160,255)); d.text((346,115),'01',font=f(ARB,24),fill=white); d.text((383,123),'/ 12',font=f(ARB,13),fill=(190,200,207,255))
d.rectangle((307,149,470,153),fill=(19,37,48,255)); d.rectangle((307,149,320,153),fill=cyan)

# Temporary clear overlay: appears above the arena for a short time only.
d.polygon([(74,174),(412,174),(424,186),(418,222),(406,230),(80,230),(68,222),(62,186)],fill=(7,15,28,238),outline=(52,91,106,255))
d.text((243,181),'ROOM CLEAR',font=f(ARB,12),fill=white,anchor='ma')
d.text((81,185),'01',font=f(ARB,9),fill=cyan); d.text((405,185),'NEXT 02',font=f(ARB,9),fill=(173,190,198,255),anchor='ra')
y=211; x0=86; x1=400; d.line((x0,y,x1,y),fill=(45,70,81,255),width=3)
for i in range(12):
    x=round(x0+(x1-x0)*i/11)
    if i==0:
        d.ellipse((x-7,y-7,x+7,y+7),fill=(14,48,61,255),outline=cyan,width=2); d.ellipse((x-2,y-2,x+2,y+2),fill=white)
    elif i in (3,7): d.polygon([(x,y-5),(x+5,y),(x,y+5),(x-5,y)],fill=(92,58,24,255),outline=gold)
    elif i==11:
        d.ellipse((x-6,y-6,x+6,y+6),fill=(59,23,66,255),outline=purple,width=2); d.text((x,y-5),'X',font=f(ARB,7),fill=white,anchor='ma')
    else: d.ellipse((x-3,y-3,x+3,y+3),fill=(31,49,60,255),outline=(74,94,103,255))

# Lower controller deck: no objective, no persistent route.
d.rectangle((0,718,485,898),fill=(7,11,20,255))
d.rectangle((0,718,485,719),fill=(39,70,85,255)); d.rectangle((0,720,485,721),fill=(17,39,52,255))
for x in range(8,486,24): d.line((x,724,x,895),fill=(10,19,31,255),width=1)

# Current possessed HOST identity fills the center dead space.
d.polygon([(111,749),(329,749),(340,760),(340,837),(329,848),(111,848),(100,837),(100,760)],fill=panel2,outline=edge)
d.rectangle((106,755,334,757),fill=(83,45,116,255))
portrait=base.crop((4,74,85,155)).resize((66,66),Image.Resampling.NEAREST)
im.alpha_composite(portrait,(109,768)); d=ImageDraw.Draw(im)
d.text((184,768),'CURRENT HOST',font=f(ARB,8),fill=cyan)
d.text((184,782),'AMAZON',font=f(ARB,16),fill=white)
d.text((184,803),'아마존',font=f(MKB,10),fill=(157,176,185,255))
d.rectangle((184,824,258,837),fill=(16,34,46,255),outline=(43,76,89,255))
d.text((190,826),'LV. 1',font=f(ARB,9),fill=gold)
d.text((326,826),'HOST',font=f(ARB,7),fill=(82,111,123,255),anchor='ra')

# Original controls remain at the edges.
im.alpha_composite(base.crop((0,770,101,872)),(0,770)); im.alpha_composite(base.crop((339,777,486,875)),(339,777)); d=ImageDraw.Draw(im)
d.text((49,870),'MOVE',font=f(ARB,8),fill=(75,104,118,255),anchor='ma'); d.text((410,873),'SKILLS',font=f(ARB,8),fill=(75,104,118,255),anchor='ma')

im.convert('RGB').save(OUT/'AVSR_ingame_stage_hud_mockup_v2_486x899.png')
im.resize((720,1332),Image.Resampling.NEAREST).convert('RGB').save(OUT/'AVSR_ingame_stage_hud_mockup_v2.png')
