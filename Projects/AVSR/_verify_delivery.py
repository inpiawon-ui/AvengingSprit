"""납품 ZIP/폴더를 AVSR_AssetManifest.md §B 명세와 기계 대조한다.

검사 항목
  1) 파일 존재      — 누락 / 명세에 없는 과잉
  2) 규격           — width × height
  3) 알파           — alpha=Y 인데 불투명, alpha=N 인데 투명
  4) 픽셀 규율      — 반투명 픽셀(안티앨리어싱) 검출
  5) 색 수          — 스프라이트당 고유 RGB
  6) 9-slice 무결성 — 실측 테두리 두께가 slice9 값을 넘으면 확장 시 장식이 잘린다
  7) 팔레트 이탈    — 녹색 계열(hue 60~180°) 검출. 이 게임 팔레트에 녹색은 없다
"""
import io, os, re, sys, colorsys
from PIL import Image
import numpy as np

HOSTS = ['amazoness', 'rambo', 'wizard', 'ninja', 'mafia', 'hitman',
         'yogamaster', 'dragon', 'robot', 'snowwoman', 'slugger', 'vampire']
MANIFEST = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'AVSR_AssetManifest.md')


def load_spec():
    s = io.open(MANIFEST, encoding='utf-8').read()
    spec = {}
    # 주의: 파일명에 `{hostKey}` 처럼 대문자가 섞이고, pivot 은 `**left**` 로 강조된 행이 있다.
    pat = re.compile(
        r'^\|\s*`([A-Za-z0-9_{}]+\.png)`[^|]*\|[^|]*\|\s*\**(\d+)\**\s*\|\s*\**(\d+)\**\s*\|'
        r'\s*\**([YN])\**\s*\|\s*\**([0-9,\-]+)\**\s*\|\s*\**([a-z\-]+)\**\s*\|',
        re.M)
    for m in pat.finditer(s):
        fn, w, h, a, s9, piv = m.groups()
        names = [fn.replace('{hostKey}', k) for k in HOSTS] if '{hostKey}' in fn else [fn]
        for n in names:
            spec[n] = {'w': int(w), 'h': int(h), 'alpha': a,
                       'slice9': None if s9.strip() == '-' else [int(x) for x in s9.split(',')],
                       'pivot': piv}
    return spec


def border_thickness(a, alpha_mask):
    """중앙 행/열에서 배경색과 달라지는 구간 = 테두리 실측 두께."""
    H, W = a.shape[:2]
    col = a[:, W // 2, :3].astype(int)
    row = a[H // 2, :, :3].astype(int)
    cb, rb = col[H // 2], row[W // 2]
    T = next((y for y in range(H // 2) if abs(col[y] - cb).sum() < 12), 0)
    B = next((y for y in range(H // 2) if abs(col[H - 1 - y] - cb).sum() < 12), 0)
    L = next((x for x in range(W // 2) if abs(row[x] - rb).sum() < 12), 0)
    R = next((x for x in range(W // 2) if abs(row[W - 1 - x] - rb).sum() < 12), 0)
    return L, R, T, B


def check(folder):
    spec = load_spec()
    have = {f for f in os.listdir(folder) if f.lower().endswith('.png')}
    have.discard('AVSR_UI_full_contact_sheet.png')

    missing = sorted(set(spec) - have)
    extra = sorted(have - set(spec))
    rows, issues = [], []

    for fn in sorted(have & set(spec)):
        sp = spec[fn]
        im = Image.open(os.path.join(folder, fn)).convert('RGBA')
        a = np.array(im)
        al = a[:, :, 3]
        m = al > 8
        if not m.any():
            issues.append(f'{fn}: 전부 투명'); continue

        # 2) 규격
        if (im.width, im.height) != (sp['w'], sp['h']):
            issues.append(f'{fn}: 규격 {im.width}x{im.height} ≠ 명세 {sp["w"]}x{sp["h"]}')

        # 3) 알파
        transparent = bool((al < 248).any())
        if sp['alpha'] == 'Y' and not transparent:
            issues.append(f'{fn}: alpha=Y 인데 투명 영역 없음')
        if sp['alpha'] == 'N' and transparent:
            issues.append(f'{fn}: alpha=N 인데 투명 픽셀 존재')

        # 4) 안티앨리어싱
        semi = int(((al > 8) & (al < 248)).sum())
        if semi > 0:
            issues.append(f'{fn}: 반투명 픽셀 {semi}개 (안티앨리어싱)')

        # 5) 색 수
        cols = len({tuple(c) for c in a[m][:, :3]})
        if cols > 16:
            issues.append(f'{fn}: 색 {cols}개 (상한 16)')

        # 6) 9-slice
        s9warn = ''
        if sp['slice9']:
            L, R, T, B = border_thickness(a, m)
            sL, sR, sT, sB = sp['slice9']
            over = [f'{k}({v}<{d})' for k, v, d in
                    [('L', sL, L), ('R', sR, R), ('T', sT, T), ('B', sB, B)] if v < d]
            if over:
                s9warn = ' '.join(over)
                issues.append(f'{fn}: slice9 부족 {s9warn} — 확장 시 테두리 잘림')

        # 7) 녹색 이탈
        g = 0
        for c in {tuple(x) for x in a[m][:, :3]}:
            h, l, s = colorsys.rgb_to_hls(*[v / 255 for v in c])
            if 60 <= h * 360 < 180 and s > 0.2 and 0.15 < l < 0.9:
                g += 1
        if g:
            issues.append(f'{fn}: 녹색 계열 {g}색 (팔레트 이탈)')

        rows.append((fn, cols, semi, s9warn, g))

    out = []
    out.append(f'명세 {len(spec)}개 / 납품 {len(have)}개 / 대조 {len(rows)}개')
    out.append('')
    out.append(f'[1] 누락 {len(missing)}건')
    out += ['    ' + x for x in missing[:20]]
    out.append(f'[2] 명세에 없는 과잉 {len(extra)}건')
    out += ['    ' + x for x in extra[:20]]
    out.append('')
    out.append(f'[3] 문제 {len(issues)}건')
    out += ['    ' + x for x in issues[:40]]
    if len(issues) > 40:
        out.append(f'    ... 외 {len(issues)-40}건')
    io.open(os.path.join(os.path.dirname(MANIFEST), '_verify_result.txt'),
            'w', encoding='utf-8').write('\n'.join(out))


if __name__ == '__main__':
    check(sys.argv[1])
