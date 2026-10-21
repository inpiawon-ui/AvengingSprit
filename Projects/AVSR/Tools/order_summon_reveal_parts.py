# -*- coding: utf-8 -*-
"""소환 「실루엣 → 공개」 부품 발주(스킬 fx-art-pipeline 5단계) — 고른 시안을 앵커로 한 장씩.
가이드: Projects/AVSR/_exchange/guide_summon_reveal.md
앵커: in/mock_summon_reveal_free_idle.png(기다림 무대) · free_peak(공개 순간 대박 · 픽업) · guide_peak(공개 순간 새 몸)
납품: Projects/AVSR/_exchange/in/reveal_*.png
"""
import io
import os

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

HEAD = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「실루엣 → 공개」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (이 게임 빛 · 연출 원칙) · Projects/AVSR/_exchange/guide_summon_reveal.md (박자표).
PD 가 고른 시안(이 화풍이 정답): {anchors}
PD 말: 「motion-library buy_shop_item_popup_2 영상(실루엣 → 새 스킨 공개)을 참고해서 우리 가차 연출을 적용」.

[부품] {what}
{spec}
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

A_IDLE = 'Projects/AVSR/_exchange/in/mock_summon_reveal_free_idle.png'
A_PEAK = 'Projects/AVSR/_exchange/in/mock_summon_reveal_free_peak.png'
A_SOFT = 'Projects/AVSR/_exchange/in/mock_summon_reveal_guide_peak.png'
LIGHT = '- 빛 그림이다: **검은 바탕(#000000)** 위에 그린다(게임이 가산으로 겹친다 — 검은색 = 투명). 마젠타 금지.\n'
FORM = '- 형태 그림이다: **마젠타 #FF00FF 단색 바탕**(반투명 헤일로 금지, 검은 바탕 금지).\n'

ORDERS = [
    ('reveal_stage', [A_IDLE],
     '영혼 무대 바탕 — 실루엣이 자라는 기다림 단계 전체 화면 그림(세로 720×1280 비율, 한 장)',
     '- 시안의 기다림 장면처럼 밤의 영혼 무대 — 짙은 남색 · 푸른 안개, 화면 아래 1/3 쯤 가운데에 둥근 영혼 발판(바닥 문양).\n'
     '- 공개 무대(밤 도시 물가)와 **확실히 달라 보여야** 한다(공개 순간에 컷으로 바뀐다) — 도시 · 건물 없이 안개 낀 영혼의 방.\n'
     '- 빛기둥 · 바닥 빛 고리 · 실루엣 · 글자 · 판은 넣지 마라(따로 겹친다). 가운데는 비워 둔다.'),
    ('reveal_pillar', [A_IDLE],
     '세로 빛기둥 — 실루엣 아래에서 솟는 영혼 빛, 깜빡이는 루프 6장',
     LIGHT + '- 가로 6칸 한 줄, 칸마다 같은 크기의 세로로 긴 빛기둥(칸 비율 1:4). 1→6→1 로 이어져 끊김 없이 깜빡 · 일렁인다.\n'
     '- 시안의 푸른 빛기둥처럼 도트 빛줄기 + 둘레 은은한 번짐 + 위로 오르는 빛 알갱이. 색은 하늘색(게임이 보라 · 금으로 물들인다 — 흰 심 + 하늘 번짐).'),
    ('reveal_ring', [A_IDLE],
     '바닥 빛 고리 — 실루엣 발밑에 깔리는 납작한 타원 빛 고리, 도는 루프 6장',
     LIGHT + '- 가로 6칸 한 줄(칸 비율 2:1 — 납작한 타원). 고리 무늬가 천천히 돈다. 1→6→1 끊김 없이.\n'
     '- 시안 기다림 장면 바닥의 푸른 빛 고리처럼. 흰 심 + 하늘 번짐(게임이 등급 색으로 물들인다).'),
    ('reveal_burst', [A_PEAK, A_SOFT],
     '공개 빛살 터짐 — 진짜 몸이 나타나는 순간 몸 뒤에서 터지는 빛살, 8장(한 번 재생)',
     LIGHT + '- 가로 4칸 × 2줄 = 8칸(정사각 칸). 1 작은 섬광 → 3 빛살이 사방으로 가장 크게 → 8 빛살이 가늘어지며 흩어짐.\n'
     '- 자유 시안 공개 순간의 푸른 빛살 · 반짝 별처럼(도트 빛살). 가운데는 몸이 서므로 빛살 뿌리만, 심이 하얗게 덮지 않게.'),
    ('reveal_impact', [A_PEAK],
     '착지 충격 — 몸이 바닥에 착지할 때 튀는 바닥 파편 · 먼지, 6장(한 번 재생)',
     FORM + '- 가로 6칸 한 줄(칸 비율 2:1 — 바닥 높이 장면). 1 착지 순간 바닥 금 · 파편이 튀어오름 → 3 가장 크게 → 6 파편이 떨어져 사라짐.\n'
     '- 자유 시안 공개 순간 발밑의 깨진 돌 조각 · 먼지처럼 도트로. 빛 번짐은 그리지 마라(빛은 따로).'),
    ('reveal_plate', [A_PEAK],
     '공개 정보 판 — 「새 몸! / 대박! / PICK UP」 꼬리표 + 이름 + 등급이 들어갈 비스듬한 띠 판(글자 없이 빈 판)',
     FORM + '- 가로로 긴 판 한 장(비율 약 2:1). 왼쪽 위에 작은 꼬리표 자리(빈 리본), 아래로 이름 두 줄이 들어갈 넓은 칸.\n'
     '- 시안의 남색 판보다 특별하게 — 짙은 남색 몸 + 금 테 · 오른쪽 끝이 비스듬히 잘린 띠. **글자는 넣지 마라**(언어팩이 넣는다).'),
]


def main():
    keys = []
    for name, anchors, what, spec in ORDERS:
        body = HEAD.format(anchors=' · '.join(anchors), what=what, spec=spec, name=name)
        io.open(os.path.join(EX, f'order_{name}.ps1'), 'w', encoding='utf-8-sig').write(body)
        keys.append(name)
    run = ['Set-Location "C:\\won\\UnityProject\\AvengingSprit"',
           'foreach ($k in @(' + ','.join(f'"{k}"' for k in keys) + ')) {',
           '    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "skip $k"; continue }',
           '    for ($try = 0; $try -lt 2; $try++) {',
           '        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"',
           '        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }',
           '    }',
           '    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k" }',
           '}']
    io.open(os.path.join(EX, 'run_reveal_parts.ps1'), 'w', encoding='utf-8-sig').write('\n'.join(run) + '\n')
    print('orders', len(keys))


if __name__ == '__main__':
    main()
