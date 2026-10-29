$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아웃게임 연출 **시안** — coin_burst.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙. Projects/AVSR/_exchange/codex_brief_ui_reborn.md 도 읽는다(새 UI 방향).
[바탕] Projects/AVSR/_exchange/ref/ui_reborn_1023/anchor/base_reward.png — 새 UI 시안 화면. 창 · 칸 · 글자 · 캐릭터는 **그대로** 두고 그 위에 연출을 덧그린다.
[장면] 보상 받음 — 코인 · 보석이 회전하며 화면 위 재화 칩으로 날아가는 순간(날아가는 길 · 칩에 닿을 때 반짝)
네가 아트 디렉터라면 어떻게 꾸미겠나 — 더 멋진 안이 있으면 그걸로. 도트 모양 + 부드럽게 퍼지는 광원(블룸 · 빛 받는 주변) + 떠오르는 빛 알갱이를 함께. 게임 정보(글자 · 숫자)는 가리지 않는다.
- 크기는 바탕과 같은 720x1560. 아래 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/rb_mock_coin_burst.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
