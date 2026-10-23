$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 「정유 공장」 「가시 · 울타리」 부품 4종 (한 칸짜리)

■ 화풍 정답(PD 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_stage_refinery.png — 「정유 공장」 시안의 물건과 **같은 결 · 같은 색 · 같은 굵기**.
  무대 바닥 : C:/won/UnityProject/AvengingSprit/Assets/BundleResource/RoomFloor/roomfloor_env_refinery.png
■ 판 형식 정답(쓰레기장 — 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_block.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_spike_fence.png ·
  C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_water.png — **칸 수 · 칸 크기 · 칸 안 자리 · 마젠타 여백**을 이것과 똑같이. 모양 · 색은 이 무대 것으로.
■ 게임 크기 기준 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png (72 x 102 — 아래 72 x 72 발자국 + 위 30 윗면)
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 위에서 비스듬히 내려다본 시점.
  **물건에 빛 · 아우라 · 반짝이를 굽지 않는다.** 그림자는 그리지 않는다(게임이 따로 깐다).
■ 판 규칙 — 배경은 **마젠타 #FF00FF 단색**(검정 · 회색 바탕 금지). **물건은 제 칸 안에만**. 칸마다 하나, 칸 바닥에 붙여 가운데.

■ 하는 일
  · 가시 — **늘 솟아 있다** · 밟으면 아프다 · 몸도 탄도 지나간다. 바닥에 붙어 위로 안 솟고 72 x 72 발자국 안에. 한눈에 「밟으면 아프다」.
  · 울타리 — 몸은 막고 **탄은 지나간다** — 틈이 보여야 한다(꽉 막힌 벽처럼 보이면 안 된다). 72 x 102 칸.
■ 4종(칸 차례대로)
  1 가시 A — 달군 쇠가시 판(쇠가시 끝이 주황빛으로 달아오른 색 — 빛 번짐 없이 도트 색만, 쓰레기장 가시 판과 다른 모양)
  2 가시 B — 바닥 격자 사이로 솟은 뜨거운 철침
  3 울타리 A — 파이프 난간
  4 울타리 B — 철망 안전 펜스(노랑 띠)
  · 1 · 2 는 칸 아래 288 x 288 안에만(위 30 줄 몫은 비운다), 3 · 4 는 칸 전체 안에.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_refinery_spike_fence.png — 가로 4 x 세로 1 판, 칸 288 x 408, 칸 사이 마젠타 32.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
