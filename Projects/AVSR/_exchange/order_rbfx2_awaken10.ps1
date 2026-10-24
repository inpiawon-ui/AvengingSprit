$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 칸 비율이 안 맞게 그려 와도 지우지 말고 그대로 저장한다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(특히 0-2 RE:BORN 화면 · 3-1 등급 계단).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아웃게임 연출 **부품**. 기준 그림(이 모양 · 색 · 화풍 그대로): Projects/AVSR/_exchange/in/mock_rbfx_awaken10_free_peak.png · Projects/AVSR/_exchange/in/rbfx_awaken6.png
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있게 · 글자 · 숫자 · 캐릭터 · 창은 그리지 않는다(부품만) · 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
[만들 것] **10성 각성 금빛 2겹 링 + 빛살** 8장(번쩍 → 넓어짐 → 문양 원이 돌며 머묾 → 사그라짐). 시안(첫 그림) 몸 뒤 금빛 고리 두 겹 · 마름모 반짝 별 · 문양 원 그대로. 크기 · 결은 6성 링 시트(둘째 그림)와 **같은 칸 · 같은 지름**(10성이라고 키우지 않는다 — 겹을 하나 더할 뿐). 가운데(몸 자리)는 비운다. ⚠ 지난 납품은 링 바깥에 갈색 안개 판이 깔리고 가운데 구멍 가장자리에 빨강 · 초록 테가 묻었다 — 빛 바깥은 순수 검정, 다른 색 테 금지
[시트] 한 장 384x384, 8장을 가로 4칸 x 2줄(왼쪽 위부터 시간 순) — 전체 1536x768.
[저장] Projects/AVSR/_exchange/in/rbfx2_awaken10.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
