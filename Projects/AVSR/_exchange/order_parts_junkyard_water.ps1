$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 쓰레기장 「물(오수 웅덩이)」 타일 한 벌 — 칸끼리 이어 붙는다

■ 화풍 정답(PD 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_room_obj_b.png — 이 화면의 쓰레기장 물건과 **같은 결 · 같은 색 · 같은 굵기**.
  그 화면에서 자른 앵커 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_topleft.png(폐차 · 드럼통) · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_topright.png(컨테이너 · 냉장고 · 타이어) ·
  C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_bottomleft.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_obj_parts/ref_junk_bottomright.png(타이어 · 드럼통 · 고철 더미)
■ 지금 게임 물건(크기 · 시점 기준) : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png (72 x 102 — 아래 72 x 72 발자국 + 위 30 윗면)
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 위에서 비스듬히 내려다본 시점(지금 방 물건과 같게).
  **물건에 빛 · 아우라 · 반짝이를 굽지 않는다.** 그림자는 그리지 않는다(게임이 발밑 그림자를 따로 깐다).
■ 판 규칙 — 배경은 **마젠타 #FF00FF 단색**(검정 · 회색 바탕 금지). 칸끼리 마젠타 여백으로 떨어뜨린다.
  **물건은 제 칸 안에만** — 칸 밖으로 한 픽셀도 나가지 않는다. 칸마다 물건 하나, 칸 바닥에 붙여 가운데.

■ 하는 일 — 원작(궁수의 전설)의 물 칸. **몸은 못 건너고 탄은 지나간다.** 방마다 물 칸이 여러 개 이어져 웅덩이 · 강 · 십자 모양이 된다.
  게임은 물 칸마다 「수면」 한 장을 깔고, 옆 칸이 물이 아닌 쪽에만 「가장자리」를 얹고, 두 쪽이 다 땅인 귀퉁이에 「귀퉁이」를 얹는다.
  그래서 수면은 **위 · 아래 · 왼쪽 · 오른쪽 어느 쪽으로 이어 붙여도 이음매가 안 보여야** 한다.
■ 화풍 정답 — 시안의 기름 섞인 오수 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_room_obj_a.png 가운데 웅덩이(어두운 수면 · 녹슨 흙 가장자리 · 기름 무지개 몇 점).
  바닥보다 한 단 낮게 보이게. 수면 반짝임 · 빛은 그리지 않는다(나중에 이펙트로).

■ 10칸(모두 같은 크기 — 게임 한 칸 72 x 72 의 4배 288 x 288)
  1 수면 A — 칸을 꽉 채운 오수 수면(가장자리 없음) · 네 변이 어디로 이어도 맞게
  2 수면 B — 같은 수면, 기름 무늬 · 떠 있는 작은 쓰레기 자리만 다르게
  3 가장자리 위 — 칸 **위쪽 변에만** 녹슨 흙 둑(칸 높이의 약 1/4), 나머지는 마젠타
  4 가장자리 아래 — 아래쪽 변에만          5 가장자리 왼쪽 — 왼쪽 변에만          6 가장자리 오른쪽 — 오른쪽 변에만
  7 귀퉁이 왼쪽 위 — 왼쪽 위 모서리에만(위 · 왼쪽 둑이 만나는 둥근 모서리), 나머지는 마젠타
  8 귀퉁이 오른쪽 위   9 귀퉁이 왼쪽 아래   10 귀퉁이 오른쪽 아래
  · 3~10 의 둑은 수면 위에 얹힌다 — 둑 안쪽 경계가 수면으로 자연스럽게 내려가는 턱으로 보이게. 둑은 같은 두께 · 같은 색.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_water.png — 가로 4 칸 x 세로 3 칸 판(1~4 / 5~8 / 9 · 10), 칸 288 x 288, 칸 사이 마젠타 여백 32.
  · **칸 안을 꽉 채우는 것은 1 · 2 수면뿐**이다. 3~10 은 지정한 변 · 모서리에만 그리고 나머지는 마젠타로 비운다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
