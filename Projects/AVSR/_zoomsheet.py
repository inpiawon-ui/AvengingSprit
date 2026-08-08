"""에셋 확대 검수 시트 — 한 장씩 크게 보기 위한 것.

_contact_sheet.py 는 150px 셀이라 작은 아이콘의 결함(배경 잔재·뭉갬·내용 오류)이
안 보인다. 여기서는 셀을 크게 잡고 페이지로 나눠, 실제로 눈에 보이는 크기로 띄운다.

  python _zoomsheet.py <폴더> [페이지당개수]
"""
import os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')

CELL = 300          # 셀 한 변
COLS = 4
LABEL = 30


def checker(w, h, s=12):
    im = Image.new('RGBA', (w, h), (52, 54, 66, 255))
    d = ImageDraw.Draw(im)
    for y in range(0, h, s):
        for x in range(0, w, s):
            if (x // s + y // s) % 2:
                d.rectangle([x, y, x + s - 1, y + s - 1], fill=(74, 76, 90, 255))
    return im


def main():
    folder = sys.argv[1]
    per = int(sys.argv[2]) if len(sys.argv) > 2 else 16
    d = os.path.join(BASE, folder)
    files = sorted(f for f in os.listdir(d) if f.endswith('.png'))

    for page in range((len(files) + per - 1) // per):
        chunk = files[page * per:(page + 1) * per]
        rows = (len(chunk) + COLS - 1) // COLS
        im = Image.new('RGBA', (CELL * COLS, (CELL + LABEL) * rows), (24, 26, 34, 255))
        dr = ImageDraw.Draw(im)

        for i, f in enumerate(chunk):
            a = Image.open(os.path.join(d, f)).convert('RGBA')
            # 정수배 확대(NEAREST)로 픽셀을 그대로 보여준다. 축소가 필요하면 LANCZOS.
            k = min((CELL - 20) / a.width, (CELL - 20) / a.height)
            if k >= 1:
                k = max(1, int(k))
                t = a.resize((a.width * k, a.height * k), Image.NEAREST)
            else:
                t = a.resize((max(1, int(a.width * k)), max(1, int(a.height * k))), Image.LANCZOS)

            cx = (i % COLS) * CELL + (CELL - t.width) // 2
            cy = (i // COLS) * (CELL + LABEL) + LABEL + (CELL - LABEL - t.height) // 2
            im.alpha_composite(checker(t.width, t.height), (cx, cy))
            im.alpha_composite(t, (cx, cy))
            dr.rectangle([cx - 1, cy - 1, cx + t.width, cy + t.height], outline=(255, 60, 140, 255))

            lx = (i % COLS) * CELL + 6
            ly = (i // COLS) * (CELL + LABEL) + 6
            opaque = a.getchannel('A').getextrema()[0] > 250
            dr.text((lx, ly), f[:-4], fill=(255, 150, 130, 255) if opaque else (160, 240, 180, 255))
            dr.text((lx, ly + 12), f'{a.width}x{a.height} x{k}' + ('  불투명' if opaque else ''),
                    fill=(180, 190, 205, 255))

        p = os.path.join(HERE, f'_zoom_{folder}_{page + 1}.png')
        im.save(p)
        print(p)


if __name__ == '__main__':
    main()
