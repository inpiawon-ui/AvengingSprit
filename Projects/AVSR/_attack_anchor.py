"""공격·피격 프레임 앵커.

확정된 idle 5장이 이번 작업의 정본이다. 공격·피격은 **같은 인물이 같은 자리에
선 채** 상체만 움직이는 것이어야 한다. 발이 움직이면 쏠 때마다 캐릭터가
미끄러지고, 키가 바뀌면 프레임이 넘어갈 때 튄다 — 8방향에서 이미 두 번 겪은
실패라 이번에는 기준선을 그려서 넘긴다.

초록 = 발이 닿는 선(y=87)   분홍 = 발 중심(x=48)   노랑 = 실루엣 상단(키 76px)
"""
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit', 'rambo')
OUT = os.path.join(HERE, '_exchange', 'out', '08_attack')

DIRS = ['s', 'se', 'e', 'ne', 'n']
Z = 4
FOOT_Y = 87          # 발이 닿는 행 (아래에서 8px 위)
CENTER_X = 48        # 발 중심
TOP_Y = FOOT_Y - 75  # 키 76px 의 머리끝


def main():
    os.makedirs(OUT, exist_ok=True)
    cell = 96 * Z
    sheet = Image.new('RGBA', (cell * 5 + 20 * 6, cell + 116), (24, 20, 38, 255))
    d = ImageDraw.Draw(sheet)

    d.text((20, 12), 'IDLE - the reference. attack/hit must keep the SAME feet and the SAME height.',
           fill=(255, 255, 255, 255))
    d.text((20, 30), 'green = ground line (y=87)   pink = foot center (x=48)   yellow = top of head (76px tall)',
           fill=(180, 180, 210, 255))

    for i, k in enumerate(DIRS):
        im = Image.open(os.path.join(SRC, f'unit_rambo_{k}.png')).convert('RGBA')
        im = im.resize((cell, cell), Image.NEAREST)
        x = 20 + i * (cell + 20)
        sheet.paste(im, (x, 56), im)
        d.line((x, 56 + FOOT_Y * Z + Z, x + cell, 56 + FOOT_Y * Z + Z), fill=(80, 255, 150, 170), width=2)
        d.line((x + CENTER_X * Z, 56, x + CENTER_X * Z, 56 + cell), fill=(255, 90, 160, 130), width=2)
        d.line((x, 56 + TOP_Y * Z, x + cell, 56 + TOP_Y * Z), fill=(255, 210, 60, 150), width=2)
        d.text((x + 8, 58), k.upper(), fill=(255, 255, 255, 255))

    sheet.convert('RGB').save(os.path.join(OUT, '01_정본_idle_5방향_기준선.png'))
    print('saved 01_정본_idle_5방향_기준선.png', sheet.size)

    # 원본 5장도 그대로 넘긴다 — 위에 겹쳐 그려서 작업하라고.
    for k in DIRS:
        Image.open(os.path.join(SRC, f'unit_rambo_{k}.png')).save(
            os.path.join(OUT, f'02_원본_unit_rambo_{k}.png'))
    print('saved 02_원본_* 5개')


if __name__ == '__main__':
    main()
