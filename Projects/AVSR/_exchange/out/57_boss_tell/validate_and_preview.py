from pathlib import Path
from PIL import Image

ROOT=Path(r'C:\won\UnityProject\AvengingSprit'); IN=ROOT/'Projects/AVSR/_exchange/in'; OUT=Path(__file__).parent
spec={'fx_danger_hatch.png':(64,64),'fx_safe_hatch.png':(64,64),'ui_dodge_arrow.png':(48,48)}
for n,size in spec.items():
    im=Image.open(IN/n).convert('RGBA'); alpha={p[3] for p in im.getdata()}
    print(n,im.size,sorted(alpha),'PASS' if im.size==size and alpha<={0,255} else 'FAIL')

# 3x3 tiling proof plus arrow comparison.
canvas=Image.new('RGBA',(520,320),(8,13,22,255))
for col,n in enumerate(('fx_danger_hatch.png','fx_safe_hatch.png')):
    tile=Image.open(IN/n).convert('RGBA')
    for yy in range(3):
        for xx in range(3): canvas.alpha_composite(tile,(20+col*220+xx*64,20+yy*64))
old=Image.open(ROOT/'Assets/BaseResource/InGameMainUI/possessmark_arrow.png').convert('RGBA').resize((96,96),Image.Resampling.NEAREST)
new=Image.open(IN/'ui_dodge_arrow.png').convert('RGBA').resize((96,96),Image.Resampling.NEAREST)
canvas.alpha_composite(old,(20,208)); canvas.alpha_composite(new,(140,208))
canvas.convert('RGB').save(OUT/'boss_tell_preview.png')
