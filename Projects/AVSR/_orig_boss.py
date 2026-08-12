"""원작 보스 시트를 조각으로 나누고 크기를 잰다.

보스는 캐릭터와 다르다. **한 장짜리 스프라이트가 아니라 부품 묶음**이다 —
아레나 배경, 움직이는 장치, 몸통, 피격 섬광이 따로 그려져 있다.
그래서 96×96 같은 공통 캔버스를 쓸 수 없고 부품마다 규격을 따로 정해야 한다.

조각 찾기는 연결 성분으로 한다(`_orig_hud.py` 와 같은 방식).
격자 위치를 손으로 적으면 시트가 조금만 달라져도 전부 어긋난다.

사용: python _orig_boss.py
산출: _exchange/out/17_boss/{보스}/  조각 PNG(2배) + 번호 붙인 대조 시트 + 치수표
"""
import io
import os
import shutil
import sys
from collections import Counter, deque

from PIL import Image, ImageDraw

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'Reference', 'Original')
OUT = os.path.join(HERE, '_exchange', 'out', '17_boss')

# 정본 CH1~3 보스. 나머지 셋(Guardian·Kingpin·Sludge)은 정본 v1.5 밖이다.
BOSSES = {
    'robot_snakes': 'Bosses - Robot Snakes',
    'demolisher':   'Bosses - Crusher',
    'python':       'Bosses - Python',
}

MIN_AREA = 300      # 이보다 작은 덩어리는 글자·먼지다
ZOOM = 2            # 우리 규격 배율(원작 캐릭터 40여px → 우리 76px)


def bg_key(im):
    px = im.load()
    w, h = im.size
    c = Counter([px[0, 0], px[w - 1, 0], px[0, h - 1], px[w - 1, h - 1]])
    return c.most_common(1)[0][0]


def components(im, key):
    px = im.load()
    w, h = im.size
    seen = [[False] * h for _ in range(w)]
    out = []
    for y in range(h):
        for x in range(w):
            if seen[x][y] or px[x, y] == key:
                continue
            q = deque([(x, y)])
            seen[x][y] = True
            x0 = x1 = x
            y0 = y1 = y
            n = 0
            while q:
                cx, cy = q.popleft()
                n += 1
                x0 = min(x0, cx); x1 = max(x1, cx)
                y0 = min(y0, cy); y1 = max(y1, cy)
                # 도트 그림은 모서리로만 붙은 부분이 많아 대각선까지 잇는다
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        nx, ny = cx + dx, cy + dy
                        if (0 <= nx < w and 0 <= ny < h and not seen[nx][ny]
                                and px[nx, ny] != key):
                            seen[nx][ny] = True
                            q.append((nx, ny))
            if n >= MIN_AREA:
                out.append((x0, y0, x1 + 1, y1 + 1, n))
    out.sort(key=lambda b: (b[1], b[0]))
    return out


def main():
    shutil.rmtree(OUT, ignore_errors=True)
    for key, sheet in BOSSES.items():
        d = os.path.join(OUT, key)
        os.makedirs(d, exist_ok=True)

        im = Image.open(os.path.join(SRC, sheet + '.png')).convert('RGB')
        bg = bg_key(im)
        boxes = components(im, bg)

        rgba = im.convert('RGBA')
        px = rgba.load()
        for y in range(rgba.height):
            for x in range(rgba.width):
                if px[x, y][:3] == bg:
                    px[x, y] = (0, 0, 0, 0)

        lines = [f'# {key} — {sheet}', '',
                 f'원작 시트 {im.width}x{im.height} · 조각 {len(boxes)}개',
                 '',
                 '| 번호 | 원작 크기 | 우리 크기(x2) | 화소 |',
                 '|---|---|---|---|']
        for i, (x0, y0, x1, y1, n) in enumerate(boxes):
            cut = rgba.crop((x0, y0, x1, y1))
            cut.resize((cut.width * ZOOM, cut.height * ZOOM), Image.NEAREST).save(
                os.path.join(d, f'part_{i:02d}.png'))
            lines.append(f'| {i:02d} | {x1-x0}x{y1-y0} | '
                         f'{(x1-x0)*ZOOM}x{(y1-y0)*ZOOM} | {n} |')

        # 번호를 붙여 한 장에 모은다 — 어느 조각이 무엇인지 눈으로 짚어야 한다
        per = 4
        cw = max(b[2] - b[0] for b in boxes) + 12
        ch = max(b[3] - b[1] for b in boxes) + 26
        rows = (len(boxes) + per - 1) // per
        sheet_im = Image.new('RGB', (per * cw, rows * ch), (26, 26, 34))
        dr = ImageDraw.Draw(sheet_im)
        for i, (x0, y0, x1, y1, _) in enumerate(boxes):
            cut = rgba.crop((x0, y0, x1, y1))
            cx = (i % per) * cw + 6
            cy = (i // per) * ch + 18
            sheet_im.paste(cut, (cx, cy), cut)
            dr.text((cx, cy - 14), f'{i:02d}  {x1-x0}x{y1-y0}', fill=(220, 220, 236))
        sheet_im.save(os.path.join(d, '00_parts.png'))

        io.open(os.path.join(d, 'SIZES.md'), 'w', encoding='utf-8').write(
            '\n'.join(lines) + '\n')

        # 색 규율 — 캐릭터 작업과 같은 팔레트 파일을 함께 둔다
        pal = os.path.join(HERE, '_exchange', 'out', '15_original',
                           f'{sheet}__palette.png')
        if os.path.exists(pal):
            shutil.copy2(pal, os.path.join(d, '00_palette.png'))
        else:
            print(f'  팔레트 없음 {sheet}')

        print(f'{key:<14} 조각 {len(boxes):>2}개 → {d}')


if __name__ == '__main__':
    main()
