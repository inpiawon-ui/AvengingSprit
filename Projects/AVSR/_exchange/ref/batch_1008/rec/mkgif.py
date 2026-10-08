import glob, sys
from PIL import Image
def gif(name, box=(0,230,720,1230), w=360, step=1, dur=66):
    fs = sorted(glob.glob(f'{name}/f_*.png'))[::step]
    fr = [Image.open(f).convert('RGB').crop(box).resize((w, int(w*(box[3]-box[1])/(box[2]-box[0]))), Image.LANCZOS) for f in fs]
    q = [f.quantize(128) for f in fr]
    q[0].save(f'{name}.gif', save_all=True, append_images=q[1:], duration=dur, loop=0)
    pick = fr[::max(1, len(fr)//8)][:8]
    s = Image.new('RGB', (pick[0].width//2*8, pick[0].height//2))
    for i,p in enumerate(pick): s.paste(p.resize((p.width//2,p.height//2)), (i*(p.width//2),0))
    s.save(f'{name}_frames.png'); print(name, len(fr))
for n in sys.argv[1:]: gif(n)
