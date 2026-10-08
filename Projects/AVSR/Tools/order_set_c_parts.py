"""C 「원혼 회로」 한 벌 — 게임에 넣을 부품 발주문 (2026-10-08).

PD 가 시안 C(in/mock_set_C_{room|levelup|altar|devil|shop}.png)를 골랐다. 아이콘(카드 그림 · 상점 물건)도 좋다고 했다 — 아이콘은 2단계.
같은 오브젝트가 방 화면과 팝업 위에서 다르게 그려져 있어 **방 화면 쪽**으로 통일한다(Claude 판단 — 실루엣이 더 뚜렷).
부품마다 한 장씩: 앵커(시안 크롭 in/anchor_c/{이름}.png)를 그대로 따라, 마젠타 바탕 · 글자 없이 · 크게 그린다.
자르기 · 크기 맞춤은 Tools/fit_set_c_parts.py.
python order_set_c_parts.py → _exchange/order_setc_{이름}.ps1 · run_setc_parts.ps1
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

# 이름: (앵커 · 그 시안 화면 · 게임 표시 크기 w x h · 설명)
PARTS = {
    # ── 방 오브젝트 ────────────────────────────────────────
    'obj_heal_shrine': ('obj_heal_shrine', 'room', (300, 380),
                        '회복 오브젝트 — 고리 코일 · 배관 캡슐 안에 청록 유령(혼백)이 갇혀 빛나고, 세 갈래 금속 다리가 받친다. 바닥 링 · 머리 위 표식은 빼고 몸체만'),
    'obj_devil_altar': ('obj_devil_altar', 'room', (300, 380),
                        '악마 오브젝트 — 굵은 뿔 두 개가 달린 사슬 봉인 캡슐, 안에 붉은 해골 원혼. 바닥 링 · 머리 위 표식은 빼고 몸체만'),
    'obj_shop_stall': ('obj_shop_stall', 'room', (300, 380),
                       '상점 오브젝트 — 모자 · 코트 차림 노란 눈 해골 유령 상인이 등에 반원형 진열 프레임(호박빛 등불 · 상품 캡슐)을 멘 모습. 바닥 링 · 머리 위 표식은 빼고 몸체만'),
    'obj_ring_heal': ('obj_ring_heal', 'room', (240, 90), '회복 오브젝트 바닥의 얇은 청록 이중 링(위에서 본 납작한 타원) — 링만, 오브젝트는 그리지 않는다'),
    'obj_ring_devil': ('obj_ring_devil', 'room', (240, 90), '악마 오브젝트 바닥의 얇은 진홍 이중 링(납작한 타원) — 링만'),
    'obj_ring_shop': ('obj_ring_shop', 'room', (240, 90), '상점 오브젝트 바닥의 얇은 호박 이중 링(납작한 타원) — 링만'),
    'obj_mark_heal': ('obj_mark_heal', 'room', (72, 72), '회복 머리 위 표식 — 청록 물방울 문장(선 아이콘, 받침 고리)'),
    'obj_mark_devil': ('obj_mark_devil', 'room', (72, 72), '악마 머리 위 표식 — 진홍 뿔 · 사슬 문장(선 아이콘)'),
    'obj_mark_shop': ('obj_mark_shop', 'room', (72, 72), '상점 머리 위 표식 — 호박 등불 · 저울 문장(선 아이콘)'),
    # ── 레벨업 ────────────────────────────────────────────
    'levelupframe': ('levelupframe', 'levelup', (690, 790),
                     '레벨업 창 전체 틀 — 금빛 원혼 관로 · 흑철 기계 틀 · 위 유령 문장 · 좌우 유리관. 제목 판(「LEVEL UP!」 자리)과 안내 줄 자리는 **글자 없이 빈 판**, 카드 3장 자리는 비워 어두운 바탕만'),
    'cardpanel_common': ('levelup_card', 'levelup', (190, 350), '카드 틀(일반) — 흑철 테 · 회색 강철 빛. 위 등급 칩 자리 · 그림 칸 · 이름 칸 · 설명 칸은 **비운다**(그림 · 글자 없이)'),
    'cardpanel_rare': ('levelup_card', 'levelup', (190, 350), '카드 틀(레어) — 같은 모양, 테두리 원혼 빛이 청색'),
    'cardpanel_epic': ('levelup_card', 'levelup', (190, 350), '카드 틀(에픽) — 같은 모양, 테두리 원혼 빛이 보라'),
    'cardpanel_legendary': ('levelup_card', 'levelup', (190, 350), '카드 틀(전설) — 같은 모양, 테두리 원혼 빛이 금'),
    'cardpanel_evolution': ('levelup_card', 'levelup', (190, 350), '카드 틀(진화) — 같은 모양, 테두리 원혼 빛이 진홍 + 금 이중'),
    'cardchip_rarity': ('levelup_chip', 'levelup', (150, 34), '카드 위 등급 칩 — 남흑색 판 + 흑철 테, **글자 없이**'),
    'cardchip_level': ('levelup_chip', 'levelup', (150, 34), '카드 위 레벨 칩 — 같은 모양, 테가 금빛, **글자 없이**'),
    # ── 회복 제단 ─────────────────────────────────────────
    'shrineframe': ('shrineframe', 'altar', (590, 530),
                    '회복의 제단 창 틀 — 흑철 · 청록 액체가 흐르는 좌우 유리관 · 배관. 제목 판은 **글자 없이 빈 판**, 위 물방울 문장 포함, 선택 칸 3줄 자리는 비운다'),
    'shrinehintpill': ('shrinehintpill', 'altar', (280, 34), '안내 알약 판 — 남흑색 + 흑철 테 + 양끝 화살 장식, **글자 없이**'),
    'shrinechoiceslot': ('shrinechoiceslot', 'altar', (440, 94), '선택 칸 — 흑철 테 · 왼쪽 아이콘 자리(빈 네모) · 오른쪽 청록 물방울 표식. 아이콘 · 글자는 **그리지 않는다**'),
    # ── 악마의 거래 ───────────────────────────────────────
    'eventframe': ('eventframe', 'devil', (660, 595),
                   '악마의 거래 창 틀 — 굵은 강철 뿔 두 개 · 사슬 · 붉은 배관 · 아래 찢어진 진홍 천. 제목 판은 **글자 없이 빈 판**, 위 뿔 문장 포함, 본문 · 버튼 자리는 비운다'),
    'eventrewardpill': ('eventrewardpill', 'devil', (246, 52), '보상 알약 판 — 금 테 · 남흑색 바탕, **글자 없이**'),
    'eventcostpill': ('eventcostpill', 'devil', (190, 52), '대가 알약 판 — 진홍 테 · 검붉은 바탕, **글자 없이**'),
    'eventacceptbutton': ('eventacceptbutton', 'devil', (460, 98), '수락 버튼 — 진홍 원혼 빛 테 · 갈라진 검붉은 판, **글자 없이**'),
    'eventdeclinebutton': ('eventdeclinebutton', 'devil', (446, 66), '거절 버튼 — 흑철 테 · 어두운 판(수락보다 수수하게), **글자 없이**'),
    # ── 상점 ──────────────────────────────────────────────
    'shopframe': ('shopframe', 'shop', (690, 545),
                  '상점 창 틀 — 흑철 · 호박빛 배관 · 좌우 유리관 안 노란 유령들. 제목 판은 **글자 없이 빈 판**, 위 저울 문장 포함, 상품 칸 · 버튼 자리는 비운다'),
    'shopgoldpill': ('shopgoldpill', 'shop', (256, 34), '소지 금 알약 판 — 호박 테 · 남흑색, **글자 없이**'),
    'shopitemcell': ('shopitemcell', 'shop', (272, 97), '상품 칸 — 흑철 테 · 남흑색 판 · 왼쪽 아이콘 자리(빈 네모) · 오른쪽 위 가격 판(빈). 아이콘 · 글자는 **그리지 않는다**'),
    'shopitemcell_off': ('shopitemcell', 'shop', (272, 97), '상품 칸(못 사는 상태) — 같은 모양, 회색으로 죽은 빛 · 어둡게, 아이콘 · 글자 없이'),
    'shopleavebutton': ('shopleavebutton', 'shop', (276, 64), '나가기 버튼 — 호박 테 · 어두운 판, **글자 없이**'),
}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 인게임 UI 부품 — PD 가 통과시킨 시안 C 「원혼 회로」 Projects/AVSR/_exchange/in/mock_set_C_{screen}.png 와 **똑같은 모양 · 색 · 도트 그림체**로.
앵커(시안에서 이 부품만 잘라 둔 것): Projects/AVSR/_exchange/in/anchor_c/{anchor}.png — 이걸 그대로 따라 깨끗하게 다시 그린다.
[부품] {name} — {desc}
[크기] 게임에서 {w} x {h} px 로 쓴다. 그 비율 그대로 캔버스에 크게(가로세로 비율 유지, 캔버스의 90% 정도 차게) 한 장.
- 바탕은 순수 마젠타(#FF00FF) — 부품 바깥은 전부 마젠타. 그림자 · 마젠타/분홍 색을 부품에 쓰지 않는다.
- 글자 · 숫자를 넣지 않는다(게임이 위에 글자를 얹는다). 별 · 표창 · 십자 반짝이 금지.
[저장] Projects/AVSR/_exchange/in/setc_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_setc_$k.ps1' *> 'Projects/AVSR/_exchange/log_setc_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/setc_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main(only=None):
    keys = []
    for name, (anchor, screen, (w, h), desc) in PARTS.items():
        if only and name not in only:
            continue
        body = TEMPLATE.format(screen=screen, anchor=anchor, name=name, desc=desc, w=w, h=h)
        with open(os.path.join(EX, 'order_setc_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(body)
        keys.append(name)
    with open(os.path.join(EX, 'run_setc_parts.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    import sys
    main(sys.argv[1:] or None)
