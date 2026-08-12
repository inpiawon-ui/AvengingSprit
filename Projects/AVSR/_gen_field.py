"""장판 원판 스프라이트.

캐릭터가 아니라 **코드가 색을 입히는 흰 마스크**다. 그림쟁이에게 시킬 것이 없다 —
색은 효과마다 다르고(화상 주황 · 빙결 하늘 · 저주 보라) 코드가 tint 로 넣는다.

가장자리를 딱 끊지 않는다. 경계가 칼같으면 "여기까지가 장판" 이 아니라
"바닥에 붙은 스티커" 로 보인다. 안쪽은 옅고 테두리 쪽이 진한 고리형이라야
**영역**으로 읽힌다 — 안이 진하면 그 위에 선 캐릭터가 묻힌다.

도트 게임이므로 계단을 남긴다. 부드러운 그라데이션은 이 화면에서 혼자 겉돈다.

사용: python _gen_field.py
산출: Assets/BaseResource/InGameMainUI/field.png (128×128)
"""
import io
import math
import os
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI', 'field.png')

SIZE = 128
STEPS = 5          # 알파 계단 수. 도트 느낌을 남기려고 연속값을 쓰지 않는다
INNER = 0.42       # 이 안쪽은 가장 옅다


def main():
    im = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
    px = im.load()
    c = (SIZE - 1) / 2.0

    for y in range(SIZE):
        for x in range(SIZE):
            r = math.hypot(x - c, y - c) / c
            if r > 1.0:
                continue
            # 바깥으로 갈수록 진하다. 테두리 두 칸은 가장 진하게 남겨 윤곽을 만든다
            t = 0.35 if r < INNER else 0.35 + 0.65 * ((r - INNER) / (1.0 - INNER))
            if r > 0.94:
                t = 1.0
            step = round(t * STEPS) / STEPS
            px[x, y] = (255, 255, 255, int(255 * step))

    im.save(OUT)
    print(f'장판 원판 {SIZE}x{SIZE} · 알파 {STEPS}단계 → {OUT}')


if __name__ == '__main__':
    main()
