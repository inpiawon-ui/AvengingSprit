"""인게임 목업 실측 레이아웃 → Unity 적용용 JSON.

좌표 출처: Reference/Mockups/ingame_hd_scene.jpeg — **576x1024 = 9:16 네이티브**다.
로비·호스트 선택 목업(2:3)과 달리 비율 변환이 없다. 균일 ×1.25 로 끝난다.

미결 항목 #8(조작 UI 형태 — 두 목업이 서로 다름) 처리:
  `ingame_concept.jpeg`  원형 조이스틱 + 원형 버튼
  `ingame_hd_scene.jpeg` 사각 D-패드 + 사각 버튼   ← **채택**
채택 근거 = 9:16 네이티브라 좌표를 변환 없이 쓸 수 있고 완성도가 높다.

이 화면은 프리팹이 아직 없다. `create` 로 노드까지 이 표가 만든다.
"""
import json, os

import _fit_boxes

S = 720.0 / 576.0                      # = 1.25
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'Scripts', 'Editor', 'UISpec', '_layout_InGame.json')

WHITE = '#F2F4F8'
GHOST_C = '#5AC8F0'
HOST_C = '#F0B428'
BOSS_C = '#E8404A'
MUTED = '#8C9AB4'

L = []


def cap(c):
    return round(c / 0.70 * S, 1)


def add(name, rect, parent, create, **o):
    x, y, w, h = rect
    L.append(dict(name=name, parent=parent, create=create,
                  rect=[round(x * S, 1), round(y * S, 1), round(w * S, 1), round(h * S, 1)], **o))


ROOT = 'InGameMainUI'

# ── 전투 필드 ───────────────────────────────────────────────────────
# 유닛은 런타임에 생성되므로 여기서는 바닥과 부모 컨테이너만 잡는다.
add('RoomField',      (0, 104, 576, 720), ROOT, 'GROUP')
add('RoomFloor',      (0, 104, 576, 720), 'RoomField', 'IMG')
add('UnitLayer',      (0, 104, 576, 720), 'RoomField', 'GROUP')

# ── 상단 HUD ────────────────────────────────────────────────────────
add('TopHudGroup',    (0, 0, 576, 118), ROOT, 'GROUP')

add('GhostHudIcon',   (3, 8, 39, 40), 'TopHudGroup', 'IMG')
add('GhostLabel',     (47, 10, 60, 16), 'TopHudGroup', 'TMP',
    text='GHOST', size=cap(12), align='L', color=GHOST_C)
add('GhostHpBarBg',   (47, 31, 91, 13), 'TopHudGroup', 'IMG')
add('GhostHpBarFill', (0, 0, 91, 13), 'GhostHpBarBg', 'IMG', local=True)
add('GhostHpText',    (142, 29, 62, 17), 'TopHudGroup', 'TMP',
    size=cap(13), align='L', color=WHITE)

add('HostPortraitFrame', (8, 55, 58, 54), 'TopHudGroup', 'IMG')
add('HostPortraitImage', (12, 59, 50, 46), 'TopHudGroup', 'IMG')
add('HostLabel',      (76, 58, 50, 16), 'TopHudGroup', 'TMP',
    text='HOST', size=cap(12), align='L', color=HOST_C)
add('HostHpBarBg',    (76, 81, 78, 12), 'TopHudGroup', 'IMG')
add('HostHpBarFill',  (0, 0, 78, 12), 'HostHpBarBg', 'IMG', local=True)
add('HostHpText',     (158, 79, 58, 16), 'TopHudGroup', 'TMP',
    size=cap(13), align='L', color=WHITE)
add('HostNameText',   (76, 99, 150, 13), 'TopHudGroup', 'TMP',
    size=cap(10), align='L', color=HOST_C)

add('BossGroup',      (223, 6, 210, 58), 'TopHudGroup', 'GROUP')
add('BossLabel',      (302, 8, 50, 15), 'BossGroup', 'TMP',
    text='BOSS', size=cap(12), align='C', color=BOSS_C)
add('BossHpBarBg',    (223, 29, 208, 18), 'BossGroup', 'IMG')
add('BossHpBarFill',  (0, 0, 208, 18), 'BossHpBarBg', 'IMG', local=True)
add('BossHpText',     (286, 47, 82, 14), 'BossGroup', 'TMP',
    size=cap(11), align='C', color=WHITE)

add('GoldIcon',       (459, 18, 16, 15), 'TopHudGroup', 'IMG')
add('GoldText',       (479, 16, 52, 18), 'TopHudGroup', 'TMP', size=cap(13), align='L', color=WHITE)
add('GemIcon',        (461, 44, 16, 15), 'TopHudGroup', 'IMG')
add('GemText',        (481, 42, 52, 18), 'TopHudGroup', 'TMP', size=cap(13), align='L', color=WHITE)
add('PauseButton',    (536, 11, 29, 36), 'TopHudGroup', 'BTN')

# ── 스테이지 표기 (목업 concept 시트의 `STAGE 7`) ───────────────────
add('StageText',      (223, 66, 208, 16), 'TopHudGroup', 'TMP',
    size=cap(11), align='C', color=MUTED)

# ── 하단 조작 ───────────────────────────────────────────────────────
add('ControlGroup',   (0, 850, 576, 174), ROOT, 'GROUP')
add('DPadBase',       (11, 857, 133, 128), 'ControlGroup', 'IMG')
add('DPadKnob',       (41, 30, 60, 55), 'DPadBase', 'IMG', local=True)

add('UltimateButton', (328, 858, 115, 132), 'ControlGroup', 'BTN')
add('UltimateCooldown', (0, 0, 115, 132), 'UltimateButton', 'IMG', local=True)
add('PossessButton',  (448, 858, 114, 132), 'ControlGroup', 'BTN')


# ── 룸 클리어 버프 3택1 (기본 숨김, 코드가 켠다) ────────────────────
# 로그라이크 축 — 룸마다 런 한정 빌드를 쌓는다 (GameComposition 콘텐츠 5).
add('BuffChoicePanel', (0, 0, 576, 1024), ROOT, 'IMG')          # 딤 + 입력 차단
add('BuffTitleText',   (88, 250, 400, 40), 'BuffChoicePanel', 'TMP',
    text='ROOM CLEAR', size=cap(26), align='C', color=HOST_C)
add('BuffSubText',     (88, 296, 400, 24), 'BuffChoicePanel', 'TMP',
    text='하나를 선택하세요', size=cap(13), align='C', color=MUTED)
for i in range(3):
    y = 350 + i * 148
    add(f'BuffCard{i}',      (108, y, 360, 128), 'BuffChoicePanel', 'BTN')
    add(f'BuffCard{i}Accent', (0, 0, 10, 128), f'BuffCard{i}', 'IMG', local=True)
    add(f'BuffCard{i}Name',  (140, y + 26, 300, 34), 'BuffChoicePanel', 'TMP',
        size=cap(19), align='L', color=WHITE)
    add(f'BuffCard{i}Desc',  (140, y + 68, 300, 26), 'BuffChoicePanel', 'TMP',
        size=cap(13), align='L', color=MUTED)


def main():
    rows = []
    for e in L:
        x, y, w, h = e['rect']
        r = {'name': e['name'], 'parent': e['parent'], 'create': e['create'],
             'x': x, 'y': y, 'w': w, 'h': h}
        for k in ('text', 'size', 'align', 'color', 'wrap', 'local', 'zero', 'knob', 'cooldown'):
            if k in e:
                r[k] = e[k]
        rows.append(r)

    # 목업 실측 박스와 실제 에셋 비율이 어긋나면 여기서 맞춘다

    _fit_boxes.report(_fit_boxes.fit(rows, 'InGameMainUI'), 'InGame')

    out = os.path.abspath(OUT)
    with open(out, 'w', encoding='utf-8') as f:
        json.dump({'screen': 'InGame', 'root': ROOT, 'build': True,
                   'origin': [0, 0], 'items': rows}, f, ensure_ascii=False, indent=1)
    print(f'{len(rows)}개 → {out}')


if __name__ == '__main__':
    main()
