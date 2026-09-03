from pathlib import Path
from PIL import Image,ImageDraw
root=Path(r"C:/Users/inpia/.codex/generated_images/01a03196-5880-70f2-82bb-e4a9f3a7276d")
names=['exec-2481bd06-c666-4c4e-8a12-56c652e31218.png','exec-b2780ba7-2c1c-4868-a2d0-d429ba01f48c.png','exec-b03cbeca-67a4-49c1-bf0f-963a7d24928c.png','exec-e3e8351f-42d2-4202-b270-558ceefc2885.png','exec-72cc3d55-a5b0-4af8-accc-273de64a2492.png','exec-522ab37e-b8d8-48d3-ae2d-888577248284.png','exec-eb2c8164-ec68-4d7f-81f0-6af79596c762.png','exec-36eebf4f-51be-4621-a0d5-047671ab0c72.png']
sheet=Image.new('RGB',(4*300,2*320),(28,28,34)); d=ImageDraw.Draw(sheet)
for i,n in enumerate(names):
 im=Image.open(root/n).convert('RGBA'); bg=Image.new('RGBA',im.size,(28,28,34,255)); bg.alpha_composite(im); bg.thumbnail((280,280),Image.Resampling.NEAREST)
 x=(i%4)*300+10;y=(i//4)*320+28;sheet.paste(bg.convert('RGB'),(x,y));d.text((x,8+(i//4)*320),n[5:13],fill='white')
sheet.save(r"C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/out/64_crusher/death_generated.png")
