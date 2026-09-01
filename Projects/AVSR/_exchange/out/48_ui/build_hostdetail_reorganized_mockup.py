from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
UI=ROOT/'Assets'/'BaseResource'/'HostSelectPanel'
IN=ROOT/'Projects'/'AVSR'/'_exchange'/'in'
OUT=ROOT/'Projects'/'AVSR'/'_exchange'/'out'/'48_ui'
OUT.mkdir(parents=True,exist_ok=True)

def font(path,size): return ImageFont.truetype(str(path),size)
AR=Path(r'C:\Windows\Fonts\arial.ttf'); ARB=Path(r'C:\Windows\Fonts\arialbd.ttf')
MK=Path(r'C:\Windows\Fonts\malgun.ttf'); MKB=Path(r'C:\Windows\Fonts\malgunbd.ttf')
def asset(name,size=None):
    p=(IN/name) if (IN/name).exists() else (UI/name)
    im=Image.open(p).convert('RGBA')
    return im.resize(size,Image.Resampling.NEAREST) if size else im
def paste(dst,im,xy): dst.alpha_composite(im,xy)

card=Image.new('RGBA',(283,737),(0,0,0,0)); d=ImageDraw.Draw(card)
paste(card,asset('hostdetailframe.png',(283,737)),(0,0))
white=(226,234,239,255); dim=(139,157,170,255); cyan=(95,210,214,255); purple=(211,93,255,255); gold=(246,183,50,255)

# 1) Identity header: name owns the first row; grade/level are compact, isolated chips.
d.text((12,16),'GANGSTER',font=font(ARB,18),fill=white)
paste(card,asset('icon_grade_b.png'),(194,11)); paste(card,asset('icon_lv.png'),(226,11))
d.text((250,16),'03',font=font(ARB,13),fill=white)
d.text((12,44),'갱스터',font=font(MKB,14),fill=(194,207,215,255))
paste(card,asset('jobbadge_ranged.png'),(183,42)); d.text((207,46),'원거리',font=font(MKB,11),fill=(199,239,244,255))
d.text((12,72),'탄환은 적을 관통하며 뒤쪽 대상까지 공격합니다.',font=font(MK,10),fill=dim)
d.line((12,94,271,94),fill=(41,74,86,255),width=1)

# Portrait, with a quiet framed stage so metadata stays visually dominant.
d.rectangle((91,104,191,193),fill=(8,14,25,255),outline=(35,62,77,255),width=1)
p=UI/'hostportraitimage_gangster.png'
if p.exists(): paste(card,asset('hostportraitimage_gangster.png',(72,72)),(105,111))

# Job trait becomes a compact rule card rather than competing with the identity row.
d.polygon([(10,201),(273,201),(273,272),(268,277),(10,277)],fill=(8,15,28,255),outline=(73,43,101,255))
d.rectangle((14,205,269,207),fill=(124,61,196,255)); d.text((20,214),'JOB TRAIT',font=font(ARB,10),fill=(218,151,255,255))
d.text((20,235),'저 너머까지',font=font(MKB,13),fill=white)
d.text((20,255),'직선 탄환이 적 한 명을 관통합니다.',font=font(MK,10),fill=dim)

# Stats: stable label/value columns and equal bars.
d.text((14,291),'능력치',font=font(MKB,12),fill=(142,177,185,255))
stats=[('staticon_hp.png','HP',69,88),('staticon_atk.png','ATK',81,102),('staticon_spd.png','SPD',75,94),('staticon_dash.png','DASH',66,84)]
for i,(ico,label,val,fillw) in enumerate(stats):
    y=316+i*31; paste(card,asset(ico,(18,18)),(14,y)); d.text((38,y+1),label,font=font(ARB,13),fill=white)
    paste(card,asset('statbarbg.png',(116,8)),(91,y+6)); paste(card,asset('statbarfill.png',(fillw,8)),(91,y+6))
    d.text((242,y+1),str(val),font=font(ARB,12),fill=white,anchor='ra')

# Active skill keeps the existing visual language.
paste(card,asset('ultimatecard.png',(263,142)),(10,446)); d.text((20,455),'ACTIVE SKILL',font=font(ARB,10),fill=(190,145,238,255))
paste(card,asset('ultimateicon_gangster.png',(52,52)),(20,477)); d.text((80,480),'표식 사격',font=font(MKB,13),fill=gold)
d.text((80,501),'MARK SHOT',font=font(ARB,9),fill=(159,120,198,255))
d.text((20,542),'표식 탄환을 발사합니다. 적중한 대상은\n6초 동안 받는 피해가 증가합니다.',font=font(MK,10),fill=dim,spacing=3)

# Passive-free hosts use a deliberately compact disabled row, avoiding a large empty box.
d.rectangle((10,597,273,642),fill=(9,15,27,255),outline=(53,41,72,255),width=1)
d.rectangle((14,601,269,602),fill=(65,43,86,255)); d.text((20,608),'PASSIVE',font=font(ARB,9),fill=(105,100,121,255)); d.text((251,608),'없음',font=font(MKB,10),fill=(94,101,113,255),anchor='ra')
d.text((20,625),'이 호스트는 고유 패시브가 없습니다.',font=font(MK,9),fill=(74,86,101,255))

# Mastery is one semantic block: level → progress → named shard count.
d.rectangle((10,651,273,724),fill=(8,15,28,255),outline=(47,75,88,255),width=1)
d.rectangle((14,655,269,656),fill=(55,208,222,255)); d.text((20,663),'숙련도',font=font(MKB,12),fill=white)
d.text((263,664),'Lv 0 / 10',font=font(ARB,10),fill=(203,176,231,255),anchor='ra')
paste(card,asset('shardbarbg.png'),(20,683));
# zero fill: only the track is shown.
paste(card,asset('icon_shard.png',(18,18)),(20,700)); d.text((43,702),'영혼 파편',font=font(MKB,10),fill=(171,182,195,255)); d.text((263,702),'0 / 8',font=font(ARB,11),fill=white,anchor='ra')

card.save(OUT/'AVSR_HostDetail_reorganized_card.png')

# Presentation sheet: exact card enlarged with nearest-neighbour pixels.
sheet=Image.new('RGBA',(720,1280),(7,10,18,255)); sd=ImageDraw.Draw(sheet)
sd.text((42,38),'HOST DETAIL — INFORMATION HIERARCHY',font=font(ARB,20),fill=white)
sd.text((42,69),'이름 · 등급 · 레벨 · 직업 / 숙련도 · 영혼 파편 정리 시안',font=font(MK,12),fill=(116,136,151,255))
zoom=card.resize((424,1106),Image.Resampling.NEAREST); paste(sheet,zoom,(148,112))
sd.text((42,1242),'실제 카드 기준 283 × 737 px · 미리보기는 1.5배 확대',font=font(MK,11),fill=(75,100,112,255))
sheet.convert('RGB').save(OUT/'AVSR_HostDetail_reorganized_mockup.png')
