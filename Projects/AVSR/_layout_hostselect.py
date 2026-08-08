"""호스트 선택 목업 실측 레이아웃 → Unity 적용용 JSON.

좌표 출처: Reference/Mockups/host_select.jpeg (683x1024) 격자 실측 + 밝기 스캔.
이 파일이 호스트 선택 레이아웃의 **단일 출처**다.

로비와 다른 점 — 로비는 하늘(배경)이 넓어 남는 세로를 전부 흡수했지만,
이 화면은 콘텐츠가 위아래로 꽉 차 있다. 그래서 세 구간으로 나눠 잡는다.

  상단(헤더)   HUD 바로 아래 고정        y' = 108 + (y-58)*S
  하단(버튼·팁) 화면 바닥 고정            y' = 1280 - (1008-y)*S
  중단(목록·상세) 남는 세로를 흡수        직접 지정

승인된 목업 편차 (AVSR_Decisions.md)
  · 그리드 15칸(5행) → **12칸(4행)** — 색상 변형은 코스튬으로 분리 (확정 10)
  · 4번째 스탯 `JUMP` → **`DASH`** — 완전 탑다운이라 점프가 없다 (확정 2)
  4행이 되며 남는 세로는 셀 높이로 흡수한다(목업 100x120 → 105x150).
"""
import json, os

import _fit_boxes

S = 720.0 / 683.0
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'Scripts', 'Editor', 'UISpec', '_layout_HostSelect.json')

HUD_H = 105          # 상단 HUD 높이 (로비 소유, 이 화면과 공유)
PANEL_H = 1280 - HUD_H

WHITE = '#F2F4F8'
GOLD = '#F0B428'
BLUE = '#4AA8E8'
PURPLE = '#C98CF0'
MUTED = '#8C9AB4'
DARK = '#241A08'
GREEN = '#8CD048'

STAT_COLOR = {'HP': '#E8404A', 'ATK': '#F08828', 'SPD': '#3C9CF0', 'DASH': '#5CC850'}

L = []


def cap(c):
    """목업 대문자 높이 → TMP fontSize (Noto cap ratio 0.70)"""
    return round(c / 0.70 * S, 1)


def add(name, rect, **o):
    L.append(dict(name=name, rect=[round(v, 1) for v in rect], **o))


def top(y):
    """상단 밴드 — HUD 바로 아래 고정"""
    return 108 + (y - 58) * S


def bot(y):
    """하단 밴드 — 화면 바닥 고정"""
    return 1280 - (1008 - y) * S


def mx(x):
    return x * S


# ── 헤더 (상단 밴드) ────────────────────────────────────────────────
add('HeaderGroup', (0, top(58), 720, top(190) - top(58)))
add('HeaderGhostDeco', (mx(14), top(62), mx(60), (124 - 62) * S))
add('HeaderTitleText', (mx(84), top(103), mx(310), (142 - 103) * S),
    size=cap(26), align='L', color=WHITE)
add('HeaderDescText', (mx(85), top(150), mx(400), (176 - 150) * S),
    size=cap(13), align='TL', color=MUTED, wrap=True)
add('HeaderPortalDeco', (mx(505), top(58), mx(202), (188 - 58) * S))

# ── 중단 · 호스트 목록 ──────────────────────────────────────────────
# 그리드는 4행이라 목업(5행)보다 셀이 높다. 남는 세로를 여기서 흡수한다.
add('HostListTitle', (mx(12), 256, mx(322), 34))
add('HostListTitleText', (mx(12), 258, mx(322), 30),
    text='HOST LIST', size=cap(16), align='C', color=GOLD)
add('HostGrid', (mx(14), 300, 327, 624), grid=[105, 150, 6, 8, 3])

# 셀 내부 — `HostSlot` 은 런타임에 복제되므로 좌표가 **셀 기준 로컬**이다
add('HostSlotFrame',        (0, 0, 105, 150), local=True)
add('HostSlotPortrait',     (7, 12, 91, 84), local=True)
add('HostSlotNameText',     (2, 108, 101, 26), local=True,
    size=cap(13), align='C', color=WHITE)
add('HostSlotLockIcon',     (33, 46, 40, 40), local=True)
add('HostSlotSelectMarker', (4, 4, 26, 26), local=True)

# 목록과 상세 사이의 유령 화살표
add('GhostArrowGroup',   (348, 548, 50, 104))
add('GhostArrowSprite',  (350, 548, 44, 60))
add('GhostArrowChevron', (352, 616, 40, 34))

# ── 중단 · 상세 카드 ────────────────────────────────────────────────
CARD_X, CARD_Y, CARD_W, CARD_H = 420, 250, 283, 737
add('HostDetailCard',  (CARD_X, CARD_Y, CARD_W, CARD_H))
add('HostDetailFrame', (CARD_X, CARD_Y, CARD_W, CARD_H))
add('HostNameEnText',  (CARD_X + 10, 268, CARD_W - 20, 32), size=cap(15), align='C', color=PURPLE)
add('HostNameKrText',  (CARD_X + 10, 302, CARD_W - 20, 26), size=cap(14), align='C', color=WHITE)
add('HostPortalPedestal', (438, 545, 246, 62))
add('HostPortraitImage',  (470, 392, 182, 168))   # 원본 크기 — 비정수 확대 금지
add('HostDetailLockIcon', (513, 428, 96, 96))
add('StatGroupLabel',  (440, 630, 120, 24), text='능력치', size=cap(12), align='L', color=MUTED)

# 스탯 4행 — 확정 2: 목업의 JUMP 는 DASH 로 재정의됐다
for i, stat in enumerate(('HP', 'ATK', 'SPD', 'DASH')):
    y = 662 + i * 38
    add(f'StatRow_{stat}', (437, y, 250, 34))
    add(f'StatIcon_{stat}', (437, y + 8, 20, 20))
    add(f'StatRow_{stat}/StatLabelText', (465, y + 6, 60, 24),
        text=stat, size=cap(14), align='L', color=WHITE)
    add(f'StatRow_{stat}/StatBarBg', (531, y + 11, 122, 12))
    # 목업은 스탯마다 바 색이 다르다 (HP 적 / ATK 주황 / SPD 청 / DASH 녹)
    add(f'StatRow_{stat}/StatBarBg/StatBarFill', (531, y + 11, 122, 12), color=STAT_COLOR[stat])
    add(f'StatRow_{stat}/StatValueText', (657, y + 6, 40, 24),
        size=cap(14), align='C', color=WHITE)

add('UltimateCard',     (430, 822, 263, 150))
add('UltimateLabel',    (442, 830, 130, 20), text='ULTIMATE', size=cap(10), align='L', color=PURPLE)
add('UltimateIcon',     (442, 856, 62, 68))
add('UltimateNameText', (512, 856, 170, 24), size=cap(12), align='L', color=GOLD)
add('UltimateDescText', (512, 884, 170, 78), size=cap(11), align='TL', color=MUTED, wrap=True)

# ── 하단 (바닥 고정) ────────────────────────────────────────────────
UP_Y = bot(757)
add('HostUpgradeButton',    (mx(358), UP_Y, mx(307), (838 - 757) * S))
add('HostUpgradeIcon',      (mx(387), UP_Y + 20, mx(36), 46))
add('HostUpgradeTitleText', (mx(378), UP_Y + 12, mx(250), 32), size=cap(22), align='C', color=WHITE)
add('HostUpgradeSubText',   (mx(432), UP_Y + 52, mx(200), 22), size=cap(12), align='C', color=BLUE)
add('HostUpgradeButton/ArrowIcon', (mx(626), UP_Y + 28, mx(20), 30))

PS_Y = bot(848)
add('PossessStartButton',   (mx(355), PS_Y, mx(312), (941 - 848) * S))
add('PossessGhostIcon',     (mx(388), PS_Y + 26, mx(38), 52))
add('PossessTitleText',     (mx(378), PS_Y + 22, mx(260), 36), size=cap(27), align='C', color=DARK)
add('PossessSubText',       (mx(378), PS_Y + 60, mx(260), 24), size=cap(13), align='C', color=DARK)
add('PossessStartButton/ArrowIcon', (mx(628), PS_Y + 38, mx(20), 28))

TIP_Y = bot(950) - 16      # 2줄 팁 + 바닥 여백 확보
add('TipBar',              (mx(10), TIP_Y, mx(663), 70))
add('TipIcon',             (mx(24), TIP_Y + 24, mx(20), 20))
add('TipText',             (mx(50), TIP_Y + 12, mx(370), 50), size=cap(11), align='TL', color=MUTED, wrap=True)
add('OwnedHostChestIcon',  (mx(452), TIP_Y + 20, mx(34), 28))
add('OwnedHostCountText',  (mx(496), TIP_Y + 24, mx(175), 26), size=cap(14), align='L', color=GOLD)


def main():
    rows = []
    for e in L:
        x, y, w, h = e['rect']
        r = {'name': e['name'], 'x': round(x, 1), 'y': round(y, 1),
             'w': round(w, 1), 'h': round(h, 1)}
        for k in ('text', 'size', 'align', 'color', 'wrap', 'local', 'grid', 'all'):
            if k in e:
                r[k] = e[k]
        rows.append(r)

    # 목업 실측 박스와 실제 에셋 비율이 어긋나면 여기서 맞춘다

    _fit_boxes.report(_fit_boxes.fit(rows, 'HostSelectPanel'), 'HostSelect')

    out = os.path.abspath(OUT)
    with open(out, 'w', encoding='utf-8') as f:
        json.dump({'screen': 'HostSelect', 'root': 'HostSelectPanel',
                   'origin': [0, HUD_H], 'rootRect': [0, HUD_H, 720, PANEL_H],
                   'items': rows}, f, ensure_ascii=False, indent=1)
    print(f'{len(rows)}개 → {out}')


if __name__ == '__main__':
    main()
