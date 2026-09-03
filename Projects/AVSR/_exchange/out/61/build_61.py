from pathlib import Path
from PIL import Image,ImageFilter,ImageDraw

ROOT=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR")
IN=ROOT/"_exchange/in"; OUT=ROOT/"_exchange/out/61"; OUT.mkdir(parents=True,exist_ok=True)
GEN=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d")
NEAR=Image.Resampling.NEAREST

# A. Exact cut_start_1 base plus hand pixels only.
base=Image.open(IN/"cut_start_1.png").convert("RGB")
src=Image.open(GEN/"exec-f74ff3f9-0e4a-4a4b-b431-9a4e99c63fb5.png").convert("RGB").resize((640,640),NEAR)
sp=src.load(); mask=Image.new("L",(640,640),0); mp=mask.load()
for y in range(426,615):
 for x in range(238,427):
  r,g,b=sp[x,y]
  if r>82 and r>g*1.08 and g>b*1.03: mp[x,y]=255
mask=mask.filter(ImageFilter.MaxFilter(5))
d=ImageDraw.Draw(mask); d.rectangle((0,0,237,639),fill=0); d.rectangle((427,0,639,639),fill=0); d.rectangle((0,0,639,425),fill=0); d.rectangle((0,615,639,639),fill=0)

base_colors=[c for _,c in base.getcolors(1_000_000)]
skin=sorted([c for c in base_colors if c[0]>80 and c[0]>c[1]*1.08 and c[1]>c[2]*1.03],key=sum)
skin=skin[-6:] if len(skin)>=6 else skin
outline=min(base_colors,key=lambda c:sum(c))
hands=Image.new("RGB",(640,640),(0,0,0)); hp=hands.load()
for y in range(426,615):
 for x in range(238,427):
  if not mask.getpixel((x,y)): continue
  r,g,b=sp[x,y]
  if r>75 and r>g*1.05 and g>b:
   lum=(299*r+587*g+114*b)//1000
   hp[x,y]=skin[min(len(skin)-1,lum*len(skin)//256)]
  else: hp[x,y]=outline
s10=base.copy(); s10.paste(hands,(0,0),mask); s10.save(IN/"cut_start_10.png",optimize=True)

# C. Crusher sample: 256x256 RGBA, binary alpha, palette locked to reference.
raw=Image.open(GEN/"exec-cd0d80b0-1ec2-42e9-b447-4afff5cea15a.png").convert("RGBA").resize((256,256),NEAR)
pal_img=Image.open(ROOT/"Reference/Palette/Bosses - Crusher.png").convert("RGB")
palette=list({c for c in pal_img.getdata()})
# Beige reference-sheet backgrounds are not machine colors.
palette=[c for c in palette if not (c[0]>145 and c[1]>125 and c[2]>95 and abs(c[0]-c[1])<55)]
rp=raw.load(); out=Image.new("RGBA",(256,256),(0,0,0,0)); op=out.load()
for y in range(256):
 for x in range(256):
  r,g,b,a=rp[x,y]
  # Built-in output may encode the displayed checker in RGB; treat neutral near-white as matte.
  checker=(abs(r-g)<8 and abs(g-b)<8 and r>220)
  if a<128 or checker: continue
  c=min(palette,key=lambda q:(q[0]-r)**2+(q[1]-g)**2+(q[2]-b)**2)
  op[x,y]=(*c,255)
out.save(IN/"unit_crusher_s.png",optimize=True)

# Preview.
sheet=Image.new("RGB",(896,640),(18,18,22)); sheet.paste(s10,(0,0))
preview=Image.new("RGBA",(256,256),(32,32,38,255)); preview.alpha_composite(out)
sheet.paste(preview.convert("RGB").resize((256,256),NEAR),(640,192))
sheet.save(OUT/"preview.png")

for n in ("cut_start_10.png","unit_crusher_s.png"):
 im=Image.open(IN/n); alpha=sorted(set(im.getchannel("A").getdata())) if im.mode=="RGBA" else []
 print(n,im.size,im.mode,len(im.convert("RGB").getcolors(1_000_000)),alpha)
