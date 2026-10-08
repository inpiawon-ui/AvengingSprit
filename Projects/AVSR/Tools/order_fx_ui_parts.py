"""팝업 · 방 오브젝트 · 클리어 연출용 공통 이펙트 부품 발주 (2026-10-08).

목록은 상의 `_exchange/consult_popup_fx_parts.md` 2절 — 8장 프레임 애니 8종 + 빔 1장.
색 곱하기용은 백색~중성 회색 + 알파로 받는다(게임에서 금 · 청록 · 진홍 · 호박을 곱한다).
시트: 8장짜리는 1024 x 512 마젠타(칸 256 x 256, 4열 x 2줄, 시간 순), 빔은 1024 x 256 한 줄.
자르기는 cut_skill_parts.py 와 같은 key_magenta(빛 부품은 unmix).
python order_fx_ui_parts.py [이름 ...]
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

PARTS = {
    'ring_pulse': ('흰색 에너지 고리가 가운데서 생겨 바깥으로 퍼지며 옅어진다(정원, 칸의 20% → 90%, 두께 얇게 → 더 얇게). 십자 광채 금지', 'mul'),
    'energy_arc': ('짧은 흰 전기 줄기(원혼 기운) 몇 가닥이 꿈틀대며 번쩍였다 사라진다 — 가운데 밝은 심, 끝은 끊어진 도트. 1 생김 → 3~4 최대 → 8 사라짐', 'mul'),
    'burst_rays': ('가운데서 굵고 짧은 불규칙 빛줄기 8~10개가 터져 나와 퍼지며 사라진다 — 별 모양 중심 · 십자 끝 금지, 줄기 길이 제각각', 'mul'),
    'soul_stream': ('오른쪽으로 날아가는 원혼 덩어리 — 둥근 머리 + 짧은 유선형 꼬리(왼쪽), 8장이 꼬리가 일렁이는 반복', 'mul'),
    'smoke_wisp': ('낮은 회색 연기 한 줄기가 피어올라 위로 흩어진다(아래 → 위), 가장자리 부드럽게', 'mul'),
    'chain_snap': ('검은 쇠사슬 한 토막이 붉게 달아올랐다가 가운데서 툭 끊어지며 양쪽으로 튀어 사라진다(쇠 회색 + 붉은 열광, 색 그대로)', 'own'),
    'coin_orbit': ('금화 도트 8~10개가 납작한 타원(위에서 본 고리)을 따라 한 바퀴 돌며 퍼졌다 사라진다(금색 그대로)', 'own'),
    # PD 2026-10-08 「팝업이 닫힐 때 캐릭터에 능력치가 적용된 듯한 이펙트 — 이벤트마다 색이 다르게」
    'powerup': ('캐릭터 몸에 힘이 깃드는 순간 — 발밑에서 납작한 빛 고리가 퍼지고, 몸 둘레를 따라 위로 솟는 빛줄기 · 작은 둥근 빛 알갱이가 올라가 머리 위에서 사라진다. '
                '가운데(몸 자리)는 비워 캐릭터가 가려지지 않게. 1 고리 생김 → 3~4 빛줄기 최대 → 8 사라짐. 별 · 십자 금지', 'mul'),
    'reward_pop': ('물건이 바닥에 쿵 떨어진 순간 — 낮게 퍼지는 먼지 고리 + 짧은 파편 몇 개가 튀고 사라진다(흰~회색)', 'mul'),
}
BEAM = ('beam_soft', '가로로 긴 부드러운 흰 빛 띠(가운데 밝고 위아래 · 양끝으로 알파가 빠짐), 시트 가운데 한 줄 가득')

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 연출용 이펙트 부품 시트 — 게임과 같은 도트(픽셀 아트). 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_*_v1.png 의 빛 · 원혼 표현과 같은 그림체.
[부품] fx_ui_{name} — {desc}
[색] {color}
{sheet}
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍 색을 그림에 쓰지 않는다. 별 · 표창 · 십자 반짝이 모양 금지. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxui_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

SHEET8 = '시트: 1024 x 512, 칸 256 x 256 이 4열 x 2줄 — 왼쪽 위부터 1 → 8 시간 순. 그림은 칸 가운데, 칸 밖으로 넘치지 않게.'
SHEET_BEAM = '시트: 1024 x 256 한 장 — 빔 한 줄이 가로 가득(양끝 알파 페이드).'
COLOR = {'mul': '흰색 ~ 밝은 중성 회색만(게임에서 색을 곱한다). 가운데가 하얗게 날아가지 않게 명암 단계를 남긴다.',
         'own': '설명의 색 그대로.'}

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_fxui_$k.ps1' *> 'Projects/AVSR/_exchange/log_fxui_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/fxui_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main(only=None):
    keys = []
    items = [(n, d, c, SHEET8) for n, (d, c) in PARTS.items()] + [(BEAM[0], BEAM[1], 'mul', SHEET_BEAM)]
    for name, desc, color, sheet in items:
        if only and name not in only:
            continue
        with open(os.path.join(EX, 'order_fxui_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(name=name, desc=desc, color=COLOR[color], sheet=sheet))
        keys.append(name)
    with open(os.path.join(EX, 'run_fxui.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    import sys
    main(sys.argv[1:] or None)
