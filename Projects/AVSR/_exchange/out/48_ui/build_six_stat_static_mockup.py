from pathlib import Path
import runpy
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
OUT=ROOT/'Projects'/'AVSR'/'_exchange'/'out'/'48_ui'
base=runpy.run_path(str(OUT/'build_hostdetail_reorganized_mockup.py'))
card=base['card'].copy(); UI=base['UI']; IN=base['IN']
d=ImageDraw.Draw(card)
ARB=Path(r'C:\Windows\Fonts\arialbd.ttf'); MKB=Path(r'C:\Windows\Fonts\malgunbd.ttf'); MK=Path(r'C:\Windows\Fonts\malgun.ttf')
def ft(p,s): return ImageFont.truetype(str(p),s)
def load(name,size):
    p=(IN/name) if (IN/name).exists() else (UI/name)
    return Image.open(p).convert('RGBA').resize(size,Image.Resampling.NEAREST)

# Replace the four tall rows with six compact comparison cells.
d.rectangle((11,282,272,443),fill=(7,15,26,255))
d.text((14,287),'능력치',font=ft(MKB,12),fill=(151,190,199,255))
d.text((269,289),'6 STAT',font=ft(ARB,8),fill=(84,116,129,255),anchor='ra')

stats=[
 ('staticon_hp.png','HP','69',69,(237,51,70,255)),
 ('staticon_atk.png','ATK','81',81,(239,172,29,255)),
 ('staticon_spd.png','SPD','75',75,(50,194,236,255)),
 ('staticon_dash.png','DASH','66',66,(178,74,227,255)),
 (None,'RNG','7.5',88,(72,211,206,255)),
 (None,'RATE','0.9',72,(224,130,42,255)),
]
for i,(ico,label,value,ratio,color) in enumerate(stats):
    col=i%2; row=i//2; x=14+col*129; y=307+row*43
    d.rectangle((x,y,x+122,y+37),fill=(9,20,33,255),outline=(23,47,58,255),width=1)
    if ico: card.alpha_composite(load(ico,(17,17)),(x+5,y+5))
    else:
        symbol='◎' if label=='RNG' else '»'
        d.text((x+7,y+3),symbol,font=ft(ARB,13),fill=color)
    d.text((x+26,y+4),label,font=ft(ARB,10),fill=(230,237,240,255))
    d.text((x+116,y+4),value,font=ft(ARB,9),fill=(230,237,240,255),anchor='ra')
    d.rectangle((x+6,y+25,x+116,y+29),fill=(18,35,44,255))
    d.rectangle((x+6,y+25,x+6+int(110*ratio/100),y+29),fill=color)
    d.line((x+7,y+25,x+5+int(110*ratio/100),y+25),fill=(245,211,110,255),width=1)

card.save(OUT/'AVSR_HostDetail_6stats_card.png')

sheet=Image.new('RGBA',(720,1280),(6,9,17,255)); sd=ImageDraw.Draw(sheet)
sd.text((42,36),'HOST DETAIL — 6 STAT LAYOUT',font=ft(ARB,20),fill=(226,234,239,255))
sd.text((42,67),'추천안: 2열 × 3행 · 카드 전체 높이 유지',font=ft(MK,12),fill=(116,136,151,255))
zoom=card.resize((424,1106),Image.Resampling.NEAREST); sheet.alpha_composite(zoom,(148,110))
sd.text((42,1240),'※ 추가 능력치는 RNG(사거리) · RATE(공격 속도)로 가정',font=ft(MK,11),fill=(80,107,119,255))
sheet.convert('RGB').save(OUT/'AVSR_HostDetail_6stats_mockup.png')
