"""스킬 시안 검수 판 — 시안(세 컷) 옆에 원작 캐릭터를 같은 배율로 붙여 한 장으로 본다.
쓰기: python skill_mock_board.py <시안 이름> ...   (예: gangster commando_grenade_v2)
결과: Projects/AVSR/_exchange/ref/skillmock/board_<이름>.png
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
SM = os.path.join(EX, 'ref', 'skillmock')
FONT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 20)


def board(name):
    src = os.path.join(EX, 'in', f'mock_skill_{name}.png')
    if not os.path.exists(src):
        return None
    m = Image.open(src).convert('RGB')
    key = name.replace('_v2', '').replace('_v3', '')
    orig = os.path.join(SM, f'orig_{key}_sheet.png')
    h = 760
    mm = m.resize((int(m.width * h / m.height), h))
    pad = 260
    out = Image.new('RGB', (mm.width + pad, h + 40), (20, 22, 30))
    out.paste(mm, (0, 40))
    d = ImageDraw.Draw(out)
    d.text((8, 8), f'{name} — 시안 {m.size[0]}x{m.size[1]}', fill=(240, 240, 240), font=FONT)
    if os.path.exists(orig):
        o = Image.open(orig).convert('RGB')
        # 시안 한 칸(720) 기준 배율 그대로 — 원작 크기와 시안 속 크기를 나란히 잰다
        k = h / (m.height if m.height <= 1300 else 1280) * (m.height / 1280 if m.height > 1300 else 1)
        oo = o.resize((max(1, int(o.width * k * 1.6)), max(1, int(o.height * k * 1.6))), Image.NEAREST)
        out.paste(oo, (mm.width + 10, 60))
        d.text((mm.width + 10, 40), '원작 (×1.6 확대)', fill=(240, 240, 240), font=FONT)
    path = os.path.join(SM, f'board_{name}.png')
    out.save(path)
    return path


if __name__ == '__main__':
    for n in sys.argv[1:]:
        print(n, board(n))
