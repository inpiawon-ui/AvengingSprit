from pathlib import Path
from PIL import Image, ImageDraw

ROOT=Path(r'C:\won\UnityProject\AvengingSprit')
BASE=ROOT/'Assets/BaseResource/Unit'
OUT=ROOT/'Projects/AVSR/_exchange/out/54_mobs'
OUT.mkdir(parents=True,exist_ok=True)
dirs=['s','se','e','ne','n']
acts=['','_walk1','_walk2','_atk1','_atk2','_hit','_die1','_die2']

for species in ['skeleton','scrapgunner','bat']:
    paths=[BASE/species/f'unit_{species}_{direction}{act}.png' for direction in dirs for act in acts]
    missing=[str(p) for p in paths if not p.exists()]
    union=set(); bad=[]; rows=[]
    sheet=Image.new('RGBA',(8*96,5*96),(8,13,22,255))
    for i,p in enumerate(paths):
        if not p.exists(): continue
        im=Image.open(p).convert('RGBA')
        alpha={v[3] for v in im.getdata()}
        colors={v[:3] for v in im.getdata() if v[3]}
        union |= colors
        bbox=im.getchannel('A').getbbox()
        opaque87=[x for x in range(96) if im.getpixel((x,87))[3]]
        center=(min(opaque87)+max(opaque87))/2 if opaque87 else None
        ok=im.size==(96,96) and alpha<={0,255} and len(colors)<=40
        if not ok: bad.append((p.name,im.size,len(colors),sorted(alpha),bbox,center))
        sheet.alpha_composite(im,((i%8)*96,(i//8)*96))
    sheet.convert('RGB').save(OUT/f'{species}_contact.png')
    print(species,'count',len(paths)-len(missing),'missing',len(missing),'union_colors',len(union),'bad',len(bad))
    for b in bad[:10]: print(' ',b)
