"""1단계 카드 아이콘 납품(in/card_icon_cXXX[_v2].png · shrine_card_*.png, 분홍 바탕 320) → 게임 크기 (2026-10-22).

카드: card_cXXX 160 px(레벨업 카드 · 카드 아틀라스) + buffcard_cXXX 96 px(상점 칸 · 인게임 아틀라스).
제단: shrine_card_pick · shrine_card_up 96 px. 자르기 · 가운데 맞춤은 fit_set_c_icons 와 같은 규칙(square).
결과는 in/ 에 게임 이름으로 둔다 → `ExchangeImporter` 가 갈 자리 · 임포트 설정 · 아틀라스를 맡는다.
원본(card_icon_*)은 반영 도구가 건너뛴다(IsSuperseded). `_v2` 가 있으면 그것이 정본이다.
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from fit_set_c_icons import square  # noqa: E402
from icon_review_sheet import cut  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')


def focus(rgba, ratio=1.25):
    """옆으로 긴 그림을 **그림이 가장 몰린 쪽**으로 잘라 정사각에 가깝게 만든다.

    상점 칸은 72 px 로 그려지는데, 가로로 긴 구도(유령 → 탄 → 해골)를 통째로 줄이면
    높이가 10 px 남짓이 되어 안 읽혔다(2026-10-22 게임 캡처 — 관통탄 · 유도탄 강화).
    알파가 가장 많은 가로 구간(폭 = 높이 × ratio)을 남긴다 — 보통 맞는 쪽(해골 · 효과)이 남는다.
    """
    box = rgba.getchannel('A').point(lambda a: 255 if a > 24 else 0).getbbox()
    art = rgba.crop(box)
    w, h = art.size
    want = int(h * ratio)
    if w <= want:
        return art
    col = np.asarray(art.getchannel('A'), dtype=np.float32).sum(axis=0)
    run = np.convolve(col, np.ones(want), mode='valid')
    x = int(run.argmax())
    return art.crop((x, 0, x + want, h))


def main():
    done = []
    for i in range(14, 37):
        cid = f'c{i:03d}'
        src = next((os.path.join(IN, f'card_icon_{cid}{v}.png') for v in ('_v2', '')
                    if os.path.exists(os.path.join(IN, f'card_icon_{cid}{v}.png'))), None)
        if src is None:
            print('없음', cid)
            continue
        rgba, _, _ = cut(src)
        square(rgba, 160).save(os.path.join(IN, f'card_{cid}.png'))
        square(focus(rgba), 96).save(os.path.join(IN, f'buffcard_{cid}.png'))
        done.append(f'{cid}{" (v2)" if src.endswith("_v2.png") else ""}')
    for key in ('shrine_card_pick', 'shrine_card_up'):
        src = os.path.join(IN, key + '.png')
        # 납품 원본과 같은 이름이라 덮어쓰기 전에 원본을 `_raw` 로 남기고, 다시 돌릴 때도 원본에서 자른다
        raw = os.path.join(IN, key + '_raw.png')
        if not os.path.exists(raw):
            Image.open(src).save(raw)
        rgba, _, _ = cut(raw)
        square(rgba, 96).save(src)
        done.append(key)
    print(len(done), ', '.join(done))


if __name__ == '__main__':
    main()
