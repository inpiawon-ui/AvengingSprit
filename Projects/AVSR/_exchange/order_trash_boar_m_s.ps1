$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 잡몹 「돌진 쇄석기」 게임 그림 (s 방향 · 9칸) — 원작 결(기계)로 겉모습을 바꾼 몹

생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v4/ref_boar_machine.png (통과한 기계 시안 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v4.png 에서 자른 것) — **모양은 그대로, 색만 아래처럼 바꾼다.**
  대표 색 : **중장비 노랑**이 몸의 대부분 + 검정 경고 줄무늬 · 쇠색 파쇄 날 · 붉은 경광등 하나(남색은 쓰지 않는다). 다섯 기계가 한 방에 섞여도 색으로 바로 구별되게 — 시안의 남색 + 주황을 그대로 쓰지 않는다.
원작 결 앵커 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/robot/unit_robot_s.png · C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/roadwarden/unit_roadwarden_s.png
하는 일 : 평타 없이 **일직선으로 돌진**한다. 벽에 박으면 잠깐 휘청.
칸 · 동작 형식 · 그림체는 먼저 통과한 잡몹과 같게 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_watcher_eye_e.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_mimic_e.png
동쪽 판(통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_boar_m_e.png — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체**로 방향만 바꾼다. 칸 차례도 같다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_boar_m_s.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 방향 : 아래(남쪽, 화면 앞쪽)를 보는 앞모습. 아홉 칸 모두 같은 방향.
  · 칸 차례 : 1 대기 · 2 전진1(궤도 굴러감) · 3 전진2 / 4 공격1(돌진 준비 — 몸을 낮추고 배기통에서 연기 없이 떨림) · 5 공격2(돌진 — 날을 앞으로 내밂) · 6 피격 / 7 부서짐1(날이 빠지고 기울어짐) · 8 부서짐2(바닥에 주저앉은 잔해) · 9 빈 칸
  · 크기 : 바닥에서 등까지 50~56 px · 길이 76~84 px(낮고 길게).
  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
