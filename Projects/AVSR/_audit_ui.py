"""UI 에셋 감사 — 레이아웃 박스와 실제 이미지가 맞는지 수치로 대조한다.

눈으로 "괜찮아 보인다" 로 넘어가면 크롭 오차가 그대로 남는다. 세 가지를 잰다.

1. 왜곡    레이아웃 박스와 이미지의 가로·세로 배율이 다르면 늘어나 보인다.
2. 여백    이미지 안에서 내용이 차지하는 범위. 여백이 크면 크롭이 헐거운 것이고,
           내용이 가장자리에 닿아 있으면 잘려 나간 것이다.
3. 배율    픽셀아트를 비정수 배율로 늘리면 점이 뭉갠다.

9-slice(늘어나도 되는 프레임)는 왜곡 검사에서 제외한다 — `_import.json` 의 slice9 기준.
"""
import json, os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SPEC = os.path.join(ROOT, 'Assets', 'Scripts', 'Editor', 'UISpec')
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')

SCREENS = {
    'Lobby':      'LobbyMainUI',
    'HostSelect': 'HostSelectPanel',
    'InGame':     'InGameMainUI',
    'Title':      'TitleMainUI',
}

# 늘어나도 되는 것 — 9-slice 프레임, 바탕, 채움 바
STRETCHABLE = ('frame', 'card', 'button', 'background', 'bg', 'bar', 'fill',
               'panel', 'counter', 'widget', 'title', 'tipbar', 'floor')


def content_box(im):
    """알파가 있으면 알파 기준, 없으면 배경색과 다른 픽셀 기준으로 내용 범위를 잡는다."""
    if im.mode != 'RGBA':
        im = im.convert('RGBA')
    a = im.getchannel('A')
    if a.getextrema()[0] < 250:            # 투명 영역이 있다
        return a.getbbox()
    # 완전 불투명 — 모서리 색을 배경으로 보고 다른 픽셀을 찾는다
    rgb = im.convert('RGB')
    bg = rgb.getpixel((0, 0))
    w, h = rgb.size
    px = rgb.load()
    xs, ys = [], []
    for y in range(0, h, max(1, h // 120)):
        for x in range(0, w, max(1, w // 120)):
            r, g, b = px[x, y]
            if abs(r - bg[0]) + abs(g - bg[1]) + abs(b - bg[2]) > 40:
                xs.append(x); ys.append(y)
    if not xs:
        return None
    return (min(xs), min(ys), max(xs) + 1, max(ys) + 1)


def audit():
    imports = {}
    ip = os.path.join(SPEC, '_import.json')
    if os.path.exists(ip):
        for row in json.load(open(ip, encoding='utf-8')).get('items', []):
            imports[row.get('filename', '').lower()] = row

    rows = []
    for screen, folder in SCREENS.items():
        lp = os.path.join(SPEC, f'_layout_{screen}.json')
        if not os.path.exists(lp):
            continue
        spec = json.load(open(lp, encoding='utf-8'))
        adir = os.path.join(BASE, folder)

        for it in spec['items']:
            name = it['name'].split('/')[-1]
            png = os.path.join(adir, name.lower() + '.png')
            if not os.path.exists(png):
                continue
            im = Image.open(png)
            sw, sh = im.size
            bw, bh = it['w'], it['h']
            if sw == 0 or sh == 0 or bw == 0 or bh == 0:
                continue

            sx, sy = bw / sw, bh / sh
            skew = max(sx, sy) / min(sx, sy)
            box = content_box(im)
            if box:
                l, t, r, b = box
                margin = (l, t, sw - r, sh - b)
                clipped = [s for s, v in zip('좌상우하', margin) if v == 0]
            else:
                margin, clipped = (0, 0, 0, 0), []

            rows.append(dict(screen=screen, name=name, sw=sw, sh=sh,
                             bw=round(bw), bh=round(bh), sx=sx, sy=sy,
                             skew=skew, margin=margin, clipped=''.join(clipped),
                             stretch=any(k in name.lower() for k in STRETCHABLE),
                             slice9=bool(imports.get(name.lower() + '.png', {}).get('slice9'))))
    return rows


def main():
    rows = audit()
    print(f'대상 {len(rows)}개\n')

    bad = [r for r in rows if not r['stretch'] and not r['slice9'] and r['skew'] > 1.12]
    print(f'■ 가로·세로 배율이 달라 늘어남 ({len(bad)}개)')
    for r in sorted(bad, key=lambda x: -x['skew'])[:20]:
        print(f"   {r['screen']:<10} {r['name']:<24} 원본 {r['sw']}x{r['sh']} → 박스 {r['bw']}x{r['bh']}"
              f"   x{r['sx']:.2f} / y{r['sy']:.2f}  왜곡 {r['skew']:.2f}배")

    loose = [r for r in rows if not r['stretch'] and max(r['margin']) > max(r['sw'], r['sh']) * 0.14]
    print(f'\n■ 크롭이 헐거움 — 이미지 안 여백 ({len(loose)}개)')
    for r in sorted(loose, key=lambda x: -max(x['margin']))[:20]:
        print(f"   {r['screen']:<10} {r['name']:<24} {r['sw']}x{r['sh']}  여백 좌{r['margin'][0]} 상{r['margin'][1]} 우{r['margin'][2]} 하{r['margin'][3]}")

    # 잘려 나갔는지 여부는 여백을 잘라낸 뒤에는 판정할 수 없다(내용이 항상 가장자리에 닿는다).
    # 대신 알파가 없는 불투명 에셋만 본다 — 이쪽은 트리밍 대상이 아니라 판정이 유효하다.
    clip = [r for r in rows if len(r['clipped']) >= 3 and not r['stretch'] and not r['slice9']
            and Image.open(os.path.join(BASE, SCREENS[r['screen']], r['name'].lower() + '.png')).mode != 'RGBA']
    print(f'\n■ 불투명 에셋인데 내용이 가장자리에 닿음 — 잘렸을 가능성 ({len(clip)}개)')
    for r in clip[:20]:
        print(f"   {r['screen']:<10} {r['name']:<24} {r['sw']}x{r['sh']}  닿은 변: {r['clipped']}")

    nonint = [r for r in rows if not r['stretch'] and not r['slice9']
              and abs(r['sx'] - round(r['sx'])) > 0.08 and r['sx'] > 1.02]
    print(f'\n■ 비정수 확대 — 픽셀이 뭉갬 ({len(nonint)}개)')
    for r in sorted(nonint, key=lambda x: -x['sx'])[:20]:
        print(f"   {r['screen']:<10} {r['name']:<24} {r['sw']}x{r['sh']} → {r['bw']}x{r['bh']}  x{r['sx']:.2f}")


if __name__ == '__main__':
    main()
