$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 쓰레기장 「막는 물건」 부품 6종 (한 칸짜리)

■ 화풍 정답(PD 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_room_obj_b.png — 이 화면의 쓰레기장 물건과 **같은 결 · 같은 색 · 같은 굵기**.
  그 화면에서 자른 앵커 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_topleft.png(폐차 · 드럼통) · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_topright.png(컨테이너 · 냉장고 · 타이어) ·
  C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_bottomleft.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_bottomright.png(타이어 · 드럼통 · 고철 더미)
■ 지금 게임 물건(크기 · 시점 기준) : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png (72 x 102 — 아래 72 x 72 발자국 + 위 30 윗면)
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 위에서 비스듬히 내려다본 시점(지금 방 물건과 같게).
  **물건에 빛 · 아우라 · 반짝이를 굽지 않는다.** 그림자는 그리지 않는다(게임이 발밑 그림자를 따로 깐다).
■ 판 규칙 — 배경은 **마젠타 #FF00FF 단색**(검정 · 회색 바탕 금지). 칸끼리 마젠타 여백으로 떨어뜨린다.
  **물건은 제 칸 안에만** — 칸 밖으로 한 픽셀도 나가지 않는다. 칸마다 물건 하나, 칸 바닥에 붙여 가운데.

■ 하는 일 — 원작(궁수의 전설)에서 옮긴 방의 「막는 칸」(몸 · 탄 모두 막는다)에 하나씩 놓인다. 옆 칸끼리 붙으면 이어진 벽 · 귀퉁이 덩어리가 된다.
  원작은 테마마다 막는 물건 **모양이 통째로 다르다** — 쓰레기장도 서로 다른 실루엣 6가지가 섞여 보여야 한다(같은 네모를 색만 바꾸지 않는다).
■ 6종(칸 차례대로)
  1 압착한 고철 큐브(네모난 압축 덩어리 · 삐져나온 파이프)   2 폐타이어 세 개 쌓은 더미
  3 찌그러진 냉장고(문이 살짝 열림)                         4 녹슨 드럼통 둘을 묶은 것(위험 줄무늬 하나)
  5 폐차 앞부분 토막(보닛 · 깨진 전조등)                      6 부서진 컨테이너 조각(골판 철판 · 녹)
  · 모두 같은 크기 칸 안 — 발자국(아래 72 x 72 칸)을 꽉 채우고 위로 30 만큼 솟는다. 키가 그보다 크면 안 된다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_block.png — 가로 3 칸 x 세로 2 칸 판, 칸 하나 288 x 408(게임 72 x 102 의 4배),
  칸 사이 마젠타 여백 32. 위 줄 1 · 2 · 3, 아래 줄 4 · 5 · 6.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
