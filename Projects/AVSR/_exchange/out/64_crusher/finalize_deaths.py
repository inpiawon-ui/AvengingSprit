from pathlib import Path
from PIL import Image

GEN=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d")
BASE=Path(r"C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/crusher")
DEST=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in")
OUT=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/out/64_crusher")

sources={
 ('se','die1'):'exec-2481bd06-c666-4c4e-8a12-56c652e31218.png',
 ('se','die2'):'exec-b2780ba7-2c1c-4868-a2d0-d429ba01f48c.png',
 ('e','die1'):'exec-b03cbeca-67a4-49c1-bf0f-963a7d24928c.png',
 ('e','die2'):'exec-e3e8351f-42d2-4202-b270-558ceefc2885.png',
 ('ne','die1'):'exec-72cc3d55-a5b0-4af8-accc-273de64a2492.png',
 ('ne','die2'):'exec-522ab37e-b8d8-48d3-ae2d-888577248284.png',
 ('n','die1'):'exec-eb2c8164-ec68-4d7f-81f0-6af79596c762.png',
 ('n','die2'):'exec-36eebf4f-51be-4621-a0d5-047671ab0c72.png',
}

def finish(direction,stage,src_name):
 raw=Image.open(GEN/src_name).convert('RGBA'); mask=Image.new('L',raw.size,0)
 rp=raw.load(); mp=mask.load()
 for y in range(raw.height):
  for x in range(raw.width):
   r,g,b,a=rp[x,y]
   checker=(max(r,g,b)-min(r,g,b)<=7 and min(r,g,b)>=170)
   if a>=128 and not checker: mp[x,y]=255
 box=mask.getbbox(); crop=raw.crop(box); crop.putalpha(mask.crop(box))
 ref=Image.open(BASE/f'unit_crusher_s_{stage}.png').convert('RGBA')
 rb=ref.getchannel('A').getbbox(); target_h=rb[3]-rb[1]
 scale=target_h/crop.height; target_w=min(rb[2]-rb[0],max(1,round(crop.width*scale)))
 crop=crop.resize((target_w,target_h),Image.Resampling.NEAREST)
 out=Image.new('RGBA',(256,256),(0,0,0,0)); out.alpha_composite(crop,((256-target_w)//2,233-target_h))
 allowed=list({p[:3] for p in ref.getdata() if p[3]}); px=out.load()
 for y in range(256):
  for x in range(256):
   r,g,b,a=px[x,y]
   if a:
    q=min(allowed,key=lambda c:(c[0]-r)**2+(c[1]-g)**2+(c[2]-b)**2); px[x,y]=(*q,255)
   else: px[x,y]=(0,0,0,0)
 out.save(DEST/f'unit_crusher_{direction}_{stage}.png',optimize=True)
 return out

frames={k:finish(*k,v) for k,v in sources.items()}
sheet=Image.new('RGBA',(4*256,2*256),(24,24,30,255))
for c,d in enumerate(('se','e','ne','n')):
 for r,st in enumerate(('die1','die2')): sheet.alpha_composite(frames[(d,st)],(c*256,r*256))
sheet.save(OUT/'crusher_death_8_preview.png')
for k,im in frames.items(): print(k,im.size,im.getchannel('A').getbbox(),sorted(set(im.getchannel('A').getdata())))
