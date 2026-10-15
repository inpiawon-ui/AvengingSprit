$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래에 적은 것만(3장 이하). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 호퍼(기관단총) 필살기 「돌진 난사」 이펙트 **부품**. PD 가 고른 시안(이 모양 · 색 · 화풍 그대로): Projects/AVSR/_exchange/ref/skillmock/sr/hs_2.png (시안의 그 컷).
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있는 빛 · 캐릭터 · 적 · 바닥 타일 · 글자는 그리지 않는다(부품만).
[만들 것] 도착 뒤 몸 **전체를 둥글게 감싸는 하늘색 보호막 구**(정면에서 본 동그란 거품 — 바닥 고리가 아니다. 반투명 빛 막 안이 비쳐 몸이 보이고, 테두리가 밝게 빛나며 왼쪽 위에 흰 하이라이트 반짝, 표면을 흐르는 가는 빛 무늬) 6장 반복(은은하게 일렁 · 하이라이트가 살짝 돈다).
지난 납품은 납작한 바닥 고리라 반려: Projects/AVSR/_exchange/ref/skillmock/sr/reject_hs_shield_v1.png — 이렇게 하지 말 것. 시안 컷의 몸을 감싼 파란 구 그대로.
한 장 192x192, 6장 가로 한 줄(1152x192). **검은 바탕(#000000)**(게임에서 더하기로 그린다).
[저장] Projects/AVSR/_exchange/in/part_hs_shield.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
