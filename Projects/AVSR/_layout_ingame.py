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
#
# **HUD 바로 아래에서 시작한다.** BattleDirector 의 ClampToField 가 모든 유닛을
# 이 사각형 안에 가두므로, 필드를 내리는 것만으로 몬스터가 HUD 를 가리는 일이
# 없어진다. 코드에 별도 예외 영역을 두지 않는 이유다.
#
# ⚠️ HUD 보다 **먼저** 선언해야 한다. Unity UI 는 형제 순서대로 그리므로,
# 뒤에 오면 바닥 이미지가 HUD 를 덮는다.
HUD_H = 184                             # 2행 바닥(178)에 여유 6
FIELD_BOTTOM = 824                      # 하단 조작부(850) 위까지

add('RoomField',      (0, HUD_H, 576, FIELD_BOTTOM - HUD_H), ROOT, 'GROUP')
add('RoomFloor',      (0, HUD_H, 576, FIELD_BOTTOM - HUD_H), 'RoomField', 'IMG')
add('UnitLayer',      (0, HUD_H, 576, FIELD_BOTTOM - HUD_H), 'RoomField', 'GROUP')

# ── 상단 HUD ────────────────────────────────────────────────────────
# 목업 크기 그대로는 실기에서 글자와 바가 식별이 안 됐다. 모든 요소를 **1.5배**로
# 키운다. Transform 스케일이 아니라 각 요소의 실제 크기·글자 크기를 키운다 —
# 스케일은 자식까지 함께 늘어나고 픽셀 정렬이 깨진다.
#
# 다만 단순히 좌표까지 1.5배 하면 가로가 넘친다. 보스 바만 해도 208 → 312 라
# 우측 재화·일시정지와 겹친다. 그래서 **행을 나눠 재배치**한다.
#
#   1행  고스트 상태 · 재화 · 일시정지
#   2행  왼쪽 호스트 초상·상태 / 오른쪽 방 표기·보스 게이지
#
# 보스 게이지는 처음에 3행으로 따로 뒀는데, 보스방이 아닐 때 그 자리가 빈 띠로
# 남아 전투 필드를 82px 잡아먹었다. 2행 오른쪽 빈 공간으로 넣어 없앴다.
# 대신 가로가 312 → 272px 로 줄었다 — 읽는 데 문제되는 건 두께지 길이가 아니다.
#
# 목업의 2단 구성과 달라지지만, 목업 좌표를 그대로 쓰면 읽을 수 없다는 것이
# 실기에서 확인됐다. 읽히는 쪽을 택한다.
add('TopHudGroup',    (0, 0, 576, HUD_H), ROOT, 'GROUP')

# HUD 판. 목업에는 이 높이의 패널이 없어(1.5배 확대에서 나온 높이다) 목업을 늘리는
# 대신 이 게임의 다른 UI 와 같은 언어로 짰다 — 어두운 남색 판 + 밝은 아래 경계.
add('HudBackdrop',    (0, 0, 576, HUD_H), 'TopHudGroup', 'IMG')

# 1행 — 고스트
add('GhostHudIcon',   (4, 6, 58, 60), 'TopHudGroup', 'IMG')
add('GhostLabel',     (70, 6, 84, 22), 'TopHudGroup', 'TMP',
    text='GHOST', size=cap(18), align='L', color=WHITE)
add('GhostHpBarBg',   (70, 32, 136, 20), 'TopHudGroup', 'IMG')
add('GhostHpBarFill', (0, 0, 136, 20), 'GhostHpBarBg', 'IMG', local=True)
add('GhostHpText',    (212, 30, 92, 26), 'TopHudGroup', 'TMP',
    size=cap(19), align='L', color=WHITE)

# 런 EXP — 적을 잡아 모으고 차면 버프 3택1 이 열린다(기획서 A 5-2).
# 게이지가 없으면 언제 선택 창이 뜨는지 예측할 수 없어 성장이 우연처럼 느껴진다.
add('ExpBarBg',       (70, 56, 136, 10), 'TopHudGroup', 'IMG')
add('ExpBarFill',     (0, 0, 136, 10), 'ExpBarBg', 'IMG', local=True)
add('LevelText',      (212, 52, 92, 18), 'TopHudGroup', 'TMP',
    text='Lv.1', size=cap(13), align='L', color=WHITE)

# 1행 — 재화·일시정지 (원래 세로로 쌓여 있던 골드/젬을 가로로 편다)
add('GoldIcon',       (310, 18, 24, 22), 'TopHudGroup', 'IMG')
add('GoldText',       (340, 16, 70, 26), 'TopHudGroup', 'TMP', size=cap(19), align='L', color=WHITE)
add('GemIcon',        (416, 18, 24, 22), 'TopHudGroup', 'IMG')
add('GemText',        (446, 16, 52, 26), 'TopHudGroup', 'TMP', size=cap(19), align='L', color=WHITE)
add('PauseButton',    (502, 6, 72, 72), 'TopHudGroup', 'BTN')

# 2행 — 호스트
add('HostPortraitFrame', (4, 78, 100, 100), 'TopHudGroup', 'IMG')
add('HostPortraitImage', (16, 90, 76, 76), 'TopHudGroup', 'IMG')
add('HostLabel',      (112, 80, 72, 22), 'TopHudGroup', 'TMP',
    text='HOST', size=cap(18), align='L', color=WHITE)
add('HostHpBarBg',    (112, 106, 118, 18), 'TopHudGroup', 'IMG')
add('HostHpBarFill',  (0, 0, 118, 18), 'HostHpBarBg', 'IMG', local=True)
add('HostHpText',     (238, 104, 86, 24), 'TopHudGroup', 'TMP',
    size=cap(19), align='L', color=WHITE)
add('HostNameText',   (112, 132, 230, 20), 'TopHudGroup', 'TMP',
    size=cap(15), align='L', color=WHITE)

# 2행 오른쪽 — 방 표기 (목업 concept 시트의 `STAGE 7`) 와 보스 게이지
add('StageText',      (352, 80, 218, 22), 'TopHudGroup', 'TMP',
    size=cap(16), align='C', color=WHITE)

add('BossGroup',      (352, 106, 218, 72), 'TopHudGroup', 'GROUP')
add('BossLabel',      (352, 106, 218, 20), 'BossGroup', 'TMP',
    text='BOSS', size=cap(17), align='C', color=WHITE)
add('BossHpBarBg',    (352, 128, 218, 22), 'BossGroup', 'IMG')
add('BossHpBarFill',  (0, 0, 218, 22), 'BossHpBarBg', 'IMG', local=True)
add('BossHpText',     (352, 152, 218, 18), 'BossGroup', 'TMP',
    size=cap(14), align='C', color=WHITE)

# ── 하단 조작 ───────────────────────────────────────────────────────
add('ControlGroup',   (0, 850, 576, 174), ROOT, 'GROUP')
# 목업에 격자를 얹어 실측한 D패드 경계 — 바깥 금속 테두리 기준.
# 노브는 패드 한가운데가 기본 자리다(_recut_dpad.py 가 같은 값을 계산해 출력한다).
add('DPadBase',       (12, 841, 139, 141), 'ControlGroup', 'IMG')
add('DPadKnob',       (38, 38, 64, 64), 'DPadBase', 'IMG', local=True)

add('UltimateButton', (324, 854, 122, 140), 'ControlGroup', 'BTN')
add('UltimateCooldown', (0, 0, 122, 140), 'UltimateButton', 'IMG', local=True)
add('PossessButton',  (445, 855, 110, 136), 'ControlGroup', 'BTN')


# ── 레벨업 버프 3택1 (기본 숨김, 코드가 켠다) ──────────────────────
# 로그라이크 축 — 잡는 만큼 런 한정 빌드를 쌓는다 (기획서 A 5-2).
# 제목·부제는 코드가 레벨을 넣어 덮어쓴다. 여기 값은 편집기에서 보이는 기본값.
add('BuffChoicePanel', (0, 0, 576, 1024), ROOT, 'IMG')          # 딤 + 입력 차단
add('BuffTitleText',   (88, 250, 400, 40), 'BuffChoicePanel', 'TMP',
    text='LEVEL UP', size=cap(26), align='C', color=HOST_C)
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
