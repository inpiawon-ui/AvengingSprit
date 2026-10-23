$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 3단계 방 물건 시안 B 다시 「가시 고리 방」 — **칸 격자에 딱 맞춰서**

■ 화풍 정답(통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_room_obj_b.png — 물건 모양 · 색 · 결은 **이것 그대로**
  (막는 물건 = 폐차 · 드럼통 · 타이어 더미 · 냉장고 · 컨테이너, 가시 = 녹슨 판 위 쇠가시, 물 = 기름 섞인 오수).
■ 이번에 고칠 것 — 칸 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_tpl_mock/guide_ch1_room28.png
  · 가이드는 실제 게임 배율이다 — 화면 폭 720 px 에 가로 11 칸(한 칸 약 65 px). 이 칸을 **한 칸도 옮기지 않는다.**
  · 빨강 「가시」 칸 = 가시 판 하나(그 칸 안에만), 파랑 「물」 칸 = 오수(이어지는 웅덩이), 동그라미 = 몬스터 자리
    (원 = 고철 전갈 C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_s.png · 비 = 박쥐 C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/bat/unit_bat_s.png ·
     근 = 해골 C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/skeleton/unit_skeleton_s.png, 같은 배율 · 아래를 본다).
  · **모든 물건은 제 칸 안에만** 그린다 — 칸 밖으로 한 픽셀도 삐져나오지 않는다. 가시 판 둘이 나란히 있어도 칸 경계가 보이게.
  · **빈 칸은 바닥이 그대로 보인다.** 특히 물 웅덩이와 양옆 가시 줄 사이의 빈 세로 줄(왼쪽 셋째 · 오른쪽 일곱째 칸)은
    캐릭터가 지나가는 길이다 — 한눈에 「여기로 지나갈 수 있다」로 읽혀야 한다.
  · 가이드의 격자선 · 글자 · 색 상자 · 동그라미는 지운다(게임 그림만 남긴다).
  · 바탕 화면의 군더더기(빨간 피해 숫자 · 쓰러진 해골 · 갱스터 · 모래주머니)도 지우고 깨끗한 바닥으로. 플레이어(갱스터)는 아래 입구 난간 바로 위 가운데에 하나.
  · 화면 위 HUD · 아래 조이스틱 · 버튼 셋 · 방 벽 · 입구 난간은 그대로.
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 물건에 빛 · 아우라를 굽지 않는다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_room_obj_b2.png — 720 x 1280, 가이드와 같은 구도 · 같은 픽셀 크기.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
