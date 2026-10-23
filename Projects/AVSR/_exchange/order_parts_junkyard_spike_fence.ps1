$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 쓰레기장 「가시 판 · 울타리」 부품 4종 (한 칸짜리)

■ 화풍 정답(PD 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_room_obj_b.png — 이 화면의 쓰레기장 물건과 **같은 결 · 같은 색 · 같은 굵기**.
  그 화면에서 자른 앵커 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_topleft.png(폐차 · 드럼통) · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_topright.png(컨테이너 · 냉장고 · 타이어) ·
  C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_bottomleft.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_bottomright.png(타이어 · 드럼통 · 고철 더미)
■ 지금 게임 물건(크기 · 시점 기준) : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png (72 x 102 — 아래 72 x 72 발자국 + 위 30 윗면)
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 위에서 비스듬히 내려다본 시점(지금 방 물건과 같게).
  **물건에 빛 · 아우라 · 반짝이를 굽지 않는다.** 그림자는 그리지 않는다(게임이 발밑 그림자를 따로 깐다).
■ 판 규칙 — 배경은 **마젠타 #FF00FF 단색**(검정 · 회색 바탕 금지). 칸끼리 마젠타 여백으로 떨어뜨린다.
  **물건은 제 칸 안에만** — 칸 밖으로 한 픽셀도 나가지 않는다. 칸마다 물건 하나, 칸 바닥에 붙여 가운데.

■ 하는 일
  · 가시(원작 가시 칸) — **늘 솟아 있는** 쇠가시. 밟으면 아프다 · 몸도 탄도 지나간다. 바닥에 붙은 판이라 **위로 솟는 높이 없이 72 x 72 발자국 안**에.
    한눈에 「밟으면 아프다」로 읽혀야 한다. 시안의 녹슨 판 위 쇠가시와 같은 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_spike.png
  · 울타리(원작 울타리 칸) — 몸은 막고 **탄은 지나간다** — 그래서 틈이 보이는 낮은 철망 · 쇠창살이어야 한다(꽉 막힌 벽처럼 보이면 안 된다). 72 x 102 칸.
■ 4종(칸 차례대로)
  1 가시 판 A — 녹슨 사각 판 위 쇠가시 세 줄          2 가시 판 B — 같은 판, 가시 배치만 조금 다르게(되풀이가 덜 보이게)
  3 울타리 A — 낮은 철망 한 칸(양 끝 쇠기둥)           4 울타리 B — 녹슨 쇠창살 한 칸(위에 철조망 한 줄)
  · 1 · 2 는 칸 아래 72 x 72(4배 288 x 288) 안에만, 그 위 30 줄은 비운다. 3 · 4 는 칸 전체(72 x 102) 안에.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_spike_fence.png — 가로 4 칸 x 세로 1 칸 판, 칸 하나 288 x 408(게임 72 x 102 의 4배),
  칸 사이 마젠타 여백 32.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
