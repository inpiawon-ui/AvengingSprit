from pathlib import Path
from PIL import Image,ImageFilter

ROOT=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR"); IN=ROOT/"_exchange/in"; OUT=ROOT/"_exchange/out/62"; OUT.mkdir(parents=True,exist_ok=True)
GEN=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d"); NEAR=Image.Resampling.NEAREST
pal_img=Image.open(ROOT/"Reference/Palette/Bosses - Crusher.png").convert("RGB")
PALETTE=list({c for c in pal_img.getdata()})

def mapped(src,size):
 raw=Image.open(src).convert("RGBA").resize(size,NEAR); rp=raw.load(); out=Image.new("RGBA",size,(0,0,0,0)); op=out.load()
 for y in range(size[1]):
  for x in range(size[0]):
   r,g,b,a=rp[x,y]; checker=abs(r-g)<8 and abs(g-b)<8 and r>220
   if a<128 or checker: continue
   c=min(PALETTE,key=lambda q:(q[0]-r)**2+(q[1]-g)**2+(q[2]-b)**2); op[x,y]=(*c,255)
 return out

def align_contact(im,yline=232):
 b=im.getchannel("A").getbbox(); crop=im.crop(b); out=Image.new("RGBA",im.size,(0,0,0,0)); x=(im.width-crop.width)//2; y=yline-(crop.height-1); out.paste(crop,(x,y),crop); return out

# A directions.
ne=align_contact(mapped(GEN/"exec-53830186-3377-4d29-91f2-ac2921a90f8c.png",(256,256)))
n=align_contact(mapped(GEN/"exec-76bc234c-e2ec-4ecb-8e81-bfc75e3312ff.png",(256,256)))
ne.save(IN/"unit_crusher_ne.png",optimize=True); n.save(IN/"unit_crusher_n.png",optimize=True)

# B hands: extract latest interlaced source, shrink away from all edit-box edges,
# then remap relative luminance to the face's existing skin ramp.
base=Image.open(IN/"cut_start_1.png").convert("RGB")
src=Image.open(GEN/"exec-f74ff3f9-0e4a-4a4b-b431-9a4e99c63fb5.png").convert("RGB").resize((640,640),NEAR); sp=src.load()
mask=Image.new("L",(640,640),0); mp=mask.load()
for y in range(420,630):
 for x in range(220,440):
  r,g,b=sp[x,y]
  if r>75 and r>g*1.06 and g>b: mp[x,y]=255
mask=mask.filter(ImageFilter.MaxFilter(3)); box=mask.getbbox(); hand_rgb=src.crop(box); hand_mask=mask.crop(box)
hand_rgb=hand_rgb.resize((165,170),NEAR); hand_mask=hand_mask.resize((165,170),NEAR)
colors=[c for _,c in base.getcolors(1_000_000)]; skin=sorted([c for c in colors if c[0]>80 and c[0]-c[1]>18 and c[1]-c[2]>10],key=sum); outline=min(colors,key=sum)
hp=hand_rgb.load(); hm=hand_mask.load(); vals=[sum(hp[x,y]) for y in range(170) for x in range(165) if hm[x,y] and hp[x,y][0]>hp[x,y][1]*1.04]; lo,hi=min(vals),max(vals)
for y in range(170):
 for x in range(165):
  if not hm[x,y]: continue
  r,g,b=hp[x,y]
  if r>70 and r>g*1.04 and g>b:
   idx=min(len(skin)-1,(sum((r,g,b))-lo)*len(skin)//max(1,hi-lo+1)); hp[x,y]=skin[idx]
  else: hp[x,y]=outline
s10=base.copy(); s10.paste(hand_rgb,(250,438),hand_mask); s10.save(IN/"cut_start_10.png",optimize=True)

# C parts.
crane=mapped(GEN/"exec-0b6bb8f5-88b4-4fa5-880f-88097f0c7012.png",(512,320)); crane.save(IN/"obj_crusher_crane.png",optimize=True)
ball=mapped(GEN/"exec-67711488-fbb5-4450-8c9b-2afaca84d35d.png",(128,128)); ball.save(IN/"obj_crusher_ball.png",optimize=True)
chain=mapped(GEN/"exec-f3bd779f-1e32-4092-b463-b37e70ab7c1d.png",(32,64)); cp=chain.load()
for x in range(32): cp[x,63]=cp[x,0]
chain.save(IN/"obj_crusher_chain.png",optimize=True)
shield=mapped(GEN/"exec-6356453f-a0a5-4b75-b272-fd06e6f5cac3.png",(128,128)); shield.save(IN/"obj_crusher_shield.png",optimize=True)

# Five warning-lamp charge frames, extracted from the approved boss beacon.
s=Image.open(IN/"unit_crusher_s.png").convert("RGBA"); beacon=s.crop((105,8,151,54)).resize((40,40),NEAR)
orange=sorted([c for c in PALETTE if c[0]>c[1]*1.25 and c[0]>c[2]*1.8],key=sum)
for i in range(5):
 frame=Image.new("RGBA",(48,48),(0,0,0,0)); bp=beacon.load(); lamp=Image.new("RGBA",(40,40),(0,0,0,0)); lp=lamp.load()
 for y in range(40):
  for x in range(40):
   r,g,b,a=bp[x,y]
   if not a: continue
   if r>g*1.2 and r>b*1.7 and orange: lp[x,y]=(*orange[min(len(orange)-1,(i+1)*len(orange)//5-1)],255)
   else: lp[x,y]=(r,g,b,255)
 frame.paste(lamp,(4,4),lamp); frame.save(IN/f"fx_crusher_lamp_{i+1}.png",optimize=True)

# Contact sheet.
items=["unit_crusher_ne.png","unit_crusher_n.png","cut_start_10.png","obj_crusher_crane.png","obj_crusher_ball.png","obj_crusher_chain.png","obj_crusher_shield.png",*[f"fx_crusher_lamp_{i}.png" for i in range(1,6)]]
sheet=Image.new("RGB",(1024,768),(22,22,28)); x=y=0
for name in items:
 im=Image.open(IN/name); bg=Image.new("RGBA",im.size,(35,35,42,255)); bg.alpha_composite(im.convert("RGBA")); thumb=bg.convert("RGB"); thumb.thumbnail((240,220),NEAR); sheet.paste(thumb,(x,y)); x+=256
 if x>=1024: x=0;y+=256
sheet.save(OUT/"preview.png")
for name in items:
 im=Image.open(IN/name); av=sorted(set(im.getchannel("A").getdata())) if im.mode=="RGBA" else []; print(name,im.size,im.mode,av)
