from pathlib import Path
from PIL import Image, ImageDraw

ROOT=Path(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR")
IN=ROOT/"_exchange/in"
OUT=ROOT/"_exchange/out/59_cutscene"
base=Image.open(IN/"cut_prologue_7.png").convert("RGB")
navy=(7,16,78); red=(239,8,8)

def save(im,n):
    assert im.size==(640,640) and im.mode=="RGB"
    im.save(IN/f"cut_prologue_{n}.png",optimize=True)

# 5: exact approved frame, ballistic trace removed.
f5=base.copy(); d=ImageDraw.Draw(f5)
d.rectangle((512,326,639,354),fill=navy)
save(f5,5)

# 6: same fixed aiming pose, one small muzzle flash only (38 px wide).
f6=f5.copy(); d=ImageDraw.Draw(f6)
d.polygon([(512,331),(520,335),(532,327),(528,337),(549,340),(528,344),(534,353),(520,347),(512,351)],fill=(239,74,8))
d.polygon([(512,336),(522,339),(540,340),(523,344),(512,347)],fill=(255,190,42))
d.rectangle((512,339,521,344),fill=(255,244,179))
save(f6,6)

# 7 remains the approved source and is deliberately not rewritten.

palette=[rgb for _,rgb in base.getcolors(1_000_000)]
def nearest(rgb):
    if rgb==red: return red
    r,g,b=rgb
    return min(palette,key=lambda p:(p[0]-r)**2+(p[1]-g)**2+(p[2]-b)**2)

def prepared(path):
    src=Image.open(path).convert("RGB").resize((640,640),Image.Resampling.NEAREST)
    px=src.load()
    # Connected dark-blue background becomes the exact approved navy.
    def bg(c):
        r,g,b=c; return b>=70 and b-r>=32 and b-g>=25 and r<85 and g<110
    stack=[]; seen=set()
    for x in range(640): stack.extend(((x,0),(x,639)))
    for y in range(640): stack.extend(((0,y),(639,y)))
    while stack:
        x,y=stack.pop()
        if (x,y) in seen or not(0<=x<640 and 0<=y<640) or not bg(px[x,y]): continue
        seen.add((x,y)); px[x,y]=navy
        stack.extend(((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
    # Lock imported arm colors to the already-approved palette.
    cache={}
    for y in range(640):
        for x in range(640):
            c=px[x,y]
            if c==navy: continue
            if c not in cache: cache[c]=nearest(c)
            px[x,y]=cache[c]
    return src

src8=prepared(Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d/exec-eb3b58f7-ca3c-4375-9b9c-b1c238b89a79.png"))
src9=prepared(Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d/exec-12e7dfd0-37a2-4949-b90e-a78232f2c640.png"))

def composite_arm(src,polygon,n):
    dst=base.copy()
    mask=Image.new("1",(640,640),0); ImageDraw.Draw(mask).polygon(polygon,fill=1)
    # Clear the approved horizontal firing arm/gun only inside the editable area.
    dst.paste(navy,(0,0,640,640),mask)
    sp=src.load(); arm=Image.new("RGB",(640,640),navy); ap=arm.load(); mp=mask.load()
    for y in range(640):
        for x in range(640):
            if mp[x,y] and sp[x,y]!=navy: ap[x,y]=sp[x,y]
    # Navy is transparent for this local compositing step.
    alpha=Image.new("1",(640,640),0); aa=alpha.load()
    for y in range(640):
        for x in range(640):
            if mp[x,y] and ap[x,y]!=navy: aa[x,y]=1
    dst.paste(arm,(0,0),alpha)
    # Remove disconnected remnants below the raised recoil arm.
    ImageDraw.Draw(dst).rectangle((350,360,560,460),fill=navy)
    save(dst,n)

def rotate_approved_arm(angle,n):
    dst=base.copy()
    # Exact arm/hand/gun pixels from frame 7. The shoulder pivot remains fixed.
    poly=[(178,291),(232,278),(297,305),(383,315),(520,315),(526,455),(345,455),(272,417),(208,395),(176,354)]
    mask=Image.new("L",(640,640),0); ImageDraw.Draw(mask).polygon(poly,fill=255)
    arm=Image.new("RGBA",(640,640),(0,0,0,0)); arm.paste(base.convert("RGBA"),(0,0),mask)
    ImageDraw.Draw(dst).polygon(poly,fill=navy)
    # The horizontal arm hid this part of the coat. Fill the newly revealed
    # torso with existing approved coat colors before laying the raised arm on top.
    under=ImageDraw.Draw(dst)
    coat_dark=nearest((22,28,83)); coat_mid=nearest((62,66,126)); coat_hi=nearest((104,105,165))
    under.polygon([(176,292),(229,278),(286,294),(334,329),(351,382),(336,432),(281,451),(220,415),(179,366)],fill=coat_dark)
    under.polygon([(184,302),(228,289),(275,302),(313,331),(325,374),(314,407),(274,424),(226,397),(190,356)],fill=coat_mid)
    under.line([(198,317),(242,307),(281,322),(303,347)],fill=coat_hi,width=5)
    turned=arm.rotate(angle,resample=Image.Resampling.NEAREST,center=(190,340),expand=False)
    dst.paste(turned.convert("RGB"),(0,0),turned.getchannel("A"))
    # Clear the original cuff tip that sits outside the extracted arm polygon.
    ImageDraw.Draw(dst).rectangle((350,360,402,418),fill=navy)
    ImageDraw.Draw(dst).rectangle((300,430,375,500),fill=navy)
    # Every recoil frame owns its dots; remove the old horizontal trace first.
    ImageDraw.Draw(dst).rectangle((512,326,639,354),fill=navy)
    dd=ImageDraw.Draw(dst)
    if n==8:
        dd.rectangle((477,124,482,128),fill=red); dd.rectangle((489,115,493,119),fill=red)
    else:
        dd.rectangle((389,73,393,77),fill=red); dd.rectangle((390,61,393,64),fill=red)
    save(dst,n)

rotate_approved_arm(45,8)
rotate_approved_arm(80,9)

# Frame 9 begins from the approved frame 8. Replace the union of its 45-degree
# arm area and the new 80-degree arm area; source navy cleanly removes the old arm.
f8=Image.open(IN/"cut_prologue_8.png").convert("RGB")
f9=f8.copy(); editable=Image.new("L",(640,640),0); ed=ImageDraw.Draw(editable)
ed.polygon([(170,321),(196,278),(270,207),(326,76),(395,24),(472,24),(548,83),(560,194),(538,306),(478,390),(385,438),(278,447),(202,391)],fill=255)
f9.paste(src9,(0,0),editable)
# Absolute protection for hat, glasses, face, and non-arm coat regions.
f9.paste(f5.crop((0,0,250,330)),(0,0))
f9.paste(f5.crop((0,320,170,640)),(0,320))
f9.paste(f5.crop((170,455,640,640)),(170,455))
# Original horizontal trace is never present in frame 9.
ImageDraw.Draw(f9).rectangle((512,326,639,354),fill=navy)
ImageDraw.Draw(f9).rectangle((389,73,393,77),fill=red)
ImageDraw.Draw(f9).rectangle((390,61,393,64),fill=red)
ImageDraw.Draw(f9).rectangle((300,430,375,454),fill=navy)
save(f9,9)

# Contact sheet and compact QA.
sheet=Image.new("RGB",(1000,200),navy)
for i,n in enumerate(range(5,10)):
    im=Image.open(IN/f"cut_prologue_{n}.png").convert("RGB")
    sheet.paste(im.resize((200,200),Image.Resampling.NEAREST),(i*200,0))
sheet.save(OUT/"prologue_5_9_contact.png")
for n in range(5,10):
    im=Image.open(IN/f"cut_prologue_{n}.png").convert("RGB")
    print(n,im.size,len(im.getcolors(1_000_000)),im.getpixel((0,0)))
