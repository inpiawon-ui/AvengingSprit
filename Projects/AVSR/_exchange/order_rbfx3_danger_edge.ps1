$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 도구가 크게 그려 오면 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 전투 화면 **위험 경고 테두리** 부품. 기준 시안(유령 체력이 바닥일 때 화면 가장자리가 붉게 물든 모습): Projects/AVSR/_exchange/ref/ui_reborn_1023/fx_anchor/danger_mock.png
[만들 것] 세로 화면 가장자리를 따라 안쪽으로 번지는 **붉은 빛 테두리** 1장 — 네 변 모두, 모서리가 조금 더 진하고 안쪽으로 갈수록 사그라짐(화면 폭의 12% 정도 깊이). 가운데는 완전히 비운다. 테두리 바로 안쪽에 아주 가는 도트 결(계단)이 살짝 보이게. 색은 진한 빨강(#FF2A3A 계열), 하얗게 뜨지 않게.
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다 · 글자 · 숫자 · 캐릭터 · UI 는 그리지 않는다(테두리만).
[크기] 360x640 한 장.
[저장] Projects/AVSR/_exchange/in/rbfx3_danger_edge.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
