$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 잡몹 「폭탄 드럼통」 게임 그림 (ne 방향 · 9칸) — 원작 결(기계)로 겉모습을 바꾼 몹

생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v4/ref_mushroom_machine.png (통과한 기계 시안 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v4.png 에서 자른 것) — **모양은 그대로, 색만 아래처럼 바꾼다.**
  대표 색 : **녹슨 빨강 드럼통** + 노랑과 검정 위험 줄무늬 · 쇠색 다리 · 붉은 경광등(남색은 쓰지 않는다). 다섯 기계가 한 방에 섞여도 색으로 바로 구별되게 — 시안의 남색 + 주황을 그대로 쓰지 않는다.
원작 결 앵커 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/robot/unit_robot_s.png · C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/roadwarden/unit_roadwarden_s.png
하는 일 : 다가와 **깜빡이다 자폭**한다(폭발 · 깜빡임 빛은 게임이 그린다).
칸 · 동작 형식 · 그림체는 먼저 통과한 잡몹과 같게 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_watcher_eye_e.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_mimic_e.png
동쪽 판(통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_mushroom_m_e.png — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체**로 방향만 바꾼다. 칸 차례도 같다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_mushroom_m_ne.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 방향 : 오른쪽 위(북동쪽, 화면 안쪽 오른쪽)를 보는 비스듬한 뒷모습. 아홉 칸 모두 같은 방향.
  · 칸 차례 : 1 대기 · 2 걷기1(짧은 다리) · 3 걷기2 / 4 공격1(웅크려 떨림 — 경광등만 밝은 색) · 5 공격2(부풀어 오름 — 터지기 직전, 빛 없이 몸이 둥글게) · 6 피격 / 7 부서짐1(뚜껑이 튀어나감) · 8 부서짐2(찌그러진 통) · 9 빈 칸
  · 크기 : 바닥에서 경광등까지 58~64 px.
  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
