"""RE:BORN 묶음 원본 부품으로 되돌리기(2026-10-26).

PD 2026-10-26 호스트 창 짚기 — 「칩 · 유령 판 · 성장 버튼 · ? · < 버튼 · 카드 틀 · 안쪽 빛이 시안이랑 다름」.
10-23 에 묶음 부품을 「코드로 그린 것」이라 코덱스로 다시 그렸는데, 시안은 그 묶음 부품으로 조립된 화면이라
다시 그린 것을 넣으면 시안과 달라진다. 시안이 정답이라 묶음 원본으로 되돌린다.

- 원본: REBORN_UI/Components/2x(2160 기준) → 1/3 로 줄여 720 기준(키트 크기)으로
- 9분할: sprite-manifest.json 의 1x(1080 기준) 테두리 × 2/3
- 다시 그린 키트 그림은 지우지 않고 Projects/AVSR/_archive/reborn_kit/reorder_1023/ 로 옮겨 둔다
- 같은 파일 이름에 덮어쓰니 .meta(GUID)는 그대로 — 프리팹 연결이 안 끊긴다

python Projects/AVSR/Tools/rb_bundle_parts.py
"""
import glob
import json
import os
import shutil

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
PKG = os.path.join(ROOT, 'Projects', 'AVSR', '_handoff', 'reborn_ui_1023', '03_리소스_및_시안', 'REBORN_UI_FINAL', 'REBORN_UI')
KIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'ReBornKit')
ARCHIVE = os.path.join(ROOT, 'Projects', 'AVSR', '_archive', 'reborn_kit', 'reorder_1023')

# 묶음 id → 키트 이름(Components/ 아래)
PARTS = {
    'btn_icon_round': 'rb_btn_icon_round',
    'btn_icon_round_pressed': 'rb_btn_icon_round_pressed',
    'btn_icon_round_disabled': 'rb_btn_icon_round_disabled',
    'btn_chip': 'rb_btn_chip',
    'btn_chip_pressed': 'rb_btn_chip_pressed',
    'btn_chip_disabled': 'rb_btn_chip_disabled',
    'btn_secondary': 'rb_btn_secondary',
    'btn_secondary_pressed': 'rb_btn_secondary_pressed',
    'btn_secondary_disabled': 'rb_btn_secondary_disabled',
    'panel_base': 'rb_panel_base',
    'host_frame_r': 'rb_host_frame_r',
    'host_frame_sr': 'rb_host_frame_sr',
    'host_frame_ssr': 'rb_host_frame_ssr',
    'host_frame_random': 'rb_host_frame_random',
    'host_frame_ghost': 'rb_host_frame_ghost',
    'host_inner_glow': 'rb_host_inner_glow',
    'progress_track': 'rb_bar_track',
    'progress_fill_cyan': 'rb_bar_fill_cyan',
}


def main():
    manifest = json.load(open(os.path.join(PKG, 'Editor', 'sprite-manifest.json'), encoding='utf8'))
    border1x = {a['id']: a['border'] for a in manifest['assets'] if a['path'].startswith('Components/1x/')}
    borders_path = os.path.join(KIT, 'borders.json')
    borders = json.load(open(borders_path, encoding='utf8'))
    os.makedirs(ARCHIVE, exist_ok=True)
    for bid, kit in PARTS.items():
        src = glob.glob(os.path.join(PKG, 'Components', '2x', f'ui_{bid}@*.png'))
        if len(src) != 1:
            print('원본 없음', bid, src)
            continue
        dst = os.path.join(KIT, 'Components', kit + '.png')
        if os.path.exists(dst) and not os.path.exists(os.path.join(ARCHIVE, kit + '.png')):
            shutil.copy2(dst, os.path.join(ARCHIVE, kit + '.png'))
        im = Image.open(src[0]).convert('RGBA')
        w, h = round(im.width / 3), round(im.height / 3)
        im.resize((w, h), Image.LANCZOS).save(dst)
        b = border1x.get(bid, [0, 0, 0, 0])
        key = 'Components/' + kit
        if any(b):
            borders[key] = [round(v * 2 / 3) for v in b]   # L · B · R · T(묶음 border_order 와 Unity Vector4 순서가 같다)
        else:
            borders.pop(key, None)
        print(kit, (w, h), borders.get(key))
    with open(borders_path, 'w', encoding='utf8') as f:
        f.write('{\n' + ',\n'.join(
            f' "{k}": [\n' + ',\n'.join(f'  {v}' for v in vals) + '\n ]' for k, vals in borders.items()) + '\n}\n')


if __name__ == '__main__':
    main()
