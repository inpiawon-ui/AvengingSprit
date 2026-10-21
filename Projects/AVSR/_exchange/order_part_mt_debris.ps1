$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래에 적은 것만(2장). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 드라군 필살기 「메테오」 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_dragoon_meteor.png
그 시안에서 이 부분만 잘라 낸 기준 그림(이 모양 · 색 · 화풍 그대로): Projects/AVSR/_exchange/ref/rework1021/anchor_mt_blast.png
공통: **순수 마젠타(#FF00FF) 바탕**(게임에서 뚫는다) — 도구가 **투명 바탕**으로 그려 오면 그것도 그대로 저장한다(지우지 말 것) · 그림에 마젠타 · 분홍 색 금지 · 테두리를 바탕과 섞지 말고 또렷하게 · 도트 결이 살아 있게 · 캐릭터 · 적 · 바닥 타일 · 글자 · 숫자는 그리지 않는다(부품만) · 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
[만들 것] 착탄 때 사방으로 튀는 **검은 바위 파편**만(틈이 주황으로 살짝 빛나는 검은 돌 조각 12~16개, 크기 8~28 px). 8장 — 1 가운데 뭉침 → 2~5 바깥으로 흩어지며 날아감(위쪽은 조금 높이 솟음) → 6~8 바닥에 떨어져 작아지며 사라짐. 불 · 빛 · 연기는 그리지 않는다(빛은 따로 받는다)
[시트] 한 장 320x320, 8장을 가로 4칸 x 2줄(왼쪽 위부터 시간 순) — 전체 1280x640.
[저장] Projects/AVSR/_exchange/in/part_mt_debris.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
