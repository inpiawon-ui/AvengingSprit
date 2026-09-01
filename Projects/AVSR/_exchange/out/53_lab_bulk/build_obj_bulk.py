from pathlib import Path
from PIL import Image, ImageDraw

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
SRC=ROOT/'Assets/BaseResource/InGameMainUI/obj_rooftop_bulk.png'
OUT=ROOT/'Projects/AVSR/_exchange/in/obj_bulk.png'
OUT.parent.mkdir(parents=True,exist_ok=True)

src=Image.open(SRC).convert('RGBA')
px=src.load()

# Preserve the already-approved BULK volume, top view and grounding while
# shifting the material into the laboratory's navy/indigo family.
for y in range(src.height):
    for x in range(src.width):
        r,g,b,a=px[x,y]
        if a < 128:
            px[x,y]=(0,0,0,0); continue
        lum=(r*299+g*587+b*114)//1000
        if lum>225: c=(252,252,252,255)
        elif lum>175: c=(92,103,154,255)
        elif lum>125: c=(51,60,163,255)
        elif lum>82: c=(42,53,79,255)
        elif lum>48: c=(28,33,52,255)
        elif lum>23: c=(12,19,60,255)
        else: c=(0,0,0,255)
        px[x,y]=c

d=ImageDraw.Draw(src)
black=(0,0,0,255); deep=(10,17,50,255); navy=(11,18,63,255)
mid=(28,33,52,255); steel=(42,53,79,255); blue=(51,60,163,255)
cyan=(76,156,190,255); white=(252,252,252,255)

# Replace the rooftop fan identity with one sealed isolation/power assembly.
# Stepped top hood makes the upward-facing plane explicit.
d.polygon([(43,18),(101,18),(111,28),(105,48),(95,54),(49,54),(39,48),(33,28)],fill=black)
d.polygon([(45,21),(99,21),(106,29),(101,44),(92,49),(52,49),(43,44),(38,29)],fill=blue)
d.polygon([(48,23),(96,23),(101,29),(97,34),(47,34),(43,29)],fill=(92,103,154,255))
d.line([(49,24),(95,24)],fill=white,width=1)

# Main sealed core body, fully opaque and connected to the base.
d.polygon([(39,48),(105,48),(113,58),(113,174),(104,184),(40,184),(31,174),(31,58)],fill=black)
d.polygon([(43,53),(101,53),(107,61),(107,169),(99,178),(45,178),(37,169),(37,61)],fill=deep)
d.rectangle((45,65,99,151),fill=navy,outline=steel,width=2)
d.rectangle((51,72,93,143),fill=(12,19,60,255),outline=blue,width=2)

# Contained blue energy window: modest, not a holding-stage purple core.
d.polygon([(58,78),(86,78),(91,84),(91,132),(86,138),(58,138),(53,132),(53,84)],fill=black)
d.polygon([(61,82),(83,82),(87,87),(87,129),(83,134),(61,134),(57,129),(57,87)],fill=(18,49,91,255))
d.rectangle((62,87,82,129),fill=(31,91,133,255))
d.rectangle((66,88,70,128),fill=cyan)
d.rectangle((67,89,68,127),fill=white)
d.rectangle((75,88,81,128),fill=(24,70,116,255))

# Reinforced side ribs and closed lower service panels keep it sight-blocking.
for x0,x1 in [(22,35),(109,122)]:
    d.rectangle((x0,69,x1,184),fill=black)
    d.rectangle((x0+3,73,x1-3,180),fill=mid)
    for yy in (82,116,150): d.rectangle((x0+2,yy,x1-2,yy+4),fill=steel)
d.polygon([(25,181),(119,181),(128,191),(128,221),(119,232),(25,232),(16,221),(16,191)],fill=black)
d.polygon([(29,187),(115,187),(121,194),(121,216),(114,225),(30,225),(23,216),(23,194)],fill=deep)
d.rectangle((33,195,68,217),fill=mid,outline=steel)
d.rectangle((76,195,111,217),fill=mid,outline=steel)
for xx in (39,49,59,82,92,102): d.line((xx,198,xx,214),fill=(12,19,60,255))
d.rectangle((55,225,89,231),fill=black)
d.rectangle((61,226,83,228),fill=blue)

# Reassert a full-width grounded base and binary alpha at the contact line.
d.rectangle((18,232,126,237),fill=black)
d.rectangle((24,232,120,234),fill=steel)

src.save(OUT)
