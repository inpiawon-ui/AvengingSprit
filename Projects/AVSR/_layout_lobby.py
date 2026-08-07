"""로비 목업 실측 레이아웃 → Unity 적용용 JSON.

좌표 출처: Reference/Mockups/lobby_hub.jpeg (683x1024) 에 20px 격자를 씌워 실측.
이 파일이 로비 레이아웃의 **단일 출처**다. 프리팹을 직접 손보지 말고 여기를 고친다.

좌표계 변환 — 목업 683x1024(2:3) → 캔버스 720x1280(9:16)
  S = 720/683
  x' = x * S                                (가로는 균일 스케일)
  y' : 밴드별
    hud     y' = y * S                      상단 고정
    content y' = 1280 - (1024 - y) * S      하단 고정
  세로로 남는 200px 은 HUD 와 콘텐츠 사이 배경 영역이 전부 흡수한다.
  (목업의 하늘/유령 영역이 넓어질 뿐 요소 간 간격비는 보존된다)

폰트 크기 — 목업에서 잰 대문자 높이(cap) 로부터
  fontSize = cap / 0.70 * S                 (Noto Sans KR cap ratio 0.70)
"""
import json, os

S = 720.0 / 683.0
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'Scripts', 'Editor', 'UISpec', '_layout_Lobby.json')

# 팔레트 (목업 스포이드)
WHITE = '#F2F4F8'
GOLD = '#F0B428'
BLUE = '#4AA8E8'
RED = '#E8404A'
DIM = '#6E90B8'
DARK = '#241A08'          # 골드 버튼 위 어두운 글자
LAV = '#C9A0F0'           # HOST 라벨
LAV_D = '#9B7ACC'
SHOP = '#6EC0F0'
SHOP_D = '#5A96C0'
GOLD_D = '#C89830'
GREEN = '#8CD048'
GREY = '#4A5468'


def cap(c):
    """대문자 높이 → TMP fontSize"""
    return round(c / 0.70 * S, 1)


# (이름, 밴드, 목업 rect, 옵션)
#   옵션 키: text / size / align(L|C|R) / color / wrap / clone / dx / parent
L = []


def add(name, band, rect, **o):
    L.append(dict(name=name, band=band, rect=list(rect), **o))


# ── 상단 HUD ────────────────────────────────────────────────────────
add('TopHudGroup', 'hud', (0, 0, 683, 100))
add('TopHudBackground', 'hud', (0, 0, 683, 100))
add('GhostWidget', 'hud', (9, 10, 161, 77))
add('GhostPortraitIcon', 'hud', (12, 13, 58, 58))
add('GhostLabelText', 'hud', (78, 16, 82, 16), text='GHOST', size=cap(9), align='L', color=WHITE)
add('GhostLevelText', 'hud', (78, 33, 82, 21), size=cap(12), align='L', color=GOLD)
add('GhostExpBarBg', 'hud', (78, 55, 80, 11))
add('GhostExpBarFill', 'hud', (78, 55, 80, 11))
add('GhostExpText', 'hud', (96, 66, 62, 15), size=cap(9), align='R', color=WHITE)

add('StaminaCounter', 'hud', (179, 21, 106, 34))
add('StaminaIcon', 'hud', (186, 27, 16, 21))
add('StaminaText', 'hud', (206, 28, 52, 19), size=cap(13), align='C', color=WHITE)
add('StaminaCounter/PlusButton', 'hud', (261, 30, 16, 16))

add('GoldCounter', 'hud', (292, 21, 108, 34))
add('GoldIcon', 'hud', (299, 27, 21, 21))
add('GoldText', 'hud', (324, 28, 56, 19), size=cap(13), align='C', color=WHITE)
add('GoldCounter/PlusButton', 'hud', (381, 30, 16, 16))

add('GemCounter', 'hud', (409, 21, 110, 34))
add('GemIcon', 'hud', (415, 27, 21, 21))
add('GemText', 'hud', (440, 28, 56, 19), size=cap(13), align='C', color=WHITE)
add('GemCounter/PlusButton', 'hud', (498, 30, 16, 16))

add('MailButton', 'hud', (567, 26, 35, 26))
add('MailButton/NotifyBadge', 'hud', (593, 19, 19, 19))
add('SettingsButton', 'hud', (631, 25, 27, 27))

# ── 배경 연출 ───────────────────────────────────────────────────────
add('GhostAvatar', 'content', (256, 294, 176, 220))
add('PortalRing', 'content', (256, 598, 176, 54))

# ── 챕터 카드 ───────────────────────────────────────────────────────
add('ChapterCard', 'content', (14, 288, 198, 290))
add('ChapterNumberText', 'content', (22, 298, 130, 21), size=cap(14), align='L', color=BLUE)
add('ChapterNameText', 'content', (20, 320, 160, 30), size=cap(24), align='L', color=WHITE)
add('BossLabel', 'content', (20, 353, 60, 19), text='BOSS', size=cap(13), align='L', color=RED)
add('BossNameText', 'content', (20, 377, 78, 21), size=cap(13), align='L', color=WHITE)
add('BossPortrait', 'content', (100, 352, 72, 80))
add('ProgressLabel', 'content', (20, 448, 80, 19), text='PROGRESS', size=cap(13), align='L', color=DIM)
add('ProgressBarBg', 'content', (20, 470, 122, 9))
add('ProgressBarFill', 'content', (20, 470, 122, 9))
add('ProgressText', 'content', (20, 463, 80, 21), size=cap(14), align='L', color=WHITE)
add('ProgressRewardChest', 'content', (148, 450, 34, 33))
add('ContinueButton', 'content', (30, 502, 175, 62))
add('ContinueButtonText', 'content', (30, 510, 175, 26), size=cap(18), align='C', color=DARK)
# 번개 글리프는 폰트에 없어 두부(□)가 된다 — 스태미나 아이콘 스프라이트로 대체
add('ContinueButton/StaminaIcon', 'content', (95, 537, 18, 21), create='IMG', parent='ContinueButton')
add('ContinueCostText', 'content', (116, 538, 44, 19), text='x5', size=cap(13), align='L', color=DARK)

# ── 우측 프로모 (1차 범위 밖 — 정적 표기만) ─────────────────────────
add('SidePromoGroup', 'content', (458, 288, 209, 300))
add('BattlePassCard', 'content', (458, 288, 209, 95))
add('BattlePassTitleText', 'content', (466, 297, 130, 21), text='BATTLE PASS',
    size=cap(15), align='L', color=GOLD, create='TMP', parent='BattlePassCard')
add('BattlePassSeasonText', 'content', (468, 321, 130, 20), text='SEASON 1',
    size=cap(14), align='L', color=WHITE, create='TMP', parent='BattlePassCard')
add('BattlePassBadge', 'content', (466, 346, 34, 34))
add('BattlePassBarBg', 'content', (505, 351, 78, 10))
add('BattlePassBarFill', 'content', (505, 351, 78, 10))
add('BattlePassExpText', 'content', (503, 361, 82, 19), text='45 / 100',
    size=cap(13), align='C', color=WHITE, create='TMP', parent='BattlePassCard')
add('BattlePassArt', 'content', (585, 290, 78, 92))

add('EventCard', 'content', (458, 390, 209, 97))
add('EventTitleText', 'content', (466, 398, 130, 26), text='EVENT',
    size=cap(20), align='L', color=BLUE, create='TMP', parent='EventCard')
add('EventTimerText', 'content', (466, 450, 130, 21), text='6D 18H',
    size=cap(14), align='L', color=BLUE, create='TMP', parent='EventCard')
add('EventArt', 'content', (570, 404, 76, 78))
add('EventCard/NotifyBadge', 'content', (638, 392, 22, 22))

add('DailyLoginCard', 'content', (458, 492, 209, 96))
add('DailyLoginTitleText', 'content', (466, 498, 150, 24), text='DAILY LOGIN',
    size=cap(18), align='L', color=GREEN, create='TMP', parent='DailyLoginCard')
add('DailyLoginArt', 'content', (572, 512, 52, 46))
add('DailyLoginCheck', 'content', (462, 555, 16, 16), clone=4, dx=17)
add('DailyLoginDayText', 'content', (612, 559, 50, 18), text='DAY 5',
    size=cap(11), align='R', color=WHITE, create='TMP', parent='DailyLoginCard')
add('DailyLoginCard/NotifyBadge', 'content', (638, 494, 22, 22))

# ── 기능 탭 바 (1차 범위 밖) ────────────────────────────────────────
TAB_C = [111, 222, 333, 444, 555]
TABS = ['Mission', 'Achievement', 'Ranking', 'Inventory', 'Friends']
add('FeatureTabBar', 'content', (20, 618, 640, 76))
add('FeatureTabBarBackground', 'content', (20, 618, 640, 76))
for c, t in zip(TAB_C, TABS):
    add(f'{t}Tab', 'content', (c - 50, 620, 100, 72))
    add(f'{t}TabIcon', 'content', (c - 20, 624, 40, 40))
    add(f'{t}TabLabel', 'content', (c - 50, 674, 100, 17),
        text=t.upper(), size=cap(9), align='C', color=WHITE)
add('MissionTab/NotifyBadge', 'content', (141, 619, 18, 18))
add('FriendsTabLock', 'content', (549, 641, 21, 21))

# ── 하단 3버튼 ──────────────────────────────────────────────────────
add('MainActionBar', 'content', (12, 706, 645, 196))
add('HostButton', 'content', (12, 712, 200, 188))
add('HostButtonArt', 'content', (20, 718, 184, 116))
add('HostButtonTitleText', 'content', (12, 836, 200, 34), text='HOST', size=cap(28), align='C', color=LAV)
add('HostButtonSubText', 'content', (12, 869, 200, 20),
    text='육성 · ULTIMATE · 도감', size=cap(12), align='C', color=LAV_D)
add('HostButton/NotifyBadge', 'content', (194, 715, 20, 20))

add('ChapterButton', 'content', (228, 708, 215, 194))
add('ChapterButtonArt', 'content', (235, 714, 201, 122))
add('ChapterButtonTitleText', 'content', (228, 838, 215, 34), text='CHAPTER', size=cap(32), align='C', color=GOLD)
add('ChapterButtonSubText', 'content', (228, 869, 215, 20),
    text='게임 시작', size=cap(12), align='C', color=GOLD_D)

add('ShopButton', 'content', (452, 712, 203, 188))
add('ShopButtonArt', 'content', (458, 718, 191, 116))
add('ShopButtonTitleText', 'content', (452, 836, 203, 34), text='SHOP', size=cap(28), align='C', color=SHOP)
add('ShopButtonSubText', 'content', (452, 869, 203, 20),
    text='상점 · 패키지 · 재화', size=cap(12), align='C', color=SHOP_D)
add('ShopButton/NotifyBadge', 'content', (637, 715, 20, 20))

add('LogoLockup', 'content', (250, 908, 160, 108))
add('VersionText', 'content', (14, 992, 70, 15), size=cap(10), align='L', color=GREY)


def y_of(band, y):
    return y * S if band == 'hud' else 1280.0 - (1024.0 - y) * S


def main():
    rows = []
    for e in L:
        x, y, w, h = e['rect']
        r = {
            'name': e['name'],
            'x': round(x * S, 1), 'y': round(y_of(e['band'], y), 1),
            'w': round(w * S, 1), 'h': round(h * S, 1),
        }
        for k in ('text', 'size', 'align', 'color', 'wrap', 'clone', 'dx', 'create', 'parent'):
            if k in e:
                r[k] = e[k]
        if 'dx' in r:
            r['dx'] = round(r['dx'] * S, 1)
        rows.append(r)

    out = os.path.abspath(OUT)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, 'w', encoding='utf-8') as f:
        json.dump({'screen': 'Lobby', 'root': 'LobbyMainUI', 'items': rows}, f,
                  ensure_ascii=False, indent=1)
    print(f'{len(rows)}개 → {out}')


if __name__ == '__main__':
    main()
