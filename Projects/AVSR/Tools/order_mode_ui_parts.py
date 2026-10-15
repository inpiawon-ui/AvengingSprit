"""추가 모드(망령의 탑 · 망령의 밤) UI 부품 발주 (2026-10-15).

PD: 「통과야 부품 발주 진행해줘」 — 완성 시안 7장 _exchange/in/mock_mode_{L,T1,T5,T8,N1,N3,N7}.png 통과.
앵커: 시안에서 그 부분을 잘라 둔 _exchange/ref/mode_ui_parts/anchor_*.png — 「이 모양 · 색 · 재질 그대로, 글자만 뺀다」.
글자 · 숫자 · 아이콘 · 초상은 그리지 않는다(게임이 TMP · 기존 아이콘으로 얹는다).
크기는 게임 크기(720 기준)의 2배로 받아 자르기에서 반으로 줄인다(2x2 픽셀 = 한 도트).
python order_mode_ui_parts.py [이름 ...] → _exchange/order_modeui_{name}.ps1 · run_modeui_parts.ps1(5개씩 동시)
자르기: cut_mode_ui_parts.py (납품 뒤)
"""
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
A = 'Projects/AVSR/_exchange/ref/mode_ui_parts'
M = 'Projects/AVSR/_exchange/in'

NO_TEXT = '글자 · 숫자 · 아이콘 · 초상 · 보석은 그리지 않는다 — 그 자리는 판 바탕 그대로 비워 둔다(게임이 위에 얹는다).'
NINE = ('9분할로 늘려 쓴다 — 장식(볼트 · 모서리 · 끝 장식)은 네 모서리와 양 끝에만, 가운데 줄과 가운데 칸은 무늬 없이 고르게(늘려도 깨지지 않게). '
        '모서리 장식은 가장자리에서 {c} px 안에 모은다.')

# 이름: (게임 크기 w, h, 장 수, 열 수, 바탕, 설명, 앵커, 정답 시안)
PARTS = {
    'tower_plate': (136, 128, 1, 1, 'mag',
                    '인게임 위 가운데 **탑 판** — 앵커의 「TOWER 13F 남은 적 9」 판에서 글자만 뺀 빈 판(해골 장식 · 양옆 푸른 혼불 · 석조 테두리 그대로). ' + NO_TEXT,
                    'anchor_tower_plate.png', 'mock_mode_T5.png'),
    'night_plate': (144, 106, 1, 1, 'mag',
                    '인게임 위 가운데 **밤 판** — 앵커의 「NIGHT 2:47 처치 132」 판에서 글자만 뺀 빈 판(보라 테두리 · 오른쪽 위 작은 유령 장식 그대로). ' + NO_TEXT,
                    'anchor_night_plate.png', 'mock_mode_N3.png'),
    'mode_chip': (160, 46, 1, 1, 'mag',
                  '조건 · 무리 속성 **알약 칩** — 앵커의 「무리: 마법」 칩에서 보석 · 글자를 뺀 빈 칩. ' + NINE.format(c=20) + ' ' + NO_TEXT,
                  'anchor_mode_chip.png', 'mock_mode_N3.png'),
    'mode_band': (720, 207, 1, 1, 'mag',
                  '화면을 가로지르는 **소개 띠**(층 소개 · 사건 알림) — 앵커 띠에서 글자 · 보석을 뺀 것. 양 끝 해골 · 푸른 혼불 · 사슬 장식 그대로, '
                  '가운데(글자 자리)는 어두운 반투명 판처럼 고르게. 위아래 하늘색 선 그대로. ' + NO_TEXT,
                  'anchor_mode_band.png', 'mock_mode_T5.png'),
    'tower_ladder_cell': (270, 96, 4, 2, 'mag',
                          '탑 입장 패널 왼쪽 **층 사다리 칸 4종**(한 칸에 하나, 왼쪽 위부터) — 앵커의 칸 모양 그대로, 글자 · ✓ · 「도전」 · 보스 초상을 뺀 빈 칸. '
                          '1 지난 층(어둡고 차분한 남색 · 얇은 테두리, 앵커 12층 · 11층) · 2 앞 층(잠긴 느낌의 회남색, 앵커 14층) · '
                          '3 지금 층(금테 · 안쪽 은은한 금빛, 앵커 13층 — 오른쪽 「도전」 표식 자리는 비운다) · 4 보스 층(어두운 판에 붉은 기운 테두리, 왼쪽에 초상 넣을 사각 자리, 앵커 15층). '
                          '칸 사이 계단 · 기둥은 그리지 않는다(바탕 그림에 있다). ' + NO_TEXT,
                          'anchor_tower_ladder.png', 'mock_mode_T1.png'),
    'tower_badge_go': (78, 40, 1, 1, 'mag',
                       '지금 층 칸 오른쪽의 작은 금 **「도전」 표식**에서 글자만 뺀 빈 알약 — 앵커처럼 **입체감**: 짙은 갈색 바깥 테두리 · 밝은 금 윗면에서 아래로 진해지는 명암 · '
                       '위쪽 가는 하이라이트 줄 · 아래쪽 그림자 줄. 평평한 단색 알약 금지(1차 납품이 평평해서 다시 발주). ' + NO_TEXT,
                       'anchor_tower_ladder.png', 'mock_mode_T1.png'),
    'tower_floor_card': (400, 470, 1, 1, 'mag',
                         '탑 입장 패널 오른쪽 **지금 층 카드 틀**(금테 · 모서리 볼트 · 남색 판) — 앵커에서 안의 글자 · 초상 · 구분선 · 보석을 모두 뺀 빈 틀. ' + NINE.format(c=28),
                         'anchor_tower_floor_card.png', 'mock_mode_T1.png'),
    'mode_frame': (340, 120, 1, 1, 'mag',
                   '하늘색 네온 테두리 **기본 칸**(소탕 칸 · 주간 줄 · 입장 줄 · 결과 창 안 칸에 같이 씀) — 앵커 「오늘의 소탕」 칸에서 글자 · 버튼 · 금화를 뺀 빈 칸. ' + NINE.format(c=24),
                   'anchor_mode_frame.png', 'mock_mode_T1.png'),
    'entry_pip': (24, 24, 2, 2, 'mag',
                  '**입장 점** 2종, **둘 다 완전한 동그라미** — 1 켜짐(밝은 하늘색 구슬: 가운데 밝고 가장자리 진한 명암 · 왼쪽 위 작은 하이라이트 · 둘레 은은한 빛), '
                  '2 꺼짐(어두운 남색 구슬: 속이 비어 보이는 짙은 남색 · 가는 회남색 테두리). 앵커 「오늘 입장 ●●○」의 점 그대로. 네모 · 둥근 네모 금지(1차 납품의 꺼짐이 네모라 다시 발주).',
                  'anchor_entry_pips.png', 'mock_mode_T1.png'),
    'night_day_cell': (124, 112, 2, 2, 'mag',
                       '밤 입장 패널 **요일 칸** 2종 — 1 보통(남색 판 · 은빛 볼트 테두리), 2 오늘(금테로 빛나는). 앵커 요일 줄의 칸 그대로, 글자를 뺀 빈 칸. ' + NO_TEXT,
                       'anchor_night_day_strip.png', 'mock_mode_N1.png'),
    'night_today_card': (690, 445, 1, 1, 'mag',
                         '밤 입장 패널 **오늘 카드 틀**(굵은 금테 · 모서리 볼트) — 앵커에서 글자 · 초상 · 안쪽 두 칸 · 오른쪽 위 그림을 모두 뺀 빈 틀. ' + NINE.format(c=34),
                         'anchor_night_today_card.png', 'mock_mode_N1.png'),
    'night_today_art': (340, 190, 1, 1, 'own',
                        '밤 오늘 카드 오른쪽 위에 깔리는 **그림** — 보랏빛 달 · 밤 도시 · 떠도는 푸른 유령 · 몰려오는 적 떼(앵커 카드 오른쪽 위 그림 그대로). '
                        '왼쪽 3분의 1은 남색으로 자연스럽게 사라진다(글자가 얹힌다). 글자 금지. 사각 한 장, 바탕 마젠타 아님.',
                        'anchor_night_today_card.png', 'mock_mode_N1.png'),
    'modeart_tower': (330, 150, 1, 1, 'own',
                      '로비 「망령의 탑」 **모드 칸 그림** — 앵커 그대로: 밤하늘로 솟은 유령 탑(층마다 창에 혼불 · 꼭대기 붉은 눈의 보스 그림자) · 왼쪽 위 달과 십자가 첨탑 · 날아다니는 작은 유령들. '
                      '글자 · 칩 · 빨간 점 · 테두리는 그리지 않는다(칸 틀 · 글자는 게임이 얹는다). 아래 3분의 1은 글자가 얹히므로 어둡고 단순하게. 사각 한 장.',
                      'anchor_modeart_tower.png', 'mock_mode_L.png'),
    'night_result_frame': (662, 876, 1, 1, 'mag',
                           '**밤 결과 창 틀** — 앵커 그대로(보라 · 금 배관 · 금속 프레임 · 위 둥근 유령 원판 · 양옆 보라 불꽃 · 유령 깃발 · 제목 판의 밤 도시 실루엣). '
                           '안의 글자 · 숫자 · 초상 · 버튼 · 안쪽 칸들은 모두 빼고 남색 판으로 비운다(게임이 안쪽 칸 · 버튼을 얹는다). 제목 판은 남기되 글자만 뺀다.',
                           'anchor_night_result.png', 'mock_mode_N7.png'),
    'bighost_plate': (204, 92, 1, 1, 'mag',
                      '밤 인게임 **대형 호스트 머리 위 이름판** — 앵커 그대로: 위 「대형 · 설녀」 이름 칸(어두운 남색 판 · 은빛 테두리) + 아래 막대 두 줄의 **빈 홈**(체력 막대 홈 · 「경직」 막대 홈, 홈만 — 채움 색 · 글자 · % 숫자는 빼고). '
                      '게임이 이름 글자와 막대 채움을 얹는다. ' + NO_TEXT,
                      'anchor_bighost_plate.png', 'mock_mode_N3.png'),
    'tower_panel_bg': (720, 1280, 1, 1, 'own',
                       '탑 입장 패널 **바탕 그림** — 시안 T1 의 탑 안 풍경 그대로(석조 벽 · 왼쪽 위로 오르는 계단과 기둥 · 푸른 혼불 등 · 유령 깃발 · 위쪽 창과 달빛). '
                       '**UI(판 · 칸 · 버튼 · 글자 · 사다리 칸)는 모두 빼고** 바탕만. 가운데 · 오른쪽은 판이 덮이므로 너무 밝지 않게. 화면 꽉 찬 한 장.',
                       'anchor_tower_panel_bg.png', 'mock_mode_T1.png'),
    'night_panel_bg': (720, 1280, 1, 1, 'own',
                       '밤 입장 패널 **바탕 그림** — 시안 N1 의 밤 풍경 그대로(보랏빛 큰 달 · 밤 도시 지붕 · 떠도는 푸른 유령들 · 아래 가로등 · 담 위 고양이 · A-2 상자). '
                       '**UI(판 · 칸 · 버튼 · 글자)는 모두 빼고** 바탕만. 가운데는 판이 덮이므로 너무 밝지 않게. 화면 꽉 찬 한 장(세로 720x1280 비율).',
                       'anchor_night_panel_bg.png', 'mock_mode_N1.png'),
}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: {M}/{mock} — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: {A}/{anchor} — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] {name} — {desc}
[크기] {sheet}
{bg}
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_{name}.txt" $prompt
'''

BG = {'mag': '- 바탕은 순수 마젠타(#FF00FF) — 부품 바깥은 전부 마젠타. 그림에 마젠타 · 분홍을 쓰지 않는다. 부품 가장자리에 마젠타가 섞인 번짐을 만들지 않는다.',
      'own': '- 바탕까지 그림이다(투명 처리 안 함). 가장자리까지 꽉 채운다.'}

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_modeui_$k.ps1' *> 'Projects/AVSR/_exchange/run_modeui_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/modeui_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def sheet_text(w, h, n, cols):
    k = 4 if max(w, h) < 100 else 2   # 작은 부품은 4배로 받아야 명암이 산다(1차 2배는 평평했다)
    W, H = w * k, h * k
    if n == 1:
        return f'{W} x {H} 한 장(게임 크기 {w}x{h} 의 {k}배). 부품이 칸을 꽉 채우되 잘리지 않게.'
    rows = (n + cols - 1) // cols
    return (f'칸 {W} x {H}(게임 크기 {w}x{h} 의 {k}배)가 {cols}열 x {rows}줄, 시트 전체 {W * cols} x {H * rows}. '
            f'칸마다 하나, 왼쪽 위부터 설명 순서. 칸 경계를 넘지 않게, 칸 사이는 마젠타.')


def main(names):
    for name in names:
        w, h, n, cols, bg, desc, anchor, mock = PARTS[name]
        with open(os.path.join(EX, f'order_modeui_{name}.ps1'), 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(M=M, A=A, mock=mock, anchor=anchor, name=name, desc=desc,
                                    sheet=sheet_text(w, h, n, cols), bg=BG[bg]))
    with open(os.path.join(EX, 'run_modeui_parts.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join(f"'{k}'" for k in names)))
    print('ok', names)


if __name__ == '__main__':
    main(sys.argv[1:] or list(PARTS))
