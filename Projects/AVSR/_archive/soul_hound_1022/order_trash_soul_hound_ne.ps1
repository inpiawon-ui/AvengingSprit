$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 새 잡몹 「혼 사냥개」 게임 그림 (ne 방향 · 9칸)

생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v3/ref_soul_hound_concept.png (라인업 3차 시안 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v3.png 에서 자른 것) — 생김새 · 색을 바꾸지 않는다.
  마른 검은 개 · 해골 머리 · 드러난 등뼈 · 눈과 꼬리 끝이 푸른 혼불 색(도트 색만, 빛 번짐 금지).
하는 일 : 몸이 있을 땐 어슬렁거리고, 유령이 되면 빨라져 쫓아와 문다.
칸 · 동작 형식 · 그림체는 먼저 통과한 잡몹과 같게 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/boar_e_raw.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/mantis_e_raw.png
동쪽 판(통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_soul_hound_e.png — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체**로 방향만 바꾼다. 칸 차례도 같다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_soul_hound_ne.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 방향 : 오른쪽 위(북동쪽, 화면 안쪽 오른쪽)를 보는 비스듬한 뒷모습. 아홉 칸 모두 같은 방향.
  · 칸 차례 : 1 대기 · 2 달리기1 · 3 달리기2 / 4 공격1(웅크려 이빨을 드러냄) · 5 공격2(앞으로 뛰어들며 물기) · 6 피격 / 7 쓰러짐1 · 8 쓰러짐2(뼈가 흩어짐) · 9 빈 칸
  · 크기 : 바닥에서 등까지 40~46 px, 길이 68~76 px.
  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
