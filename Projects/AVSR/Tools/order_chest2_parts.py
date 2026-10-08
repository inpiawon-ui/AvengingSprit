"""결과창 상자 빛 부품 — 새 시안(mock_chest2_free_*) 앵커 (스킬 fx-art-pipeline 5단계, PD 2026-10-09 「진행」).

요소 · 무엇으로(PD 에게 보인 표):
  상자를 감싸는 광원(머묾)          셰이더(ResultLightFx 의 빛 셰이더 번짐) — 그림 없음
  착지 순간 바닥에서 솟는 빛살      그림 2장(번갈아 깜빡) + 곡선으로 커졌다 사라짐  ← 여기서 받는다
  반짝 별 · 도트 알갱이             파티클 — 입자 그림 2종                          ← 여기서 받는다
  등급색                            흰 그림 + 색 곱하기
자르기: cut_chest2_parts.py
python order_chest2_parts.py [이름 ...]
"""
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

# 이름: (칸 w, h, 설명) — 2배 도트로 그려 받는다
PARTS = {
    'chest_rise_a': (480, 300, '상자가 바닥에 닿는 순간 **바닥에서 위로 솟구치는 빛살** — 정답 시안 mock_chest2_free_peak.png 의 상자 아래 · 둘레에서 솟는 뾰족한 빛줄기 묶음 그대로. '
                     '칸 아래 가운데(바닥 줄 y 270)에서 위 · 양옆으로 부채처럼 뻗는다, 가운데가 가장 길다. 칸 가운데 아래 280x192 는 상자 자리(상자에 일부 가려진다). '
                     '빛줄기마다 가운데 흰 심 + 바깥으로 부드럽게 퍼지는 광원(딱딱한 윤곽선 금지). 흰색 ~ 밝은 회색(게임에서 등급색을 곱한다)'),
    'chest_rise_b': (480, 300, '위 chest_rise_a 와 **같은 빛살 묶음의 다른 순간** — 빛줄기 길이 · 갈래가 조금 다르다(두 장을 번갈아 깜빡여 일렁이게). 나머지 조건은 chest_rise_a 와 같다'),
    'chest_halo': (480, 300, '상자가 놓인 뒤 **머무는 동안 상자 모양을 따라 감싸는 부드러운 광원(후광)** — 정답 시안 mock_chest2_free_idle.png 의 상자 둘레 파란 빛 그대로. '
                   '칸 가운데에 상자(280x192, 물건 그림 chest_silver_clean.png 모양 그대로)를 놓았다고 치고, 그 **실루엣 바깥으로 6~24px 부드럽게 번지는 빛**. '
                   '상자 자리 안쪽은 가장 밝게 채워도 된다(상자에 가려진다). 가장자리로 갈수록 부드럽게 사그라든다 — **딱딱한 윤곽선 · 테두리 선 금지**. '
                   '위쪽 · 양옆에 짧은 빛 번짐 결 몇 가닥. 흰색 ~ 밝은 회색(게임에서 등급색을 곱한다)'),
    'mote_star': (64, 64, '파티클로 뿌릴 **작은 반짝 별 하나** — 정답 시안의 상자 둘레 흰 십자 반짝임. 칸 가운데, 4갈래 가는 빛 + 가운데 흰 점 + 둘레 부드러운 광원. '
                  '흰색(게임에서 색을 곱한다). 갈래 끝이 칸 가장자리 6px 안에서 끝난다'),
    'mote_dot': (32, 32, '파티클로 뿌릴 **작은 도트 빛 알갱이 하나** — 정답 시안의 상자 위로 떠 있는 작은 네모 빛 점. 칸 가운데 네모 도트(2배로 8x8 정도) + 둘레 아주 옅은 광원. 흰색'),
}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(도트 모양 + 셰이더 광원 · 파티클을 섞는 게임 — 이 그림은 그중 도트 모양 · 입자 층).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 상자 연출 부품. **PD 가 고른 정답 시안**: Projects/AVSR/_exchange/in/mock_chest2_free_peak.png · mock_chest2_free_idle.png
[물건 그림(자리 · 크기 기준)] Assets/BaseResource/ChapterScreens/chest_silver_clean.png
[부품] fx_result_{name} — {desc}
[크기] {w} x {h} 한 장. 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 상자 그림은 그리지 않는다, 빛만.
- 빛 가장자리는 검정으로 바로 사그라든다 — 바깥에 어두운 색 판 · 윤곽선 금지. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/c2_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_c2_$k.ps1' *> 'Projects/AVSR/_exchange/log_c2_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/c2_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main(only=None):
    keys = []
    for name, (w, h, desc) in PARTS.items():
        if only and name not in only:
            continue
        with open(os.path.join(EX, 'order_c2_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(name=name, desc=desc, w=w, h=h))
        keys.append(name)
    with open(os.path.join(EX, 'run_c2.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    main(sys.argv[1:] or None)
