$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 잡몹 「방패 로봇」 게임 그림 (e 방향 · 9칸) — 원작 결(기계)로 겉모습을 바꾼 몹

생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v4/ref_armadillo_machine.png (통과한 기계 시안 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v4.png 에서 자른 것) — **모양은 그대로, 색만 아래처럼 바꾼다.**
  대표 색 : **짙은 남색 몸** + 흰색과 검정 줄무늬 방패판 · 붉은 경광등(주황 · 노랑은 쓰지 않는다). 다섯 기계가 한 방에 섞여도 색으로 바로 구별되게 — 시안의 남색 + 주황을 그대로 쓰지 않는다.
원작 결 앵커 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/robot/unit_robot_s.png · C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/roadwarden/unit_roadwarden_s.png
하는 일 : 정면 **방패판**으로 앞에서 온 탄을 막는다 — 등 뒤는 얇다. 가끔 몸을 접어 둥근 쇠공으로 구른다(구르는 그림은 따로 받는다).
칸 · 동작 형식 · 그림체는 먼저 통과한 잡몹과 같게 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_watcher_eye_e.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_mimic_e.png

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_armadillo_m_e.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 방향 : 오른쪽(동쪽)을 보는 옆모습. 아홉 칸 모두 같은 방향.
  · 칸 차례 : 1 대기(방패를 앞에) · 2 걷기1(궤도) · 3 걷기2 / 4 공격1(방패를 들어 밀칠 준비) · 5 공격2(방패로 밀침) · 6 피격 / 7 부서짐1(방패가 떨어져 나감) · 8 부서짐2(주저앉은 잔해) · 9 빈 칸
  · 크기 : 바닥에서 방패 끝까지 50~56 px · 폭 70~80 px(낮고 넓게).
  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
