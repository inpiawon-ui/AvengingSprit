"""프리팹 스펙 좌표 + 실제 에셋으로 720×1280 화면 미리보기를 합성한다.

Unity 게임 뷰가 Screen Space - Overlay 캔버스를 캡처하지 못해, 배선 결과를
눈으로 확인하기 위한 대체 수단이다. 스펙 좌표를 그대로 쓰므로
"프리팹에 이렇게 배치돼 있다"를 그대로 반영한다.

9-slice 에셋은 명세 border 로 실제 확장 렌더링한다.
"""
import io, os, json, sys
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SPECDIR = os.path.join(ROOT, 'Assets', 'Scripts', 'Editor', 'UISpec')
BASERES = os.path.join(ROOT, 'Assets', 'BaseResource')
HOSTS = ['amazoness', 'rambo', 'wizard', 'ninja', 'mafia', 'hitman',
         'yogamaster', 'dragon', 'robot', 'snowwoman', 'slugger', 'vampire']
SCREENS = [('Title', 'TitleMainUI', 0), ('Lobby', 'LobbyMainUI', 0),
           ('HostSelect', 'HostSelectPanel', 128)]


def slice9(im, L, R, T, B, nw, nh):
    w, h = im.size
    nw, nh = max(nw, L + R + 1), max(nh, T + B + 1)
    out = Image.new('RGBA', (nw, nh), (0, 0, 0, 0))
    nc, nr = nw - L - R, nh - T - B
    P = {'tl': (0, 0, L, T), 'tr': (w - R, 0, w, T), 'bl': (0, h - B, L, h), 'br': (w - R, h - B, w, h),
         'top': (L, 0, w - R, T), 'bot': (L, h - B, w - R, h),
         'lef': (0, T, L, h - B), 'rig': (w - R, T, w, h - B), 'ctr': (L, T, w - R, h - B)}
    g = lambda k: im.crop(P[k])
    out.paste(g('tl'), (0, 0)); out.paste(g('tr'), (nw - R, 0))
    out.paste(g('bl'), (0, nh - B)); out.paste(g('br'), (nw - R, nh - B))
    if nc > 0:
        out.paste(g('top').resize((nc, T), Image.NEAREST), (L, 0))
        out.paste(g('bot').resize((nc, B), Image.NEAREST), (L, nh - B))
    if nr > 0:
        out.paste(g('lef').resize((L, nr), Image.NEAREST), (0, T))
        out.paste(g('rig').resize((R, nr), Image.NEAREST), (nw - R, T))
    if nc > 0 and nr > 0:
        out.paste(g('ctr').resize((nc, nr), Image.NEAREST), (L, T))
    return out


def main():
    imp = {a['file'][:-4]: a for a in
           json.load(io.open(os.path.join(SPECDIR, '_import.json'), encoding='utf-8'))['assets']}
    outdir = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'wireframes')
    report = []

    for key, prefab, yoff in SCREENS:
        spec = json.load(io.open(os.path.join(SPECDIR, f'{key}.json'), encoding='utf-8'))['nodes']
        canvas = Image.new('RGBA', (720, 1280), (5, 8, 18, 255))
        drawn = missing = 0
        for n in spec:
            if n['comp'] not in ('Image', 'ImageSliced', 'Button'):
                continue
            name = n['name'].lower()
            if name not in imp and f'{name}_amazoness' in imp:
                name = f'{name}_amazoness'
            if name not in imp:
                missing += 1
                continue
            a = imp[name]
            p = os.path.join(BASERES, a['prefab'], a['file'])
            if not os.path.exists(p):
                missing += 1
                continue
            im = Image.open(p).convert('RGBA')
            if n['stretch'] or not n['rect']:
                x, y, w, h = 0, yoff, 720, 1280 - yoff
            else:
                x, y, w, h = n['rect']
            if a['slice9'] and (w, h) != im.size:
                L, R, T, B = a['slice9']
                im = slice9(im, L, R, T, B, w, h)
            elif (w, h) != im.size:
                im = im.resize((w, h), Image.NEAREST)
            canvas.alpha_composite(im, (x, y))
            drawn += 1
        canvas.convert('RGB').save(os.path.join(outdir, f'AVSR_Preview_{key}.png'))
        report.append(f'{key:12} 이미지 {drawn:3}개 합성 / 에셋없음 {missing}')
    io.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '_preview_report.txt'),
            'w', encoding='utf-8').write('\n'.join(report))


if __name__ == '__main__':
    main()
