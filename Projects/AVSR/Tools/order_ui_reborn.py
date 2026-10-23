"""RE:BORN UI 리뉴얼(2026-10-23) — 재발주 · 시안 발주문 한꺼번에 만들기.

PD 2026-10-23: 묶음의 판 · 버튼 · 카드 · 전투 HUD 는 코드로 그린 것이라 못 쓴다 → 스타일 타일 · 처음 HUD 시안을 기준으로 다시 발주.
아웃게임 연출 17종은 그림 시안이 없어 코덱스 자유 시안부터(스킬 fx-art-pipeline). 챕터 배너 11~20 · 히든 실루엣도 같이.
기준 그림: Tools/ui_reborn_anchors.py 가 자른 _exchange/ref/ui_reborn_1023/anchor/*.png

python order_ui_reborn.py → _exchange/order_rb_*.ps1 + 실행기 _exchange/run_ui_reborn.ps1(한 줄로만 돌린다)
납품: _exchange/in/rb_*.png
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
A = 'Projects/AVSR/_exchange/ref/ui_reborn_1023/anchor/'
PKG = 'Projects/AVSR/_handoff/reborn_ui_1023/'
STYLE = PKG + '04_검수기록/style_tile.png'
SCR = PKG + '03_리소스_및_시안/REBORN_UI_FINAL/Screens/'
ART1X = PKG + '03_리소스_및_시안/REBORN_UI_FINAL/REBORN_UI/Art/1x/'

HEAD = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
'''

# ── 판 · 버튼 · 카드 ─────────────────────────────────────────
# 이름: (기준 그림들, 칸 폭, 칸 높이, 열, 행, 무엇을 — 칸 순서대로)
UI = {
    'rb_btn_primary': ([A + 'style_buttons.png'], 320, 176, 3, 1,
        '**주요 버튼**(화면당 하나 — 출전 · 확인 · 구매) 320x160 — ① 기본: 코인 노랑 앞면 + 위쪽 흰 광택 띠 + 아래 두께 12 px(진한 호박색) '
        '② 누름: 두께만큼 아래로 내려간 모습(두께 거의 없음) ③ 비활성: 어두운 남회색 앞면 · 두께 없음. 기준 그림 왼쪽 위 「출전 ⚡5」 · 「누름」 버튼'),
    'rb_btn_secondary': ([A + 'style_buttons.png'], 320, 160, 3, 1,
        '**보조 버튼** 320x144 — ① 기본: 짙은 남색 앞면 + 얇은 시안 테두리 + 아래 두께 8 px(어두운 청록) ② 누름 ③ 비활성. 기준 그림 「로비로」 · 「누름」'),
    'rb_btn_price': ([A + 'style_buttons.png'], 320, 160, 4, 1,
        '**가격 버튼** 320x144 — ① 기본: 어두운 갈색 앞면 + 골드 테두리 + 아래 두께 ② 누름 ③ 부족(같은 모양, 테두리 · 앞면이 조금 어둡고 붉은 기) '
        '④ 비활성(회색 · 두께 없음). 기준 그림 「60」 · 「200」 · 「조각 8개 부족」 버튼(코인 아이콘 · 숫자는 그리지 않는다)'),
    'rb_btn_danger': ([A + 'style_buttons.png'], 320, 160, 3, 1,
        '**위험 버튼**(포기 · 삭제) 320x144 — ① 기본: 검붉은 앞면 + 빨간 테두리 + 아래 두께 ② 누름 ③ 비활성. 기준 그림 「포기」'),
    'rb_btn_max': ([A + 'style_buttons.png'], 320, 160, 2, 1,
        '**MAX · 완료 버튼** 320x144 — ① 어두운 초록 앞면 + 초록 테두리 + 아래 두께 ② 누름. 기준 그림 「MAX」 — **글자 「MAX」는 그리지 않는다**(글자는 게임이 얹는다, 지난 납품 반려)'),
    'rb_btn_chip': ([A + 'style_buttons.png'], 320, 160, 3, 1,
        '**칩 버튼**(둥근 끝 알약, 기준 그림의 「전체 23 ›」 모양) 320x144 안에 높이 88 — ① 기본: 남색 앞면 + 시안 테두리 + 두께 6 px ② 누름 ③ 비활성. **글자 · 숫자 · 화살표는 그리지 않는다**(지난 납품 반려)'),
    'rb_btn_icon_round': ([A + 'style_buttons.png'], 144, 144, 3, 1,
        '**아이콘 버튼 원형** 144x144(지름 132) — ① 기본: 남색 원 + 시안 테두리 + 아래 두께 ② 누름 ③ 비활성. 기준 그림 「<」 · 「X」 버튼(기호는 그리지 않는다, 비운 원판만)'),
    'rb_panels': ([A + 'style_panels.png', A + 'style_popup.png'], 400, 300, 4, 1,
        '칸 순서대로 실제 크기로 — ① **일반 판** 256x180: 위→아래 남색 그라데이션 + 윗선 하이라이트 + 옅은 시안 안쪽 테두리 '
        '② **히어로 판** 400x240: 일반 판 + 위 테두리 가운데 시안 **네온관**(가운데가 가장 밝은 빛 선) + 네 모서리 네온 꺾쇠 '
        '③ **모달(팝업 틀)** 400x300: 히어로 재질 + 위 가운데 제목판이 걸칠 자리 ④ **팝업 제목판** 320x112: 양끝을 깎은 육각 오락기 명판(금속성 2단, 리벳 대신 네온 점)'),
    'rb_rows': ([A + 'style_panels.png', SCR + 'mail.png', SCR + 'missions.png'], 520, 160, 4, 2,
        '칸 순서대로 실제 크기로 — ① 목록 줄 400x144(투명에 가까운 면 + 아래 구분선) ② 목록 줄 선택(왼쪽 시안 막대 + 옅은 시안 바탕) '
        '③ 정보 칩 220x64(누를 수 없는 정보, 두께 없음) ④ 재화 칩 260x100(상단 재화 바 — 그림 위에 얹는 유리 느낌) '
        '⑤ 말풍선 320x144 ⑥ 알림 띠 400x144 ⑦ 경고 알림 띠 400x144(왼쪽 빨간 기) ⑧ 보상 알림 띠 520x144(왼쪽 골드 기). 말풍선 꼬리는 그리지 않는다'),
    'rb_stat_tile': ([A + 'style_statpanel.png', SCR + 'growth.png', SCR + 'growth-states.png'], 508, 160, 4, 2,
        '**능력치 타일** 508x146(타일 전체가 강화 버튼) — ① 기본 ② 누름 ③ 부족(강화 띠 회색) ④ MAX(어두운 초록) ⑤ 연속 강화 중(강화 띠 밝게) '
        '⑥ 강화 띠만 140x146(초록 버튼) ⑦ 강화 띠 누름. 타일 오른쪽 140 이 강화 띠 자리. 아이콘 · 숫자 · 글자는 그리지 않는다. ⑧ 칸은 비운다'),
    'rb_host_frame': ([A + 'style_hostcards.png', A + 'lobby_hostrow.png'], 220, 290, 4, 2,
        '**호스트 카드 틀** 200x262 — ① R(파랑, 단순 테두리) ② SR(보라, 위 모서리 2곳 장식) ③ SSR(금, 4모서리 장식 + 위 왕관형 장식) '
        '④ 랜덤(시안, 회로 무늬) ⑤ 유령(은빛 안개 무늬) ⑥ 안쪽 빛(카드 안 아래에서 올라오는 은은한 빛, 틀 없음) '
        '⑦ 선택 링 212x274(노란 링 + 빛) ⑧ 이름 띠 200x104(카드 아래 어두운 띠). 캐릭터 · 글자 · 배지는 그리지 않는다'),
    'rb_badges': ([A + 'style_hostcards.png', A + 'lobby_hostrow.png'], 128, 128, 4, 2,
        '작은 배지 — ① 등급 태그 「R」 88x52 ② 「SR」 ③ 「SSR」 — **셋 다 글자를 넣는다**(같은 글꼴 · 같은 크기, 지난 납품은 SSR 만 비어 반려) ④ BEST 배지 120x52(글자 「BEST」 포함) ⑤ 오늘 배지 120x52(바탕만) '
        '⑥ NEW 배지 120x52(글자 「NEW」 포함) ⑦ 레벨 배지 80x52(시안, 바탕만) ⑧ 프로필 틀 116x116(원형)'),
    'rb_card_option': ([SCR + 'levelup.png', SCR + 'levelup-selected.png', SCR + 'rest.png'], 400, 300, 4, 2,
        '**레벨업 · 휴식 카드** 400x300 — 윗줄 기본, 아랫줄 선택(노란 링 + 빛). 등급별 **배경 무늬**: ① COMMON 무늬 없음 ② RARE 격자 ③ EPIC 소용돌이 ④ 최상 빛살. '
        '위에 등급 띠 자리(글자 없음) + 왼쪽 아이콘 자리 원형 받침. 아이콘 · 글자는 그리지 않는다'),
    'rb_slot_item': ([A + 'style_slots.png', SCR + 'reward.png'], 200, 200, 4, 2,
        '**보상 칸** 170x196 — ① COMMON ② RARE ③ SR ④ SSR: 등급색 받침 + 아래 등급색 빛 + 모서리 장식(SR 이상) + 아래 수량 자리 어두운 띠. '
        '⑤ 칸 빛 시안 160x160 ⑥ 칸 빛 골드 ⑦ 칸 빛 보라(동그랗게 퍼지는 부드러운 빛만) ⑧ 칸은 비운다. 아이템 그림 · 숫자는 그리지 않는다'),
    'rb_tab_bar': ([A + 'lobby_tabbar.png'], 400, 200, 3, 1,
        '**하단 탭 바** — ① 탭 바 바탕 400x192(가로로 늘려 쓴다 — 위쪽 시안 네온 선 + 아래로 진해지는 질감) '
        '② 가운데 전투 탭 원 184x184(오락기 버튼처럼 입체 링 + 안쪽 오목) ③ 같은 원 선택(노란 링 + 빛). 아이콘 · 글자 없음'),
    'rb_controls': ([SCR + 'settings.png', SCR + 'shop.png', SCR + 'fragments-exchange.png'], 320, 144, 4, 2,
        '① 구간 선택 바탕 320x120(일일 · 주간 같은 탭) ② 구간 선택 켜진 칸 320x120 ③ 토글 꺼짐 140x76 ④ 토글 켜짐 140x76(시안) '
        '⑤ 토글 손잡이 64x64 ⑥ 수량 − 버튼 144x144 ⑦ 수량 + 버튼 144x144(기호는 그리지 않는다) ⑧ 칸은 비운다'),
    'rb_bars': ([SCR + 'growth.png', SCR + 'pass.png'], 320, 48, 1, 5,
        '**진행 바** 160x24(가로로 늘려 쓴다) — 위부터 ① 빈 바(어두운 홈, 양끝 꺾쇠 + 10% 눈금) ② 시안 채움 ③ 초록 채움 ④ 노랑 채움 ⑤ 빨강 채움(채움 위 광택선)'),
    'rb_pass_node': ([SCR + 'pass.png'], 144, 144, 4, 1,
        '**패스 칸 동그라미** 144x144 — ① 받기(골드 링 + 빛) ② 받음(초록 링, 차분) ③ 현재(골드, 가장 밝게) ④ 잠김(어두운 남색). 숫자 · 기호 없음'),
    'rb_summon_card': ([SCR + 'summon-result-10.png', SCR + 'summon-result-1.png'], 200, 280, 3, 1,
        '**소환 결과 카드 틀** 200x280 — ① R(파랑) ② SR(보라) ③ SSR(금, 모서리 장식 + 빛). 카드 안은 어두운 면(캐릭터가 들어갈 자리). 글자 없음'),
    # 로비를 세워 보니 없던 것(2026-10-23) — 챕터 카드 아래가 시안처럼 가라앉게 하는 덮개 · 알림 점
    'rb_misc': ([SCR + 'lobby.png'], 128, 512, 2, 1,
        '① **아래 어둡게 덮개** 128x512 — 위는 완전 투명 → 아래로 갈수록 짙은 남색(시안 로비 챕터 카드 아래 바탕색)으로 부드럽게 짙어지는 세로 그라데이션 한 장 '
        '(가로로 늘려 쓴다 · 위 1/3 은 투명). 도트 디더링 결 ② **빨간 알림 점** 지름 32(칸 가운데) — 시안 로비 「우편 · 미션」 아이콘 오른쪽 위 빨간 점, 밝은 테두리 1 px'),
    'rb_result_panels': ([SCR + 'result-story.png', SCR + 'chest-open.png', SCR + 'awaken-6.png'], 520, 280, 2, 2,
        '① 결과 기록 판 520x280 ② 결과 보상 줄 520x240 ③ 상자 보상 줄 520x240 ④ 각성 띠 520x200(히어로 재질, 위 네온). 글자 · 아이콘 없음'),
}

# ── 전투 HUD (처음 시안 07_blueprint_I-02 가 기준) ─────────────────
HUD = {
    'rb_hud_bars': ([A + 'hud_top.png', SCR + 'fight.png'], 520, 160, 2, 2,
        '① 위 반투명 띠 520x160(판 재질, 양끝 꺾쇠, 9-slice) ② 모드 판 460x144(밤 KILLS · 탑 층 표시용) '
        '③ 보스 체력 바 **틀만** 520x100(왼쪽 끝 해골 장식 + 비어 있는 홈 — 빨간 채움은 그리지 않는다, 유령 얼굴 금지 · 지난 납품 반려) ④ 경고 띠 520x144(빨강 사선 줄무늬 띠). 글자 · 숫자 없음. **방 진행 칸은 그리지 않는다**(없앴다)'),
    'rb_hud_hp': ([A + 'hud_top.png'], 320, 48, 3, 3,
        '체력 바 320x40(가로로 늘려 쓴다) — 윗줄 유령(시안 영혼 질감), 가운데 줄 호스트(초록), 아랫줄 경험치(노랑). '
        '각 줄 ① 빈 바탕(홈) ② 채움(눈금 · 광택선) ③ 잔상(깎이고 남는 옅은 색)'),
    'rb_hud_possess': ([A + 'hud_buttons.png'], 240, 240, 4, 1,
        '**빙의 버튼** 230x230(가장 큼) — ① 기본: 어두운 원판 + 시안 링 ② 누름 ③ 준비(시안 링이 밝게 빛남) ④ 쿨다운 덮개(원판 위 반투명 어두운 원, 돌아가며 채워질 원). '
        '가운데는 비운다(유령 얼굴은 따로 얹는다). 장식 테두리 금지'),
    'rb_hud_skill': ([A + 'hud_buttons.png'], 176, 176, 4, 2,
        '버튼 170x170 — 윗줄 **액티브**(시안 계열 링), 아랫줄 **필살기**(노란 링). 각 줄 ① 기본 ② 누름 ③ 준비(링이 빛남) ④ 쿨다운 덮개. 가운데는 비운다(스킬 아이콘은 따로 얹는다)'),
    'rb_hud_misc': ([A + 'hud_joystick.png', A + 'hud_buttons.png'], 280, 280, 3, 2,
        '① 조이스틱 바탕 280x280(반투명 링 + 옅은 네온 테두리) ② 조이스틱 손잡이 112x112 ③ 일시정지 버튼 120x120(기호 없음) ④ 일시정지 누름 '
        '⑤ READY 칩 바탕 160x56(시안 테두리 알약, 글자 없음) ⑥ 칸은 비운다'),
}

UI_TPL = HEAD + '''먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다(방향 · 9-slice 제약).
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼 — {kind} 리소스. 승인 디자인: {style}
기준 그림(배치 · 색 · 비율은 이대로, 재질 · 마감은 손으로 만든 출시 품질로): {refs}
[만들 것] {what}
[시트] 칸 {w}x{h}, {n}칸을 가로 {cols}칸 x {rows}줄(왼쪽 위부터) — 전체 {sw}x{sh}, **투명 바탕**. 칸마다 부품 하나, 칸 가운데.
⚠ **각 부품은 적힌 크기 그대로**(예: 320x160 이면 가로 320 · 세로 160) 칸을 채운다 — 칸 여백은 사방 8 px 정도. 납작하거나 작게 그리지 말 것
(지난 샘플은 320x160 버튼을 290x60 으로 그려 반려).
[저장] Projects/AVSR/_exchange/in/{sheet}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

# ── 챕터 배너 11~20 ─────────────────────────────────────────
# 챕터: (같은 무대 배너 번호, 분위기 — 같은 무대의 앞 챕터와 확 달라 보이게)
BANNERS = {
    11: (5, '감시 구역 — 천장 곳곳의 붉은 감시 렌즈 · 어둠 속 붉은 탐조등 빛'),
    12: (6, '그림자 공장 — 불이 꺼진 정유 공장, 보랏빛 어둠과 긴 그림자, 희미한 비상등'),
    13: (1, '톱날 고물상 — 녹슨 고철 더미 사이 회전 톱날 · 쇳가루 불똥이 튀는 주황빛'),
    14: (2, '푸른 등불 기지 — 미사일 기지에 꺼지지 않는 청록 등불이 줄지어 떠 있다'),
    15: (3, '속임수 골목 — 밤 골목, 수상한 상자 · 쓰레기통이 늘어선 보라 · 녹색 간판빛'),
    16: (4, '불타는 옥상 — 도시 꼭대기까지 번진 불길, 밤하늘을 붉게 물든 연기'),
    17: (5, '오염 실험동 — 독 연두 액체와 하얗게 언 얼음이 뒤섞인 실험실'),
    18: (6, '용광로 심장부 — 쇳물이 끓는 내부, 주황 · 빨강 쇳물 빛과 열기'),
    19: (1, '폐허의 왕국 — 고철로 쌓은 왕좌 · 버려진 것들의 왕국, 차가운 달빛과 낡은 금빛'),
    20: (2, '최후의 발사대 — 붉은 경보등이 도는 발사대, 로켓 아래 불꽃 · 연기'),
}
BANNER_TPL = HEAD + '''먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」 챕터 {ch} 배너 그림(로비 · 챕터 선택 카드용, 글자 없음).
같은 무대의 앞 챕터 배너(화풍 · 장소 · 구도 · 도트 결은 이대로): {anchor}
[분위기] {mood}. **앞 배너와 한눈에 다른 챕터로 보이게** — 빛 색 · 시간 · 상황을 바꾼다.
- 1032x420 가로, 꽉 찬 그림(투명 없음). 캐릭터 · 글자 · 숫자 · 로고 · 버튼 · 틀 금지. 왼쪽 절반은 글자가 얹히니 조금 어둡고 차분하게.
[저장] Projects/AVSR/_exchange/in/rb_banner_{ch:02d}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

SILHOUETTE = HEAD + '''먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」 **히든 캐릭터 공개 전 실루엣**(랜덤 출전에서 아주 드물게 나오는 숨은 몸).
정체는 사신이다 — 원본 그림: Assets/BaseResource/HostSelectPanel/hostportraitimage_death.png (이 자세 · 외곽 모양을 따른다)
[만들 것] 512x512 투명 바탕, 사신의 외곽(두건 · 낫 끝이 살짝 보이는)을 **검은 실루엣**으로 + 가장자리를 도는 가는 금빛 테두리 빛. 안쪽은 정체를 알 수 없게 까맣게.
도트 결이 살아 있게(뭉툭하게 키운 모양 금지). 칸 가운데, 글자 없음.
[저장] Projects/AVSR/_exchange/in/rb_hidden_silhouette.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

# ── 아웃게임 연출 시안(자유 시안 — 코덱스가 아트 디렉터) ─────────────────
# 이름: (바탕, 장면 · 강약)
FX = {
    'summon_beam': ('base_summon-flash.png', '소환 — 펼친 영혼 명부에서 빛기둥이 솟는 순간. **세 등급을 한 장에 나란히**(왼쪽 R 파랑 · 가운데 SR 보라 · 오른쪽 SSR 금) — '
                    '등급이 오를수록 빛살 · 입자 · 겹이 한 겹씩 늘고, SSR 은 화면 번쩍까지. 크기는 셋 다 같게(위 등급이라 키우지 않는다)'),
    'card_flip': ('base_summon-result-1.png', '소환 결과 카드가 뒤집히며 공개되는 순간 — 카드 가장자리를 도는 등급색 빛 + 뒤집힐 때 튀는 빛 조각'),
    'stamp_jackpot': ('base_summon-result-jackpot.png', '10회 소환 대박 — 붉은 잉크 원형 도장이 쾅 찍히는 순간(커짐 → 찍힘 → 잉크 먼지). 가장자리 거친 잉크 질감, 도장 안 글자는 비운다'),
    'chest_open': ('base_chest-opening.png', '상자 열기 — 뚜껑이 열리며 빛과 내용물이 튀는 순간. **은 · 금 · 백금 세 등급을 한 장에 나란히**, 위 등급일수록 겹이 늘고 백금만의 겹을 하나 남긴다. 크기는 같게'),
    'coin_burst': ('base_reward.png', '보상 받음 — 코인 · 보석이 회전하며 화면 위 재화 칩으로 날아가는 순간(날아가는 길 · 칩에 닿을 때 반짝)'),
    'star_fill': ('base_star-success.png', '성급 강화 성공 — 빈 별이 금빛으로 채워지는 순간 작은 폭발 + 반짝임'),
    'awaken_6': ('base_awaken-6.png', '6성 각성 — 캐릭터 둘레 시안 링이 폭발하듯 퍼짐 + 영혼 입자'),
    'awaken_10': ('base_awaken-10.png', '10성 각성(최상위) — 금빛 링 2겹 + 빛살 + 금빛 입자, 6성보다 확실히 크고 화려하게'),
    'upgrade_tick': ('base_growth-success.png', '능력치 강화 성공 — 누른 능력치 타일 위를 지나가는 초록 펄스 + 오른 숫자 옆 작은 반짝(약하게, 자주 보는 연출)'),
    'reward_reveal': ('base_reward-reveal.png', '보상 칸이 하나씩 열리는 순간 — 칸 테두리 등급색 빛 + 안쪽에서 터지는 빛(중간 세기)'),
    'button_shine': ('base_lobby.png', '로비 「출전」 주요 버튼 위를 비스듬히 지나가는 빛 띠(가끔 반복, 은은하게 — 누르고 싶게)'),
}
FX_TPL = HEAD + '''게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아웃게임 연출 **시안** — {name}.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙. Projects/AVSR/_exchange/codex_brief_ui_reborn.md 도 읽는다(새 UI 방향).
[바탕] Projects/AVSR/_exchange/ref/ui_reborn_1023/anchor/{base} — 새 UI 시안 화면. 창 · 칸 · 글자 · 캐릭터는 **그대로** 두고 그 위에 연출을 덧그린다.
[장면] {scene}
네가 아트 디렉터라면 어떻게 꾸미겠나 — 더 멋진 안이 있으면 그걸로. 도트 모양 + 부드럽게 퍼지는 광원(블룸 · 빛 받는 주변) + 떠오르는 빛 알갱이를 함께. 게임 정보(글자 · 숫자)는 가리지 않는다.
- 크기는 바탕과 같은 720x1560. 아래 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/rb_mock_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''param([string[]]$Sheets)
# RE:BORN UI 재발주 · 시안을 한 장씩 차례로 돌린다. 이미 받은 것은 건너뛴다.
# ⚠ 이 실행기는 **한 줄로만** 돌린다 — 두 줄을 동시에 돌리면 겹치는 순간 429(짧은 시간 요청 몰림)가 난다(2026-10-22).
$ex = 'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange'
$empty = "$ex\ref\rework1021\empty.txt"
$all = @(KEYS)
if ($Sheets) { $all = ($Sheets -join ',').Split(',') }
foreach ($s in $all) {
    if (Test-Path "$ex\in\$s.png") { continue }
    for ($try = 1; $try -le 5; $try++) {
        cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File `"$ex\order_$s.ps1`" < `"$empty`" > `"$ex\log_$s.txt`" 2>&1"
        if (Test-Path "$ex\in\$s.png") { break }
        Start-Sleep -Seconds 120
    }
    "[ORDER] $s $(if (Test-Path "$ex\in\$s.png") {'ok'} else {'fail'})"
}
'''


def write(name, text):
    with open(os.path.join(EX, 'order_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
        f.write(text)


def main():
    keys = []
    for group, kind in ((UI, '판 · 버튼 · 카드'), (HUD, '전투 HUD')):
        for sheet, (refs, w, h, cols, rows, what) in group.items():
            write(sheet, UI_TPL.format(kind=kind, style=STYLE, refs=' · '.join(refs), what=what, w=w, h=h,
                                       n=cols * rows, cols=cols, rows=rows, sw=w * cols, sh=h * rows, sheet=sheet))
            keys.append(sheet)
    write('rb_hidden_silhouette', SILHOUETTE)
    keys.append('rb_hidden_silhouette')
    for ch, (src, mood) in BANNERS.items():
        anchor = ART1X + 'ui_art_chapter_banner_%02d@1032x420.png' % src
        write('rb_banner_%02d' % ch, BANNER_TPL.format(ch=ch, anchor=anchor, mood=mood))
        keys.append('rb_banner_%02d' % ch)
    for name, (base, scene) in FX.items():
        write('rb_mock_' + name, FX_TPL.format(name=name, base=base, scene=scene))
        keys.append('rb_mock_' + name)
    # 판 샘플(주요 버튼)을 맨 앞에 — 받자마자 보고 틀렸으면 줄을 멈추고 발주문을 고친다
    with open(os.path.join(EX, 'run_ui_reborn.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    main()
