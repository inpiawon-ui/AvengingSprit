"""창 틀을 세로로 늘린다 — 칸마다 설명 줄 높이의 민무늬 가로줄 한 줄을 되풀이해 끼운다 (PD 2026-10-08).

PD 「팝업을 위아래로 크게 해서 텍스트 영역을 더 확보」. 틀을 통째로 늘리면 장식이 찌그러져서,
칸(제단 3칸 · 상점 3줄 · 악마 본문) 안의 **설명 줄 높이**에서만 늘린다. 제목 줄 · 장식은 그대로다.
줄 위치는 위아래 줄과 차이가 가장 작은 줄을 재서 골랐다(make_tall_frames 주석 · 2026-10-08 실측).

INSERTS 는 popup_c_layout.py · fx_story.py 가 같이 읽는다(좌표를 같은 규칙으로 옮긴다) — 단일 출처.
원본 out/setc/{틀}.png → out/setc_tall/{틀}.png
python make_tall_frames.py
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
EX = os.path.join(HERE, '..', '_exchange', 'out')

# 틀 이름 → [(틀 안 y(원본 px), 끼울 줄 수, 결 띠 시작, 결 띠 끝)]. y 위 줄은 그대로, y 부터 아래는 끼운 만큼 내려간다.
# ⚠ 한 줄만 되풀이하면 결이 세로 줄무늬로 남았다(2026-10-08 확대 확인). 칸 안 결 띠를 거울로 번갈아 이어 붙인다.
INSERTS = {
    'shrineframe': [(239, 36, 219, 239), (327, 36, 307, 327), (418, 36, 398, 418)],   # 칸 3개 — 설명 줄 26 → 62
    'shopframe': [(223, 30, 203, 223), (343, 30, 323, 343), (420, 30, 400, 420)],     # 상품 3줄 — 설명 46 → 76
    'eventframe': [(242, 90, 242, 272)],                                              # 본문 80 → 170
}
PANEL_OF = {'ShrinePanel': 'shrineframe', 'ShopPanel': 'shopframe', 'EventPanel': 'eventframe'}


def grow(frame):
    return sum(ins[1] for ins in INSERTS.get(frame, []))


def remap(frame, y):
    """틀 안 y(원본) → 늘린 틀 안 y. 끼운 줄과 같은 줄은 아래로 밀린다."""
    return y + sum(ins[1] for ins in INSERTS.get(frame, []) if y >= ins[0])


def build():
    out = os.path.join(EX, 'setc_tall')
    os.makedirs(out, exist_ok=True)
    for frame, ins in INSERTS.items():
        src = Image.open(os.path.join(EX, 'setc', frame + '.png')).convert('RGBA')
        w, h = src.size
        dst = Image.new('RGBA', (w, h + grow(frame)), (0, 0, 0, 0))
        y_src = y_dst = 0
        for at, n, b0, b1 in sorted(ins):
            part = src.crop((0, y_src, w, at))
            dst.paste(part, (0, y_dst))
            y_dst += at - y_src
            # 거울 순서 — 띠가 위 줄에서 이어지면(띠 끝 = at) 거꾸로부터, 아래에서 이어지면(띠 시작 = at) 바로부터
            fwd = list(range(b0, b1))
            seq = []
            down = b0 == at
            while len(seq) < n:
                seq += fwd if down else fwd[::-1]
                down = not down
            for k, r in enumerate(seq[:n]):
                dst.paste(src.crop((0, r, w, r + 1)), (0, y_dst + k))
            y_dst += n
            y_src = at
        dst.paste(src.crop((0, y_src, w, h)), (0, y_dst))
        dst.save(os.path.join(out, frame + '.png'))
        print(frame, (w, h), '->', dst.size)


if __name__ == '__main__':
    build()
