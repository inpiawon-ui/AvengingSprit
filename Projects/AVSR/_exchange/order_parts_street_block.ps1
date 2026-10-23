$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 「밤거리」 「막는 물건」 부품 6종 (한 칸짜리)

■ 화풍 정답(PD 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_stage_street.png — 「밤거리」 시안의 물건과 **같은 결 · 같은 색 · 같은 굵기**.
  무대 바닥 : C:/won/UnityProject/AvengingSprit/Assets/BundleResource/RoomFloor/roomfloor_env_street.png
■ 판 형식 정답(쓰레기장 — 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_block.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_spike_fence.png ·
  C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_water.png — **칸 수 · 칸 크기 · 칸 안 자리 · 마젠타 여백**을 이것과 똑같이. 모양 · 색은 이 무대 것으로.
■ 게임 크기 기준 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png (72 x 102 — 아래 72 x 72 발자국 + 위 30 윗면)
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 위에서 비스듬히 내려다본 시점.
  **물건에 빛 · 아우라 · 반짝이를 굽지 않는다.** 그림자는 그리지 않는다(게임이 따로 깐다).
■ 판 규칙 — 배경은 **마젠타 #FF00FF 단색**(검정 · 회색 바탕 금지). **물건은 제 칸 안에만**. 칸마다 하나, 칸 바닥에 붙여 가운데.

■ 하는 일 — 원작(궁수의 전설)에서 옮긴 방의 막는 칸(몸 · 탄 모두 막음)에 하나씩. 옆 칸끼리 붙으면 이어진 벽 · 귀퉁이 덩어리가 된다.
  **서로 다른 실루엣 6가지** — 쓰레기장(고철 · 타이어 · 냉장고 · 드럼통 · 폐차 · 컨테이너)과 겹치는 모양이 없어야 한다.
  1 자판기
  2 대형 쓰레기통(뚜껑 닫힘)
  3 소화전 + 쓰러진 표지판
  4 넘어진 오토바이
  5 신문 가판대
  6 망가진 네온 간판 조각(불 꺼진 것 — 빛을 굽지 않는다)
  · 모두 발자국(아래 72 x 72)을 꽉 채우고 위로 30 만큼만 솟는다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_street_block.png — 가로 3 x 세로 2 판, 칸 288 x 408, 칸 사이 마젠타 32. 위 줄 1 · 2 · 3, 아래 줄 4 · 5 · 6.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
