from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(r'C:\won\UnityProject\AvengingSprit'); IN=ROOT/'Projects/AVSR/_exchange/in'; OUT=Path(__file__).parent
groups={
'burst':(4,(96,96)),'muzzle':(2,(48,48)),'mark':(2,(48,48)),'reflect':(3,(64,64)),
'burn':(3,(48,48)),'freeze':(3,(144,144)),'drain':(3,(64,64)),'smoke':(3,(96,96)),'missile_trail':(2,(24,24)),
'scythe':(5,(216,216)),'ward':(3,(216,216)),'chain':(3,(72,24)),'breath_fire':(5,(360,360)),'breath_ice':(5,(360,360)),
'lava':(4,(144,144)),'grenade_burst':(4,(144,144)),'shatter':(4,(216,216)),'slam':(4,(216,216)),'curse_beam':(3,(72,24)),
'holy_beam':(4,(144,576)),'laser_wide':(3,(216,576)),'turret':(3,(96,96))}
bad=[]; total=0
for g,(count,size) in groups.items():
    for i in range(1,count+1):
        p=IN/f'fx_{g}_{i}.png'; total+=1
        if not p.exists(): bad.append((p.name,'missing')); continue
        im=Image.open(p).convert('RGBA'); alpha={v[3] for v in im.getdata()}
        if im.size!=size or not alpha<={0,255}: bad.append((p.name,im.size,sorted(alpha)))
print('total',total,'bad',bad)
for g in ('chain','curse_beam'):
    for i in range(1,4):
        im=Image.open(IN/f'fx_{g}_{i}.png').convert('RGBA'); print(g,i,'seam',all(im.getpixel((0,y))==im.getpixel((im.width-1,y)) for y in range(im.height)))
for i in range(1,6):
    a=Image.open(IN/f'fx_breath_fire_{i}.png').getchannel('A'); b=Image.open(IN/f'fx_breath_ice_{i}.png').getchannel('A'); print('breath silhouette',i,list(a.getdata())==list(b.getdata()))

# Group overview: first frame of each effect, fitted without changing source assets.
font=ImageFont.truetype(r'C:\Windows\Fonts\arial.ttf',13)
thumbs=[]
for g,(count,size) in groups.items():
    im=Image.open(IN/f'fx_{g}_1.png').convert('RGBA'); im.thumbnail((150,150),Image.Resampling.NEAREST); thumbs.append((g,im.copy()))
sheet=Image.new('RGBA',(720,760),(5,10,18,255)); d=ImageDraw.Draw(sheet)
for idx,(g,im) in enumerate(thumbs):
    col=idx%5; row=idx//5; x=15+col*140; y=15+row*165
    sheet.alpha_composite(im,(x+(120-im.width)//2,y)); d.text((x,y+152),g,font=font,fill=(215,225,230,255))
sheet.convert('RGB').save(OUT/'fx_overview.png')
