"""화면 설계서(.md) 요소 트리 표 → 프리팹 빌더용 JSON 스펙 변환.

좌표계: 720×1280 좌상단 원점 (x, y, w, h)
앵커 규약: 자식은 top-left 앵커(anchorMin=anchorMax=(0,1), pivot=(0,1))로 배치하여
          설계서 좌표를 그대로 anchoredPosition=(x, -y) 로 옮긴다.
"""
import io, re, json, glob, os

DEPTH_TOKENS = ['│', '├', '└', '&nbsp;']


def depth_of(raw):
    """트리 마커로 깊이 판정. '├'/'└' 1단계, '│'·'&nbsp;' 쌍이 추가 단계."""
    if not any(t in raw for t in DEPTH_TOKENS):
        return 0
    nb = raw.count('&nbsp;')
    bar = raw.count('│')
    # &nbsp; 2개 = 1단계, │ 1개 = 1단계
    return 1 + bar + nb // 2


def clean(s):
    s = re.sub(r'&nbsp;', '', s)
    s = re.sub(r'[│├└`*⭐🟡✅⛔⚠️]', '', s)
    return s.strip()


def parse_xywh(cell):
    m = re.search(r'\((\-?\d+)\s*,\s*(\-?\d+)\s*,\s*(\d+)\s*,\s*(\d+)\)', cell)
    return [int(g) for g in m.groups()] if m else None


def comp_of(typecell, poscell):
    t = typecell.lower()
    if 'button' in t:
        return 'Button'
    if 'tmp' in t or 'text' in t:
        return 'TMP'
    if 'gridlayout' in t:
        return 'Grid'
    if 'image' in t:
        return 'ImageSliced' if '9-slice' in t or 'sliced' in t else 'Image'
    if '~ui' in t:
        return 'RootUI'
    if '~panel' in t:
        return 'RootPanel'
    return 'Group'


def parse(path):
    src = io.open(path, encoding='utf-8').read()
    rows = [l for l in src.split('\n')
            if l.startswith('|') and l.count('|') >= 6
            and '요소 이름' not in l and '---' not in l]
    items, stack = [], {}
    for l in rows:
        c = [x.strip() for x in l.strip('|').split('|')]
        if len(c) < 3:
            continue
        raw_name = c[0]
        name = clean(raw_name)
        if not name or ' ' in name.replace('×', ''):
            # '×12' 같은 반복 표기는 살리고, 서술형 셀은 버린다
            if '×' not in name:
                continue
        d = depth_of(raw_name)
        rect = parse_xywh(c[2])
        comp = comp_of(clean(c[1]), c[2])
        stack[d] = name
        parent = stack.get(d - 1) if d > 0 else None
        repeat = 1
        m = re.search(r'×\s*(\d+)', name)
        if m:
            repeat = int(m.group(1))
            name = name.split('×')[0].strip()
        items.append({
            'name': name, 'parent': parent, 'depth': d,
            'comp': comp, 'rect': rect, 'repeat': repeat,
            'stretch': 'stretch full' in c[2].lower(),
            'raw_pos': clean(c[2])[:60],
        })
    return items


if __name__ == '__main__':
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    out = {}
    for f in sorted(glob.glob('AVSR_Screen_*.md')):
        key = f.replace('AVSR_Screen_', '').replace('.md', '')
        out[key] = parse(f)
    io.open('_tree.json', 'w', encoding='utf-8').write(
        json.dumps(out, ensure_ascii=False, indent=1))
    for k, v in out.items():
        miss = [i['name'] for i in v if i['rect'] is None and not i['stretch']]
        print(f'{k:12} {len(v):3}개  좌표없음 {len(miss)}: {miss[:6]}')
