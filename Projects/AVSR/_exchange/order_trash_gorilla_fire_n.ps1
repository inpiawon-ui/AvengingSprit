$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 잡몹 원소판 「불 고릴라」 게임 그림 (n 방향 · 8칸)

원래 몸(지금 게임 그림 8장을 3배로 붙인 판) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v3/game_actor_enforcer_n_x3.png
  → **칸 · 자세 · 실루엣 · 크기를 그대로 두고 색 · 작은 부속만** 바꾼다. 한 칸이라도 자세가 달라지면 안 된다.
원소 생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_elements_v1.png 의 1줄 오른쪽(불 고릴라).
바꿀 것 : 돌 몸 틈이 용암처럼 주황 · 빨강으로 갈라지고 어깨 · 머리에 작은 도트 불꽃 몇 개.
동쪽 판(통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_gorilla_fire_e.png — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체**로 방향만 바꾼다. 칸 차례도 같다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_gorilla_fire_n.png — 정사각(864 x 864, 원래 판과 같은 칸), 배경 **마젠타 #FF00FF 단색**
  · 칸 차례는 원래 판과 같다 : 1 대기 · 2 걷기1 · 3 걷기2 / 4 공격1 · 5 공격2 · 6 피격 / 7 쓰러짐1 · 8 쓰러짐2 · 9 빈 칸
  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
