"""인게임 UI 크롬을 목업에서 다시 잘라낸다.

납품본이 테두리를 물고 잘려 있었다 — 프레임의 오른쪽·아래가 없어지거나
버튼 외곽선이 중간에서 끊겼다. 목업(576x1024)에 10px 격자를 씌워 다시 재고,
그 좌표로 잘라 ×1.25(=720/576) 로 확대한다.

`hostportraitframe` 은 목업에 초상이 들어 있는 상태다. 프레임만 필요하므로
안쪽을 지운다 — 지우지 않으면 다른 호스트에 빙의해도 람보 얼굴이 비친다.
"""
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
MOCK = os.path.join(HERE, 'Reference', 'Mockups', 'ingame_hd_scene.jpeg')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

S = 720.0 / 576.0        # 목업이 9:16 네이티브라 균일 확대로 끝난다

# 이름 → 목업 rect (10px 격자 실측 + 사방 3px 여유)
#
# 좌표를 딱 맞게 재면 몇 px 어긋날 때 테두리를 문다 — 실제로 그렇게 잘려 있었다.
# 주변이 어두운 컨트롤 바라 여유분은 화면에서 보이지 않는다. 잘리는 쪽이 훨씬 나쁘다.
# 레이아웃 박스도 같은 rect ×1.25 를 쓰므로 여유분만큼 함께 커진다.
CROPS = {
    'hostportraitframe': (5, 54, 67, 67),
    'pausebutton':       (527, 10, 48, 48),
    'ultimatebutton':    (324, 854, 122, 140),
    'possessbutton':     (445, 855, 110, 136),
    'dpadbase':          (7, 848, 144, 135),
    'dpadknob':          (51, 880, 63, 62),
}

FRAME_INNER = (10, 13, 22, 255)   # 초상 자리를 덮을 HUD 바탕색


def circular_alpha(im):
    """노브는 동그랗다. 사각 크롭 그대로 두면 모서리 배경이 패드 위에 얹힌다."""
    w, h = im.size
    mask = Image.new('L', (w * 4, h * 4), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, w * 4 - 1, h * 4 - 1), fill=255)
    im.putalpha(mask.resize((w, h), Image.LANCZOS))
    return im


def main():
    src = Image.open(MOCK).convert('RGB')
    for name, (x, y, w, h) in CROPS.items():
        c = src.crop((x, y, x + w, y + h)).convert('RGBA')

        if name == 'hostportraitframe':
            # 테두리만 남기고 안쪽을 지운다 (여유 3px + 테두리 두께 5px)
            d = ImageDraw.Draw(c)
            d.rectangle((8, 8, w - 9, h - 9), fill=FRAME_INNER)

        c = c.resize((round(w * S), round(h * S)), Image.LANCZOS)
        if name == 'dpadknob':
            c = circular_alpha(c)

        p = os.path.join(DST, name + '.png')
        before = Image.open(p).size if os.path.exists(p) else None
        c.save(p)
        print(f'  {name:<20} {before} → {c.size}   목업 ({x},{y},{w},{h})')

    print(f'\n다시 잘라낸 에셋 {len(CROPS)}개')


if __name__ == '__main__':
    main()
