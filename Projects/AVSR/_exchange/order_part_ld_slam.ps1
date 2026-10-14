$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 아마존 스킬 「번개 무희」 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_skill_amazon.png (PD 「시안 퀄 정도는 나와야 한다 · 과하지 않으면서 이쁘게」).
[기준 그림 — 이 모양 · 색 · 화풍 그대로] Projects/AVSR/_exchange/ref/skillmock/anchor_amazon_slam.png (오른쪽 컷 — 하늘에서 꽂히는 번개 기둥 + 바닥 번개 충격 고리)
공통: **검은 바탕**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있는 청백 번개(가운데 흰 심 + 하늘 · 파랑 테두리) · 캐릭터 · 적 · 바닥은 그리지 않는다(부품만).
[만들 것] 마무리 **내려찍기** 8장(한 번) — 한 장 384x768(세로로 긴 칸). 위에서 굵은 번개 기둥이 내려꽂히고(1~3장) 바닥에 닿는 순간 타원 전기 충격 고리가 퍼지며(4~6장) 파편 빛 · 잔전기만 남고 꺼진다(7~8장). 바닥 고리는 칸 아래쪽 1/4 에 눕는 타원. 가로 한 줄(3072x768).
[저장] Projects/AVSR/_exchange/in/part_ld_slam.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
