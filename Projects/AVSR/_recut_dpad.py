"""D패드를 목업에서 다시 자른다.

기존 dpadbase 는 크롭 박스가 실제 패드 경계와 어긋나 좌우·아래가 잘렸고,
모서리를 flood fill 로 파낸 자리에 흰·검은 얼룩이 남았다.

목업에 격자를 얹어 실측한 경계는 x 8~148, y 838~980 이다.
모서리 투명화는 flood fill 대신 **둥근 사각형 마스크**로 한다. 패드가
실제로 둥근 사각형이므로 기하학적으로 정확하고, 배경색을 따라가지 않으니
얼룩이 남지 않는다. 노브도 같은 이유로 타원 마스크를 쓴다.
"""
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
IG = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')
MOCK = os.path.join(HERE, 'Reference', 'Mockups', 'ingame_hd_scene.jpeg')

S = 720 / 576                  # 인게임 목업 → 캔버스 배율
BASE_BOX = (12, 841, 139, 141) # 실측: 바깥 금속 테두리 딱 맞게 (모서리 곡선 확대해 확인)
KNOB_BOX = (49, 878, 64, 64)   # 실측: 빨간 면 54px + 검은 베젤
CORNER = 25                    # 실측 모서리 반지름. 크면 모서리를 깎고, 작으면 배경이 샌다
SS = 4                         # 마스크를 4배로 그린 뒤 줄여 계단을 없앤다


def cut(box, mask_kind, radius=0):
    x, y, w, h = box
    src = Image.open(MOCK).convert('RGB').crop((x, y, x + w, y + h))
    src = src.resize((round(w * S), round(h * S)), Image.LANCZOS).convert('RGBA')

    m = Image.new('L', (src.width * SS, src.height * SS), 0)
    d = ImageDraw.Draw(m)
    if mask_kind == 'round':
        d.rounded_rectangle([0, 0, m.width - 1, m.height - 1],
                            radius=round(radius * S * SS), fill=255)
    else:
        d.ellipse([0, 0, m.width - 1, m.height - 1], fill=255)
    src.putalpha(m.resize(src.size, Image.LANCZOS))
    return src


def erase_knob(base):
    """베이스에 박혀 있는 빨간 버튼을 지우고 패드 표면색으로 메운다.

    노브는 별도 오브젝트로 움직인다. 베이스에도 버튼이 남아 있으면 패드를
    옮길 때 빨간 버튼이 두 개로 보인다. 투명하게 뚫으면 그 자리로 바닥이
    비치므로, 패드의 어두운 표면색으로 메운다.
    """
    px = base.load()
    # 버튼도 화살표도 없는 깨끗한 표면에서 색을 딴다
    sx = round((40 - BASE_BOX[0]) * S)
    sy = round((870 - BASE_BOX[1]) * S)
    surf = px[sx, sy]

    kx = round((KNOB_BOX[0] - BASE_BOX[0]) * S)
    ky = round((KNOB_BOX[1] - BASE_BOX[1]) * S)
    kw = round(KNOB_BOX[2] * S)
    kh = round(KNOB_BOX[3] * S)
    pad = 3                                   # 베젤 그림자까지 확실히 덮는다

    hole = Image.new('L', ((kw + pad * 2) * SS, (kh + pad * 2) * SS), 0)
    ImageDraw.Draw(hole).ellipse([0, 0, hole.width - 1, hole.height - 1], fill=255)
    hole = hole.resize((kw + pad * 2, kh + pad * 2), Image.LANCZOS)

    fill = Image.new('RGBA', hole.size, (surf[0], surf[1], surf[2], 255))
    base.paste(fill, (kx - pad, ky - pad), hole)
    return base


def main():
    b = erase_knob(cut(BASE_BOX, 'round', CORNER))
    old = Image.open(os.path.join(IG, 'dpadbase.png')).size
    b.save(os.path.join(IG, 'dpadbase.png'))
    print(f'  dpadbase  {old} → {b.size}  (박혀 있던 버튼 제거)')

    k = cut(KNOB_BOX, 'ellipse')
    old = Image.open(os.path.join(IG, 'dpadknob.png')).size
    k.save(os.path.join(IG, 'dpadknob.png'))
    print(f'  dpadknob  {old} → {k.size}')

    # 레이아웃에 넣을 값 — 노브는 패드 안에서 중앙에 놓는다
    bx, by, bw, bh = BASE_BOX
    print(f'\n  레이아웃 DPadBase (목업좌표) = {(bx, by, bw, bh)}')
    print(f'  DPadKnob local = '
          f'{(round((bw - KNOB_BOX[2]) / 2), round((bh - KNOB_BOX[3]) / 2), KNOB_BOX[2], KNOB_BOX[3])}')


if __name__ == '__main__':
    main()
