"""보상 연출(창을 닫고 캐릭터에 「무엇을 얻었나」를 보여 주는 장면) 부품 발주 (PD 2026-10-08 반려 뒤 다시 짬).

원칙(memory fx-needs-story-and-purpose): 효과마다 「무엇을 얻었는지」가 그림으로 읽혀야 한다 — 색만 바꾼 빛 금지.
시트: 마젠타 바탕, 칸 크기 · 장 수는 아래 표. 자르기는 cut_fx_present.py.
python order_fx_present.py [이름 ...]
"""
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

# 이름: (칸 w, h, 장 수, 열 수, 설명)
PARTS = {
    'heal_burst': (256, 256, 8, 4, '캐릭터 몸(칸 가운데 아래쪽 2/3, 비워 둔다)을 둘러싸고 회복되는 순간 — 발밑에서 연한 청록 · 초록 빛 고리가 퍼지고, '
                   '몸 둘레에서 작은 초록 「+」 표시 6~10개와 둥근 빛 알갱이가 위로 천천히 떠오르며 사라진다. 몸을 감싸는 은은한 세로 빛 기둥. '
                   '1 고리 생김 → 3~5 「+」 최대 → 8 사라짐. 가운데(몸 자리)는 비워 캐릭터가 보이게'),
    'power_up': (256, 256, 8, 4, '공격력이 오르는 순간 — 몸 둘레에서 붉은 주황 기운이 불꽃처럼 솟고, 위를 가리키는 작은 화살표(꺾쇠) 3~4개가 차례로 올라가 사라진다. '
                 '발밑엔 붉은 납작한 고리. 가운데(몸 자리)는 비움. 1 → 8 시간 순'),
    'speed_up': (256, 256, 8, 4, '빨라지는 순간 — 몸 양옆으로 가는 흰 · 하늘색 바람줄기(속도선) 여러 가닥이 뒤로 스치고, 발밑에 작은 먼지 회오리. '
                 '가운데(몸 자리)는 비움. 1 → 8 시간 순'),
    'vital_up': (256, 256, 8, 4, '체력 상한이 늘어나는 순간 — 몸 뒤에 반투명 초록 하트 모양 빛이 커졌다 몸에 스며들고, 둘레에 작은 하트 알갱이 몇 개가 떠오른다. '
                 '가운데(몸 자리)는 비움. 1 → 8 시간 순'),
    'curse': (256, 256, 8, 4, '악마와 거래한 대가를 받는 순간 — 검붉은 · 보라 연기 덩굴이 발밑에서 올라와 몸을 휘감고, 희미한 쇠사슬 고리가 몸을 한 바퀴 감았다 '
              '부서지며, 마지막에 붉은 낙인 같은 빛이 몸에 스며든다. 가운데(몸 자리)는 비움. 1 → 8 시간 순'),
    'card_absorb': (256, 256, 8, 4, '얻은 카드의 힘이 몸에 깃드는 순간 — 위에서 내려온 흰 · 금빛 빛 알갱이 무리가 몸 가운데로 빨려 들어가고, 둥근 빛 고리가 '
                    '안쪽으로 좁혀지며 사라진다. 마지막 장에 몸 둘레 얇은 빛 테두리만. 흰색 ~ 밝은 회색(게임에서 카드 등급색을 곱한다). 가운데(몸 자리)는 비움'),
    'heal_orb': (96, 96, 4, 4, '제단에서 고스트 몸으로 날아가는 작은 회복 빛 구슬 — 동그란 청록 · 초록 구슬(지름 칸의 35%) + 왼쪽으로 짧은 꼬리. 4장 반복(꼬리 일렁임)'),
    'card_rim_thin': (190, 350, 4, 4, '카드 테두리를 따라 아주 가는(2~3px) 흰 빛줄기 한 토막이 천천히 한 바퀴 도는 것. 테두리 선은 칸 가장자리에서 6px 안쪽. '
                      '카드 안쪽은 완전히 비움(마젠타). 흰색 ~ 밝은 회색(게임에서 등급색을 곱한다). 빛 토막 길이는 둘레의 1/6, 4장이 1/4 바퀴씩 이동'),
}
# 금화 더미 단계 — 결과창 금화 그림 자체가 없다가 늘어나야 한다(PD 「이미지가 늘어나는 연출」)
PILE = ('goldpile_stages', 200, 140, 4, 4, '기준 그림 Assets/BaseResource/ChapterScreens/reward_goldpile.png (200x140 금화 더미) 와 **똑같은 그림체 · 색 · 금화 모양**으로 '
        '더미가 쌓여 가는 4단계. 1 = 바닥에 금화 3~4개, 2 = 작은 더미(기준의 1/3), 3 = 중간 더미(2/3), 4 = 기준 그림과 같은 크기의 가득 찬 더미. '
        '네 장 모두 더미 바닥 위치 · 가로 가운데가 같게(칸 아래 가운데에 놓인다). 반짝이 별 장식은 4단계에만 1~2개')

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 캐릭터 연출 부품 시트. 그림체는 기존 연출 부품 Projects/AVSR/_exchange/out/fxs/levelup/ 와 같은 도트.
[부품] fx_present_{name} — {desc}
[시트] 칸 {w} x {h} 가 {cols}열로 {n}장, 왼쪽 위부터 시간 순(시트 전체 {sw} x {sh}). 각 그림은 칸 안에, 넘치지 않게.
- 바탕은 순수 마젠타(#FF00FF). 마젠타/분홍을 그림에 쓰지 않는다. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/fxp_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_fxp_$k.ps1' *> 'Projects/AVSR/_exchange/log_fxp_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/fxp_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def items():
    for name, (w, h, n, cols, desc) in PARTS.items():
        yield name, w, h, n, cols, desc
    yield PILE


def main(only=None):
    keys = []
    for name, w, h, n, cols, desc in items():
        if only and name not in only:
            continue
        rows = (n + cols - 1) // cols
        with open(os.path.join(EX, 'order_fxp_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(name=name, desc=desc, w=w, h=h, n=n, cols=cols, sw=w * cols, sh=h * rows))
        keys.append(name)
    with open(os.path.join(EX, 'run_fxp.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    main(sys.argv[1:] or None)
