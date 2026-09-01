from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageChops

ROOT=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\out\51_stage_ui')
SRC=ROOT/'AVSR_ingame_ghost_host_hud_486x899.png'
im=Image.open(SRC).convert('RGBA')
d=ImageDraw.Draw(im)
ARB=Path(r'C:\Windows\Fonts\arialbd.ttf')
font=lambda s: ImageFont.truetype(str(ARB),s)

# Preserve the actual controls while replacing only their unfinished black field.
original=im.copy()
joy=original.crop((0,748,124,899))
skills=original.crop((326,738,486,899))

top=720; bottom=899
for y in range(top,bottom):
    t=(y-top)/(bottom-top)
    c=(5+int(3*t),12+int(5*t),20+int(8*t),255)
    d.line((0,y,485,y),fill=c)

# Thin frame tied to the upper HUD, with a subdued cyan/violet energy seam.
d.rectangle((0,720,485,723),fill=(2,5,10,255))
d.line((0,724,485,724),fill=(34,73,87,255))
d.line((0,725,485,725),fill=(11,32,44,255))
d.line((19,728,194,728),fill=(27,64,76,255))
d.line((292,728,467,728),fill=(53,38,77,255))
d.polygon([(221,724),(265,724),(273,730),(265,736),(221,736),(213,730)],fill=(7,18,29,255),outline=(39,75,88,255))
d.rectangle((235,728,251,731),fill=(49,211,222,255))

# Subtle grid fills the dead space without defining a fixed joystick socket.
for x in range(20,486,54):
    d.line((x,756,x,894),fill=(9,25,34,255))
for y in range(759,900,36):
    d.line((0,y,485,y),fill=(8,23,31,255))
for x,y in [(146,769),(202,833),(280,781),(315,854)]:
    d.rectangle((x,y,x+2,y+2),fill=(24,58,68,255))

# The whole lower field is a free-touch zone. There is deliberately no fixed
# joystick socket because the stick appears wherever the player presses.
d.text((16,742),'FREE MOVE AREA',font=font(6),fill=(35,73,83,255))
d.line((16,752,300,752),fill=(12,36,46,255))

# Only persistent buttons get a fixed, unmistakable action frame.
d.polygon([(338,746),(478,746),(485,753),(485,892),(478,899),(338,899),(329,890),(329,755)],
          fill=(7,14,25,255),outline=(74,58,95,255))
d.polygon([(344,752),(472,752),(479,758),(479,886),(472,892),(344,892),(336,884),(336,760)],
          outline=(35,70,85,255))
d.rectangle((351,756,464,758),fill=(119,56,138,255))
d.text((464,763),'ACTION',font=font(7),fill=(151,118,169,255),anchor='ra')

def keyed_paste(tile, xy, cutoff=24):
    rgb=tile.convert('RGB')
    lum=rgb.convert('L')
    mask=lum.point(lambda p: 255 if p>cutoff else 0)
    im.paste(tile,xy,mask)

keyed_paste(joy,(0,748),38)
keyed_paste(skills,(326,738),35)

# Reassert small finish details over the preserved controls.
d.line((120,894,324,894),fill=(14,38,49,255))
d.rectangle((226,891,260,894),fill=(24,61,72,255))

im.convert('RGB').save(ROOT/'AVSR_ingame_complete_hud_v2_486x899.png')
im.resize((720,1332),Image.Resampling.NEAREST).convert('RGB').save(ROOT/'AVSR_ingame_complete_hud_v2.png')
im.crop((0,704,486,899)).resize((972,390),Image.Resampling.NEAREST).convert('RGB').save(ROOT/'AVSR_ingame_lower_control_deck_v2_crop.png')
