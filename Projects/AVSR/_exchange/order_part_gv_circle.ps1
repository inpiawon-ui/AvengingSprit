$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래에 적은 것만(2장). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 백마법사 액티브 「중력진」 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_wizard_gravity.png
그 시안에서 이 부분만 잘라 낸 기준 그림(이 모양 · 색 · 화풍 그대로): Projects/AVSR/_exchange/ref/rework1021/anchor_gv_circle.png
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있게 · 캐릭터 · 적 · 바닥 타일 · 글자 · 숫자는 그리지 않는다(부품만) · 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
[만들 것] 바닥에 깔리는 **황금 룬 마법진**(탑뷰 타원, 가로 약 360 · 세로 약 290 px) — 겹 원 2~3개 · 테두리를 도는 룬 문자 · 안쪽 문양, 가운데는 비워 둔다. 6장 반복(룬이 조금씩 돌고 빛이 흐르듯 반짝이게, 모양 · 크기는 같게)
[시트] 한 장 384x320, 6장을 가로 3칸 x 2줄(왼쪽 위부터 시간 순) — 전체 1152x640.
[저장] Projects/AVSR/_exchange/in/part_gv_circle.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
