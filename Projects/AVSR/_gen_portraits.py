"""호스트 초상을 **우리 스프라이트에서** 만든다.

원작 HUD 시트에도 초상 22종이 있지만 16×16 이고, 어느 칸이 누구인지 확실히
가려낼 방법이 없다(캐릭터끼리 살색·검정을 공유해 팔레트 대조로도 안 갈린다).

우리는 이미 22종의 정면 idle 을 96×96 으로 갖고 있다. 그 머리를 잘라내면
 · 짝을 지을 필요가 없다 — 파일 이름이 곧 캐릭터다
 · 원작 초상보다 해상도가 높다
 · 캐릭터를 다시 뽑으면 초상도 같이 갱신된다

머리 범위는 불투명 영역의 위쪽 일부다. 비율로 자르면 드래곤처럼 머리가 큰 종과
사람처럼 작은 종이 함께 어긋나므로, **가로로 가장 넓어지는 지점**까지를 머리로 본다 —
사람은 어깨, 드래곤은 몸통이 시작되는 곳이다.

사용: python _gen_portraits.py
산출: Assets/BaseResource/HostSelectPanel/hostportrait_{슬러그}.png (64×64)
"""
import io
import os
import sys

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'HostSelectPanel')

SIZE = 64          # 초상 캔버스
PAD = 3            # 머리 둘레 여백(원본 픽셀)


def head_box(im):
    """
    머리 범위. **몸 높이의 위 38%** 를 잘라 가로는 그 구간의 폭에 맞춘다.

    처음에는 "가로가 급히 넓어지는 곳이 어깨" 라는 규칙을 썼는데, 드래곤처럼
    머리가 몸통만큼 넓은 종에서 통째로 잡히고 사람은 머리카락만 잡혔다
    (13px ~ 62px 로 들쭉날쭉했다). 종마다 다른 판정보다 **모두 같은 비율**이
    초상으로는 더 고르게 나온다.
    """
    px = im.load()
    w, h = im.size
    rows = []
    for y in range(h):
        xs = [x for x in range(w) if px[x, y][3] > 8]
        rows.append((min(xs), max(xs)) if xs else None)

    top = next((y for y, r in enumerate(rows) if r), None)
    if top is None:
        return None
    bottom_body = max(y for y, r in enumerate(rows) if r)

    head_h = max(16, int((bottom_body - top + 1) * 0.38))
    bottom = min(bottom_body, top + head_h)

    xs = [r for r in rows[top:bottom + 1] if r]
    x0 = min(a for a, _ in xs)
    x1 = max(b for _, b in xs)
    return x0, top, x1 + 1, bottom + 1


def main():
    os.makedirs(OUT, exist_ok=True)
    n = 0
    for key in sorted(os.listdir(UNIT)):
        d = os.path.join(UNIT, key)
        src = os.path.join(d, f'unit_{key}_s.png')
        if not os.path.isdir(d) or not os.path.exists(src):
            continue

        im = Image.open(src).convert('RGBA')
        box = head_box(im)
        if box is None:
            continue
        x0, y0, x1, y1 = box
        x0 = max(0, x0 - PAD); y0 = max(0, y0 - PAD)
        x1 = min(im.width, x1 + PAD); y1 = min(im.height, y1 + PAD)
        head = im.crop((x0, y0, x1, y1))

        # 정수 배율로만 키운다. 소수 배율은 도트를 뭉갠다.
        z = max(1, min(SIZE // max(head.width, head.height), 8))
        big = head.resize((head.width * z, head.height * z), Image.NEAREST)

        out = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
        out.paste(big, ((SIZE - big.width) // 2, (SIZE - big.height) // 2), big)
        out.save(os.path.join(OUT, f'hostportrait_{key}.png'))
        print(f'{key:<20} 머리 {x1-x0}x{y1-y0} → {z}배')
        n += 1
    print(f'\n초상 {n}장 → {OUT}')


if __name__ == '__main__':
    main()
