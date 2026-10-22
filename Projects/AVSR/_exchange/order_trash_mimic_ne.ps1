$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 새 잡몹 「미믹 상자」 게임 그림 (ne 방향 · 9칸)

생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v3/ref_mimic_concept.png (라인업 3차 시안 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v3.png 에서 자른 것) — 생김새 · 색을 바꾸지 않는다.
  방 상자와 **같은 겉모습**의 철 덧댄 상자(C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_crate_1.png). 깨면 뚜껑이 입처럼 벌어져 이빨 · 붉은 혀, 밑에 짧은 다리 넷.
하는 일 : 물건인 척 가만히 있다가 가까이 가거나 맞으면 깨어나 깨문다.
칸 · 동작 형식 · 그림체는 먼저 통과한 잡몹과 같게 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/boar_e_raw.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/mantis_e_raw.png
동쪽 판(통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_mimic_e.png — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체**로 방향만 바꾼다. 칸 차례도 같다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_mimic_ne.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 방향 : 오른쪽 위(북동쪽, 화면 안쪽 오른쪽)를 보는 비스듬한 뒷모습. 아홉 칸 모두 같은 방향.
  · 칸 차례 : 1 대기(깨어남 — 입을 조금 벌림) · 2 걷기1(다리) · 3 걷기2 / 4 공격1(입을 크게 벌림) · 5 공격2(앞으로 덮쳐 깨묾) · 6 피격 / 7 쓰러짐1(뚜껑이 털썩) · 8 쓰러짐2(부서진 상자) · 9 **잠든 상자**(완전히 닫힌 상자 — 방 상자와 구별되지 않게, 다리 · 이빨 안 보임)
  · 크기 : 방 상자와 같은 크기 — 폭 56~62 px.
  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
