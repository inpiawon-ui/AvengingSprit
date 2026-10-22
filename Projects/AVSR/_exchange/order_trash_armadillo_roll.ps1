$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 잡몹 「갑옷 아르마딜로」 구르기 그림 (6칸)

원래 몸(게임 그림, 동쪽 판) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v2/armadillo_e_raw.png — **같은 몸 · 같은 색 · 같은 크기 · 같은 그림체.**
하는 일 : 몸을 말아 점점 빨라지며 굴러온다 → 벽에 박으면 뒤집혀 휘청(배가 드러난다 — 이때는 정면도 맞는다).

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/trash_armadillo_roll.png — 정사각(864 x 864 쯤, 96 x 96 칸 3 x 3 을 3배), 배경 **마젠타 #FF00FF 단색**
  · 칸 차례 : 1 말기(반쯤 말린 몸 — 머리를 넣는 중) · 2 공1 · 3 공2 / 4 공3 · 5 공4(공은 칸마다 90°씩 더 돌아간 모습 — 마디 무늬로 회전이 보이게) · 6 휘청1(뒤집혀 배를 보임) /
            7 휘청2(배를 보인 채 발을 버둥) · 8 빈 칸 · 9 빈 칸
  · 공 크기 : 96 칸에서 지름 48~54 px, 바닥에 붙여. 빛 · 속도선 · 먼지를 그리지 않는다.
  · 배경은 **반드시 마젠타 #FF00FF 단색** — 몸이 어두워도 검정 · 회색 바탕 금지(자르는 도구가 마젠타만 뚫는다).
  · 1 px 짙은 외곽선 · 12~16색 · 2~3단 끊기는 명암. 질감 노이즈 금지. 몸에 빛 · 아우라 · 반짝이를 굽지 않는다.
  · 여덟 칸 같은 몸 · 같은 크기 · 같은 색. 칸마다 몸을 칸 가운데 · 바닥에 붙여 그린다.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
