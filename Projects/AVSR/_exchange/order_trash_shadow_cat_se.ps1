$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 새 잡몹 「그림자 살쾡이」 게임 그림 (se 방향 · 9칸)

생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v3/ref_shadow_cat_concept.png (라인업 3차 시안 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v3.png 에서 자른 것) — 생김새 · 색을 바꾸지 않는다.
  검보라 그림자 몸의 고양잇과 요괴 · 노란 눈 둘 · 길게 휜 흰 앞발톱 · 꼬리 끝이 연기처럼 흩어짐.
하는 일 : 멀어지면 **연기로 흩어져 사라졌다가** 내 옆에 나타나 앞발로 벤다. 벤 뒤 잠깐 선다.
칸 · 동작 형식 · 그림체는 먼저 통과한 잡몹과 같게 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/boar_e_raw.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/mantis_e_raw.png
동쪽 판(통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_shadow_cat_e.png — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체**로 방향만 바꾼다. 칸 차례도 같다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_shadow_cat_se.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 방향 : 오른쪽 아래(남동쪽, 화면 앞 오른쪽)를 보는 비스듬한 앞모습. 아홉 칸 모두 같은 방향.
  · 칸 차례 : 1 대기 · 2 걷기1 · 3 걷기2 / 4 공격1(웅크려 앞발을 치켜듦) · 5 공격2(앞발로 크게 할퀴며 뻗음) · 6 피격 / 7 쓰러짐1 · 8 쓰러짐2(연기로 흩어지며 반쯤 사라짐) · 9 **사라짐**(몸이 연기 덩어리로 흩어지는 순간 — 눈만 남음)
  · 크기 : 바닥에서 등까지 44~50 px, 길이 70~80 px(낮고 길게).
  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
