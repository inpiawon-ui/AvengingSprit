# -*- coding: utf-8 -*-
"""방 전체 스샷(Tools/Game/방 전체 스샷 (배치 확인))을 챕터마다 한 장으로 늘어놓는다 — 몬스터 · 오브젝트 배치 확인용.

입력  Projects/AVSR/_exchange/ref/room_layout/{방 ID}.png   (720 x 1280, 게임 화면 그대로)
      Projects/AVSR/Rooms/rooms90.tsv · chapters.tsv          (방 이름 · 의도 · 적/물건 수 · 챕터 이름)
출력  Projects/AVSR/_exchange/ref/room_layout/sheets/ch01.png … ch10.png

칸마다 : 방 번호 · 이름 · 적 수(빼앗을 수 있는 몸) · 물건 수 · 기획 의도(방 설명).
그림은 위 HUD 를 잘라 낸 방 창(y 228 ~ 1280)만 쓴다.
"""
import os
import re
import textwrap

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SHOTS = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'room_layout')
OUT = os.path.join(SHOTS, 'sheets')
ROOMS = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR.ttf')
FONT_B = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')

CROP = (0, 228, 720, 1280)   # 방 창 — 위 HUD 를 뺀다
TILE_W = 330
TILE_H = int(TILE_W * (CROP[3] - CROP[1]) / (CROP[2] - CROP[0]))
CAP_H = 150
COLS = 4
PAD = 16
BG = (18, 22, 32)
FG = (235, 238, 245)
DIM = (150, 160, 180)
GOLD = (255, 210, 80)


def read_rooms():
    rooms, cur = {}, None
    for line in open(os.path.join(ROOMS, 'rooms90.tsv'), encoding='utf-8'):
        c = line.rstrip('\n').split('\t')
        if c[0] == 'ROOM':
            cur = c[1]
            rooms[cur] = {'name': c[2] if len(c) > 2 else '', 'desc': c[3] if len(c) > 3 else '',
                          'enemy': 0, 'host': 0, 'obj': 0}
        elif cur and c[0] == 'SPAWN':
            rooms[cur]['enemy'] += 1
            if len(c) > 4 and c[4].strip() == '1':
                rooms[cur]['host'] += 1
        elif cur and c[0] == 'OBJ':
            rooms[cur]['obj'] += 1
    return rooms


def read_chapters():
    names = {}
    for line in open(os.path.join(ROOMS, 'chapters.tsv'), encoding='utf-8'):
        if line.startswith('#') or line.startswith('ch\t'):
            continue
        c = line.rstrip('\n').split('\t')
        if c and c[0].isdigit():
            names[int(c[0])] = c[23] if len(c) > 23 else ''
    return names


def wrap(text, font, width, draw):
    """글자 폭으로 줄을 나눈다(한글은 띄어쓰기가 드물어 글자 단위로 자른다)."""
    lines, line = [], ''
    for ch in text:
        if draw.textlength(line + ch, font=font) > width:
            lines.append(line)
            line = ch
        else:
            line += ch
    if line:
        lines.append(line)
    return lines


def main():
    os.makedirs(OUT, exist_ok=True)
    rooms = read_rooms()
    chapters = read_chapters()
    f_title = ImageFont.truetype(FONT_B, 34)
    f_head = ImageFont.truetype(FONT_B, 20)
    f_body = ImageFont.truetype(FONT, 15)

    by_ch = {}
    for rid in rooms:
        m = re.match(r'ROOM_CH(\d+)_(\d+)', rid)
        if m:
            by_ch.setdefault(int(m.group(1)), []).append((int(m.group(2)), rid))

    for ch in sorted(by_ch):
        items = sorted(by_ch[ch])
        rows = (len(items) + COLS - 1) // COLS
        W = PAD + COLS * (TILE_W + PAD)
        H = 70 + rows * (TILE_H + CAP_H + PAD) + PAD
        sheet = Image.new('RGB', (W, H), BG)
        d = ImageDraw.Draw(sheet)
        d.text((PAD, 18), 'CHAPTER %d  %s  — 전투방 %d개 (몬스터 · 오브젝트 배치)' % (ch, chapters.get(ch, ''), len(items)),
               font=f_title, fill=FG)
        for i, (no, rid) in enumerate(items):
            x = PAD + (i % COLS) * (TILE_W + PAD)
            y = 70 + (i // COLS) * (TILE_H + CAP_H + PAD)
            path = os.path.join(SHOTS, rid + '.png')
            if os.path.exists(path):
                im = Image.open(path).convert('RGB').crop(CROP).resize((TILE_W, TILE_H), Image.LANCZOS)
                sheet.paste(im, (x, y))
            else:
                d.rectangle((x, y, x + TILE_W, y + TILE_H), outline=(200, 60, 60), width=3)
                d.text((x + 20, y + 20), '스샷 없음', font=f_head, fill=(255, 120, 120))
            r = rooms[rid]
            cy = y + TILE_H + 6
            d.text((x, cy), '%d-%02d  %s' % (ch, no, r['name']), font=f_head, fill=GOLD)
            d.text((x, cy + 28), '적 %d (빙의 가능 %d) · 물건 %d   %s' % (r['enemy'], r['host'], r['obj'], rid),
                   font=f_body, fill=DIM)
            for k, line in enumerate(wrap(r['desc'], f_body, TILE_W, d)[:5]):
                d.text((x, cy + 50 + k * 19), line, font=f_body, fill=FG)
        out = os.path.join(OUT, 'ch%02d.png' % ch)
        sheet.save(out, optimize=True)
        print(out, sheet.size)


if __name__ == '__main__':
    main()
