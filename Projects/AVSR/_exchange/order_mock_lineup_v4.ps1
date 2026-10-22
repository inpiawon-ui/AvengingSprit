$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 잡몹 5종 **겉모습 바꾸기** 시안 (원작 결로)

PD 2026-10-22 「몹도 IP 를 벗어난 느낌」 — 비교 시트 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/ip_compare/ip_compare_sheet.png 의 빨강 5종
(쇠뿔 멧돼지 · 외눈 사마귀 · 갑옷 아르마딜로 · 폭탄 버섯 · 톱날 원숭이)이 원작에 없는 「숲 · 던전 짐승」으로 읽힌다.
**하는 일은 그대로 두고 몸만 원작 결(기계)로 바꾼다.**

■ 원작 결 앵커(반드시 본다)
  · 원작 적 로봇 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/robot/unit_robot_s.png
  · 원작 보스 크러셔 · 로봇 스네이크 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/crusher/unit_crusher_s.png · C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/robot_snakes/unit_robot_snakes_s.png
  · 원작 느낌으로 통과한 우리 잡몹(순찰기 · 코일 보행기 · 고철 전갈) : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/roadwarden/unit_roadwarden_s.png · C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/coilwalker/unit_coilwalker_s.png · C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_s.png
  → 1990년 아케이드의 **굵고 밝은 쇠 · 둥근 장갑 · 붉은 경광등 · 줄무늬 경고 띠** 결. 사람 모양 · 짐승 얼굴 · 털 · 버섯 · 곤충 금지.

■ 우리 그림체 (반드시 — 먼저 통과한 잡몹과 같게)
  · 통과한 잡몹 라인업 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/mock_lineup_v2_labeled.png
  · 지금 게임 캐릭터 4배 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/ref_game_sprites_x4.png
  · 실제 화면 크기 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/ref_ingame_scale.png
  · 1991 아케이드 16비트 픽셀 아트. **96 x 96 픽셀 캔버스 안에 그린 크기** — 위 참조와 키 · 굵기가 같아야 한다.
  · 1 픽셀 짙은 외곽선, 한 마리 12~16색, 명암 2~3단 뚝뚝 끊기게(그라데이션 · 잔털 · 질감 노이즈 금지).
  · 밝고 채도 높은 색, 실루엣이 한눈에 읽히는 만화 비례. **사람 모양 금지**(사람 = 빼앗을 수 있는 몸이라 헷갈린다).
  · 몸에 빛 · 아우라 · 반짝이를 굽지 않는다(빛은 게임에서 이펙트로 따로 낸다).
  · 그린 뒤 **4배로 키워(가장 가까운 이웃)** 늘어놓는다. 글자 · 번호를 그리지 마라.

■ 5종 — 하는 일이 몸에서 읽혀야 한다
  1 돌진 쇄석기 (옛 쇠뿔 멧돼지 자리) — 낮고 긴 몸 · 앞에 쇠뿔 모양 파쇄 날 · 무한궤도 · 뒤에 배기통. 평타 없이 **일직선 돌진**만, 벽에 박으면 휘청.
  2 삼각대 저격기 (옛 외눈 사마귀 자리) — 가는 세 다리 · 긴 저격 총열 · 붉은 **외눈 조준 렌즈** 하나. 멀리서 조준선을 긋고 한 발. 감시 눈(둥근 비행 드론)과 다르게 **땅에 서는 키 큰 기계**.
  3 방패 로봇 (옛 갑옷 아르마딜로 자리) — 낮고 넓은 몸 · 정면에 두꺼운 **방패판**(앞에서 친 것을 막는다) · 등 뒤는 얇다. 몸을 접으면 **둥근 쇠공**이 되어 구른다(그 모습도 한 칸).
  4 폭탄 드럼통 (옛 폭탄 버섯 자리) — 녹슨 드럼통 몸 · 짧은 다리 · 머리 위 붉은 경광등 · 위험 줄무늬. 다가와 **깜빡이다 자폭**.
  5 톱날 투척기 (옛 톱날 원숭이 자리) — 고철 몸통 · 한 팔이 **톱날 원반 발사대**, 등에 예비 톱날 둘. 톱날을 던지면 돌아와 다시 끼운다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v4.png — 가로형(1672 x 941 쯤), 배경 어두운 회청 단색
  · 위 줄 : 5종 대기 자세(오른쪽을 보는 옆모습), 방패 로봇은 접은 쇠공 모습을 옆에 한 칸 더.
  · 아래 줄 : 크기 · 결 비교로 원작 로봇 · 우리 순찰기 · 코일 보행기 · 해골을 **같은 배율**로(새로 그리지 말고 위 그림 그대로 4배).
  · 크기 : 돌진 쇄석기 · 방패 로봇은 해골보다 낮고 길게, 삼각대 저격기는 해골보다 조금 크게, 폭탄 드럼통 · 톱날 투척기는 해골쯤.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
