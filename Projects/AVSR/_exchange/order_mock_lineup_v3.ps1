$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 새 잡몹 라인업 3차 시안 (6종, 생김새 정하기)

■ 우리 그림체 (반드시 — 먼저 통과한 잡몹과 같게)
  · 통과한 잡몹 라인업 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/mock_lineup_v2_labeled.png
  · 지금 게임 캐릭터 4배 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/ref_game_sprites_x4.png
  · 실제 화면 크기 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/ref_ingame_scale.png
  · 1991 아케이드 16비트 픽셀 아트. **96 x 96 픽셀 캔버스 안에 그린 크기** — 위 참조와 키 · 굵기가 같아야 한다.
  · 1 픽셀 짙은 외곽선, 한 마리 12~16색, 명암 2~3단 뚝뚝 끊기게(그라데이션 · 잔털 · 질감 노이즈 금지).
  · 밝고 채도 높은 색, 실루엣이 한눈에 읽히는 만화 비례. **사람 모양 금지**(사람 = 빼앗을 수 있는 몸이라 헷갈린다).
  · 몸에 빛 · 아우라 · 반짝이를 굽지 않는다(빛은 게임에서 이펙트로 따로 낸다).
  · 그린 뒤 **4배로 키워(가장 가까운 이웃)** 늘어놓는다. 글자 · 번호를 그리지 마라.

■ 6종 — 하는 일이 몸에서 읽혀야 한다
  1 감시 눈 (떠다니는 기계 · 레이저) — 둥근 금속 몸에 **큰 붉은 렌즈 하나**, 짧은 안테나 · 작은 추진구. 렌즈에서 예고선 → 레이저를 쏜다. 바닥에서 조금 떠 있고 밑에 그림자.
  2 그림자 살쾡이 (근접 · 순간이동) — 검보라 그림자 몸의 고양잇과 요괴, 노란 눈 둘, 길고 휜 앞발톱. 꼬리 끝이 연기처럼 흩어진다(사라졌다 나타나는 놈).
  3 톱날 원숭이 (원거리 · 부메랑) — 고철 더미에서 사는 원숭이, 등에 녹슨 **톱날 원반**을 여러 장 메고 한 장을 손에 들었다. 던지면 돌아온다.
  4 미믹 상자 (근접 · 물건인 척) — 방에 있는 나무 상자와 **똑같은 겉모습** : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_crate_1.png · C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_junkyard_crate_1.png
       두 칸으로 그린다 — (a) 가만히 있는 상자(방 상자와 구별 안 됨) (b) 깨어난 모습 : 뚜껑이 입처럼 벌어져 이빨 · 혀, 밑에 짧은 다리 넷.
  5 수호 등불 (지원 · 방패 걸기) — 푸른 불꽃이 든 낡은 랜턴이 떠다니는 정령. 작은 눈 둘. 곁의 적에게 방패를 씌우는 놈 — 겁 많고 뒤에 숨는 느낌.
  6 혼 사냥개 (근접 · 유령을 쫓음) — 마른 검은 개, 머리는 뼈가 드러난 해골, 눈 · 꼬리 끝이 푸른 혼불 색(빛 번짐 없이 도트 색으로만).

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v3.png — 가로형(1672 x 941 쯤), 배경 어두운 회청 단색
  · 두 줄. 모두 오른쪽(동쪽)을 보는 옆모습 · 대기 자세. 미믹 상자는 (a)(b) 두 칸을 나란히.
  · 줄 끝에 크기 비교로 참조의 **해골 · 박쥐 · 갱스터를 같은 배율로** 세운다(새로 그리지 말고 같은 크기 · 같은 그림체).
  · 크기 : 감시 눈 · 수호 등불은 해골보다 조금 작게(떠 있음), 그림자 살쾡이 · 혼 사냥개는 해골 키쯤 · 길게, 톱날 원숭이는 해골쯤, 미믹 상자는 방 상자 크기 그대로.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
