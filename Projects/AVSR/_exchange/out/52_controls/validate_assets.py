from pathlib import Path
from PIL import Image
p=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in')
for n,size in [('hud_control_grid.png',(720,230)),('hud_action_frame.png',(224,150))]:
    im=Image.open(p/n).convert('RGBA')
    colors={x[:3] for x in im.getdata() if x[3]}
    alpha={x[3] for x in im.getdata()}
    print(n,im.size,'colors',len(colors),'alpha',sorted(alpha),'PASS' if im.size==size and len(colors)<=12 and alpha<={0,255} else 'FAIL')
