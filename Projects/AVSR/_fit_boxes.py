"""레이아웃 박스를 실제 에셋 비율에 맞춘다.

목업에서 잰 박스와 납품된 에셋의 가로세로 비가 다르면 둘 중 하나가 일어난다.
  · 그대로 늘리면 → 아이콘이 눌리거나 길어진다
  · 비율을 지키면 → 박스 안에 남는 여백만큼 작게 그려진다

둘 다 틀렸다. 박스를 **에셋 비율로 다시 잡되 중심과 큰 쪽 치수를 유지**해서
원래 자리에 원래 크기로 놓이게 한다. 에셋이 바뀌면 다시 돌리면 된다.

늘어나야 하는 것은 건드리지 않는다 — 9-slice 프레임, 배경·바닥, 채움 바.
"""
import json, os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')
IMPORT = os.path.join(ROOT, 'Assets', 'Scripts', 'Editor', 'UISpec', '_import.json')

ALWAYS_STRETCH = ('background', 'floor', 'fill', 'cooldown')
# 비율이 크게 어긋날 때만 손댄다. 미세한 차이까지 맞추면 목업 실측이 흔들린다.
SKEW_LIMIT = 1.06


def _slice9():
    try:
        rows = json.load(open(IMPORT, encoding='utf-8'))['assets']
    except (OSError, KeyError):
        return set()
    return {r['file'].lower() for r in rows if r.get('slice9')}


def fit(items, folder):
    """items 를 제자리에서 수정하고, 바뀐 항목 목록을 돌려준다."""
    adir = os.path.join(BASE, folder)
    if not os.path.isdir(adir):
        return []
    protected = _slice9()
    changed = []

    for it in items:
        name = it['name'].split('/')[-1]
        low = name.lower()
        if low + '.png' in protected or any(k in low for k in ALWAYS_STRETCH):
            continue

        png = os.path.join(adir, low + '.png')
        if not os.path.exists(png):
            continue

        sw, sh = Image.open(png).size
        bw, bh = it['w'], it['h']
        if sw <= 0 or sh <= 0 or bw <= 0 or bh <= 0:
            continue

        sx, sy = bw / sw, bh / sh
        if max(sx, sy) / min(sx, sy) < SKEW_LIMIT:
            continue

        # 큰 쪽 배율을 버리고 작은 쪽에 맞춘다 — 박스 밖으로 넘치지 않는다
        k = min(sx, sy)
        nw, nh = round(sw * k, 1), round(sh * k, 1)
        it['x'] = round(it['x'] + (bw - nw) * 0.5, 1)
        it['y'] = round(it['y'] + (bh - nh) * 0.5, 1)
        it['w'], it['h'] = nw, nh
        changed.append((name, f'{bw:g}x{bh:g}', f'{nw:g}x{nh:g}'))

    return changed


def report(changed, screen):
    if not changed:
        print(f'   [{screen}] 비율 보정 없음')
        return
    print(f'   [{screen}] 비율 보정 {len(changed)}개')
    for n, a, b in changed:
        print(f'      {n:<24} {a} → {b}')
