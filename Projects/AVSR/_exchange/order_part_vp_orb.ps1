$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래에 적은 것만(3장 이하). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 흡혈귀 필살기 「피의 폭주」 이펙트 **부품**. PD 가 고른 시안(이 모양 · 색 · 화풍 그대로): Projects/AVSR/_exchange/ref/skillmock/ssr/vp_frenzy.png (시안의 그 부분만 잘라 낸 것).
공통: **분홍 바탕(#FF00FF) 단색**(나중에 뚫는다) · 바탕에 그림자 · 판 금지 · 도트 결이 살아 있는 빛 · 캐릭터 · 적 · 바닥 타일 · 글자는 그리지 않는다(부품만, 단 「흡혈귀에게 날아드는 **피 구슬**(동그란 진홍 구슬 + 흰 반짝 · 짧은 꼬리) 4장 반복 — 한 장 32x32, 4장 가로 한 줄(128x32).」에 적힌 것은 그린다).
[만들 것] 흡혈귀에게 날아드는 **피 구슬**(동그란 진홍 구슬 + 흰 반짝 · 짧은 꼬리) 4장 반복 — 한 장 32x32, 4장 가로 한 줄(128x32).
[저장] Projects/AVSR/_exchange/in/part_vp_orb.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
