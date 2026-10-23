# -*- coding: utf-8 -*-
"""방 물건 시안 발주용 칸 격자 가이드 — 원작에서 옮긴 방의 칸을 실제 게임 배율로 바탕 화면 위에 표시한다 (3단계, 2026-10-22).

PD 10-22 「여기 캐릭터가 지나갈 수 있어? 간격이 애매해 보이는데」 — 코덱스가 원작 칸을 안 지켜 가시를 물에 바짝 붙여 그렸다.
게임은 칸 그대로 놓이므로(한 칸 65 px · 몸 판정 42 px), 시안도 이 격자에 맞춰 그리게 한다.
⚠ 발주 참고 그림이다 — 게임에 들어가지 않는다.

쓰는 법: python room_grid_guide.py <챕터> <방 번호> <첫 줄> <줄 수>
결과  : _exchange/ref/room_tpl_mock/guide_ch<챕터>_room<방>.png (720 x 1280)
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
BASE = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'room_tpl_mock', 'base_room_now.png')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')
CELL = 720 / 11          # 화면 폭 720 = 방 폭 10 m = 11 칸
TOP = 205                # 골드 · 일시정지 줄 아래(바탕 화면에서 잰 값) — 위 칸이 HUD 에 안 가리게

MARK = {'#': ((128, 120, 112), '막음'), 'o': ((128, 120, 112), '막음'), '~': ((50, 110, 200), '물'),
        '=': ((205, 165, 60), '울타리'), '^': ((210, 60, 60), '가시'), 't': ((175, 175, 190), '레일')}
ROLE = {'melee': ('근', (250, 250, 250)), 'ranged': ('원', (255, 160, 40)), 'flying': ('비', (110, 230, 250)),
        'turret': ('포', (210, 110, 250))}


def main(ch, room_no, first, rows):
    d = json.load(open(os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms', f'origin_ch{ch}.json'), encoding='utf-8'))
    r = next(x for x in d['rooms'] if x['room'] == room_no)
    w, cells = r['w'], r['cells']
    img = Image.open(BASE).convert('RGB')
    over = Image.new('RGBA', img.size, (0, 0, 0, 0))
    dr = ImageDraw.Draw(over)
    font = ImageFont.truetype(FONT, 15)
    for gy in range(rows):
        y = first + gy
        for x in range(w):
            c = cells[y * w + x]
            x0, y0 = x * CELL, TOP + gy * CELL
            box = [x0, y0, x0 + CELL - 1, y0 + CELL - 1]
            if c in MARK:
                col, label = MARK[c]
                dr.rectangle(box, fill=col + (150,), outline=(255, 255, 255, 220), width=2)
                dr.text((x0 + 8, y0 + 22), label, font=font, fill=(255, 255, 255, 255))
            else:
                dr.rectangle(box, outline=(255, 255, 255, 110), width=1)
            sp = r['spawns'].get(f'{x},{y}')
            if sp:
                t, col = ROLE[sp['role']]
                dr.ellipse([x0 + 14, y0 + 14, x0 + CELL - 14, y0 + CELL - 14], fill=col + (230,), outline=(20, 20, 24, 255), width=2)
                dr.text((x0 + 25, y0 + 22), t, font=font, fill=(20, 20, 24, 255))
    img = Image.alpha_composite(img.convert('RGBA'), over).convert('RGB')
    out = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'room_tpl_mock', f'guide_ch{ch}_room{room_no}.png')
    img.save(out)
    print(out, img.size, f'줄 {first}~{first + rows - 1}')


if __name__ == '__main__':
    main(*(int(a) for a in sys.argv[1:5]))
