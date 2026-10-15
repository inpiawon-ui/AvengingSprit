import codecs, json

EX = r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange' + '\\'
A = 'Projects/AVSR/_exchange/in/'

# (시트 키, 바탕, 앵커 장면들, [(부품 이름, 대략 크기, 설명)])
SHEETS = [
    ('p2_bottombar', 'pink', ['scene_s01_lobby', 'mock_summon_a'], [
        ('nav_plate', '170x110', '하단 바 칸 판 — 남색 바탕 · 하늘 네온 테두리(HOST · PLAY · SHOP 이 같이 쓴다) · 아이콘 · 글자 없음'),
        ('nav_plate_summon', '170x110', '하단 바 SUMMON 칸 판 — 같은 모양, 보라 네온 · 은은한 보라 불꽃 결'),
        ('nav_sel_rim', '180x120', '고른 칸 금 네온 테두리 덧판(속 비움)'),
        ('nav_icon_summon', '96x96', 'SUMMON 아이콘 — 기준 유령이 한 손에 보라 영혼 불꽃을 든 모습'),
    ]),
    ('p2_topbar', 'pink', ['scene_s20_stamina', 'scene_s01_lobby'], [
        ('top_stamina_plate', '150x56', '상단 바 행동력 칸 판 — 골드 · 젬 칸과 같은 알약 모양(글자 · 숫자 없음)'),
        ('icon_stamina', '48x48', '행동력 아이콘 — 하늘빛 영혼 불꽃 번개'),
        ('top_pass_plate', '300x44', '패스 배지 판 — 「SEASON 1 · GHOST REVENGE · Lv 12」가 들어갈 가로 띠(글자 없음)'),
        ('icon_season_crown', '36x36', '시즌 왕관 아이콘(금)'),
        ('icon_reddot', '24x24', '빨간 점(알림)'),
    ]),
    ('p2_startmode', 'pink', ['scene_s22a_host', 'scene_s22b_random', 'scene_s22c_ghost'], [
        ('mode_card_plate', '190x200', '출전 방식 큰 카드 판 — 남색 바탕 · 하늘 네온 · 위에 제목 띠 자리(글자 없음)'),
        ('mode_card_sel', '200x210', '고른 출전 방식 카드 금 테 덧판(속 비움) · 오른쪽 위 체크 칸'),
        ('mode_random_art', '140x140', '「?」 랜덤 그림 — 검은 실루엣 머리 + 하늘빛 물음표(S22 와 같은 그림)'),
        ('row_dim', '700x190', '호스트 줄을 덮는 어둡게 판 — 남색 검정 60% 반투명 느낌 · 모서리 둥글게 · 가장자리 은은한 네온'),
        ('row_notice_band', '560x44', '어둡게 판 가운데 안내 띠 판(글자 없음)'),
        ('start_btn_off', '520x110', '도전하기 버튼 비활성(회색) — 지금 노란 도전하기 버튼(Assets/BaseResource/ChapterHost/ch_start_button.png)과 같은 모양'),
        ('ribbon_luck', '260x34', '버튼 위 작은 띠 — 행운 보너스(하늘 · 금 반짝)'),
        ('ribbon_ghost', '260x34', '버튼 위 작은 띠 — 유령(보라 불꽃)'),
        ('ribbon_today', '220x34', '버튼 위 작은 띠 — 오늘의 몸(보라 리본, 카드 띠와 같은 결)'),
    ]),
    ('p2_shop_a', 'pink', ['scene_s02_daily', 'scene_s03_chest'], [
        ('shop_header_plate', '680x90', '상점 머리 판 — 왼쪽 보물상자 아이콘 자리 · 오른쪽 재화 칸 셋 자리(글자 없음)'),
        ('shop_tab', '210x56', '상점 탭 판(보통)'),
        ('shop_tab_sel', '210x56', '상점 탭 판(고름 — 금 테 · 밝게)'),
        ('daily_slot', '200x250', '오늘의 상점 칸 판(보통)'),
        ('daily_slot_free', '200x250', '오늘의 상점 칸 판(무료 — 초록 빛 테)'),
        ('ribbon_new', '90x40', '「NEW」 대각 띠(빨강) — 이 세 글자만 그림에'),
        ('sold_overlay', '200x250', '「구매 완료」 덮개 — 어둡게 덮는 판 + 가운데 빈 띠(글자 없음)'),
        ('chest_slot', '660x150', '상자 칸 판(가로로 긴 판 · 왼쪽 상자 자리 · 오른쪽 값 · 내용 자리)'),
        ('ribbon_open_now', '150x36', '「바로 열림」 띠 판(번개 표시 · 글자 없음)'),
        ('btn_rate', '230x44', '「확률 보기」 작은 버튼 판(글자 없음, 왼쪽 % 아이콘)'),
    ]),
    ('p2_shop_b', 'pink', ['scene_s04_currency'], [
        ('card_slot_small', '330x210', '계약 칸 판(영혼 계약) — 하늘 네온'),
        ('card_slot_big', '330x210', '계약 칸 판(망령 군주의 계약) — 금빛 테'),
        ('starter_slot', '220x170', '입문 패키지 칸 판'),
        ('ribbon_best', '90x40', '「추천」 대각 띠 자리(빨강, 글자 없음)'),
        ('chapter_pack_plate', '680x120', '챕터 돌파 기념 판(가로로 긴)'),
        ('gem_slot', '210x120', '젬 묶음 칸 판'),
        ('ribbon_first2x', '150x30', '「첫 구매 2배」 띠 판(빨강 · 글자 없음)'),
        ('gold_slot', '210x110', '골드 칸 판'),
        ('btn_price', '200x50', '값 버튼 판(노랑 — 지금 도전하기 결)'),
        ('btn_small', '160x40', '작은 버튼 판(표기 · 구매 복원)'),
    ]),
    ('p2_products_gem', 'pink', ['scene_s04_currency'], [
        (f'prod_gem_{i}', '150x110', f'젬 더미 {i}단 — 1단 작은 한 줌 → 6단 산더미(파란 다이아 젬, 빛 굽지 않기)') for i in range(1, 7)
    ]),
    ('p2_products_misc', 'pink', ['scene_s04_currency', 'scene_s07_reward'], [
        ('prod_gold_1', '150x110', '골드 더미 작게'), ('prod_gold_2', '150x110', '골드 더미 중간'), ('prod_gold_3', '150x110', '골드 더미 크게'),
        ('prod_card_small', '180x130', '영혼 계약 그림 — 기준 유령 + 젬 + 달력'),
        ('prod_card_big', '180x130', '망령 군주의 계약 그림 — 왕관 쓴 유령 + 젬 더미'),
        ('prod_starter_1', '150x110', '입문 Ⅰ 묶음 그림(젬 · 소환권 · 정수 불꽃)'),
        ('prod_starter_2', '150x110', '입문 Ⅱ 묶음 그림(젬 · 소환권 · 백금 상자)'),
        ('prod_starter_3', '150x110', '입문 Ⅲ 묶음 그림(젬 더미 · 소환권 다발 · 백금 상자 둘)'),
        ('prod_chapter', '220x120', '챕터 돌파 기념 그림(젬 · 골드 · 소환권 · 상자 쌓임)'),
        ('icon_ticket', '64x64', '소환권 아이콘 — 보라 영혼 불꽃 문양 티켓'),
        ('prod_stamina', '120x110', '행동력 묶음 그림 — 영혼 불꽃 번개 셋'),
    ]),
    ('p2_popups', 'pink', ['scene_s05_confirm', 'scene_s06_noGem', 'scene_s07_reward', 'scene_s21_staminaShort'], [
        ('popup_plate', '560x640', '확인 팝업 판(머리 띠 자리 · 닫기 X 자리 · 남색 · 하늘 네온)'),
        ('popup_plate_wide', '640x700', '획득 팝업 판(조금 큰 · 화려한 테)'),
        ('popup_small', '520x420', '작은 팝업 판(젬 부족 · 행동력 부족)'),
        ('reward_cell', '150x150', '보상 칸 판(아이콘 + 수량 자리)'),
        ('content_row', '440x46', '내용 목록 한 줄 판(아이콘 · 이름 · 수량 자리)'),
        ('btn_close_x', '56x56', '닫기 X 버튼'),
        ('btn_confirm', '360x70', '확인 · 구매 큰 버튼 판(노랑)'),
        ('btn_cancel', '300x56', '닫기 버튼 판(남색)'),
    ]),
    ('p2_summon_bg', 'pink', ['mock_summon_a'], [
        ('summon_bg', '720x820', '소환 제단 배경 — 보라 · 검붉은 밤의 제단 · 촛불 · 사슬 · 해골 장식 · 가운데 아래 마법진 놓일 자리는 비워 둔다(유령 · 마법진 · 글자 없이)'),
        ('summon_circle', '420x140', '마법진(위에서 비스듬히 본 타원) — 보라 · 해골 문양 · 따로 돌릴 것'),
        ('summon_ghost', '220x200', '제단 위 유령(기준 유령 · 양손에 보라 불꽃)'),
    ]),
    ('p2_summon_ui', 'pink', ['mock_summon_a', 'scene_s12_result1', 'scene_s13_result10', 'scene_s14_rates'], [
        ('pickup_plate', '680x220', '픽업 띠 판(왼쪽 글자 자리 · 오른쪽 몸 그림 자리 · 글자 없음)'),
        ('pull_plate', '210x150', '뽑기 버튼 판(보통)'),
        ('pull_plate_10', '210x150', '10회 뽑기 버튼 판(금 테 · 위에 「A급 1회 보장」 띠 자리)'),
        ('pity_bar_bg', '600x30', '천장 막대 바탕'), ('pity_bar_fill', '600x30', '천장 막대 채움(하늘 → 금)'),
        ('card_back', '150x200', '소환 카드 뒷면 — 보라 · 영혼 문양'),
        ('card_front_b', '150x200', '소환 카드 앞 틀 B(하늘) — 속 비움'),
        ('card_front_a', '150x200', '소환 카드 앞 틀 A(보라) — 속 비움'),
        ('card_front_jackpot', '160x210', '소환 카드 앞 틀 대박(금 · 화려) — 속 비움'),
        ('ribbon_pickup', '120x40', '「PICK UP」 대각 띠 — 이 글자만 그림에'),
        ('stamp_new', '80x50', '「NEW」 도장 — 이 글자만 그림에'),
        ('summary_plate', '560x60', '결과 요약 줄 판'),
        ('rate_row', '520x40', '확률 표 한 줄 판'),
    ]),
    ('p2_pass', 'pink', ['scene_s16_pass', 'scene_s17_mission', 'scene_s18_premium'], [
        ('pass_banner_art', '680x260', '시즌 1 띠 그림 — 「복수의 유령」(기준 유령 · 붉은 눈 · 보라 불꽃) + 밤 도시, 오른쪽에 글자 자리 비움(글자 없음)'),
        ('tier_row_free', '300x80', '단계 줄 무료 칸 판'),
        ('tier_row_premium', '300x80', '단계 줄 프리미엄 칸 판(금 · 보라)'),
        ('tier_num_plate', '70x80', '단계 번호 판'),
        ('mark_claimed', '44x44', '받음 체크(초록)'),
        ('btn_claim', '120x44', '받기 버튼 판(노랑 반짝)'),
        ('mission_row', '660x56', '미션 한 줄 판'),
        ('mission_bar_bg', '200x14', '미션 막대 바탕'), ('mission_bar_fill', '200x14', '미션 막대 채움'),
        ('premium_slot', '300x360', '프리미엄 상품 비교 칸 판(금)'),
        ('premium_slot_plus', '300x360', '프리미엄+15단계 칸 판(보라 · 「추천」 띠 자리)'),
    ]),
    ('p2_mission_icons', 'pink', ['scene_s17_mission'], [
        (f'micon_{n}', '56x56', d) for n, d in [
            ('login', '접속(달력)'), ('run', '판 시작(칼)'), ('possess', '빙의(유령 불꽃)'), ('advantage', '유리한 상성(위 화살표 칼)'),
            ('shop', '상점(수레)'), ('upgrade', '강화(망치)'), ('boss', '보스(해골)'), ('chest', '상자'), ('hosts', '서로 다른 몸(얼굴 셋)')]
    ]),
    ('p2_profile', 'pink', ['mock_store_profile'], [
        ('profile_frame_default', '160x160', '프로필 테두리 기본(하늘 네온 · 속 비움)'),
        ('profile_frame_s1', '160x160', '프로필 테두리 GHOST REVENGE(보라 불꽃 무늬 · 속 비움)'),
        ('profile_frame_ch10', '160x160', '프로필 테두리 챕터 10(금 · 월계 · 속 비움)'),
        ('title_ribbon', '260x40', '칭호 띠 판(글자 없음)'),
        ('profile_tab', '200x56', '프로필 창 탭 판'),
        ('profile_cell', '200x220', '프로필 고르는 칸 판'),
    ]),
]

# 소환 연출(검은 바탕 · 가산) — 프레임 띠
FX = [
    ('p2_fx_pillar_cyan', '불꽃 기둥 — 하늘빛(B 예고), 아래에서 솟아 위로 퍼지는 6장(0.9초)'),
    ('p2_fx_pillar_purple', '불꽃 기둥 — 보라(A 예고), 6장'),
    ('p2_fx_pillar_gold', '불꽃 기둥 — 금(대박 예고), 6장 · 가장 굵고 화려하게'),
    ('p2_fx_split', '기둥이 열 갈래 불꽃으로 터져 흩어지는 6장(보라)'),
    ('p2_fx_flip', '카드 뒤집기 — 뒷면이 돌며 빛이 번쩍하는 6장(카드 모양 빛만, 카드 그림은 따로)'),
    ('p2_fx_jackpot', '대박 금 빛살 + 조각 파편이 튀었다 모이는 8장'),
    ('p2_fx_essence', '조각이 보라 정수 불꽃으로 녹아 오르는 6장'),
]

HEAD = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라(사각형 · 원을 코드로 찍은 것은 반려). 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 로비 상점 · 소환 · 패스 · 출전 **부품 시트**. 게임에 그대로 잘라 넣을 부품이다.
[먼저 볼 것]
- 정답 장면(PD 확정 시안): {anchors}
- 부품 목록 · 원칙: Projects/AVSR/_exchange/order_store_parts.md · 장면 구성: Projects/AVSR/_exchange/store_scenes.md
- 지금 로비 부품(같은 결 · 같은 두께로): Assets/BaseResource/ChapterHost/ 의 ch_card_frame.png · ch_start_button.png · ch_pill.png · ch_panel.png, 1차 견본 Projects/AVSR/_exchange/in/parts1_cardkit.png
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로.
'''
PINK = '''[그리는 법] **분홍(#FF00FF) 바탕 한 장**에 아래 부품을 **위에서 아래 · 왼쪽에서 오른쪽 순서로** 줄지어, 부품끼리 40px 이상 띄워 그린다. **글자 · 이름표 · 숫자 없이**(목록에서 「이 글자만 그림에」라고 한 것만 예외). 판 · 틀은 남색 바탕 + 하늘 네온 결이 기본, 빛 번짐은 테두리 옆 2~3px 까지만(분홍 위로 넓게 퍼뜨리지 않는다). 물건 그림에는 빛 · 아우라를 굽지 않는다.
[부품 — 이 순서대로]
'''
BLACK = '''[그리는 법] **검은(#000000) 바탕 한 장**에 프레임을 **왼쪽에서 오른쪽으로 한 줄**(넘치면 두 줄)로, 프레임마다 같은 크기 칸(약 200x320)에 그린다. 빛 · 불꽃 · 반짝임만(가산으로 얹을 것이라 바탕은 순수 검정). 도트 빛 결 — 매끈한 셰이더 번짐 금지. 글자 없음. 박자 · 강약은 Projects/AVSR/_exchange/guide_summon_fx.md.
[그릴 것] '''
TAIL = "\n[저장] Projects/AVSR/_exchange/in/{k}.png\n'@\ncodex exec --sandbox workspace-write --skip-git-repo-check $prompt\n"

manifest = {}
for k, bg, anchors, items in SHEETS:
    an = ' · '.join(A + a + '.png' for a in anchors)
    body = HEAD.replace('{anchors}', an) + PINK
    for i, (nm, sz, d) in enumerate(items, 1):
        body += f'{i}. {d} — 약 {sz}\n'
    body += TAIL.replace('{k}', k)
    codecs.open(EX + 'order_' + k + '.ps1', 'w', 'utf-8-sig').write(body)
    manifest[k] = {'bg': 'pink', 'parts': [n for n, _, _ in items]}
for k, d in FX:
    body = HEAD.replace('{anchors}', A + 'scene_s09_fx1.png · ' + A + 'scene_s10_fx10.png · ' + A + 'scene_s11_fxSpecial.png') + BLACK + d + TAIL.replace('{k}', k)
    codecs.open(EX + 'order_' + k + '.ps1', 'w', 'utf-8-sig').write(body)
    manifest[k] = {'bg': 'black', 'parts': 'frames'}
json.dump(manifest, codecs.open(EX + 'parts2_manifest.json', 'w', 'utf-8'), ensure_ascii=False, indent=1)

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''
keys = list(manifest)
batches = [keys[i:i + 7] for i in range(0, len(keys), 7)]
lines = []
for bi, b in enumerate(batches, 1):
    codecs.open(EX + f'run_parts2_{bi}.ps1', 'w', 'utf-8-sig').write(RUN.replace('KEYS', ','.join("'" + x + "'" for x in b)))
    lines.append(f'powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/run_parts2_{bi}.ps1"')
codecs.open(EX + 'run_parts2_all.ps1', 'w', 'utf-8-sig').write('Set-Location "C:\\won\\UnityProject\\AvengingSprit"\n' + '\n'.join(lines) + '\n')
print(len(keys), 'sheets', [len(b) for b in batches])
