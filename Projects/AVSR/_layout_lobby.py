"""로비 목업 실측 레이아웃 → Unity 적용용 JSON.

좌표 출처: Reference/Mockups/lobby_hub_v2.png (941x1672) 실측 — 2026-09-16 개편 목업.
이 파일이 로비 레이아웃의 **단일 출처**다. 프리팹을 직접 손보지 말고 여기를 고친다.

── 해상도 대응 ──────────────────────────────────────────────────────
⚠ **가운데로 모으지 마라.** 태블릿 4:3 은 보이는 캔버스가 960 폭이라, 720 에 가둬
  가운데 두면 좌우 120 px 씩이 그냥 빈다. 그건 대응이 아니다(2026-09-16 지적).

  판(상단 HUD · 유령수색 · 상자 밴드 · 하단 바)은 **그린 폭을 기준 폭과 같게** 두어
  `ScreenFit` 이 화면 끝까지 늘리게 한다. 판 안의 칸(상자 3칸 · 하단 3버튼 ·
  모드 3카드)은 `ScreenFitShare` 로 남는 폭을 그린 비율대로 나눠 갖는다 —
  칸 사이 간격은 그린 값을 지키므로 줄 모양 그대로 폭만 커진다.
  붙이는 곳: `ScreenFitInstaller.Shares`

── 좌표계 ───────────────────────────────────────────────────────────
목업이 941x1672 = **정확히 9:16** 이라 캔버스 720x1280 과 비율이 같다.
  S = 720/941 = 0.7651        가로·세로 같은 배율, 밴드 나누기가 필요 없다

⚠ 예전 목업(683x1024, 2:3)은 비율이 달라 「상단 고정 / 하단 고정」 두 밴드로 나눠
  남는 200px 을 배경이 흡수하게 했다. 새 목업은 그럴 일이 없다 —
  **밴드 개념을 되살리지 마라.** 한 배율로 곱하면 목업 그대로 나온다.

── 2026-09-16 개편에서 걷어낸 것 ────────────────────────────────────
  로고(LogoLockup) · 시즌패스 · 이벤트 · 일일로그인 · 기능탭 ·
  고스트 위젯(Lv/EXP) · 스태미나 · 챕터 카드 · 가운데 큰 유령/포탈링
프리팹에서 **지운다**(숨기지 않는다). 남겨 두면 다음 사람이 왜 안 보이는지 찾는다.

── 새로 들어온 것 ───────────────────────────────────────────────────
  유령 수색(방치) 패널 — 지금은 껍데기. 기능은 나중(기획 2026-09-16)
  보물상자 3칸 — 실동작. 규칙은 Assets/Scripts/Module/CLAUDE.md 6절

글자 크기 — 목업에서 잰 대문자 높이(cap) 로부터
  fontSize = cap / 0.70 * S                 (Noto Sans KR cap ratio 0.70)

⚠ **넘친다고 여기서 크기를 줄이지 마라.** 언어마다 글자 폭이 달라 한국어에 맞추면
  일본어가 넘치고, 일본어에 맞추면 한국어가 작아진다. 여기는 목업 값만 적고,
  안 들어가는 것은 `LobbyArtBinder.FitToBox` 의 자동 축소가 알아서 줄인다.
"""
import json, os

import _fit_boxes

MOCK_W, MOCK_H = 941.0, 1672.0
S = 720.0 / MOCK_W
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'Scripts', 'Editor', 'UISpec', '_layout_Lobby.json')

# 팔레트 (목업 스포이드)
WHITE = '#F2F4F8'
GOLD = '#F5C542'
CYAN = '#4DD8FF'
BLUE = '#6EC8F0'
DIM = '#8FA8C8'
DARK = '#2A1B05'          # 금색 버튼 위 어두운 글자
LAV = '#C9A0F0'
GREY = '#4A5468'


def cap(c):
    """대문자 높이 → TMP fontSize"""
    return round(c / 0.70 * S, 1)


L = []


def add(name, rect, **o):
    L.append(dict(name=name, rect=list(rect), **o))


# ── 배경 ────────────────────────────────────────────────────────────
add('LobbyStageArea', (0, 0, MOCK_W, MOCK_H))
add('LobbyBackground', (0, 0, MOCK_W, MOCK_H), parent='LobbyStageArea')

# ── 상단 HUD ────────────────────────────────────────────────────────
#
# 목업에는 골드·젬·우편뿐이다. 설정은 남기기로 했으므로(기획 2026-09-16)
# **로고가 있던 왼쪽 빈자리**에 둔다 — 오른쪽 끝은 목업 그대로 지킨다.
add('TopHudGroup', (0, 10, MOCK_W, 80))
add('SettingsButton', (38, 18, 64, 64), parent='TopHudGroup')

add('GoldCounter', (279, 18, 306, 64), parent='TopHudGroup')
add('GoldIcon', (288, 27, 46, 46), parent='GoldCounter')
add('GoldText', (340, 28, 176, 44), size=cap(40), align='C', color=WHITE, parent='GoldCounter')
add('GoldCounter/PlusButton', (525, 27, 46, 46), parent='GoldCounter')

add('GemCounter', (607, 18, 223, 64), parent='TopHudGroup')
add('GemIcon', (616, 27, 46, 46), parent='GemCounter')
add('GemText', (666, 28, 104, 44), size=cap(40), align='C', color=WHITE, parent='GemCounter')
add('GemCounter/PlusButton', (772, 27, 46, 46), parent='GemCounter')

add('MailButton', (846, 18, 76, 64), parent='TopHudGroup')
add('MailButton/NotifyBadge', (900, 8, 34, 34), parent='MailButton')

# ── 유령 수색 (방치) ────────────────────────────────────────────────
#
# 기능은 아직 없다. 목업의 글자·그림·계층만 그대로 세워 둔다 —
# 나중에 기능을 붙일 때 화면을 다시 안 짜도 되게.
# ⚠ 목업은 이 자리를 **로고**로 채운다. 로고를 걷어냈으므로(2026-09-16) 그만큼
#   판을 위로 올려 먹게 한다 — 안 그러면 상단이 통째로 비어 보인다.
add('GhostSearchPanel', (0, 120, MOCK_W, 487), create='IMG', parent='LobbyMainUI')
add('GhostSearchArt', (27, 120, 886, 487), create='IMG', parent='GhostSearchPanel')
# 글자 뒤 어두운 판 — 골목 그림 위에서 글자가 읽히게 한다(목업 실측 27~471 / 236~510).
# ⚠ 그림 **다음**, 글자 **앞**에 적는다. 표 순서가 곧 그리는 순서다.
# ⚠ 납품 그림 444x274 안에서 판은 **324x260 만** 차지한다(오른쪽·아래가 투명).
#   칸을 그림 크기대로 두면 판이 73 % 폭으로 그려져 글자가 판 밖으로 나간다.
#   판이 27~471 / 236~510 에 떨어지게 칸을 키운다 — 444*444/324, 274*274/260.
add('GhostSearchScrim', (27, 236, 608, 289), create='IMG', parent='GhostSearchPanel')
add('GhostSearchIcon', (58, 250, 124, 94), create='IMG', parent='GhostSearchPanel')
# 목업 가운데에서 보물상자 위를 나는 큰 유령.
# ⚠ 판을 위로 올린 뒤(로고 자리) 유령이 제목 줄에 바싹 붙어 글자를 가렸다 —
#   글자판 오른쪽(471) 밖으로 비켜 세운다(2026-09-16 지적).
add('GhostSearchBigGhost', (446, 318, 220, 170), create='IMG', parent='GhostSearchPanel')
add('GhostSearchTitleText', (206, 256, 200, 48), text='유령 수색',
    size=cap(38), align='L', color=WHITE, create='TMP', parent='GhostSearchPanel')
add('GhostSearchHelpButton', (428, 262, 48, 44), create='BTN', parent='GhostSearchPanel')
add('GhostSearchHelpText', (428, 266, 48, 38), text='?',
    size=cap(26), align='C', color=WHITE, create='TMP', parent='GhostSearchHelpButton')
add('GhostSearchTimerText', (206, 302, 290, 54), text='04:32:18',
    size=cap(44), align='L', color=CYAN, create='TMP', parent='GhostSearchPanel')
add('GhostSearchGoldIcon', (58, 354, 76, 76), create='IMG', parent='GhostSearchPanel')
add('GhostSearchGoldText', (150, 360, 320, 58), text='+ 12,640 G',
    size=cap(40), align='L', color=GOLD, create='TMP', parent='GhostSearchPanel')
add('GhostSearchDescText', (48, 426, 440, 86), text='유령이 도시 곳곳을 떠돌며 골드를 찾아옵니다.',
    size=cap(24), align='L', color=WHITE, wrap=True, create='TMP', parent='GhostSearchPanel')
add('GhostSearchClaimButton', (628, 506, 272, 82), create='BTN', parent='GhostSearchPanel')
add('GhostSearchClaimIcon', (650, 518, 58, 58), create='IMG', parent='GhostSearchClaimButton')
add('GhostSearchClaimText', (712, 524, 178, 46), text='보상 받기',
    size=cap(34), align='C', color=DARK, create='TMP', parent='GhostSearchClaimButton')
add('GhostSearchClaimButton/NotifyBadge', (872, 488, 46, 46),
    create='IMG', parent='GhostSearchClaimButton')
# ⚠ 테두리는 **맨 나중에** 그린다. 판 자신에 테두리를 칠하면 그 위에 얹히는
#   그림이 통째로 덮어 버려 테두리가 안 보인다(2026-09-16 실제로 그랬다).
add('GhostSearchFrame', (27, 120, 886, 487), create='IMG', parent='GhostSearchPanel')

# ── 보물상자 3칸 ────────────────────────────────────────────────────
#
# 목업 실측은 칸마다 폭이 288/275/283 으로 조금씩 다르다(AI 목업의 흔들림).
# 같은 폭으로 고른다 — 27..913 안에서 282 세 칸 + 20 간격.
CHEST_X0, CHEST_Y0, CHEST_W, CHEST_H = 27, 621, 282, 275
CHEST_STEP = 302

add('ChestBand', (0, CHEST_Y0, MOCK_W, CHEST_H), create='IMG', parent='LobbyMainUI')

for i in range(3):
    slot = f'ChestSlot{i + 1}'
    x = CHEST_X0 + CHEST_STEP * i

    def c(lx, ly, w, h):
        """칸 안에서의 자리 → 캔버스 절대 좌표"""
        return (x + lx, CHEST_Y0 + ly, w, h)

    add(slot, (x, CHEST_Y0, CHEST_W, CHEST_H), create='GROUP', parent='ChestBand')
    add(f'{slot}/ChestSlotFrame', c(0, 0, CHEST_W, CHEST_H), create='IMG', parent=slot)
    add(f'{slot}/ChestArt', c(38, 12, 206, 142), create='IMG', parent=slot)
    add(f'{slot}/ChestReadyBanner', c(23, 8, 224, 49), create='IMG', parent=slot)
    add(f'{slot}/ChestReadyText', c(52, 13, 166, 38), text='완료!',
        size=cap(30), align='C', color=GOLD, create='TMP', parent=slot)
    add(f'{slot}/ChestEmptyText', c(20, 100, 242, 48), text='빈 칸',
        size=cap(28), align='C', color=DIM, create='TMP', parent=slot)
    # 목업은 시계·남은 시간 줄 뒤에 어두운 판이 깔려 있다 — 글자가 그냥 떠 있으면 안 읽힌다
    add(f'{slot}/ChestTimePlate', c(19, 125, 246, 48), create='IMG', parent=slot)
    add(f'{slot}/ChestTimeIcon', c(73, 137, 40, 38), create='IMG', parent=slot)
    add(f'{slot}/ChestTimeText', c(121, 134, 152, 40), text='3시간 12분',
        size=cap(31), align='L', color=WHITE, create='TMP', parent=slot)
    # ⚠ 버튼 글자·아이콘을 **버튼의 자식으로 두지 않는다.** 적용기의 `parent` 는 경로가
    #   아니라 단일 이름만 찾는데, 세 칸의 버튼 이름이 같아 전부 첫 칸으로 붙는다.
    #   칸 직속으로 두고 버튼 위에 그린다 — 글자는 raycast 대상이 아니라 눌림은 버튼이 받는다.
    add(f'{slot}/ChestActionButton', c(19, 181, 250, 70), create='BTN', parent=slot)
    add(f'{slot}/ChestActionGemIcon', c(101, 189, 47, 38), create='IMG', parent=slot)
    add(f'{slot}/ChestActionCostText', c(158, 187, 120, 42), text='1,000',
        size=cap(34), align='L', color=WHITE, create='TMP', parent=slot)
    # 「즉시 열기」는 젬값 줄 **아래** 한 줄이다 — 위로 올리면 젬값과 겹친다.
    # 목업 실측: 버튼 806~876 · 젬값 줄 810~854 · 라벨 850~874.
    add(f'{slot}/ChestActionLabelText', c(19, 226, 250, 32), text='즉시 열기',
        size=cap(28), align='C', color=WHITE, create='TMP', parent=slot)
    # ⚠ 완료 칸의 「보상 획득하기」는 **칸을 따로 둔다.** 예전에는 같은 칸을 22px 올려
    #   돌려 썼는데, 젬값 줄이 없어 글자가 커져야 하는데도 상자 높이가 그대로라
    #   목업의 절반 크기로 찍혔다(2026-09-16). 두 상태는 글자 크기가 다르다.
    add(f'{slot}/ChestReadyLabelText', c(19, 190, 250, 50), text='보상 획득하기',
        size=cap(34), align='C', color=DARK, create='TMP', parent=slot)

# ── 게임 모드 ───────────────────────────────────────────────────────
#
# ⚠ 이 판은 **늘리면 안 된다.** 720 폭 안의 한 덩어리 캐러셀이라 4:3 에서 늘리면
#   가운데 카드가 중앙을 벗어난다. `ScreenFitInstaller.CenterLocks` 가 잠근다.
add('GameModeGroup', (0, 905, MOCK_W, 492))
add('GameModeLabelAccent', (24, 912, 11, 40), parent='GameModeGroup')
add('GameModeLabel', (58, 908, 300, 50), size=cap(33), align='L', color=WHITE,
    parent='GameModeGroup')

# 옆 카드 두 장 — 목업에서 같은 크기다. 자리만 좌우로 다르다.
SIDE_Y, SIDE_W, SIDE_H = 968, 238, 405


def side(tag, x):
    add(tag, (x, SIDE_Y, SIDE_W, SIDE_H), parent='GameModeGroup')
    add(f'{tag}/ModeCardArt', (x + 13, 1000, 212, 240), create='IMG', parent=tag)
    add(f'{tag}/ModeLockIcon', (x + 93, 1120, 52, 72), parent=tag)
    # 기운 테 안쪽에 들어와야 한다 — 칸 폭을 다 쓰면 양끝이 테에 물린다
    add(f'{tag}/ModeTitleText', (x + 39, 1254, 160, 46), size=cap(30), align='C', color=WHITE,
        parent=tag)
    add(f'{tag}/ModeSubText', (x + 39, 1296, 160, 34), size=cap(21), align='C', color=BLUE,
        parent=tag)


side('ModeCardLeft', 27)

# ⚠ 화살표를 글꼴 글자(`◀`)로 찍지 마라. 목업은 속이 빈 **두꺼운 꺾쇠**인데
#   글자는 속이 꽉 찬 삼각형이라 결이 다르다(2026-09-16).
add('ModeArrowLeft', (0, 1110, 52, 96), create='BTN', parent='GameModeGroup')
add('ModeArrowLeftArt', (0, 1110, 52, 96), create='IMG', parent='ModeArrowLeft')

side('ModeCardRight', 677)

add('ModeArrowRight', (889, 1110, 52, 96), create='BTN', parent='GameModeGroup')
add('ModeArrowRightArt', (889, 1110, 52, 96), create='IMG', parent='ModeArrowRight')

# 가운데 칸은 **맨 나중에** — 양옆 칸 위로 올라와야 한다(표 순서 = 그리는 순서)
# ⚠ 칸이 곧 카드가 아니다. 판 그림은 430x480 이고 금테가 (30,25)~(399,454) 라,
#   칸을 그만큼 바깥으로 물려야 금테가 목업의 카드 자리에 떨어진다.
add('ModeCardCenter', (252, 923, 430, 480), parent='GameModeGroup')
add('ModeCenterArt', (292, 968, 353, 240), parent='ModeCardCenter')
add('ModeMainBadge', (292, 958, 88, 44), parent='ModeCardCenter')
add('ModeMainBadgeText', (292, 963, 88, 36), text='MAIN', size=cap(24), align='C', color=DARK,
    parent='ModeMainBadge')
add('ModeCenterLockIcon', (437, 1059, 60, 72), parent='ModeCardCenter')
add('ModeCenterIcon', (292, 1220, 70, 58), create='IMG', parent='ModeCardCenter')
add('ModeCenterTitleText', (372, 1218, 240, 54), size=cap(36), align='L', color=WHITE,
    parent='ModeCardCenter')
# ⚠ 부제는 카드 안에 가둔다. 목업 자리(342~600)에 두면 진행도 「CH 03 · 26 / 30」 이
#   카드 밖으로 흘러 옆 칸을 덮는다 — 카드 폭 전체를 쓰고 가운데 정렬한다.
add('ModeCenterSubText', (296, 1266, 342, 34), size=cap(21), align='C', color=BLUE,
    parent='ModeCardCenter')
add('ModePlayButton', (290, 1300, 355, 70), parent='ModeCardCenter')
add('ModePlayButtonText', (290, 1308, 355, 54), size=cap(28), align='C', color=DARK,
    parent='ModePlayButton')

# ── 하단 3버튼 ──────────────────────────────────────────────────────
#
# 가운데(PLAY)만 크고 금색이다 — 지금 서 있는 곳이 로비라서다.
# ⚠ 칸이 곧 판이 아니다. 납품 그림 941x206 은 **위 62 px 이 투명**이라, 칸을 목업의
#   판 자리(1466~1672)에 맞추면 판이 60 px 아래에서 시작해 버튼 허리를 가른다.
#   판이 1470~1672(202) 로 그려지게 칸을 위로 늘린다 — 202 x 206/144 = 289.
add('MainActionBar', (0, 1383, MOCK_W, 289), create='IMG')

add('HostButton', (36, 1487, 267, 128), parent='MainActionBar')
# 목업 실측(2026-09-16): 군인 50~145 / 1510~1605 · HOST 162~245 / 1525~1555
#                  부제 172~275 / 1565~1587 · 배지 268~298 / 1500~1530
# ⚠ 아이콘을 목업보다 크게 잡았더니 글자와의 간격이 벌어져 보였다
add('HostButtonArt', (48, 1508, 100, 100), parent='HostButton')
add('HostButtonTitleText', (156, 1518, 96, 42), text='HOST',
    size=cap(30), align='C', color=WHITE, parent='HostButton')
add('HostButtonSubText', (156, 1560, 122, 32), text='호스트 육성',
    size=cap(20), align='C', color=BLUE, parent='HostButton')
add('HostButton/NotifyBadge', (266, 1498, 34, 34), parent='HostButton')

add('ChapterButton', (320, 1481, 302, 140), parent='MainActionBar')
add('ChapterButtonArt', (348, 1498, 118, 106), parent='ChapterButton')
add('ChapterButtonTitleText', (470, 1518, 98, 42), size=cap(30), align='C', color=DARK,
    parent='ChapterButton')
add('ChapterButtonSubText', (466, 1560, 106, 32), size=cap(20), align='C', color=DARK,
    parent='ChapterButton')

add('ShopButton', (641, 1487, 264, 128), parent='MainActionBar')
add('ShopButtonArt', (655, 1504, 102, 100), parent='ShopButton')
add('ShopButtonTitleText', (768, 1518, 100, 42), text='SHOP',
    size=cap(30), align='C', color=WHITE, parent='ShopButton')
add('ShopButtonSubText', (768, 1560, 100, 32), text='상점',
    size=cap(20), align='C', color=BLUE, parent='ShopButton')
add('ShopButton/NotifyBadge', (872, 1498, 34, 34), parent='ShopButton')

add('VersionText', (30, 1636, 100, 22), size=cap(14), align='L', color=GREY)


def main():
    rows = []
    for e in L:
        x, y, w, h = e['rect']
        r = {
            'name': e['name'],
            'x': round(x * S, 1), 'y': round(y * S, 1),
            'w': round(w * S, 1), 'h': round(h * S, 1),
        }
        for k in ('text', 'size', 'align', 'color', 'wrap', 'clone', 'dx', 'create', 'parent'):
            if k in e:
                r[k] = e[k]
        if 'dx' in r:
            r['dx'] = round(r['dx'] * S, 1)
        rows.append(r)

    # 목업 실측 박스와 실제 에셋 비율이 어긋나면 여기서 맞춘다
    _fit_boxes.report(_fit_boxes.fit(rows, 'LobbyMainUI'), 'Lobby')

    out = os.path.abspath(OUT)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, 'w', encoding='utf-8') as f:
        # 호스트 선택 판은 표에 없다 — 로비 **위에** 떠야 하므로 맨 뒤로 보낸다
        json.dump({'screen': 'Lobby', 'root': 'LobbyMainUI', 'items': rows,
                   'last': ['HostSelectPanel']}, f,
                  ensure_ascii=False, indent=1)
    print(f'{len(rows)}개 → {out}')


if __name__ == '__main__':
    main()
