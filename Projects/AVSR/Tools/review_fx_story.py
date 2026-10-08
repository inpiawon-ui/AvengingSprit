"""연출 합성(fx_story.py) 코덱스 검수 발주문 — 화면 · 회차별 (2026-10-08).
python review_fx_story.py {화면} {회차} [이전 검수 문서 반영 메모]
→ _exchange/review_fx_story_{화면}{회차}.ps1 (실행은 PowerShell)
"""
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
MOCK = {'clear': 'mock_fxstory_clear_v2.png'}
FN = {'devil': 'layers_devil', 'levelup': 'layers_levelup', 'altar': 'layers_altar', 'shop': 'layers_shop',
      'clear': 'layers_clear', 'room_heal': 'layers_room_heal', 'room_devil': 'layers_room_devil',
      'room_shop': 'layers_room_shop'}
CUTS = {'popup': '위 = 시안 3컷, 아래 = 재현 0.25 / 2.00 / 4.05 / 4.55초 — 4컷째는 창이 닫힌 뒤 플레이어 몸의 「능력치 적용」 빛',
        'room': '위 = 시안 3컷(대기 · 다가감 · 작동), 아래 = 재현 1.00 / 2.00 / 3.05초'}
GIF = {'popup': '등장~대기 3.8초 → 고른 뒤 0.35초에 창이 닫히고 플레이어 몸에 이벤트 색 빛이 깃듦(~5.0초)',
       'room': '멀리 대기 0~1.6초 → 플레이어가 다가옴(240 안) 1.6~2.8초 → 닿아서 작동 2.8~3.8초'}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 검수 문서만 쓴다. 한 번에 끝낸다.

[목표] PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/{mock} (3컷: 1 등장 · 2 최대(떠 있는 동안 은은히) · 3 선택/수락/구매(누를 때만))을
게임 화면에 **시안과 똑같이** 재현했는지 검수. PD: 「시안대로 가자」 · 「최종본을 내가 검수할 수 있게」 · 「너혼자 하지 말고 코덱스랑 같이」.
[재현물]
- 나란히: Projects/AVSR/_exchange/ref/fx_story/{key}_vs_mock.png ({cuts})
- 움직임: Projects/AVSR/_exchange/ref/fx_story/{key}.gif ({gif})
- 부품(시안에서 잘라 발주): Projects/AVSR/_exchange/out/fxs/{key}/
- 합성 표(값 고칠 곳): Projects/AVSR/Tools/fx_story.py 의 {fn}() — L(부품, x, y, w, h, 프레임간격, t0, t1, ...) 한 줄이 한 층.
  좌표 = 720x1280 화면(위가 0). atNode='…{{slot}}' 은 고른 칸 가운데에서 시작. beam=True 는 (x,y)→(x2,y2) 로 늘인 줄기.
- 창 배치(틀 · 칸 · 글자): Projects/AVSR/Tools/popup_c_layout.py · 글자 규칙 consult_popup_typography.md
{note}
PD 기준: 글자 · 버튼을 가리지 않게 · 별/십자 반짝이 금지 · 화면 전체 물들이기 금지 · 대기 장식은 은은하게 · 누를 때 반응은 고른 칸에서 시작.

[써 낼 것] 한국어, 컷마다: 시안과 다른 점 → {fn}() 에서 바꿀 값(좌표 · 크기 · 알파 · 시작/끝 · 프레임 간격 · 가산/덮기 · back) 구체적으로.
부품 그림 자체가 시안과 달라 다시 그려야 하면 「재발주」와 사양. 마지막에 Claude 가 바로 적용할 변경 목록.
남은 차이가 없으면 맨 위 판정에 「통과」라고 써라.
[저장] Projects/AVSR/_exchange/review_fx_story_{key}{rnd}.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''


def main(key, rnd, note=''):
    kind = 'room' if key.startswith('room_') else 'popup'
    body = TEMPLATE.format(mock=MOCK.get(key, 'mock_fxstory_%s_v1.png' % key), key=key, fn=FN[key], rnd=rnd,
                           cuts=CUTS[kind], gif=GIF[kind],
                           note=('[이번 회차] ' + note) if note else '')
    path = os.path.join(EX, 'review_fx_story_%s%s.ps1' % (key, rnd))
    with open(path, 'w', encoding='utf-8-sig') as f:
        f.write(body)
    print(path)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], ' '.join(sys.argv[3:]))
