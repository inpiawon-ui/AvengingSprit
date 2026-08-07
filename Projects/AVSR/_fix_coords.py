"""목업 실측 좌표 → 720×1280 배치 좌표 보정.

목업 683×1024(2:3) → 화면 720×1280(9:16). 남는 세로 256px·가로 37px 처리 규칙:

  세로: HUD(상단 128px)는 그대로 두고, **그 아래 전부를 +256 이동**한다.
        목업에서 HUD 아래는 도시 야경만 있는 빈 띠라 거기로 흡수된다.
        → 블록 간 간격이 목업 그대로 보존된다. (균등 분배하면 간격이 다 틀어진다)

  가로: 좌우 여백을 14/13 → 18/18 로 넓히고 콘텐츠 폭 656 → 684 로 비례 확대.
        요소 크기(w,h)는 에셋 규격이므로 건드리지 않는다. 위치만 옮긴다.
"""
import io, json, os, re

HERE = os.path.dirname(os.path.abspath(__file__))
SPECDIR = os.path.join(HERE, '..', '..', 'Assets', 'Scripts', 'Editor', 'UISpec')

MOCK_W, MOCK_H = 683, 1024
DST_W, DST_H = 720, 1280
HUD_H = 128
DY = DST_H - MOCK_H           # 256
MARGIN_SRC_L, MARGIN_SRC_R = 14, 13
MARGIN_DST = 18
SRC_CONTENT = MOCK_W - MARGIN_SRC_L - MARGIN_SRC_R      # 656
DST_CONTENT = DST_W - MARGIN_DST * 2                    # 684


def map_x(x):
    return round(MARGIN_DST + (x - MARGIN_SRC_L) * DST_CONTENT / SRC_CONTENT)


def map_y(y):
    """세로 여분 256px 재분배.

    전부 HUD 아래 한 곳에 넣으면 콘텐츠가 아래로 밀려 로고와 겹친다.
    목업의 **빈 배경 구간 2곳**에 나눠 넣는다:
      ① HUD~콘텐츠 사이 야경 띠 (목업 y 92~288)   → +150
      ② 하단 3버튼~로고 사이 여백 (목업 y 860~916) → +106
    이렇게 하면 블록 간 간격은 목업 그대로 유지되고, 하단 로고도 화면 안에 들어온다.
    """
    if y < 100:            # HUD — 스펙 값 유지 (호출되지 않음)
        return y
    if y < 870:            # 본문 블록 — 야경 띠만큼 아래로
        return y + 150
    return y + DY          # 로고 등 최하단 — 화면 바닥에 맞춤


# 목업 실측 좌표 (x, y, w, h) — _build_anchor_pack.py 와 동일 출처
MOCK = {
    'ghostwidget': (8, 14, 166, 78), 'ghostportraiticon': (14, 20, 56, 62),
    'staminacounter': (176, 24, 112, 40), 'goldcounter': (292, 24, 116, 40),
    'gemcounter': (410, 24, 114, 40), 'staminaicon': (188, 30, 28, 28),
    'goldicon': (306, 30, 28, 28), 'gemicon': (424, 30, 28, 28),
    'mailbutton': (570, 28, 42, 38), 'settingsbutton': (628, 28, 40, 38),
    'chaptercard': (14, 290, 222, 344), 'bossportrait': (130, 348, 84, 84),
    'progressrewardchest': (186, 486, 42, 44), 'continuebutton': (24, 552, 198, 74),
    'progressbarbg': (24, 506, 154, 20),
    'battlepasscard': (455, 288, 215, 100), 'battlepassart': (590, 292, 74, 88),
    'battlepassbadge': (466, 344, 40, 38), 'battlepassbarbg': (512, 352, 78, 14),
    'eventcard': (455, 394, 215, 92), 'eventart': (574, 412, 72, 66),
    'dailylogincard': (455, 494, 215, 92), 'dailyloginart': (576, 508, 66, 58),
    'dailylogincheck': (462, 556, 26, 26),
    'featuretabbarbackground': (30, 626, 622, 78),
    'missiontabicon': (94, 632, 38, 40), 'achievementtabicon': (202, 632, 40, 40),
    'rankingtabicon': (310, 632, 42, 40), 'inventorytabicon': (418, 632, 42, 40),
    'friendstabicon': (528, 632, 42, 40), 'friendstablock': (546, 646, 22, 22),
    'hostbutton': (14, 706, 210, 152), 'hostbuttonart': (26, 712, 186, 130),
    'chapterbutton': (232, 702, 218, 158), 'chapterbuttonart': (238, 708, 206, 136),
    'shopbutton': (458, 706, 210, 152), 'shopbuttonart': (464, 712, 196, 130),
    'logolockup': (248, 916, 194, 102),
    'ghostavatar': (256, 294, 176, 220), 'portalring': (256, 600, 176, 52),
}


def main():
    p = os.path.join(SPECDIR, 'Lobby.json')
    spec = json.load(io.open(p, encoding='utf-8'))
    nodes = spec['nodes']
    byname = {}
    for i, n in enumerate(nodes):
        byname.setdefault(n['name'].lower(), i)

    # 자손 목록 (부모를 옮기면 자식도 같은 델타로 따라가야 한다)
    children = {}
    for i, n in enumerate(nodes):
        children.setdefault(n['parentIdx'], []).append(i)

    def descendants(i):
        out = []
        stack = list(children.get(i, []))
        while stack:
            j = stack.pop()
            out.append(j)
            stack += children.get(j, [])
        return out

    moved, missing, report = 0, [], []
    done = set()
    # 얕은 것부터 처리 — 부모를 먼저 옮기고 자식 델타가 중복 적용되지 않게 한다
    order = sorted(MOCK.items(), key=lambda kv: byname.get(kv[0], 1 << 30))
    for name, (mx, my, mw, mh) in order:
        i = byname.get(name)
        if i is None:
            missing.append(name)
            continue
        if i in done:
            continue
        n = nodes[i]
        old = list(n['rect']) if n['rect'] else None
        if not old:
            continue
        nx = map_x(mx)
        ny = my if my < 100 else map_y(my)     # HUD 는 그대로
        dx, dy = nx - old[0], ny - old[1]
        n['rect'] = [nx, ny, old[2], old[3]]   # 크기는 에셋 규격이므로 유지
        done.add(i)
        for j in descendants(i):
            r = nodes[j].get('rect')
            if r:
                nodes[j]['rect'] = [r[0] + dx, r[1] + dy, r[2], r[3]]
            done.add(j)
        if old != n['rect']:
            moved += 1
            report.append(f'  {name:26} {str(old):>22} → {n["rect"]}  (자손 {len(descendants(i))}개 동반)')

    io.open(p, 'w', encoding='utf-8').write(json.dumps(spec, ensure_ascii=False, indent=1))
    out = [f'좌표 보정 {moved}건 / 대상 {len(MOCK)}개', '']
    out += report
    if missing:
        out += ['', f'스펙에 없는 요소 {len(missing)}: {missing}']
    io.open(os.path.join(HERE, '_coord_report.txt'), 'w', encoding='utf-8').write('\n'.join(out))


if __name__ == '__main__':
    main()
