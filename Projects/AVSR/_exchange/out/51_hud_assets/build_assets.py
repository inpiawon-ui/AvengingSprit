from pathlib import Path
from PIL import Image, ImageDraw

OUT=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in')
OUT.mkdir(parents=True,exist_ok=True)
T=(0,0,0,0)
INK=(3,7,16,255); NAVY=(7,16,31,255); NAVY2=(12,29,47,255)
EDGE=(24,78,91,255); CYAN=(52,172,184,255); HI=(126,215,220,255)
RED0=(50,11,17,255); RED1=(133,28,22,255); RED2=(225,64,25,255); ORANGE=(255,141,33,255)
PUR0=(22,10,40,255); PUR1=(69,31,105,255); PUR2=(129,62,180,255); VIOLET=(185,111,225,255)
GRAY0=(64,72,80,255); GRAY1=(132,143,148,255); GRAY2=(205,211,209,255); GRAY3=(242,242,231,255)

def save(im,name): im.save(OUT/name)

# 120x120 octagonal free-position D-pad base.
im=Image.new('RGBA',(120,120),T); d=ImageDraw.Draw(im)
octo=[(22,2),(97,2),(117,22),(117,97),(97,117),(22,117),(2,97),(2,22)]
d.polygon(octo,fill=INK)
d.line(octo+[octo[0]],fill=GRAY0,width=2)
d.line([(23,5),(96,5),(114,23)],fill=GRAY2,width=2)
d.line([(5,95),(24,114),(95,114)],fill=(28,35,43,255),width=3)
d.polygon([(27,9),(92,9),(110,27),(110,92),(92,110),(27,110),(9,92),(9,27)],fill=(13,18,27,255),outline=EDGE)
d.line([(29,12),(90,12),(106,28)],fill=(62,78,87,255),width=1)
# center socket: stepped circles, no AA
d.ellipse((39,39,80,80),fill=INK)
d.ellipse((43,43,76,76),fill=(54,65,73,255))
d.ellipse((47,47,72,72),fill=(9,13,21,255))
d.arc((43,43,76,76),200,330,fill=CYAN,width=1)
# engraved directional arrows
for pts in [((60,14),(49,29),(55,29),(55,36),(65,36),(65,29),(71,29)),
            ((60,105),(49,90),(55,90),(55,83),(65,83),(65,90),(71,90)),
            ((14,60),(29,49),(29,55),(36,55),(36,65),(29,65),(29,71)),
            ((105,60),(90,49),(90,55),(83,55),(83,65),(90,65),(90,71))]:
    d.polygon(pts,fill=GRAY1,outline=INK)
save(im,'hud_dpad_base.png')

# 46x46 three-color red knob.
im=Image.new('RGBA',(46,46),T); d=ImageDraw.Draw(im)
d.ellipse((2,2,43,43),fill=INK)
d.ellipse((5,5,40,40),fill=RED1)
d.pieslice((7,7,38,38),20,200,fill=RED2)
d.rectangle((13,8,25,10),fill=ORANGE)
d.rectangle((9,13,11,24),fill=ORANGE)
save(im,'hud_dpad_knob.png')

def action_card(name,colors):
    dark,mid,bright,hot=colors
    im=Image.new('RGBA',(100,112),T); d=ImageDraw.Draw(im)
    p=[(11,1),(88,1),(98,11),(98,100),(87,111),(12,111),(1,100),(1,12)]
    d.polygon(p,fill=INK)
    d.line(p+[p[0]],fill=bright,width=2)
    d.line([(13,5),(86,5),(94,13)],fill=hot,width=1)
    d.line([(5,98),(13,107),(86,107)],fill=mid,width=2)
    inner=[(14,11),(85,11),(91,17),(91,83),(85,89),(14,89),(8,83),(8,17)]
    d.polygon(inner,fill=dark,outline=mid)
    # Empty icon well: only a quiet inset, no symbol.
    d.rectangle((16,19,83,76),fill=NAVY,outline=mid)
    # label strip; code/UI text can sit here.
    d.polygon([(10,91),(89,91),(94,96),(87,105),(12,105),(6,99)],fill=dark,outline=bright)
    d.rectangle((21,96,78,98),fill=mid)
    save(im,name)

action_card('hud_action_ultimate.png',(RED0,RED1,RED2,ORANGE))
action_card('hud_action_possess.png',(PUR0,PUR1,PUR2,VIOLET))

# Empty host portrait frame, transparent center.
im=Image.new('RGBA',(112,120),T); d=ImageDraw.Draw(im)
outer=[(9,1),(102,1),(111,10),(111,109),(101,119),(10,119),(1,110),(1,9)]
d.line(outer+[outer[0]],fill=INK,width=5)
d.line(outer+[outer[0]],fill=CYAN,width=2)
inner=[(13,9),(98,9),(103,14),(103,105),(97,111),(14,111),(8,105),(8,14)]
d.line(inner+[inner[0]],fill=EDGE,width=2)
d.line([(14,5),(97,5)],fill=HI,width=1)
for x,y in [(5,14),(101,14),(5,101),(101,101)]:
    d.rectangle((x,y,x+5,y+5),fill=NAVY2,outline=CYAN)
save(im,'hud_host_frame.png')

# Hollow 24x24 seal lock.
im=Image.new('RGBA',(24,24),T); d=ImageDraw.Draw(im)
d.line([(7,10),(7,7),(9,4),(15,4),(17,7),(17,10)],fill=INK,width=4)
d.line([(7,10),(7,7),(9,4),(15,4),(17,7),(17,10)],fill=GRAY2,width=2)
d.rectangle((4,10,19,21),outline=INK,width=3)
d.rectangle((6,12,17,19),outline=ORANGE,width=2)
d.rectangle((11,14,12,18),fill=GRAY3)
save(im,'icon_seal.png')

def dust(name,clusters):
    im=Image.new('RGBA',(48,48),T); d=ImageDraw.Draw(im)
    for x,y,w,h,c in clusters:
        d.rectangle((x,y,x+w-1,y+h-1),fill=c)
    save(im,name)

dust('fx_dash_1.png',[(19,30,10,4,GRAY1),(17,34,14,3,GRAY0),(21,26,6,4,GRAY2),(23,23,3,3,GRAY3),(15,31,3,2,GRAY2),(30,31,3,2,GRAY2)])
dust('fx_dash_2.png',[(11,31,10,4,GRAY1),(27,31,10,4,GRAY1),(16,27,16,5,GRAY2),(20,24,8,3,GRAY3),(7,35,7,3,GRAY0),(34,35,7,3,GRAY0),(10,27,4,3,GRAY2),(34,27,4,3,GRAY2)])
dust('fx_dash_3.png',[(4,31,6,3,GRAY1),(38,31,6,3,GRAY1),(12,26,5,4,GRAY2),(31,26,5,4,GRAY2),(19,33,4,3,GRAY0),(25,33,4,3,GRAY0),(8,22,3,3,GRAY3),(37,22,3,3,GRAY3),(22,25,4,3,GRAY1)])

# The control pair must remain visually identical to the control already used
# in game. Reduce its palette and resize with nearest-neighbour; do not redesign.
REF=Path(r'C:\won\UnityProject\AvengingSprit\Assets\BaseResource\InGameMainUI')
def faithful_low_color(src,size,name,colors):
    ref=Image.open(REF/src).convert('RGBA').resize(size,Image.Resampling.NEAREST)
    alpha=ref.getchannel('A').point(lambda p: 255 if p>=128 else 0)
    rgb=ref.convert('RGB').quantize(colors=colors,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert('RGB')
    out=rgb.convert('RGBA'); out.putalpha(alpha); save(out,name)

faithful_low_color('dpadbase.png',(120,120),'hud_dpad_base.png',16)
faithful_low_color('dpadknob.png',(46,46),'hud_dpad_knob.png',5)

# Review sheet only; not part of delivery.
review=Image.new('RGBA',(800,520),(5,10,18,255)); rd=ImageDraw.Draw(review)
items=[('hud_dpad_base.png',(20,20),2),('hud_dpad_knob.png',(275,70),3),
       ('hud_action_ultimate.png',(390,14),2),('hud_action_possess.png',(600,14),2),
       ('hud_host_frame.png',(20,270),2),('icon_seal.png',(280,320),4),
       ('fx_dash_1.png',(390,325),3),('fx_dash_2.png',(535,325),3),('fx_dash_3.png',(680,325),3)]
for name,(x,y),scale in items:
    tile=Image.open(OUT/name).convert('RGBA').resize((Image.open(OUT/name).width*scale,Image.open(OUT/name).height*scale),Image.Resampling.NEAREST)
    review.alpha_composite(tile,(x,y))
review.convert('RGB').save(Path(__file__).with_name('hud_assets_review.png'))
