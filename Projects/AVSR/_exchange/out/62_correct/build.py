from pathlib import Path
from PIL import Image

PROJ=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR")
BASE=Path(r"C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/crusher/unit_crusher_s.png")
IN=PROJ/"_exchange/in"; OUT=PROJ/"_exchange/out/62_correct"; OUT.mkdir(parents=True,exist_ok=True)
GEN=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d")
NEAR=Image.Resampling.NEAREST
s=Image.open(BASE).convert("RGBA"); allowed=list({c[:3] for c in s.getdata() if c[3]})
sb=s.getchannel("A").getbbox(); sw,sh=sb[2]-sb[0],sb[3]-sb[1]

def make(src,name,width):
 raw=Image.open(src).convert("RGBA"); a=raw.getchannel("A");
 # Neutral checker is display matte when alpha was flattened.
 rp=raw.load(); mask=Image.new("L",raw.size,0); mp=mask.load()
 for y in range(raw.height):
  for x in range(raw.width):
   r,g,b,aa=rp[x,y]; checker=abs(r-g)<8 and abs(g-b)<8 and r>220
   if aa>=128 and not checker: mp[x,y]=255
 box=mask.getbbox(); crop=raw.crop(box); cm=mask.crop(box); crop.putalpha(cm)
 crop=crop.resize((width,sh),NEAR)
 out=Image.new("RGBA",(256,256),(0,0,0,0)); x=(256-width)//2; y=233-sh; out.paste(crop,(x,y),crop)
 op=out.load()
 for yy in range(256):
  for xx in range(256):
   r,g,b,aa=op[xx,yy]
   if aa:
    c=min(allowed,key=lambda q:(q[0]-r)**2+(q[1]-g)**2+(q[2]-b)**2); op[xx,yy]=(*c,255)
 out.save(IN/name,optimize=True); return out

ne=make(GEN/"exec-35e33ab6-b719-48a5-a065-edb417263f6e.png","unit_crusher_ne.png",170)
n=Image.open(IN/"unit_crusher_n.png").convert("RGBA")

def ratios(im):
 pix=[c for c in im.getdata() if c[3]]; total=len(pix)
 orange=sum(1 for r,g,b,a in pix if r>g*1.35 and r>b*1.8)
 blue=sum(1 for r,g,b,a in pix if b>r*1.25 and g>r*1.18)
 return total,100*orange/total,100*blue/total

sheet=Image.new("RGBA",(768,256),(26,26,32,255)); sheet.alpha_composite(s,(0,0)); sheet.alpha_composite(ne,(256,0)); sheet.alpha_composite(n,(512,0)); sheet.convert("RGB").save(OUT/"preview.png")
for label,im in (("s",s),("ne",ne),("n",n)): print(label,im.getchannel("A").getbbox(),ratios(im),sorted(set(im.getchannel("A").getdata())))
