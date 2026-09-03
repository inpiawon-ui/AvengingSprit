from pathlib import Path
from PIL import Image

GEN=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d")
BASE=Path(r"C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/crusher")
DEST=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in")
OUT=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/out/64_crusher")
DEST.mkdir(parents=True,exist_ok=True); OUT.mkdir(parents=True,exist_ok=True)

sources={
 ('ne','atk1'):'exec-0b96c4c0-3814-440d-9fe6-b563e9ceca5a.png',
 ('ne','atk2'):'exec-34970efe-fd9b-426b-9a41-a6eb40bf7d5c.png',
 ('ne','move1'):'exec-3eb73cf6-41e2-4ee4-87f4-bdab62dbd9a6.png',
 ('ne','move2'):'exec-b9b901c8-4f63-418e-8bed-a90ff6899b00.png',
 ('ne','hit'):'exec-e39a0c7b-4605-4b07-a712-a2697ac6ee32.png',
 ('n','atk1'):'exec-44371816-bdce-4c83-a783-48d7c7867b21.png',
 ('n','atk2'):'exec-fcc4ecad-9c94-4edb-8903-dc1ef5181924.png',
 ('n','move1'):'exec-5143e52e-be8b-44cd-8561-f1882b01d293.png',
 ('n','move2'):'exec-60d69191-e344-4a9b-a2be-f1acfd8f7d6a.png',
 ('n','hit'):'exec-dc35535d-041c-4dab-acbf-6de41117ff81.png',
}

def finish(direction, action, src_name):
    raw=Image.open(GEN/src_name).convert('RGBA')
    if action == 'hit':
        idle=Image.open(BASE/f'unit_crusher_{direction}.png').convert('RGBA')
        ref=Image.open(BASE/f'unit_crusher_s_{action}.png').convert('RGBA')
        hit_colors=[p[:3] for p in ref.getdata() if p[3]]
        white=max(hit_colors,key=lambda c:sum(c))
        out=Image.new('RGBA',(256,256),(0,0,0,0))
        mask=idle.getchannel('A').point(lambda v:255 if v>=128 else 0)
        out.paste((*white,255),(0,0,256,256),mask)
        name=f'unit_crusher_{direction}_{action}.png'
        out.save(DEST/name,optimize=True)
        return out
    ap=raw.getchannel('A'); alpha=Image.new('L',raw.size,0)
    rp=raw.load(); mp=alpha.load()
    for yy in range(raw.height):
        for xx in range(raw.width):
            r,g,b,a=rp[xx,yy]
            checker=(max(r,g,b)-min(r,g,b)<=7 and min(r,g,b)>=170)
            if a>=128 and not checker: mp[xx,yy]=255
    box=alpha.getbbox()
    crop=raw.crop(box); crop.putalpha(alpha.crop(box))
    ref=Image.open(BASE/f'unit_crusher_s_{action}.png').convert('RGBA')
    rb=ref.getchannel('A').getbbox(); target_h=rb[3]-rb[1]
    scale=target_h/crop.height
    target_w=max(1,round(crop.width*scale))
    crop=crop.resize((target_w,target_h),Image.Resampling.NEAREST)
    out=Image.new('RGBA',(256,256),(0,0,0,0))
    out.alpha_composite(crop,((256-target_w)//2,233-target_h))
    allowed=list({p[:3] for p in ref.getdata() if p[3]})
    px=out.load()
    for y in range(256):
        for x in range(256):
            r,g,b,a=px[x,y]
            if a:
                q=min(allowed,key=lambda c:(c[0]-r)**2+(c[1]-g)**2+(c[2]-b)**2)
                px[x,y]=(*q,255)
            else: px[x,y]=(0,0,0,0)
    name=f'unit_crusher_{direction}_{action}.png'
    out.save(DEST/name,optimize=True)
    return out

frames={k:finish(*k,v) for k,v in sources.items()}
preview=Image.new('RGBA',(5*256,2*256),(24,24,30,255))
for row,d in enumerate(('ne','n')):
    for col,a in enumerate(('atk1','atk2','move1','move2','hit')):
        preview.alpha_composite(frames[(d,a)],(col*256,row*256))
preview.save(OUT/'crusher_10_preview.png')

for (d,a),im in frames.items():
    print(f'{d}_{a}',im.size,im.getchannel('A').getbbox(),sorted(set(im.getchannel('A').getdata())))
