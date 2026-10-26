$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
게임 「Avenging Spirit RE:BORN」 로비 호스트 카드 안에 까는 **부드러운 둥근 빛 번짐** 1장.
시안: Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens 의 collection.png — 카드 아래쪽에서 등급색(파랑 · 보라 · 금)이 **둥글게, 아주 부드럽게** 위로 퍼진다. PD 「시안은 블러를 전체적으로 먹이고 동그랗게 퍼진다 · 지금 것은 사각 블러 같다」.
[만들 것] 가운데가 가장 밝은 **흰색** 원형 빛 하나. 가운데에서 가장자리까지 **고르게 천천히** 어두워져(가우스 번짐처럼) 가장자리에서 완전한 검정이 된다. 가운데가 꽉 찬 원판처럼 보이면 안 된다 — 밝은 핵 없이 처음부터 부드럽게 줄어든다. 테두리 선 · 고리 · 반짝임 · 별 · 도트 무늬 · 글자 없음. 색 없음(흰색 → 검정만, 게임에서 등급색을 곱한다).
[바탕] 순수 검정(#000000).
[크기] 256x256 한 장(빛이 그림 안에 다 들어온다 — 네 변은 완전한 검정).
[저장] Projects/AVSR/_exchange/in/rb_card_glow.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
