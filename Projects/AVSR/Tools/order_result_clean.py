"""결과창 금화 더미 · 상자 그림에서 빛을 떼고, 빛은 움직이는 연출 부품으로 따로 받는다 (PD 2026-10-08).

PD: 「골드 마지막 이미지 뒤 이펙트는 이미지가 아니라 연출로 반짝이는 효과를 넣어야 한다 ·
      상자 뒤 파란 아우라도 원래 이미지가 아니라 이펙트로 표현해야 한다」
→ 그림(빛 없는 금화 더미 · 상자 3종)과 연출 부품(반짝임 · 아우라)을 나눠 받는다. 자르기는 cut_result_clean.py.
python order_result_clean.py [이름 ...]
"""
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

CLEAN_RULE = ('기준 그림과 **완전히 같은 그림**(모양 · 색 · 도트 · 크기 · 자리)에서 둘레의 빛 · 아우라 · 반짝이 별만 뺀다. '
              '빛이 그림 가장자리에 번져 밝아진 테두리 픽셀도 원래 재질 색으로 되돌린다. 새로 그리지 말고 기준을 그대로 옮긴다.')

# 이름: (w, h, 장 수, 열 수, 설명, 기준 그림)
PARTS = {
    'goldpile_clean': (200, 140, 1, 1, '결과창 「골드 획득」 금화 더미 — ' + CLEAN_RULE + ' 뒤의 주황 빛 번짐 · 별 반짝이 · 흩날리는 불티를 뺀 금화만.',
                       'Assets/BaseResource/ChapterScreens/reward_goldpile.png'),
    'chest_silver_clean': (206, 142, 1, 1, '결과창 은 상자 — ' + CLEAN_RULE + ' 둘레의 파란 아우라 · 빛살을 뺀 상자만.',
                           'Assets/BaseResource/LobbyMainUI/chest_silver.png'),
    'chest_gold_clean': (206, 142, 1, 1, '결과창 금 상자 — ' + CLEAN_RULE + ' 둘레의 금빛 아우라 · 빛살을 뺀 상자만.',
                         'Assets/BaseResource/LobbyMainUI/chest_gold.png'),
    'chest_platinum_clean': (206, 142, 1, 1, '결과창 백금 상자 — ' + CLEAN_RULE + ' 둘레의 하늘색 아우라 · 빛살을 뺀 상자만.',
                             'Assets/BaseResource/ChapterScreens/chest_platinum.png'),
    'gold_twinkle': (64, 64, 6, 6, '금화 더미 위에서 반짝 빛났다 사라지는 별 하나 — 가는 십자(4갈래) 빛 + 가운데 작은 흰 점. '
                     '1 아주 작은 점 → 3 가장 크고 밝음(갈래 끝이 칸 가장자리 4px 안쪽) → 6 사라짐. 흰색 ~ 연한 금색. '
                     '게임에서 금화 더미 둘레 여러 자리에 시간차를 두고 띄운다',
                     'Assets/BaseResource/ParticleFx/star4.png'),
    'chest_aura': (206, 142, 8, 4, '기준 그림(상자)에 붙어 있는 **아우라만** 떼어 움직이게 — 상자 둘레를 감싸는 빛 번짐과 바깥으로 뻗은 짧은 빛살을 '
                   '기준 그림과 **같은 자리 · 같은 크기**로 그리되 상자 몸은 그리지 않는다(상자 자리는 비우지 말고 빛으로 채워도 된다 — 게임에서 상자가 위에 덮인다). '
                   '8장이 숨 쉬듯 반복: 빛 번짐이 조금 커졌다 작아지고, 빛살이 한 가닥씩 길어졌다 짧아지며 천천히 돈다(8 다음 1로 끊김 없이). '
                   '흰색 ~ 밝은 회색만(게임에서 상자 등급색을 곱한다). 칸 가장자리 4px 안에서 끝난다 — 잘린 빛 금지',
                   'Assets/BaseResource/LobbyMainUI/chest_silver.png'),
    # 결과창 빛 알갱이 · 점멸용 도트 입자(파티클 텍스처) — 규칙 08_fx.md 0절 「파티클도 입자는 도트 질감」(PD 10-08)
    'pixel_mote': (32, 32, 4, 4, '파티클로 띄울 아주 작은 **도트 빛 알갱이** 4종(한 칸에 하나, 칸 가운데). 2x2 픽셀을 한 도트로 그린다(실제 16x16 도트 그림을 2배로 키운 모양). '
                   '1 = 지름 5도트 둥근 점(가운데 1도트 가장 밝고 바깥 1도트 테두리는 옅게), 2 = 지름 3도트 작은 둥근 점, '
                   '3 = 5도트 마름모(가운데 밝음), 4 = 7도트 마름모 테두리가 옅은 것. 흰색 ~ 밝은 회색만(게임에서 색을 곱한다). '
                   '**십자별 · 4갈래 별 · 표창 모양 금지** — 점과 마름모만. 번짐 · 안티앨리어싱 금지, 딱 떨어지는 도트',
                   'Assets/BaseResource/ParticleFx/ghostmote.png'),
}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 부품. 기준 그림: {ref} (이 파일을 열어 그림체 · 색 · 크기를 맞춘다).
[부품] {name} — {desc}
[시트] 칸 {w} x {h} 가 {cols}열로 {n}장, 왼쪽 위부터 시간 순(시트 전체 {sw} x {sh}). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/rc_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$max = 6
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_rc_$k.ps1' *> 'Projects/AVSR/_exchange/log_rc_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/rc_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main(only=None):
    keys = []
    for name, (w, h, n, cols, desc, ref) in PARTS.items():
        if only and name not in only:
            continue
        rows = (n + cols - 1) // cols
        with open(os.path.join(EX, 'order_rc_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(name=name, desc=desc, ref=ref, w=w, h=h, n=n, cols=cols, sw=w * cols, sh=h * rows))
        keys.append(name)
    with open(os.path.join(EX, 'run_rc.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    main(sys.argv[1:] or None)
