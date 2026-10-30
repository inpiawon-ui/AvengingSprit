# 강신 의식 1번 시안 — 부품만 시안 배치(× 0.75)대로 게임 화면에 겹친 합성 | 시안, 나란히(2026-10-30)
import glob
from PIL import Image, ImageDraw, ImageFont
X = 'Projects/AVSR/_exchange'
G = f'{X}/fx_medium1030_r1/game'
base = Image.open(f'{X}/mock1030_skill/now_medium_infuse.png').convert('RGBA')
FEET = (357, 742)          # 게임 캡처의 영매 발밑
S = 0.75                   # 게임 = 시안 × 0.75
def frame(name, i=None):
    fs = sorted(glob.glob(f'{G}/{name}_*.png'))
    return Image.open(fs[(len(fs) - 1) if i is None else i]).convert('RGBA')
def put(img, dx, dy, add=True):
    x = FEET[0] + dx - img.width // 2; y = FEET[1] + dy - img.height // 2
    if add:   # 가산 — 검정 바탕 빛 그림
        layer = Image.new('RGBA', base.size); layer.paste(img, (x, y), img)
        import numpy as np
        b = np.asarray(base, np.float32); l = np.asarray(layer, np.float32)
        a = l[..., 3:4] / 255.0
        b[..., :3] = np.clip(b[..., :3] + l[..., :3] * a, 0, 255)
        base.paste(Image.fromarray(b.astype('uint8'), 'RGBA'))
    else:
        base.alpha_composite(img, (x, y))
def unit(key, dx, dy, h=72):
    u = Image.open(f'Assets/BaseResource/Unit/{key}/unit_{key}_e.png').convert('RGBA')
    u = u.resize((int(u.width * h / u.height), h), Image.NEAREST)
    base.alpha_composite(u, (FEET[0] + dx - u.width // 2, FEET[1] + dy - u.height))
SUBS = [(-95, -25), (120, -25)]
# 바닥
for dx, dy in [(-110, -283), (5, -235), (120, -225), (-150, -160)]: put(frame('fx_mdr1chain', 1), dx, dy)   # 둘레 적 발밑(둔화)
put(frame('fx_mdr1rune', 3), 13, 25)
for dx, dy in SUBS: put(frame('fx_mdr1subring', 2), dx, dy)
# 몸 뒤
put(frame('fx_mdr1pillar', 1), 5, -110)
put(frame('fx_mdr1aura', 2), 2, -88)
put(frame('fx_mdr1moon', 2), 0, -150)
# 인물(보조 둘)
unit('guru', SUBS[0][0], SUBS[0][1] + 14); unit('white_wizard', SUBS[1][0], SUBS[1][1] + 14)
# 몸 앞 · 공중
put(frame('fx_mdr1ghost', 2), -55, -210); put(frame('fx_mdr1ghost', 2), 120, -115)
for dx, dy in SUBS: put(frame('fx_mdr1up', 1), dx, dy - 100)
put(frame('fx_mdr1upself', 1), 0, -222)
comp = base.convert('RGB')
mock = Image.open(f'{X}/mock1030_skill/mock_ritual_free1.png').convert('RGB')
# 같은 창: 시안은 발밑(358,787) 기준 창을 0.75 로 줄여 게임과 같은 배율로
W, H = 520, 560
mx, my = 358 - (W / 2) / S, 787 - (H * 0.72) / S
mcrop = mock.crop((int(mx), int(my), int(mx + W / S), int(my + H / S))).resize((W, H))
ccrop = comp.crop((FEET[0] - W // 2, int(FEET[1] - H * 0.72), FEET[0] + W // 2, int(FEET[1] + H * 0.28)))
font = ImageFont.truetype(r'C:\Windows\Fonts\malgunbd.ttf', 26)
out = Image.new('RGB', (W * 2 + 20, H + 44), (20, 18, 28)); d = ImageDraw.Draw(out)
d.text((8, 6), '시안 1번(× 0.75)', font=font, fill=(255, 220, 90)); d.text((W + 28, 6), '부품 합성(게임 화면 위)', font=font, fill=(255, 220, 90))
out.paste(mcrop, (0, 44)); out.paste(ccrop, (W + 20, 44))
out.save(f'{X}/fx_medium1030_r1/compose_vs_mock.png'); comp.save(f'{X}/fx_medium1030_r1/compose_full.png')
print('ok')
