from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

OUT = Path(r"C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in")
OUT.mkdir(parents=True, exist_ok=True)
C = dict(O=(5,8,18,255),N=(12,20,36,255),S=(31,53,70,255),L=(76,119,132,255),W=(202,230,222,255),P=(124,61,196,255),V=(211,93,255,255),R=(205,51,59,255),Y=(246,183,50,255),C=(55,208,222,255),B=(66,117,205,255),G=(77,190,91,255))

def canvas(w,h):
    im=Image.new('RGBA',(w,h),(0,0,0,0)); return im,ImageDraw.Draw(im)
def rect(d,c,box): d.rectangle(box,fill=C.get(c,c))
def poly(d,c,pts): d.polygon(pts,fill=C.get(c,c))
def line(d,c,pts,w=1): d.line(pts,fill=C.get(c,c),width=w)
def spark(d,x,y,c='Y'):
    rect(d,c,(x-1,y-4,x+1,y+4)); rect(d,c,(x-4,y-1,x+4,y+1)); rect(d,'W',(x,y-2,x,y+2))
def base(d): rect(d,'S',(8,39,35,40)); rect(d,'N',(11,41,32,41))
def down(d,x,y,c):
    rect(d,'O',(x-3,y-8,x+3,y+1)); poly(d,'O',[(x-8,y),(x+8,y),(x,y+9)]); rect(d,c,(x-1,y-7,x+1,y+1)); poly(d,c,[(x-5,y+1),(x+5,y+1),(x,y+6)])
def drop(d,x,y,s,c='R'):
    poly(d,'O',[(x,y-s-2),(x-s-1,y+1),(x-s+1,y+s),(x,y+s+2),(x+s-1,y+s),(x+s+1,y+1)])
    poly(d,c,[(x,y-s),(x-s+1,y+2),(x,y+s),(x+s-1,y+2)]); rect(d,(255,124,118,255),(x-1,y-s+2,x,y-s+4))
def save_icon(name,paint):
    im,d=canvas(44,44); base(d); paint(d); im.save(OUT/name)

def shield(d):
    poly(d,'O',[(22,4),(35,9),(33,25),(22,36),(11,25),(9,9)]); poly(d,'B',[(22,7),(32,11),(30,23),(22,32),(14,23),(12,11)]); poly(d,'C',[(22,9),(29,12),(27,20),(22,25),(17,20),(15,12)]); rect(d,'W',(21,10,23,23)); rect(d,'W',(17,17,27,19)); poly(d,'Y',[(30,7),(37,7),(37,14),(35,14),(35,10),(30,10)])
def ricochet(d):
    line(d,'O',[(10,33),(32,11)],7); line(d,'W',[(10,31),(30,11)],3); rect(d,'Y',(8,31,14,35)); rect(d,'R',(30,8,34,15)); poly(d,'C',[(29,24),(37,20),(39,23),(32,27)]); line(d,'C',[(33,23),(39,14)],2); spark(d,38,12)
def scythe(d):
    line(d,'O',[(14,36),(29,9)],6); line(d,'W',[(15,35),(29,10)],2); poly(d,'O',[(27,5),(40,10),(34,20),(30,18),(34,11),(27,9)]); poly(d,'L',[(29,7),(37,10),(33,16),(31,15),(34,11),(29,9)]); drop(d,8,18,4); line(d,'R',[(12,19),(20,23)],2); poly(d,'R',[(18,19),(25,24),(17,27)])
def flame(d):
    poly(d,'O',[(22,3),(29,13),(34,10),(36,23),(30,36),(14,36),(8,25),(13,14),(16,19)]); poly(d,'R',[(22,7),(27,17),(31,15),(32,25),(27,33),(16,33),(12,25),(17,17),(18,23)]); poly(d,'Y',[(22,15),(27,22),(25,31),(18,31),(16,25)]); rect(d,'W',(21,25,23,29))
def weak(d,snow=False):
    q='C' if snow else 'B'
    if snow:
        for a,b in [((22,5),(22,24)),((12,10),(32,22)),((32,10),(12,22))]: line(d,'O',[a,b],5); line(d,q,[a,b],2)
    else:
        poly(d,'O',[(22,4),(35,11),(33,26),(22,34),(11,26),(9,11)]); poly(d,q,[(22,7),(31,12),(29,23),(22,29),(15,23),(13,12)]); rect(d,'C',(20,11,23,22))
    down(d,22,33,'W' if snow else 'C')
def magazine(d):
    rect(d,'O',(9,7,31,35)); rect(d,'S',(12,9,28,31)); rect(d,'L',(14,11,26,15));
    for i in range(4): rect(d,'Y',(14+i*3,20,15+i*3,26))
    rect(d,'R',(25,20,26,26)); poly(d,'O',[(30,5),(39,11),(34,20),(29,14)]); poly(d,'Y',[(31,7),(36,11),(33,16),(31,13)]); spark(d,36,7,'W')
def fangs(d):
    poly(d,'O',[(7,9),(19,8),(20,29),(14,37),(10,26)]); poly(d,'O',[(37,9),(25,8),(24,29),(30,37),(34,26)]); poly(d,'W',[(10,11),(17,11),(17,27),(14,33),(12,25)]); poly(d,'W',[(34,11),(27,11),(27,27),(30,33),(32,25)]); drop(d,22,24,4)
def spring(d):
    rect(d,'O',(8,26,26,36)); rect(d,'G',(10,28,24,33)); line(d,'O',[(21,29),(29,22)],5); line(d,'Y',[(21,29),(29,22)],2); line(d,'O',[(28,21),(35,14)],5); line(d,'Y',[(28,21),(35,14)],2); poly(d,'O',[(31,10),(41,12),(37,20)]); poly(d,'R',[(33,12),(39,13),(36,17)]); line(d,'C',[(7,18),(17,18)],2); line(d,'C',[(10,14),(18,14)],2)
def rune(d):
    poly(d,'O',[(22,3),(35,12),(31,28),(22,35),(13,28),(9,12)]); poly(d,'P',[(22,7),(31,13),(28,25),(22,30),(16,25),(13,13)]); rect(d,'V',(20,11,23,23)); rect(d,'W',(18,17,25,19)); spark(d,10,8,'C'); down(d,34,30,'V')
def circuit(d):
    rect(d,'O',(8,8,36,35)); rect(d,'S',(11,11,33,32)); rect(d,'C',(15,14,29,25)); rect(d,'N',(18,17,26,22));
    for i in range(4): rect(d,'L',(5,12+i*5,10,13+i*5)); rect(d,'L',(34,12+i*5,39,13+i*5))
    line(d,'G',[(20,31),(20,38)],2); line(d,'G',[(24,31),(24,38)],2); rect(d,'Y',(18,36,26,39)); spark(d,31,8,'V')

icons={'passiveicon_amazon_elite.png':shield,'passiveicon_baseball.png':ricochet,'passiveicon_death.png':scythe,'passiveicon_salamander.png':flame,'passiveicon_dragon_blue.png':lambda d:weak(d,False),'passiveicon_snowwoman.png':lambda d:weak(d,True),'passiveicon_hopper_smg.png':magazine,'passiveicon_vampire.png':fangs,'passiveicon_hopper.png':spring,'passiveicon_white_wizard.png':rune,'passiveicon_robot.png':circuit}
for n,f in icons.items(): save_icon(n,f)

def badge(name,a,z):
    im,d=canvas(88,24); poly(d,'O',[(3,0),(85,0),(87,3),(87,20),(84,23),(4,23),(0,20),(0,4)]); poly(d,a,[(4,2),(84,2),(86,4),(86,19),(83,21),(5,21),(2,19),(2,5)]); rect(d,'N',(5,5,82,18)); rect(d,z,(7,5,80,6)); rect(d,a,(7,19,80,19)); rect(d,z,(4,8,5,15)); rect(d,z,(82,8,83,15)); im.save(OUT/name)
badge('jobbadge_melee.png',(91,31,38,255),'R'); badge('jobbadge_midrange.png',(92,61,22,255),'Y'); badge('jobbadge_ranged.png',(25,50,91,255),'B'); badge('jobbadge_piercing.png',(18,72,82,255),'C')

im,d=canvas(263,124); poly(d,'O',[(5,0),(258,0),(262,5),(262,119),(258,123),(5,123),(0,119),(0,5)]); poly(d,'S',[(5,2),(258,2),(260,5),(260,119),(258,121),(5,121),(2,119),(2,5)]); rect(d,'N',(5,5,257,118)); rect(d,(20,22,45,255),(7,7,255,116)); rect(d,'P',(7,7,255,8)); rect(d,'L',(7,34,255,34)); rect(d,'S',(7,100,255,100)); rect(d,'V',(12,14,14,25)); rect(d,'C',(18,14,18,25)); rect(d,'P',(244,14,246,25)); rect(d,'C',(240,14,240,25)); im.save(OUT/'passiveskillcard.png')
im,d=canvas(239,10); rect(d,'O',(0,1,238,8)); rect(d,'S',(1,2,237,7)); rect(d,'N',(2,3,236,6)); rect(d,'L',(2,3,236,3)); im.save(OUT/'shardbarbg.png')
im,d=canvas(239,10); rect(d,'P',(0,2,238,7)); rect(d,'V',(0,2,238,3)); rect(d,(95,43,164,255),(0,6,238,7));
for x in range(6,239,18): rect(d,(231,145,255,255),(x,3,min(x+1,238),4))
im.save(OUT/'shardbarfill.png')

# 720x1280 layout application preview (kept in out/, not mixed into game delivery).
UI = Path(r"C:\won\UnityProject\AvengingSprit\Assets\BaseResource\HostSelectPanel")
PREVIEW = Path(r"C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\out\44_ui\AVSR_HostDetail_applied_preview.png")
screen = Image.new('RGBA',(720,1280),(8,12,22,255)); sd=ImageDraw.Draw(screen)
sd.rectangle((0,0,719,86),fill=(6,9,17,255)); sd.rectangle((0,85,719,88),fill=(50,31,76,255))
sd.text((28,25),'HOST ARCHIVE',fill=(225,232,240,255),font=ImageFont.truetype(r'C:\Windows\Fonts\arialbd.ttf',25))
sd.text((28,56),'POSSESSION DATABASE',fill=(119,75,169,255),font=ImageFont.truetype(r'C:\Windows\Fonts\arial.ttf',11))
sd.rectangle((18,112,397,1198),fill=(10,16,28,255),outline=(34,56,72,255),width=2)
sd.text((34,132),'HOST LIST',fill=(187,201,214,255),font=ImageFont.truetype(r'C:\Windows\Fonts\arialbd.ttf',16))
for i in range(12):
    x=34+(i%3)*116; y=172+(i//3)*122
    sd.rectangle((x,y,x+98,y+102),fill=(15,23,39,255),outline=(42,63,79,255),width=2)
    sd.rectangle((x+10,y+10,x+88,y+77),fill=(19+(i%3)*5,28,46+(i%2)*8,255))
    sd.rectangle((x+10,y+84,x+65,y+89),fill=(47,68,84,255)); sd.rectangle((x+10,y+94,x+80,y+97),fill=(28,43,59,255))
sd.rectangle((34,172,132,274),outline=(220,164,46,255),width=3)

def paste_asset(path,xy,size=None):
    im=Image.open(path).convert('RGBA')
    if size: im=im.resize(size,Image.Resampling.NEAREST)
    screen.alpha_composite(im,xy)

paste_asset(UI/'hostdetailframe.png',(420,250),(283,737))
fbd=ImageFont.truetype(r'C:\Windows\Fonts\arialbd.ttf',20); fs=ImageFont.truetype(r'C:\Windows\Fonts\arial.ttf',12); fk=ImageFont.truetype(r'C:\Windows\Fonts\malgunbd.ttf',15); fks=ImageFont.truetype(r'C:\Windows\Fonts\malgun.ttf',11)
sd.text((430,270),'DEATH',fill=(229,234,239,255),font=fbd); sd.text((430,301),'사신',fill=(201,214,222,255),font=fk)
paste_asset(OUT/'jobbadge_melee.png',(605,300)); sd.text((631,304),'격투',fill=(255,227,208,255),font=fks)
sd.text((430,331),'근접 공격이 적중하면 생명력을 회수합니다.',fill=(148,163,176,255),font=fks)
portrait=UI/'hostportraitimage_death.png'
if portrait.exists(): paste_asset(portrait,(528,376),(64,64))
sd.text((440,456),'ABILITY',fill=(121,168,180,255),font=ImageFont.truetype(r'C:\Windows\Fonts\arialbd.ttf',12))
stats=[('staticon_hp.png','HP',88,'184'),('staticon_atk.png','ATK',72,'20'),('staticon_spd.png','SPD',60,'4.6'),('staticon_dash.png','DASH',48,'1.8')]
for i,(ico,label,val,num) in enumerate(stats):
    y=480+i*32; paste_asset(UI/ico,(440,y+5),(18,18)); sd.text((463,y+6),label,fill=(177,188,199,255),font=fs); paste_asset(UI/'statbarbg.png',(505,y+10),(132,8)); fill=Image.open(UI/'statbarfill.png').convert('RGBA').resize((val,8),Image.Resampling.NEAREST); screen.alpha_composite(fill,(505,y+10)); sd.text((646,y+5),num,fill=(222,228,235,255),font=fs)
paste_asset(UI/'ultimatecard.png',(430,618),(263,150)); sd.text((442,628),'ACTIVE SKILL',fill=(159,127,211,255),font=ImageFont.truetype(r'C:\Windows\Fonts\arialbd.ttf',11)); sd.text((638,628),'8s',fill=(238,185,61,255),font=fs)
paste_asset(UI/'ultimateicon_death.png',(442,650),(56,56)); sd.text((506,652),'SOUL REAPER',fill=(229,231,239,255),font=ImageFont.truetype(r'C:\Windows\Fonts\arialbd.ttf',13)); sd.text((442,714),'낫을 크게 휘둘러 주변 적을 베고\n강한 피해를 줍니다.',fill=(163,173,188,255),font=fks,spacing=4)
paste_asset(OUT/'passiveskillcard.png',(430,778)); sd.text((442,788),'PASSIVE SKILL',fill=(210,123,247,255),font=ImageFont.truetype(r'C:\Windows\Fonts\arialbd.ttf',11)); sd.text((650,788),'25%',fill=(240,177,82,255),font=fs)
paste_asset(OUT/'passiveicon_death.png',(442,810)); sd.text((494,813),'흡혈',fill=(237,225,242,255),font=fk); sd.text((442,861),'공격 적중 시 일정 확률로\n피해량의 일부를 회복합니다.',fill=(170,181,195,255),font=fks,spacing=3)
sd.text((442,920),'숙련도',fill=(189,199,212,255),font=fk); sd.text((617,920),'Lv 3 / 10',fill=(220,190,245,255),font=fs)
paste_asset(OUT/'shardbarbg.png',(442,942)); fill=Image.open(OUT/'shardbarfill.png').convert('RGBA').crop((0,0,166,10)); screen.alpha_composite(fill,(442,942)); sd.text((442,957),'파편 18 / 26',fill=(151,162,178,255),font=fks)
sd.rectangle((0,1216,719,1279),fill=(5,8,15,255)); sd.text((28,1238),'SELECT A HOST TO VIEW COMBAT DATA',fill=(77,105,119,255),font=ImageFont.truetype(r'C:\Windows\Fonts\arial.ttf',11))
screen.convert('RGB').save(PREVIEW,quality=95)
