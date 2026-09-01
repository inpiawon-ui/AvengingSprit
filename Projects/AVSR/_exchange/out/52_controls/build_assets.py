from pathlib import Path
from PIL import Image, ImageDraw

OUT=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in')
OUT.mkdir(parents=True,exist_ok=True)
T=(0,0,0,0)

# Full-width control field. Contrast is intentionally only 5-9 luminance steps.
im=Image.new('RGBA',(720,230),(4,10,18,255)); d=ImageDraw.Draw(im)
grid=(26,44,62,255); grid2=(26,44,62,255); seam=(31,53,70,255)
for x in range(0,721,72):
    d.line((x,0,x,229),fill=grid)
for y in range(0,231,46):
    d.line((0,y,719,y),fill=grid)
# One restrained top seam ties the control field to the HUD without becoming a panel.
d.line((0,0,719,0),fill=seam)
d.line((0,2,719,2),fill=(12,28,39,255))
im.save(OUT/'hud_control_grid.png')

# Empty frame around the two 100x112 action cards.
im=Image.new('RGBA',(224,150),T); d=ImageDraw.Draw(im)
ink=(3,7,16,255); edge=(31,56,73,255); violet=(84,43,107,255)
violet_hi=(132,64,154,255); cyan=(30,91,101,255)
outer=[(10,1),(213,1),(223,11),(223,138),(212,149),(11,149),(1,139),(1,11)]
d.line(outer+[outer[0]],fill=ink,width=5)
d.line(outer+[outer[0]],fill=edge,width=2)
# Quiet violet top edge identifies the action side without competing with cards.
d.line([(14,5),(209,5),(218,14)],fill=violet,width=2)
d.line([(17,8),(206,8)],fill=violet_hi,width=1)

# Recess reserved for the ACTION label; no text baked into the asset.
label=[(72,1),(151,1),(159,9),(151,17),(72,17),(64,9)]
d.line(label+[label[0]],fill=ink,width=4)
d.line(label+[label[0]],fill=cyan,width=1)

# Inner guide is only a thin frame; the two card areas remain transparent.
inner=[(8,23),(215,23),(218,26),(218,129),(212,135),(12,135),(6,129),(6,26)]
d.line(inner+[inner[0]],fill=(17,38,52,255),width=1)

# Bottom '-<>' calibration tick, centered and deliberately small.
d.line((82,143,96,143),fill=edge)
d.line([(101,143),(107,139),(107,147),(101,143)],fill=cyan)
d.line([(117,139),(123,143),(117,147),(117,139)],fill=cyan)
d.line((128,143,142,143),fill=edge)
im.save(OUT/'hud_action_frame.png')
