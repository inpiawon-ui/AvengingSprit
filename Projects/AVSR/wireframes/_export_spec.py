"""_tree.json → Unity 프리팹 빌더용 최종 스펙 (Assets/Scripts/Editor/UISpec/).

처리:
 - StatRow_ATK/SPD/DASH 의 '위와 동일 구성' 축약을 StatRow_HP 기준으로 전개 (y offset +38/+76/+114)
 - HostSlot ×12 는 GridLayoutGroup 자식이므로 rect 없이 repeat 로 표기
 - 절대좌표 유지 (부모 기준 로컬 변환은 C# 빌더가 수행)
"""
import io, json, os, copy

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', '..', 'Assets', 'Scripts', 'Editor', 'UISpec')

ROOT_TYPE = {'Title': 'RootUI', 'Lobby': 'RootUI', 'HostSelect': 'RootPanel'}

# `TopHudGroup` 은 S2가 소유하고 S3는 참조만 한다 (설계서 S3 §3 "이 패널의 자식 아님").
# 따라서 HostSelect 스펙에서만 제거한다. Lobby 에서 지우면 HUD 자식 전체가 고아가 된다.
SKIP_BY_SCREEN = {'HostSelect': {'TopHudGroup'}}


def expand_statrows(items):
    """StatRow_ATK/SPD/DASH 의 '위와 동일 구성' 축약 행을,
    StatRow_HP 의 자식 4종(StatLabelText·StatBarBg·StatBarFill·StatValueText)으로
    **원래 자리에 끼워 넣어** 전개한다. 리스트 끝에 붙이면 깊이 기반 부모 계산이 깨진다."""
    hp_row = next((i for i in items if i['name'] == 'StatRow_HP'), None)
    if hp_row is None:
        return items
    base_depth = hp_row['depth']
    # HP 행의 자식 중 아이콘을 뺀 4종 (표 등장 순서 유지)
    base = [i for i in items
            if i['depth'] > base_depth
            and not i['name'].startswith('StatIcon_')
            and items.index(i) > items.index(hp_row)
            and items.index(i) < next((items.index(x) for x in items
                                       if x['name'] == 'StatRow_ATK'), len(items))]
    offsets = {'StatRow_ATK': 38, 'StatRow_SPD': 76, 'StatRow_DASH': 114}
    out = []
    for i in items:
        if i['name'].startswith('StatLabelText/'):
            # 축약 행 — 직전 StatRow_X 를 찾아 그 오프셋으로 전개
            owner = next((o for o in reversed(out) if o['name'] in offsets), None)
            if owner is None:
                continue
            off = offsets[owner['name']]
            for b in base:
                c = copy.deepcopy(b)
                c['depth'] = b['depth']
                if c['rect']:
                    c['rect'] = [c['rect'][0], c['rect'][1] + off, c['rect'][2], c['rect'][3]]
                out.append(c)
            continue
        out.append(i)
    return out


def build(key, items):
    skip = SKIP_BY_SCREEN.get(key, set())
    items = [i for i in items if i['name'] not in skip]
    if key == 'HostSelect':
        items = expand_statrows(items)
    # 깊이 정규화 — 마크다운 트리 마커(`│ &nbsp;│ &nbsp;└`)가 단계를 건너뛰는 경우가 있다.
    # 직전 깊이 + 1 을 넘지 못하게 눌러 연속 깊이로 만든다.
    prev = 0
    for i in items:
        i['depth'] = min(i['depth'], prev + 1)
        prev = i['depth']

    # 이름 중복(NotifyBadge 6곳 등)이 있으므로 부모를 인덱스로 지정한다.
    # 직전에 등장한 같은 깊이-1 노드가 부모다.
    nodes, last_at_depth = [], {}
    for idx, i in enumerate(items):
        d = i['depth']
        parent_idx = last_at_depth.get(d - 1, -1) if d > 0 else (0 if idx > 0 else -1)
        last_at_depth[d] = idx
        # 더 깊은 단계의 기록은 무효화 (형제로 돌아왔을 때 오염 방지)
        for k in [k for k in last_at_depth if k > d]:
            del last_at_depth[k]
        n = {
            'name': i['name'],
            'parentIdx': parent_idx,
            'comp': i['comp'],
            'stretch': i['stretch'],
            'repeat': i['repeat'],
            'rect': i['rect'] if i['rect'] else [],
        }
        nodes.append(n)
    # 루트 타입 보정
    if nodes:
        nodes[0]['comp'] = ROOT_TYPE[key]
    return {
        'screen': key,
        'root': nodes[0]['name'] if nodes else None,
        'referenceResolution': [720, 1280],
        'nodes': nodes,
    }


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    tree = json.load(io.open(os.path.join(HERE, '_tree.json'), encoding='utf-8'))
    for key, items in tree.items():
        spec = build(key, items)
        p = os.path.join(OUT, f'{key}.json')
        io.open(p, 'w', encoding='utf-8').write(
            json.dumps(spec, ensure_ascii=False, indent=1))
        norect = [n['name'] for n in spec['nodes'] if 'rect' not in n and not n['stretch']]
        print(f"{key:12} root={spec['root']:16} nodes={len(spec['nodes']):3}  rect없음={norect}")
