from pathlib import Path
from collections import deque
from PIL import Image, ImageOps

ROOT=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR")
IN=ROOT/"_exchange/in"; OUT=ROOT/"_exchange/out/59_cutscene"
GEN=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d")
PY=Image.Resampling.NEAREST

def prep(path, colors=38):
    im=Image.open(path).convert("RGB").resize((640,640),PY)
    return im.quantize(colors=colors,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert("RGB")

# A: preserve each approved doctor frame, replacing only large connected chroma fields.
lab=prep(GEN/"exec-59f792cd-3287-4457-9b01-40f958b9d819.png",38)
def remove_chroma(name):
    base=Image.open(IN/f"{name}.png").convert("RGB"); bp=base.load(); lp=lab.load()
    cand=lambda c: c[1]>=45 and c[1]>c[0]*2.2 and c[1]>c[2]*1.8
    seen=bytearray(640*640); comps=[]
    for y in range(640):
      for x in range(640):
        i=y*640+x
        if seen[i] or not cand(bp[x,y]): continue
        q=deque([(x,y)]); seen[i]=1; comp=[]
        while q:
          a,b=q.popleft(); comp.append((a,b))
          for nx,ny in ((a-1,b),(a+1,b),(a,b-1),(a,b+1)):
            j=ny*640+nx if 0<=nx<640 and 0<=ny<640 else -1
            if j>=0 and not seen[j] and cand(bp[nx,ny]): seen[j]=1; q.append((nx,ny))
        if len(comp)>=120: comps.append(comp)
    for comp in comps:
      for x,y in comp: bp[x,y]=lp[x,y]
    base=base.quantize(colors=38,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert("RGB")
    base.save(IN/f"{name}.png",optimize=True)
    return sum(len(c) for c in comps)

changed1=remove_chroma("cut_start_1")
changed10=remove_chroma("cut_start_10")

# B: authoritative sepia kidnapping flashback.
s3=prep(GEN/"exec-e38fe4a2-8bc9-42db-9c97-9a0799e1623e.png",38)
s3.save(IN/"cut_start_3.png",optimize=True)

# C: black plate + blue tube, then same geometry with interior recolored using
# green hues already present in the halo, so the pair remains exactly 38 colors.
s4=prep(GEN/"exec-16379c52-a8dd-4fa4-8860-f17e11f52e1b.png",38)
s4.save(IN/"cut_start_4.png",optimize=True)
greens=[(18,92,38),(32,130,52),(52,170,70),(82,214,98),(132,250,148)]
s5=s4.copy(); p=s5.load()
for y in range(80,570):
  for x in range(210,431):
    r,g,b=p[x,y]
    if b>80 and b>r*1.25 and b>g*1.05 and greens:
      p[x,y]=greens[((x//24)+(y//32))%len(greens)]
s5.save(IN/"cut_start_5.png",optimize=True)

# Transparent ghost overlay. Black surroundings are extraction matte only.
raw=Image.open(GEN/"exec-253afad4-f1a1-4b3b-a9bf-04f987c13120.png").convert("RGB").resize((640,640),PY)
rgba=Image.new("RGBA",(640,640),(0,0,0,0)); rp=raw.load(); op=rgba.load()
for y in range(640):
  for x in range(640):
    r,g,b=rp[x,y]
    if max(r,g,b)>28 and (r+g+b)>80: op[x,y]=(r,g,b,255)

# Normalize visible colors and derive four same-bbox poses.
alpha=rgba.getchannel("A"); rgb=rgba.convert("RGB").quantize(colors=12,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert("RGB")
ghost=Image.merge("RGBA",(*rgb.split(),alpha))
box=alpha.getbbox(); crop=ghost.crop(box); w,h=crop.size; center=(320,330); dst=(center[0]-w//2,center[1]-h//2)
poses=[crop,ImageOps.mirror(crop),crop.rotate(4,resample=PY,expand=False),ImageOps.mirror(crop).rotate(-4,resample=PY,expand=False)]
for i,pose in enumerate(poses,6):
  pb=pose.getchannel("A").getbbox(); visible=pose.crop(pb).resize((w,h),PY)
  frame=Image.new("RGBA",(640,640),(0,0,0,0)); frame.paste(visible,dst,visible); frame.save(IN/f"cut_start_{i}.png",optimize=True)

# Contact sheet and numeric checks.
names=["cut_start_1","cut_start_10","cut_start_3","cut_start_4","cut_start_5","cut_start_6","cut_start_7","cut_start_8","cut_start_9"]
sheet=Image.new("RGB",(960,960),(8,10,16))
for i,n in enumerate(names):
  im=Image.open(IN/f"{n}.png"); thumb=Image.new("RGB",im.size,(10,10,12));
  if im.mode=="RGBA": thumb.paste(im,mask=im.getchannel("A"))
  else: thumb=im.convert("RGB")
  sheet.paste(thumb.resize((320,320),PY),((i%3)*320,(i//3)*320))
sheet.save(OUT/"corrections_contact.png")
print("chroma replaced",changed1,changed10)
for n in names:
  im=Image.open(IN/f"{n}.png"); alpha_vals=sorted(set(im.getchannel("A").getdata())) if im.mode=="RGBA" else []
  colors=len(im.convert("RGB").getcolors(1_000_000)); print(n,im.size,im.mode,colors,alpha_vals)
