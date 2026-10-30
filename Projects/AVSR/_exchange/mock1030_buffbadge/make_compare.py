# 비교 그림: 「지금 | 1 | 2 | 3」 — compare_medium_*.png 와 같은 꼴(칸 427x760, 머리글 60, 간격 20)
import sys
from PIL import Image, ImageDraw, ImageFont
D='C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/mock1030_buffbadge/'
F=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',34)
W,H,TOP,GAP=427,760,60,20
for k in ['infuse','ritual']:
    files=[('지금',f'now_badge_{k}.png')]+[(str(i),f'mock_badge_{k}_{i}.png') for i in (1,2,3)]
    c=Image.new('RGB',(W*4+GAP*3,TOP+H),(18,18,22)); d=ImageDraw.Draw(c)
    for n,(lab,f) in enumerate(files):
        x=n*(W+GAP)
        im=Image.open(D+f).convert('RGB').resize((W,H),Image.LANCZOS)
        c.paste(im,(x,TOP)); d.text((x+10,10),lab,font=F,fill=(255,214,90))
    c.save(D+f'compare_badge_{k}.png')
