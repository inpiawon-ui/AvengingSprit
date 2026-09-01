from pathlib import Path
from PIL import Image

root=Path(r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in')
expected={
'hud_dpad_base.png':(120,120),'hud_dpad_knob.png':(46,46),
'hud_action_ultimate.png':(100,112),'hud_action_possess.png':(100,112),
'hud_host_frame.png':(112,120),'icon_seal.png':(24,24),
'fx_dash_1.png':(48,48),'fx_dash_2.png':(48,48),'fx_dash_3.png':(48,48)}
for name,size in expected.items():
    im=Image.open(root/name).convert('RGBA')
    colors={p[:3] for p in im.getdata() if p[3]}
    alpha={p[3] for p in im.getdata()}
    ok=im.size==size and len(colors)<=20 and alpha<={0,255}
    print(f'{name:29} {im.size!s:12} colors={len(colors):2} alpha={sorted(alpha)}  {"PASS" if ok else "FAIL"}')
