$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
도구가 크게 그려 오면 칸 격자대로 지정 크기로 **축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 투명 바탕이 안 나오면 순수 마젠타(#FF00FF) 바탕도 받는다 — 지우지 말고 그대로 저장.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」 챕터 12 배너 그림(로비 · 챕터 선택 카드용, 글자 없음).
같은 무대의 앞 챕터 배너(화풍 · 장소 · 구도 · 도트 결은 이대로): Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/REBORN_UI/Art/1x/ui_art_chapter_banner_06@1032x420.png
[분위기] 그림자 공장 — 불이 꺼진 정유 공장, 보랏빛 어둠과 긴 그림자, 희미한 비상등. **앞 배너와 한눈에 다른 챕터로 보이게** — 빛 색 · 시간 · 상황을 바꾼다.
- 1032x420 가로, 꽉 찬 그림(투명 없음). 캐릭터 · 글자 · 숫자 · 로고 · 버튼 · 틀 금지. 왼쪽 절반은 글자가 얹히니 조금 어둡고 차분하게.
[저장] Projects/AVSR/_exchange/in/rb_banner_12.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
