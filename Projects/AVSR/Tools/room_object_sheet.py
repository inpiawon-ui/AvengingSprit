# -*- coding: utf-8 -*-
"""무대별 방 물건 그림을 원작 물건 칸(막음 · 물 · 울타리 · 가시 · 함정 레일 · 포탑)별로 한 장에 늘어놓는다 (3단계 조사, 2026-10-22).

새 물건 그림을 발주하기 전에 「이미 무엇이 있나」를 같은 배율로 본다.
쓰는 법: python room_object_sheet.py → _exchange/plan_content_expand/step3_research/room_objects.png
"""
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'plan_content_expand', 'step3_research', 'room_objects.png')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')

STAGES = ['junkyard', 'missile', 'street', 'rooftop', 'lab', 'refinery']
# (원작 칸, 우리 물건 이름 꼬리)
COLS = [('돌 · 나무\n(막음)', 'block_1'), ('큰 덩어리', 'bulk'), ('기둥', 'pillar'), ('상자(부서짐)', 'crate_1'),
        ('물 → 도랑\n(탄 통과)', 'channel_h'), ('울타리\n(탄 통과?)', 'barricade'), ('낮은 엄폐', 'low_cover'),
        ('가시\n(시간차)', 'timed_spike_1'), ('함정 레일', 'rail'), ('포탑', 'wallturret_s'), ('꾸밈 넓은', 'prop_wide')]
CELL = 150


def find(stage, tail):
    for name in (f'obj_{stage}_{tail}.png', f'obj_{tail}.png' if stage == 'junkyard' else None):
        if name and os.path.exists(os.path.join(SRC, name)):
            return os.path.join(SRC, name), name != f'obj_{stage}_{tail}.png'
    return None, False


def main():
    font = ImageFont.truetype(FONT, 15)
    W, H = 120 + len(COLS) * CELL, 50 + len(STAGES) * CELL
    img = Image.new('RGBA', (W, H), (44, 46, 54, 255))
    d = ImageDraw.Draw(img)
    for c, (label, _) in enumerate(COLS):
        d.multiline_text((120 + c * CELL + 6, 4), label, font=font, fill=(235, 235, 235))
    for r, st in enumerate(STAGES):
        y = 50 + r * CELL
        d.text((8, y + CELL // 2 - 10), st, font=font, fill=(235, 235, 235))
        for c, (_, tail) in enumerate(COLS):
            x = 120 + c * CELL
            d.rectangle([x, y, x + CELL - 2, y + CELL - 2], outline=(70, 74, 86))
            p, shared = find(st, tail)
            if not p:
                d.text((x + 50, y + 62), '없음', font=font, fill=(240, 110, 100))
                continue
            im = Image.open(p).convert('RGBA')
            k = min((CELL - 10) / im.width, (CELL - 10) / im.height, 1.0)
            im = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.NEAREST)
            img.alpha_composite(im, (x + (CELL - im.width) // 2, y + (CELL - im.height) // 2))
            if shared:
                d.text((x + 4, y + CELL - 20), '공용', font=font, fill=(250, 200, 120))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img.save(OUT)
    print(OUT, img.size)


if __name__ == '__main__':
    main()
