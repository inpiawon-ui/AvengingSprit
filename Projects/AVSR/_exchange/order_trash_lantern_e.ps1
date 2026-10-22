$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 새 잡몹 「수호 등불」 게임 그림 (e 방향 · 9칸)

생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v3/ref_lantern_concept.png (라인업 3차 시안 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_lineup_v3.png 에서 자른 것) — 생김새 · 색을 바꾸지 않는다.
  낡은 청동 랜턴이 떠다니는 정령 · 안에 푸른 불꽃 · 작은 검은 눈 둘. 위에 고리.
하는 일 : 곁의 적에게 방패를 씌운다(방패 빛은 게임이 따로 그린다). 겁이 많아 뒤로 숨는다.
칸 · 동작 형식 · 그림체는 먼저 통과한 잡몹과 같게 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/boar_e_raw.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/mantis_e_raw.png

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_lantern_e.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 방향 : 오른쪽(동쪽)을 보는 옆모습. 아홉 칸 모두 같은 방향.
  · 칸 차례 : 1 대기(떠 있음) · 2 떠다님1(왼쪽으로 살짝 기욺) · 3 떠다님2(오른쪽으로) / 4 공격1(문이 열리며 불꽃이 커짐 — 빛 번짐 없이 도트 불꽃으로) · 5 공격2(불꽃을 내뿜는 자세) · 6 피격(흔들림) / 7 부서짐1(유리 금) · 8 부서짐2(떨어져 꺼진 랜턴) · 9 빈 칸
  · 크기 : 고리부터 바닥까지 50~56 px(바닥에서 8 px 띄운다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
