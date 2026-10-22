$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 3단계 방 물건 시안 B 「가시 고리 방」(원작 방 그대로 · 쓰레기장 무대)

■ 하는 일 — 방 구조(칸)는 궁수의 전설 원작 그대로 옮겨 왔다. **네 몫은 이 방을 쓰레기장 무대의 물건으로 보이게 하는 것뿐**이다.
  · 칸 하나 = 물건 하나. 화면 폭 720 px 에 가로 11 칸(한 칸 약 65 px). 아래 글자 그림의 칸을 한 칸도 옮기지 않는다.
  · # 막는 물건 — 몸 · 탄 모두 막는다. **쓰레기장만의 서로 다른 모양 4~6 가지**를 섞어 놓는다(같은 네모를 색만 바꾸지 않는다).
      예를 고르는 것은 네 몫이다 — 압착한 고철 덩어리 · 폐타이어 더미 · 찌그러진 냉장고 · 드럼통 묶음 · 폐차 앞부분 · 부서진 컨테이너 조각 같은 것.
      한 칸짜리 물건끼리 붙으면 이어진 벽처럼 보여도 좋다(원작 귀퉁이 덩어리처럼).
  · ~ 물 — 몸은 못 건너고 탄은 지나간다. **여러 칸이 이어지는 웅덩이**(기름 섞인 오수 · 녹슨 가장자리) — 가장자리 · 귀퉁이가 이어지는 타일 모양.
      바닥보다 한 단 낮게, 수면은 어둡게. 빛 · 반짝이를 굽지 않는다(반짝임은 나중에 이펙트로).
  · ^ 가시 — 밟으면 아프다(몸 · 탄 모두 통과). 바닥에 붙은 낮은 쇠가시 판 · 철조망. 한눈에 「밟으면 아프다」로 읽히게.
  · o 기타 — 작은 막는 소품 하나(소화전 · 표지판 같은 것).
  · r 원거리 · m 근접 · f 비행 몬스터 자리 — 몬스터는 이 그림 그대로, 같은 배율:
      고철 전갈(원거리) C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_s.png ·
      해골(근접) C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/skeleton/unit_skeleton_s.png · 박쥐(비행) C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/bat/unit_bat_s.png
      플레이어(갱스터)는 맨 아래 가운데.

■ 바탕 — 지금 게임 화면(HUD · 조이스틱 · 버튼 · 방 바닥 · 벽은 그대로 둔다) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_tpl_mock/base_room_now.png
■ 참고 — 원작이 테마마다 물건 모양을 얼마나 다르게 썼나(모양 · 결을 베끼지 말고 「다양함」만 본다) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/plan_content_expand/step3_research/origin_obstacles.png
■ 버릴 것 — 지금 우리 물건(무대마다 같은 모양, 색만 다름) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/plan_content_expand/step3_research/room_objects.png
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 물건에 빛 · 아우라를 굽지 않는다. 지금 화면 몬스터 · 바닥과 같은 결.

■ 이 방(글자 그림 — 위가 문, 아래가 입구. 세로 21 칸 — 화면에는 아래쪽 15 칸쯤만 보여도 된다)
  ###..r..###
  #.........#
  #.........#
  ^.r^.^.^r.^
  ...........
  .....r.....
  ...........
  ..f.^^^.f..
  ...........
  ..^.~~~.^..
  ..^.~r~.^..
  ..^.~~~.^..
  ...........
  ..f.^^^.m..
  ...........
  ...........
  ...........
  #.^.^.^.^.#
  #.........#
  #.........#
  ###.....###
  · 네 귀퉁이 ㄱ 자 덩어리(#) · 가시 줄과 가시 고리 · 가운데 작은 물 섬 — 「가시로 길을 좁히고 섬 위에서 쏜다」가 이 방의 재미다.
  · 막는 물건 4~6 가지가 이 방에서 다 보이게 섞어 놓는다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_room_obj_b.png — 720 x 1280, 바탕 화면과 같은 구도 · 같은 픽셀 크기.
  네가 이 게임 아트 디렉터라면 쓰레기장을 어떻게 꾸미겠나 — 칸은 지키고 물건 모양 · 결 · 깊이 · 그림자는 자유.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
