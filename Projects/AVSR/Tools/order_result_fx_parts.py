"""결과창 금화 · 상자 연출 부품 발주 — PD 가 고른 코덱스 자유 시안이 앵커 (스킬 fx-art-pipeline 5단계, 2026-10-09).

정답 시안: Projects/AVSR/_exchange/in/mock_result_codex_peak.png · mock_result_codex_idle.png
앵커 크롭: Projects/AVSR/_exchange/ref/result_mock/anchor_{gold|chest}_{peak|idle}.png
빛은 검은 바탕(게임에서 가산으로 그린다 — 검정은 안 보인다). 2배 도트(2x2 픽셀 = 한 도트)로 그려 받아 게임에서 반으로 줄인다.
뒤 층(번짐 · 빛살)과 앞 층(반짝 별)을 나눠 받는다 — 앞 층이 물건 위에 걸치고 뒤 층은 물건에 가려진다.
자르기: cut_result_fx_parts.py
python order_result_fx_parts.py [이름 ...]
"""
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

CHEST = {'silver': ('은', '선명한 파랑 · 하늘색 빛, 가장 밝은 심은 흰색', 'Assets/BaseResource/ChapterScreens/chest_silver_clean.png'),
         'gold': ('금', '따뜻한 금빛 · 노랑 빛, 가장 밝은 심은 흰 노랑', 'Assets/BaseResource/ChapterScreens/chest_gold_clean.png'),
         'platinum': ('백금', '흰 하늘빛 · 얼음빛, 가장 밝은 심은 흰색', 'Assets/BaseResource/ChapterScreens/chest_platinum_clean.png')}

# 칸 크기(2배) · 물건 자리: 상자 그림은 게임에서 140x96 → 2배 280x192, 칸 480x300 가운데
#                          금화 더미는 150x105 → 2배 300x210, 칸 440x300 가운데
PARTS = {}
for key, (kr, color, clean) in CHEST.items():
    PARTS['chest_aura_%s' % key] = (480, 300, 8, 4, 'anchor_chest_idle',
        f'{kr} 상자 **뒤**에서 머무는 동안 계속 도는 아우라 — 정답 시안처럼 상자 둘레를 감싸는 빛 번짐 + 바깥으로 뻗은 뾰족한 도트 빛살, '
        f'{color}. 빛살이 천천히 돌며 한 가닥씩 길어졌다 짧아지고 번짐이 숨쉰다. 8장이 끊김 없이 반복(8 다음 1). '
        f'칸 가운데 280x192 는 상자 자리(상자 그림 {clean} 을 그 크기로 놓았다고 생각) — 상자에 가려지므로 빛으로 채워도 된다. 은은한 머묾 박자', clean)
    PARTS['chest_burst_%s' % key] = (480, 300, 8, 4, 'anchor_chest_peak',
        f'{kr} 상자가 바닥에 닿는 **순간 터짐**(강) — 정답 시안의 힘주는 순간처럼 상자 뒤에서 빛살이 사방으로 확 펼쳐지고 번짐이 밝게 터진다, {color}. '
        f'1 = 작은 섬광 → 3 = 가장 크고 밝음(빛살이 칸 가장자리 8px 안쪽까지) → 8 = 머묾 아우라 크기로 가라앉음. 가운데 280x192 는 상자 자리', clean)
PARTS['chest_sparkle'] = (480, 300, 8, 4, 'anchor_chest_idle',
    '상자 **앞** 모서리 · 둘레에서 반짝이는 도트 반짝 별 — 정답 시안 머묾 장면의 작은 반짝임. 흰색 ~ 밝은 회색(게임에서 상자 등급색을 살짝 곱한다). '
    '한 장에 2~3개, 장마다 자리가 바뀌며 켜졌다 꺼진다(한 별의 수명 3~4장). 가운데 280x192 상자 자리 **안쪽에는 그리지 않는다** — 모서리 · 가장자리 바깥에만. '
    '빛 번짐 · 빛살 금지(뒤 층이 따로 있다)', 'Assets/BaseResource/ChapterScreens/chest_silver_clean.png')
PARTS['gold_glow'] = (440, 300, 8, 4, 'anchor_gold_idle',
    '금화 더미 **뒤**에서 머무는 동안 계속 숨쉬는 따뜻한 금빛 — 정답 시안처럼 더미 둘레로 번지는 금빛 · 주황 번짐과 짧은 금빛 불티 몇 알이 천천히 떠오름. '
    '8장 끊김 없이 반복. 칸 가운데 300x210 은 금화 더미 자리(더미에 가려진다). 은은한 머묾 박자', 'Assets/BaseResource/PopupFx/fx_present_goldpile_stages_4.png')
PARTS['gold_burst'] = (440, 300, 8, 4, 'anchor_gold_peak',
    '금화 더미가 다 쌓인 **순간 터짐**(강) — 정답 시안의 힘주는 순간처럼 더미 뒤에서 금빛 빛살이 위로 부채꼴로 확 펼쳐지고 번짐이 밝게 터진다. '
    '1 = 작은 섬광 → 3 = 가장 크고 밝음 → 8 = 머묾 금빛 크기로 가라앉음. 가운데 300x210 은 금화 더미 자리', 'Assets/BaseResource/PopupFx/fx_present_goldpile_stages_4.png')
PARTS['gold_sparkle'] = (440, 300, 8, 4, 'anchor_gold_idle',
    '금화 더미 **앞** 둘레 · 위쪽에서 반짝이는 금빛 도트 반짝 별(4갈래 작은 별 + 점) — 정답 시안 머묾 장면처럼. 한 장에 2~3개, 장마다 자리가 바뀌며 켜졌다 꺼진다. '
    '더미 몸 한가운데보다는 둘레 · 위쪽 가장자리에. 빛 번짐 금지(뒤 층이 따로 있다)', 'Assets/BaseResource/PopupFx/fx_present_goldpile_stages_4.png')

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(도트 모양 + 셰이더 광원 · 파티클을 섞는 게임 — 이 부품은 그중 도트 모양 층).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 연출 부품. **PD 가 고른 정답 시안**: Projects/AVSR/_exchange/in/mock_result_codex_peak.png · mock_result_codex_idle.png
[앵커 크롭 — 이 화풍 · 색 · 빛살 모양 그대로] Projects/AVSR/_exchange/ref/result_mock/{anchor}.png
[물건 그림(자리 · 크기 기준)] {clean}
[부품] fx_result_{name} — {desc}
[시트] 칸 {w} x {h} 가 {cols}열로 {n}장, 왼쪽 위부터 시간 순(시트 전체 {sw} x {sh}). 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 물건(상자 · 금화) 그림은 그리지 않는다, 빛만.
- 빛 덩이 바깥에 어두운 색 판(남색 · 갈색 배경 덩어리)을 깔지 않는다 — 빛 가장자리는 검정으로 바로 사그라든다(샘플 rcfx_chest_aura_silver 에서 어두운 판이 깔려 왔다).
- 글자 · 숫자 금지. 빛이 칸 밖으로 잘리지 않게.
[저장] Projects/AVSR/_exchange/in/rcfx_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_rcfx_$k.ps1' *> 'Projects/AVSR/_exchange/log_rcfx_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/rcfx_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main(only=None):
    keys = []
    for name, (w, h, n, cols, anchor, desc, clean) in PARTS.items():
        if only and name not in only:
            continue
        rows = (n + cols - 1) // cols
        with open(os.path.join(EX, 'order_rcfx_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(name=name, desc=desc, anchor=anchor, clean=clean, w=w, h=h, n=n, cols=cols,
                                    sw=w * cols, sh=h * rows))
        keys.append(name)
    with open(os.path.join(EX, 'run_rcfx.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    main(sys.argv[1:] or None)
