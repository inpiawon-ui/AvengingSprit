from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\out\51_stage_ui')
SRC = ROOT / 'AVSR_ingame_host_hud_focus_486x899.png'
im = Image.open(SRC).convert('RGBA')
d = ImageDraw.Draw(im)

ARB = Path(r'C:\Windows\Fonts\arialbd.ttf')
MKB = Path(r'C:\Windows\Fonts\malgunbd.ttf')
def font(size): return ImageFont.truetype(str(ARB), size)
def kfont(size): return ImageFont.truetype(str(MKB), size)

navy=(4,9,19,255); panel=(7,15,28,255); panel2=(10,22,37,255)
edge=(35,67,84,255); cyan=(53,220,231,255); white=(232,239,242,255)
dim=(113,139,151,255); red=(247,79,85,255); orange=(255,144,43,255)
gold=(246,181,35,255); violet=(193,71,239,255)

# Preserve the original ghost sprite, then rebuild only the top information strip.
ghost = im.crop((0, 0, 57, 68))
d.rectangle((0,0,485,67), fill=navy)
d.polygon([(5,4),(250,4),(257,11),(257,60),(250,66),(5,66),(0,61),(0,9)],
          fill=panel, outline=edge)

# Ghost thumbnail uses the same framed portrait language as CURRENT HOST.
d.polygon([(5,6),(49,6),(54,11),(54,58),(49,63),(5,63),(1,59),(1,10)],
          fill=(5,10,18,255),outline=(91,112,124,255))
d.rectangle((5,10,50,59),outline=(30,54,67,255))
ghost_luma=ghost.convert('RGB').convert('L')
ghost_mask=ghost_luma.point(lambda p: 255 if p>22 else 0)
im.paste(ghost,(0,0),ghost_mask)

# Ghost identity follows the same hierarchy as CURRENT HOST below.
d.text((58,8),'PLAYER SOUL',font=font(7),fill=cyan)
d.text((58,18),'GHOST',font=font(15),fill=white)
d.polygon([(191,9),(238,9),(244,15),(244,30),(238,36),(191,36),(185,30),(185,15)],
          fill=panel2,outline=(69,105,118,255))
d.text((214,18),'LV. 1',font=font(9),fill=gold,anchor='ma')

# One meaningful gauge only: ghost HP.
d.text((58,43),'HP',font=font(8),fill=red)
d.rectangle((78,44,196,51),fill=(20,35,44,255))
d.rectangle((78,44,176,51),fill=red)
d.rectangle((78,44,176,45),fill=(255,153,102,255))
d.text((247,40),'83 / 100',font=font(9),fill=white,anchor='ra')
d.text((58,55),'육체와 별개로 유지되는 영혼 체력',font=kfont(6),fill=dim)

# Shared resources are detached from the ghost identity.
d.polygon([(265,4),(423,4),(430,11),(430,60),(423,66),(265,66),(259,60),(259,11)],
          fill=panel,outline=edge)
d.text((271,9),'RUN RESOURCES',font=font(7),fill=dim)

# Coin chip
d.rounded_rectangle((270,24,341,55),radius=4,fill=panel2,outline=(77,69,35,255))
d.ellipse((278,31,293,46),fill=(116,62,15,255),outline=gold)
d.ellipse((281,34,290,43),outline=(255,224,93,255))
d.text((331,31),'20',font=font(13),fill=white,anchor='ra')

# Gem chip
d.rounded_rectangle((348,24,419,55),radius=4,fill=panel2,outline=(67,40,91,255))
d.polygon([(359,31),(366,38),(359,47),(352,38)],fill=violet,outline=(230,151,255,255))
d.text((409,31),'0',font=font(13),fill=white,anchor='ra')

# Pause remains a separate system control.
d.polygon([(440,5),(478,5),(485,12),(485,59),(478,66),(440,66),(433,59),(433,12)],
          fill=(14,28,45,255),outline=(80,112,132,255))
d.rectangle((445,17,453,53),fill=(166,187,201,255))
d.rectangle((465,17,473,53),fill=(166,187,201,255))
d.rectangle((446,18,451,50),fill=(216,229,236,255))
d.rectangle((466,18,471,50),fill=(216,229,236,255))

im.convert('RGB').save(ROOT/'AVSR_ingame_ghost_host_hud_486x899.png')
im.resize((720,1332),Image.Resampling.NEAREST).convert('RGB').save(ROOT/'AVSR_ingame_ghost_host_hud.png')
im.crop((0,0,486,164)).resize((972,328),Image.Resampling.NEAREST).convert('RGB').save(ROOT/'AVSR_ingame_ghost_host_hud_crop.png')
