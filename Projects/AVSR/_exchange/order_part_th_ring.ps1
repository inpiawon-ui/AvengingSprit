$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 5장 이하(도구 한도). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 폭력배 스킬 「난사」 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_skill_thug.png (PD 「시안 퀄 정도는 나와야 한다 · 과하지 않으면서 이쁘게」).
[기준 그림 — 이 모양 · 색 · 화풍 그대로] Projects/AVSR/_exchange/ref/skillmock/anchor_thug_end.png (오른쪽 — 회전을 멈추며 사방으로 터지는 흰 별 고리 + 주황 빛살)
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있는 빛 · 캐릭터 · 적 · 바닥 · 글자는 그리지 않는다(부품만).
[만들 것] 마무리 **사방 방사 섬광** 8장 — 1~2장 흰 별이 터지고 · 3~5장 흰 고리가 퍼지며 주황 빛살 12줄이 뻗고 · 6~8장 사그라든다. 한 장 256x256, 8장 가로 한 줄(2048x256).
[저장] Projects/AVSR/_exchange/in/part_th_ring.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
