from pathlib import Path
from PIL import Image, ImageDraw

root=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d")
names=[
"exec-44371816-bdce-4c83-a783-48d7c7867b21.png",
"exec-fcc4ecad-9c94-4edb-8903-dc1ef5181924.png",
"exec-5143e52e-be8b-44cd-8561-f1882b01d293.png",
"exec-60d69191-e344-4a9b-a2be-f1acfd8f7d6a.png",
"exec-dc35535d-041c-4dab-acbf-6de41117ff81.png"]
sheet=Image.new("RGB",(5*300,330),(30,30,35)); d=ImageDraw.Draw(sheet)
for i,n in enumerate(names):
 im=Image.open(root/n).convert("RGBA"); bg=Image.new("RGBA",im.size,(30,30,35,255)); bg.alpha_composite(im)
 bg.thumbnail((280,280),Image.Resampling.NEAREST); sheet.paste(bg.convert("RGB"),(i*300+10,30)); d.text((i*300+10,8),n[5:13],fill="white")
sheet.save(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/out/64_crusher/n_generated.png")
