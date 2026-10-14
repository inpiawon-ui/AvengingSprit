$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아마존 스킬 「번개 무희」 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_skill_amazon.png (PD 「시안 퀄 정도는 나와야 한다 · 과하지 않으면서 이쁘게」).
[기준 그림 — 이 모양 · 색 · 화풍 그대로] Projects/AVSR/_exchange/ref/skillmock/anchor_amazon_mid.png (가운데 컷 — 맞은 적 자리의 흰 · 하늘 섬광)
공통: **검은 바탕**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있는 청백 번개(가운데 흰 심 + 하늘 · 파랑 테두리) · 캐릭터 · 적 · 바닥은 그리지 않는다(부품만).
[만들 것] 맞는 순간의 **타격 섬광** 6장(한 번 터지고 사라짐) — 한 장 192x192. 1~2장: 가운데 흰 십자 섬광 확 커짐 · 3~4장: 청백 번개 가지가 사방으로 튐 · 5~6장: 잔불꽃만 남고 꺼짐. 가로 한 줄(1152x192).
[저장] Projects/AVSR/_exchange/in/part_ld_spark.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
