$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 검수 문서만 쓴다. 한 번에 끝낸다.

[목표] PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_devil_v1.png (3컷: 1 등장 · 2 최대(떠 있는 동안 은은히) · 3 선택/수락/구매(누를 때만))을
게임 화면에 **시안과 똑같이** 재현했는지 검수. PD: 「시안대로 가자」 · 「최종본을 내가 검수할 수 있게」 · 「너혼자 하지 말고 코덱스랑 같이」.
[재현물]
- 나란히: Projects/AVSR/_exchange/ref/fx_story/devil_vs_mock.png (위 = 시안 3컷, 아래 = 재현 같은 순간 0.25 / 2.00 / 4.15초)
- 움직임: Projects/AVSR/_exchange/ref/fx_story/devil.gif (등장~대기 3.8초 → 고른 뒤 1.0초)
- 부품(시안에서 잘라 발주): Projects/AVSR/_exchange/out/fxs/devil/
- 합성 표(값 고칠 곳): Projects/AVSR/Tools/fx_story.py 의 layers_devil() — L(부품, x, y, w, h, 프레임간격, t0, t1, ...) 한 줄이 한 층.
  좌표 = 720x1280 화면(위가 0). atNode='…{slot}' 은 고른 칸 가운데에서 시작. beam=True 는 (x,y)→(x2,y2) 로 늘인 줄기.
- 창 배치(틀 · 칸 · 글자): Projects/AVSR/Tools/popup_c_layout.py · 글자 규칙 consult_popup_typography.md
[이번 회차] 3회차 검수(review_fx_story_devil3.md) 변경 2건 적용: button_glow 는 고르면 거둔다(untilAccept), 수락 동안 플레이어 혼 본체를 (112,292) 96x96 로 그린다(게임은 실제 몸). 3회차 지적이 고쳐졌는지만 확인하라.
PD 기준: 글자 · 버튼을 가리지 않게 · 별/십자 반짝이 금지 · 화면 전체 물들이기 금지 · 대기 장식은 은은하게 · 누를 때 반응은 고른 칸에서 시작.

[써 낼 것] 한국어, 컷마다: 시안과 다른 점 → layers_devil() 에서 바꿀 값(좌표 · 크기 · 알파 · 시작/끝 · 프레임 간격 · 가산/덮기 · back) 구체적으로.
부품 그림 자체가 시안과 달라 다시 그려야 하면 「재발주」와 사양. 마지막에 Claude 가 바로 적용할 변경 목록.
남은 차이가 없으면 맨 위 판정에 「통과」라고 써라.
[저장] Projects/AVSR/_exchange/review_fx_story_devil4.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
