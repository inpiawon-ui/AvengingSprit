"""RE:BORN UI 리뉴얼(2026-10-23) — 재발주용 기준 그림 자르기.

PD 「저걸 당연히 사용은 못하고 … 지시서나 레퍼런스가 있으면 그걸로 다시 발주」.
묶음의 판 · HUD 는 코드로 그린 것이라 쓰지 않고, 승인 스타일 타일 · 처음 HUD 시안 · 97화면에서
부품 자리를 잘라 코덱스 발주의 기준 그림으로 박는다. 연출 시안 바탕은 화면을 720 폭으로 줄여 둔다.

python ui_reborn_anchors.py → Projects/AVSR/_exchange/ref/ui_reborn_1023/anchor/*.png
"""
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
PKG = os.path.join(ROOT, 'Projects', 'AVSR', '_handoff', 'reborn_ui_1023')
FINAL = os.path.join(PKG, '03_리소스_및_시안', 'REBORN_UI_FINAL')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'ui_reborn_1023', 'anchor')

STYLE = os.path.join(PKG, '04_검수기록', 'style_tile.png')            # 2160 폭(1080 기준의 2배)
HUD = os.path.join(FINAL, 'App', 'refs', '07_blueprint_I-02.jpg')     # 720 폭
SCREEN = os.path.join(FINAL, 'Screens', '%s.png')                     # 1080 x 2340

# 이름: (원본, (왼, 위, 오른, 아래)) — 좌표는 원본 픽셀
CROPS = {
    'style_buttons':   (STYLE, (80, 300, 1000, 1190)),
    'style_panels':    (STYLE, (1117, 290, 2040, 750)),
    'style_hostcards': (STYLE, (1117, 860, 2000, 1160)),
    'style_slots':     (STYLE, (1117, 1370, 1700, 1590)),
    'style_popup':     (STYLE, (1117, 1990, 2010, 2450)),
    'style_statpanel': (STYLE, (80, 2265, 1040, 2540)),
    'hud_top':         (HUD, (0, 85, 720, 300)),
    'hud_buttons':     (HUD, (390, 1100, 700, 1430)),
    'hud_joystick':    (HUD, (30, 1190, 260, 1410)),
    'lobby_tabbar':    (SCREEN % 'lobby', (0, 2030, 1080, 2260)),
    'lobby_hostrow':   (SCREEN % 'lobby', (20, 1400, 1080, 1700)),
    'lobby_header':    (SCREEN % 'lobby', (20, 130, 1060, 260)),
}

# 연출 시안 바탕 — 화면 그대로 720 x 1560 으로(게임 기준 폭)
FX_BASES = ['summon-flash', 'summon-result-1', 'summon-result-10', 'summon-result-jackpot', 'chest-opening',
            'chest-open', 'reward', 'reward-reveal', 'star-success', 'awaken-6', 'awaken-10', 'growth-success', 'lobby']


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (src, box) in CROPS.items():
        Image.open(src).convert('RGBA').crop(box).save(os.path.join(OUT, name + '.png'))
        print(name, box)
    for sid in FX_BASES:
        im = Image.open(SCREEN % sid).convert('RGB').resize((720, 1560), Image.LANCZOS)
        im.save(os.path.join(OUT, 'base_' + sid + '.png'))
        print('base', sid)


if __name__ == '__main__':
    main()
