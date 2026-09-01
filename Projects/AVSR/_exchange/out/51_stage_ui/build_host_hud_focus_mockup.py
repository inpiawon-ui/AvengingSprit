from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

SRC=Path(r'C:\Users\inpia\AppData\Local\Temp\codex-clipboard-20d83937-054f-4757-8f03-c97f05e262c0.png')
OUT=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\out\51_stage_ui'); OUT.mkdir(parents=True,exist_ok=True)
base=Image.open(SRC).convert('RGBA'); im=base.copy(); d=ImageDraw.Draw(im)
ARB=Path(r'C:\Windows\Fonts\arialbd.ttf'); MKB=Path(r'C:\Windows\Fonts\malgunbd.ttf'); MK=Path(r'C:\Windows\Fonts\malgun.ttf')
def f(p,s): return ImageFont.truetype(str(p),s)
navy=(5,10,21,255); panel=(8,15,29,255); edge=(38,71,88,255); cyan=(56,219,229,255); purple=(184,70,238,255); gold=(243,179,38,255); white=(229,237,240,255); dim=(111,136,149,255)

# Keep the existing portrait and bottom controls exactly as-is. Rebuild only the HOST information field.
d.rectangle((88,68,294,162),fill=navy)
d.polygon([(94,72),(286,72),(293,79),(293,153),(286,160),(94,160),(88,154),(88,78)],fill=panel,outline=edge)
d.rectangle((95,76,287,78),fill=(80,43,111,255))
d.text((98,82),'CURRENT HOST',font=f(ARB,8),fill=cyan)
d.text((98,94),'AMAZON',font=f(ARB,17),fill=white)
d.text((98,115),'아마존',font=f(MKB,9),fill=dim)

# Level is isolated as a real badge instead of floating beside a generic HOST label.
d.polygon([(245,83),(282,83),(287,88),(287,105),(282,110),(245,110),(240,105),(240,88)],fill=(18,34,47,255),outline=(78,111,123,255))
d.text((263,89),'LV. 1',font=f(ARB,9),fill=gold,anchor='ma')

# One clean HP row with aligned current/max value.
d.text((98,137),'HP',font=f(ARB,9),fill=(246,91,105,255))
d.rectangle((119,139,240,145),fill=(22,37,46,255)); d.rectangle((119,139,200,145),fill=(162,64,223,255)); d.rectangle((119,139,200,140),fill=(224,135,250,255))
d.text((285,135),'107 / 159',font=f(ARB,9),fill=white,anchor='ra')

# Keep the improved stage identity on the right; progress rail remains a temporary clear-only overlay.
d.rectangle((294,67,485,161),fill=navy)
d.polygon([(302,72),(477,72),(484,79),(484,153),(477,160),(302,160),(296,153),(296,79)],fill=panel,outline=edge)
d.rectangle((301,77,479,79),fill=(83,45,116,255)); d.text((307,84),'CHAPTER 1',font=f(ARB,12),fill=cyan); d.text((307,102),'유령 연구소',font=f(MKB,10),fill=dim)
d.text((307,121),'ROOM',font=f(ARB,9),fill=(126,150,160,255)); d.text((346,115),'01',font=f(ARB,24),fill=white); d.text((383,123),'/ 12',font=f(ARB,13),fill=(190,200,207,255))
d.rectangle((307,149,470,153),fill=(19,37,48,255)); d.rectangle((307,149,320,153),fill=cyan)

im.convert('RGB').save(OUT/'AVSR_ingame_host_hud_focus_486x899.png')
im.resize((720,1332),Image.Resampling.NEAREST).convert('RGB').save(OUT/'AVSR_ingame_host_hud_focus.png')

# Enlarged crop for judging only the requested HOST area.
crop=im.crop((0,66,486,164)).resize((972,196),Image.Resampling.NEAREST)
crop.convert('RGB').save(OUT/'AVSR_ingame_host_hud_focus_crop.png')
