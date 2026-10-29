$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」 **히든 캐릭터 공개 전 실루엣**(랜덤 출전에서 아주 드물게 나오는 숨은 몸).
정체는 사신이다 — 원본 그림: Assets/BaseResource/HostSelectPanel/hostportraitimage_death.png (이 자세 · 외곽 모양을 따른다)
[만들 것] 512x512 투명 바탕, 사신의 외곽(두건 · 낫 끝이 살짝 보이는)을 **검은 실루엣**으로 + 가장자리를 도는 가는 금빛 테두리 빛. 안쪽은 정체를 알 수 없게 까맣게.
도트 결이 살아 있게(뭉툭하게 키운 모양 금지). 칸 가운데, 글자 없음.
[저장] Projects/AVSR/_exchange/in/rb_hidden_silhouette.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
