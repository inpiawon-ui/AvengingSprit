# -*- coding: utf-8 -*-
"""소환 「영혼 배양실」 깨짐 부품 3차 — PD 2026-10-22 「깨지는 이미지는?」.
2차 조각은 불투명 단색 하늘색이라(발주문에 「밝은 하늘 · 흰 유리, 물방울 그리지 마라」고 적었다) 시안과 달랐다.
시안은 속이 비치는 유리(가장자리만 흰 청록으로 빛남) + 배양액 물방울 · 물보라.
둘 다 검은 바탕으로 받아 가산으로 겹친다 — 뒤의 금빛이 유리 속으로 비친다.
납품: Projects/AVSR/_exchange/in/lab_glass_shards2 · lab_splash .png
"""
import io
import os

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

HEAD = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 배양조가 깨지는 순간의 **파티클 부품**(게임이 한 칸씩 잘라 날린다).
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md.
**정답 그림(이 모양 · 질감 · 화풍 그대로)**: Projects/AVSR/_exchange/ref/summon_lab/anchor_shards_splash.png
  — PD 가 고른 시안의 유리 조각 · 물방울 부분을 확대한 것. 전체 화면: Projects/AVSR/_exchange/in/mock_summon_lab_free_peak.png
⚠ PD: 「깨지는 이미지는?」 — 지금 조각은 속이 꽉 찬 불투명 하늘색 덩어리라 시안과 다르다. 대충 그리지 마라.

[부품] {what}
{spec}
- **검은 바탕(#000000)** — 게임이 가산(더하기)으로 겹친다. 검은 곳 = 투명, 밝을수록 빛난다.
- 칸 가운데 하나씩 · 칸끼리 닿지 않게 · 캐릭터 · 글자 · 빛줄기는 그리지 마라.
※ 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

ORDERS = [
    ('lab_glass_shards2', '속이 비치는 유리 조각 8개',
     '- 가로 4칸 × 세로 2칸, 정사각 칸.\n'
     '- 정답 그림의 유리 조각처럼: **가장자리 테두리 · 깨진 면의 모서리 선만 밝은 흰 청록(#dffcff ~ #7fe9f0)으로 또렷하게**,\n'
     '  **속은 아주 어둡게 비치는 청록(검은 바탕이 거의 보일 만큼)** + 면 하나에 옅은 반사 줄 한두 개. 속을 밝은 하늘색으로 꽉 채우지 마라.\n'
     '- 크기 · 모양을 섞는다: 큰 판 2개(넓은 오각 · 사각 판) · 중간 3개(긴 삼각 · 쐐기) · 작은 부스러기 3개(칸의 1/4 정도).\n'
     '- 도트 외곽선(1~2 px), 번짐 · 광채 후광 금지(가산이라 게임에서 빛난다).'),
    ('lab_splash', '배양액 물방울 · 물보라 6개',
     '- 가로 3칸 × 세로 2칸, 정사각 칸.\n'
     '- 정답 그림의 물 부분처럼 **청록 배양액**: 가장자리가 밝은 흰 청록, 속은 어둡게 비치는 청록(유리 조각과 같은 질감).\n'
     '- 칸마다: ① 길게 튀는 물줄기(끝이 방울로 맺힘) ② 휘어진 물보라 호 ③ 물방울 3~5개 무리 ④ 큰 물방울 하나 ⑤ 작은 물방울 2개 ⑥ 바닥에서 튀어 오르는 물보라 왕관.\n'
     '- 도트 외곽선, 번짐 · 후광 금지.'),
]


def main():
    keys = []
    for name, what, spec in ORDERS:
        io.open(os.path.join(EX, f'order_{name}.ps1'), 'w', encoding='utf-8-sig').write(HEAD.format(what=what, spec=spec, name=name))
        keys.append(name)
    run = ['Set-Location "C:\\won\\UnityProject\\AvengingSprit"',
           'foreach ($k in @(' + ','.join(f'"{k}"' for k in keys) + ')) {',
           '    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "skip $k"; continue }',
           '    for ($try = 0; $try -lt 4; $try++) {',
           '        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"',
           '        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }',
           '        Start-Sleep 300',
           '    }',
           '    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k" }',
           '}']
    io.open(os.path.join(EX, 'run_lab_parts3.ps1'), 'w', encoding='utf-8-sig').write('\n'.join(run) + '\n')
    print('orders', keys)


if __name__ == '__main__':
    main()
