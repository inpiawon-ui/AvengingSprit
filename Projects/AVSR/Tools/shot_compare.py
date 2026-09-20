# -*- coding: utf-8 -*-
"""바꾸기 전(_exchange/ref/shot_up/old) ↔ 바꾼 뒤(Assets) 대조표."""
import os
from PIL import Image, ImageDraw, ImageFont

OLD = 'Projects/AVSR/_exchange/ref/shot_up/old/'
NEW = 'Assets/BaseResource/InGameMainUI/'
OUT = r'C:\Users\inpia\AppData\Local\Temp\claude\C--won-UnityProject-AvengingSprit\80df8f18-c651-4580-b271-009885a0d916\scratchpad\shot_compare.png'

ROWS = [
    ('권총 탄',     ['shot_bullet_1'],                                     ['shot_bullet_1', 'shot_bullet_2', 'shot_bullet_3']),
    ('명중 불꽃',   ['impact_bullet_1', 'impact_bullet_2'],                ['impact_bullet_1', 'impact_bullet_2', 'impact_bullet_3']),
    ('총구 화염',   ['fx_muzzle_1', 'fx_muzzle_2'],                        ['fx_muzzle_1', 'fx_muzzle_2', 'fx_muzzle_3']),
    ('기관단총',    ['shot_smg_1', 'shot_smg_2', 'shot_smg_3'],            ['shot_smg_1', 'shot_smg_2', 'shot_smg_3']),
    ('기관총',      ['shot_mg_1', 'shot_mg_2', 'shot_mg_3'],               ['shot_mg_1', 'shot_mg_2', 'shot_mg_3']),
    ('레이저',      ['shot_laser_1'],                                      ['shot_laser_1', 'shot_laser_2', 'shot_laser_3']),
    ('쇳조각',      ['shot_scrapgunner_1', 'shot_scrapgunner_2', 'shot_scrapgunner_3'],
                    ['shot_scrapgunner_1', 'shot_scrapgunner_2', 'shot_scrapgunner_3']),
    ('야구공·창',   ['shot_ball_1', 'shot_spear_1', 'shot_spear_2'],       ['shot_ball_1', 'shot_ball_2', 'shot_spear_1', 'shot_spear_2']),
    ('레이저 명중', ['impact_laser_1', 'impact_laser_2'],                  ['impact_laser_1', 'impact_laser_2', 'impact_laser_3']),
    ('쇳조각 명중', ['impact_scrapgunner_1', 'impact_scrapgunner_2'],      ['impact_scrapgunner_1', 'impact_scrapgunner_2', 'impact_scrapgunner_3']),
    ('파동 명중',   ['impact_pulse_1', 'impact_pulse_2'],                  ['impact_pulse_1', 'impact_pulse_2', 'impact_pulse_3']),
    ('번개 명중',   [],                                                    ['impact_thunder_1', 'impact_thunder_2', 'impact_thunder_3']),
    ('독 명중',     [],                                                    ['impact_venom_1', 'impact_venom_2', 'impact_venom_3']),
    ('마법 명중',   ['impact_magic_1', 'impact_magic_2'],                  ['impact_magic_1', 'impact_magic_2', 'impact_magic_3']),
]

CELL = 104
LABEL = 150
GAP = 26
COLS = 4
W = LABEL + (COLS * CELL + GAP) * 2
H = len(ROWS) * CELL + 46
o = Image.new('RGB', (W, H), (18, 20, 26))
dr = ImageDraw.Draw(o)
f = ImageFont.truetype('Assets/BaseResource/Fonts/NotoSansKR-Bold.ttf', 20)
dr.text((LABEL + 10, 8), '지금', font=f, fill=(210, 210, 220))
dr.text((LABEL + COLS * CELL + GAP + 10, 8), '바꾼 것', font=f, fill=(255, 220, 110))

for r, (label, olds, news) in enumerate(ROWS):
    y = 40 + r * CELL
    dr.text((8, y + CELL // 2 - 12), label, font=f, fill=(230, 230, 240))
    for side, (base, files) in enumerate([(OLD, olds), (NEW, news)]):
        x0 = LABEL + side * (COLS * CELL + GAP)
        for c, n in enumerate(files[:COLS]):
            p = base + n + '.png'
            if not os.path.exists(p):
                continue
            im = Image.open(p).convert('RGBA').resize((CELL - 12, CELL - 12), Image.NEAREST)
            bg = Image.new('RGBA', im.size, (44, 48, 58, 255))
            bg.alpha_composite(im)
            o.paste(bg.convert('RGB'), (x0 + c * CELL + 6, y + 6))
        if not files:
            dr.text((x0 + 12, y + CELL // 2 - 12), '(아무것도 안 나왔다)', font=f, fill=(150, 90, 90))
o.save(OUT)
print(OUT, o.size)
