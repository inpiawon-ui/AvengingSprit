"""레벨업 카드 아이콘 → 게임 파일 128 × 128 (09_import_settings 2-1절, PD 2026-10-22 「정사각 · 2의 승수」).

한 그림을 모든 자리가 같이 쓴다 — 레벨업 카드 card_cXXX(카드 아틀라스) · 상점 칸 buffcard_cXXX(인게임 아틀라스) ·
제단 shrine_card_*(인게임 아틀라스). 이름이 둘인 것은 아틀라스가 둘이라서다. **자르지 않는다**(정사각 안에 통째로).
원본 고르는 순서: card_icon_cXXX_sq(정사각 재발주) > _v2 > 첫 판 > 지금 게임 파일(c001~c013 — 발주 원본이 없다).
결과는 in/ 에 게임 이름으로 둔다 → ExchangeImporter 가 갈 자리 · 임포트 설정 · 아틀라스를 맡는다.
쓰기: python Projects/AVSR/Tools/fit_step1_card_icons.py
"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from fit_set_c_icons import square  # noqa: E402
from icon_review_sheet import cut  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
CARD_RES = os.path.join(ROOT, 'Assets', 'BaseResource', 'Card')
SIZE = 128


def source_of(cid):
    for v in ('_sq', '_v2', ''):
        p = os.path.join(IN, f'card_icon_{cid}{v}.png')
        if os.path.exists(p):
            return p, True
    p = os.path.join(CARD_RES, f'card_{cid}.png')   # 예전 카드 — 이미 뚫린 게임 파일에서 줄인다
    return (p, False) if os.path.exists(p) else (None, False)


def main():
    done = []
    for i in range(1, 37):
        cid = f'c{i:03d}'
        src, pink = source_of(cid)
        if src is None:
            continue
        rgba = cut(src)[0] if pink else Image.open(src).convert('RGBA')
        icon = square(rgba, SIZE)
        icon.save(os.path.join(IN, f'card_{cid}.png'))
        icon.save(os.path.join(IN, f'buffcard_{cid}.png'))
        tag = os.path.basename(src).replace(f'card_icon_{cid}', '').replace('.png', '') or ('첫 판' if pink else '게임 파일')
        done.append(f'{cid}({tag})')
    for key in ('shrine_card_pick', 'shrine_card_up'):
        raw = os.path.join(IN, key + '_raw.png')
        if not os.path.exists(raw):
            Image.open(os.path.join(IN, key + '.png')).save(raw)
        square(cut(raw)[0], SIZE).save(os.path.join(IN, key + '.png'))
        done.append(key)
    print(len(done), ' '.join(done))


if __name__ == '__main__':
    main()
