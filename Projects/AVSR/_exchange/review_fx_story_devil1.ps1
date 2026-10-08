$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 검수 문서만 쓴다. 한 번에 끝낸다.

[목표] PD 가 통과시킨 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_devil_v1.png (악마의 거래 창 3컷: 1 등장 · 2 최대(떠 있는 동안) · 3 수락) 을
게임 화면에 **시안과 똑같이** 재현했는지 검수. PD: 「시안대로 가자 — 거기엔 효과가 다 잘 나왔다」. 그리고 「최종본을 내가 검수할 수 있게」.
[재현물]
- 나란히: Projects/AVSR/_exchange/ref/fx_story/devil_vs_mock.png (위 = 시안 3컷, 아래 = 재현 같은 순간 3컷, 720x1280 각각을 절반 크기로)
- 움직임: Projects/AVSR/_exchange/ref/fx_story/devil.gif (등장 0.6초 → 대기 3.2초 → 수락 1.0초)
- 부품(시안에서 잘라 발주한 것): Projects/AVSR/_exchange/out/fxs/devil/fx_devil_{pill_sparks|button_glow|heat|smoke|chain_glow|flame|soul_beam|reward_burst}_n.png
- 합성 코드(값 고칠 곳): Projects/AVSR/Tools/fx_story.py 의 devil() — Part(부품, 중심, 크기, 프레임 간격, 시작, 끝, ...), Beam(...), panel_fn · acc_fn
  좌표는 720x1280 화면. 창 틀 EventBox = (30,372, 660x595). 유령(플레이어 혼) 자리: 시안처럼 창 왼쪽 위 화면 안 — 지금은 HUD 초상(95,75)에서 나가 잘못이다.
[내가 본 차이] ① 등장 연기가 너무 크고 아래 조작 버튼까지 덮음(시안은 창 아래 바닥에서 피어오름) ② 혼 번개 출발점 ③ 사슬 빛 위치가 틀의 사슬과 어긋나 겹침
 ④ 수락 컷에 시안의 「사슬 당김」 빛줄기 없음 ⑤ 대기 열기가 시안보다 붉은 덩어리가 짙음.

[써 낼 것] 한국어, 컷마다: 시안과 다른 점 → devil() 에서 바꿀 값(좌표 · 크기 · 알파 · 시작/끝 · 프레임 간격 · 가산/덮기) 구체적으로.
부품 그림 자체가 시안과 달라 다시 그려야 하면 「재발주」와 이유. 마지막에 Claude 가 바로 적용할 변경 목록.
[저장] Projects/AVSR/_exchange/review_fx_story_devil1.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
