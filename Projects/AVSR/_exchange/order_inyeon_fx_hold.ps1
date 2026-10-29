$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 이 브리프를 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md . 그리고 Projects/AVSR/_exchange/guide_inyeon_fx.md 를 끝까지 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「인연이 켜져 메인의 액티브 스킬이 바뀌는 순간」 연출 **자유 시안**. 크기 720 x 1280.
[바탕 화면] Projects/AVSR/_exchange/in/mock_partyui_S1b.png — PD 가 확정한 전투 화면 · HUD. HUD · 필드 · 조작부는 그대로 둔다.
[장면] 메인 「드라군」(금빛) · 보조 1 「샐러맨더」(보랏빛 혼, 방금 데려옴) → 드라군의 인연 「업화」 가 켜진다(화염 지대가 1.5배 · 둔화). 오른쪽 아래 작은 스킬 버튼(파란 원)이 인연형으로 바뀐다.
[너는 아트 디렉터] 가이드의 박자표 · 강약을 참고하되 「두 혼이 이어져 스킬이 새것이 됐다」 를 가장 잘 보여 주는 연출을 네가 디자인한다. 전투는 멈추지 않는다 · 필드를 어둡게 하지 않는다 · 큰 판 금지.
[이 장 — 머무는 장면] 인연이 켜진 뒤 평소 전투. 스킬 버튼 테두리에 금 · 보라 빛이 은은하게 돌고 「인연」 작은 꼬리표, 왼쪽 시너지 칸 맨 위에 금빛 인연 칸(드라군 얼굴 문장 + 「업화」). 화면을 가리지 않게 약하게.
- 그림 오른쪽 아래 여백 없이 꽉 채우고 설명 메모는 넣지 마라.
[저장] Projects/AVSR/_exchange/in/mock_inyeon_fx_hold.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
