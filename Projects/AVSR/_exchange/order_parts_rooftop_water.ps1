$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 「빌딩 옥상」 「물」 타일 한 벌 — 칸끼리 이어 붙는다

■ 화풍 정답(PD 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_stage_rooftop.png — 「빌딩 옥상」 시안의 물건과 **같은 결 · 같은 색 · 같은 굵기**.
  무대 바닥 : C:/won/UnityProject/AvengingSprit/Assets/BundleResource/RoomFloor/roomfloor_env_rooftop.png
■ 판 형식 정답(쓰레기장 — 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_block.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_spike_fence.png ·
  C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_water.png — **칸 수 · 칸 크기 · 칸 안 자리 · 마젠타 여백**을 이것과 똑같이. 모양 · 색은 이 무대 것으로.
■ 게임 크기 기준 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png (72 x 102 — 아래 72 x 72 발자국 + 위 30 윗면)
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 위에서 비스듬히 내려다본 시점.
  **물건에 빛 · 아우라 · 반짝이를 굽지 않는다.** 그림자는 그리지 않는다(게임이 따로 깐다).
■ 판 규칙 — 배경은 **마젠타 #FF00FF 단색**(검정 · 회색 바탕 금지). **물건은 제 칸 안에만**. 칸마다 하나, 칸 바닥에 붙여 가운데.

■ 하는 일 — 원작의 물 칸. **몸은 못 건너고 탄은 지나간다.** 칸이 여럿 이어져 웅덩이 · 강 · 십자가 된다.
  게임은 물 칸마다 「수면」을 깔고, 옆 칸이 물이 아닌 쪽에만 「가장자리」를, 두 쪽이 다 땅인 모서리에 「귀퉁이」를 얹는다 —
  그래서 수면은 **어느 쪽으로 이어 붙여도 이음매가 안 보여야** 하고, 칸 테두리를 밝게 그리지 않는다.
  이 무대 물 : 방수층 위 고인 빗물 — 흐린 회청색 물, 낮은 시멘트 턱 둑. 바닥보다 한 단 낮게. 빛 · 반짝이를 굽지 않는다.
■ 10칸(칸 288 x 288) — 쓰레기장 물 판과 **같은 차례 · 같은 자리**
  1 수면 A(꽉 채움) · 2 수면 B(무늬만 다르게) · 3 가장자리 위(위 변에만 둑) · 4 가장자리 아래 · 5 가장자리 왼쪽 · 6 가장자리 오른쪽 ·
  7 귀퉁이 왼쪽 위(그 모서리에만) · 8 귀퉁이 오른쪽 위 · 9 귀퉁이 왼쪽 아래 · 10 귀퉁이 오른쪽 아래 — 3~10 은 그 변 · 모서리 밖은 마젠타.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_rooftop_water.png — 가로 4 x 세로 3 판(1~4 / 5~8 / 9 · 10), 칸 288 x 288, 칸 사이 마젠타 32.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
