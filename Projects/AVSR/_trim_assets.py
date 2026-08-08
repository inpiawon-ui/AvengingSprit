"""투명 여백이 남은 에셋을 잘라낸다.

레이아웃 박스는 **목업에서 눈에 보이는 내용의 범위**로 쟀다. 그런데 에셋 안에
투명 여백이 있으면 내용이 그 박스보다 작게 그려진다 — 아이콘이 헐거워 보이는 원인이다.

9-slice 는 제외한다. 가장자리를 자르면 늘어나는 테두리 폭이 어긋난다.
"""
import json, os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')
IMPORT = os.path.join(ROOT, 'Assets', 'Scripts', 'Editor', 'UISpec', '_import.json')

# 잘라내면 안 되는 것 — 배경·바닥은 화면을 꽉 채워야 하고, 채움 바는 폭을 코드가 조절한다
KEEP = ('background', 'floor', 'fill', 'cooldown')

MIN_MARGIN = 3      # 이보다 작은 여백은 손대지 않는다 (아틀라스 여백 역할)


def slice9_set():
    rows = json.load(open(IMPORT, encoding='utf-8'))['assets']
    return {r['file'].lower() for r in rows if r.get('slice9')}


def main():
    protected = slice9_set()
    trimmed = 0

    for folder in sorted(os.listdir(BASE)):
        d = os.path.join(BASE, folder)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith('.png'):
                continue
            low = f.lower()
            if low in protected or any(k in low for k in KEEP):
                continue

            p = os.path.join(d, f)
            im = Image.open(p)
            if im.mode != 'RGBA':
                continue
            box = im.getchannel('A').getbbox()
            if box is None:
                continue

            l, t, r, b = box
            w, h = im.size
            margin = (l, t, w - r, h - b)
            if max(margin) < MIN_MARGIN:
                continue

            im.crop(box).save(p)
            trimmed += 1
            print(f'  {folder}/{f}  {w}x{h} → {r-l}x{b-t}'
                  f'   (여백 좌{margin[0]} 상{margin[1]} 우{margin[2]} 하{margin[3]})')

    print(f'\n잘라낸 에셋 {trimmed}개 · 9-slice 보호 {len(protected)}개')


if __name__ == '__main__':
    main()
