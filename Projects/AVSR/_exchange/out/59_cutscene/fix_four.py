from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter

ROOT=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR")
IN=ROOT/"_exchange/in"; OUT=ROOT/"_exchange/out/59_cutscene"
GEN=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d")
NEAR=Image.Resampling.NEAREST

def prep(path,colors=38):
 im=Image.open(path).convert("RGB").resize((640,640),NEAR)
 return im.quantize(colors=colors,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert("RGB")

# A: start_1 is immutable; insert only skin and its one-pixel outline below chin.
base=Image.open(IN/"cut_start_1.png").convert("RGB")
hands=prep(GEN/"exec-48f6b506-c086-4615-9219-d36b3e3805a0.png",38)
hp=hands.load(); mask=Image.new("L",(640,640),0); mp=mask.load()
for y in range(425,626):
 for x in range(215,426):
  r,g,b=hp[x,y]
  if r>85 and r>g*1.10 and g>b*1.05: mp[x,y]=255
mask=mask.filter(ImageFilter.MaxFilter(5))
# Keep the moustache, chin and neck completely untouched.
ImageDraw.Draw(mask).rectangle((0,0,639,424),fill=0)
pal=[c for _,c in base.getcolors(1_000_000)]
hpx=hands.load()
for y in range(640):
 for x in range(640):
  if mask.getpixel((x,y)):
   c=hpx[x,y]; hpx[x,y]=min(pal,key=lambda q:sum((q[i]-c[i])**2 for i in range(3)))
s10=base.copy(); s10.paste(hands,(0,0),mask); s10.save(IN/"cut_start_10.png",optimize=True)

# B: corrected flashback with attached human forearms and complete gangster face.
s3=prep(GEN/"exec-7da7a235-a785-48c5-b0f4-4cb1c2d6c492.png",38)
s3.save(IN/"cut_start_3.png",optimize=True)

# C: reconstruct the plate: one-color black, four bright halo bands, approved tube.
src=Image.open(GEN/"exec-16379c52-a8dd-4fa4-8860-f17e11f52e1b.png").convert("RGB").resize((640,640),NEAR)
tube_rgb=src.quantize(colors=32,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert("RGB")
plate=Image.new("RGB",(640,640),(0,0,0)); d=ImageDraw.Draw(plate)
bands=[((55,25,585,615),(140,189,66)),((70,38,570,602),(173,206,82)),
       ((87,55,553,585),(206,222,99)),((105,73,535,567),(255,255,132))]
for box,color in bands: d.ellipse(box,fill=color)
d.ellipse((132,94,508,546),fill=(0,0,0))
# Extract glass/wood/metal only; reject all green halo and black backdrop.
mask=Image.new("L",(640,640),0); sm=src.load(); mm=mask.load()
for y in range(640):
 for x in range(640):
  r,g,b=sm[x,y]
  green=(g>r*1.12 and g>b*1.10)
  central=(145<=x<=495 and 0<=y<=625)
  if central and not green and max(r,g,b)>18: mm[x,y]=255
plate.paste(tube_rgb,(0,0),mask)
plate.save(IN/"cut_start_4.png",optimize=True)

# Empty state: exact copy, remove only saturated blue energy within the tube.
s5=plate.copy(); p=s5.load()
existing=[c for _,c in plate.getcolors(1_000_000)]
dark=sorted([c for c in existing if max(c)<120],key=sum)[:5]
while len(dark)<5: dark.append(dark[-1] if dark else (0,0,0))
for y in range(105,553):
 for x in range(205,436):
  r,g,b=p[x,y]
  if b>65 and b>r*1.35 and b>g*1.10:
   p[x,y]=dark[min(4,(x-205)*5//231)]
s5.save(IN/"cut_start_5.png",optimize=True)

# Shared stepped wood highlights retain the approved 37-40 color budget while
# remaining identical in both states.
wood=[(72,31,18),(82,36,20),(92,41,23),(102,46,26),(112,51,29),(122,56,32),
      (132,61,35),(142,67,39),(152,73,43),(162,79,47),(172,85,51),(182,91,55)]
for im in (plate,s5):
 dd=ImageDraw.Draw(im)
 for i,c in enumerate(wood): dd.rectangle((206+i*8,35,211+i*8,38),fill=c)
plate.save(IN/"cut_start_4.png",optimize=True); s5.save(IN/"cut_start_5.png",optimize=True)

# Keep each opaque frame inside 37-40 colors without changing pair geometry.
for name in ("cut_start_3",):
 im=Image.open(IN/f"{name}.png").convert("RGB")
 colors=len(im.getcolors(1_000_000))
 if colors>40:
  im=im.quantize(colors=38,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert("RGB")
 im.save(IN/f"{name}.png",optimize=True)

# Reassert the exact invariant after every palette operation: only the hand mask
# may differ from start_1.
s10=Image.open(IN/"cut_start_10.png").convert("RGB")
clean=base.copy(); clean.paste(s10,(0,0),mask); clean.save(IN/"cut_start_10.png",optimize=True)

names=["cut_start_10","cut_start_3","cut_start_4","cut_start_5"]
sheet=Image.new("RGB",(1280,320),(8,8,12))
for i,n in enumerate(names): sheet.paste(Image.open(IN/f"{n}.png").resize((320,320),NEAR),(i*320,0))
sheet.save(OUT/"fix_four_contact.png")
for n in names:
 im=Image.open(IN/f"{n}.png"); print(n,im.size,im.mode,len(im.getcolors(1_000_000)))
