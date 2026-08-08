"""화면별 에셋 컨택트 시트 — 전수 육안 확인용.

수치 검사(_audit_ui.py)로는 못 잡는 것들이 있다 — 내용이 잘못 들어갔거나,
글자가 구워져 있거나, 투명해야 할 곳이 불투명하거나.

체커보드 위에 얹어 **알파가 없는 에셋**이 바로 드러나게 한다.
불투명 에셋은 뒤 배경을 사각으로 가리므로, 아이콘류는 알파가 있어야 한다.
"""
import os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')
OUT = os.environ.get('SHEET_OUT', HERE)

CELL_W, CELL_H, COLS = 150, 156, 8
LABEL_H = 30


def checker(w, h, size=8):
    im = Image.new('RGBA', (w, h), (58, 60, 72, 255))
    d = ImageDraw.Draw(im)
    for y in range(0, h, size):
        for x in range(0, w, size):
            if (x // size + y // size) % 2:
                d.rectangle([x, y, x + size - 1, y + size - 1], fill=(78, 80, 94, 255))
    return im


def sheet(folder, names=None):
    d = os.path.join(BASE, folder)
    files = sorted(f for f in os.listdir(d) if f.endswith('.png'))
    if names:
        files = [f for f in files if f[:-4] in names]
    rows = (len(files) + COLS - 1) // COLS
    im = Image.new('RGBA', (CELL_W * COLS, CELL_H * rows), (30, 32, 42, 255))
    dr = ImageDraw.Draw(im)

    for i, f in enumerate(files):
        a = Image.open(os.path.join(d, f)).convert('RGBA')
        opaque = a.getchannel('A').getextrema()[0] > 250
        cw, ch = CELL_W - 16, CELL_H - LABEL_H - 10
        k = min(cw / a.width, ch / a.height, 3)
        thumb = a.resize((max(1, int(a.width * k)), max(1, int(a.height * k))), Image.NEAREST)

        cx = (i % COLS) * CELL_W + 8
        cy = (i // COLS) * CELL_H + LABEL_H
        im.alpha_composite(checker(thumb.width, thumb.height), (cx, cy))
        im.alpha_composite(thumb, (cx, cy))
        dr.rectangle([cx - 1, cy - 1, cx + thumb.width, cy + thumb.height],
                     outline=(255, 60, 140, 255))

        # 불투명 에셋은 이름을 붉게 — 아이콘이면 알파가 있어야 한다
        color = (255, 140, 120, 255) if opaque else (150, 235, 170, 255)
        dr.text((cx, cy - 22), f[:-4][:20], fill=color)
        dr.text((cx, cy - 11), f'{a.width}x{a.height}' + ('  불투명' if opaque else ''),
                fill=(190, 200, 215, 255))

    p = os.path.join(OUT, f'sheet_{folder}.png')
    im.save(p)
    print(f'{folder}: {len(files)}개 → {p}')


if __name__ == '__main__':
    for folder in (sys.argv[1:] or ['InGameMainUI', 'LobbyMainUI', 'HostSelectPanel', 'TitleMainUI']):
        sheet(folder)
