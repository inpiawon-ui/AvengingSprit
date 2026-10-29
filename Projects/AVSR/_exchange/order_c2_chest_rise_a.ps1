$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(도트 모양 + 셰이더 광원 · 파티클을 섞는 게임 — 이 그림은 그중 도트 모양 · 입자 층).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 상자 연출 부품. **PD 가 고른 정답 시안**: Projects/AVSR/_exchange/in/mock_chest2_free_peak.png · mock_chest2_free_idle.png
[물건 그림(자리 · 크기 기준)] Assets/BaseResource/ChapterScreens/chest_silver_clean.png
[부품] fx_result_chest_rise_a — 상자가 바닥에 닿는 순간 **바닥에서 위로 솟구치는 빛살** — 정답 시안 mock_chest2_free_peak.png 의 상자 아래 · 둘레에서 솟는 뾰족한 빛줄기 묶음 그대로. 칸 아래 가운데(바닥 줄 y 270)에서 위 · 양옆으로 부채처럼 뻗는다, 가운데가 가장 길다. 칸 가운데 아래 280x192 는 상자 자리(상자에 일부 가려진다). 빛줄기마다 가운데 흰 심 + 바깥으로 부드럽게 퍼지는 광원(딱딱한 윤곽선 금지). 흰색 ~ 밝은 회색(게임에서 등급색을 곱한다)
[크기] 480 x 300 한 장. 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 상자 그림은 그리지 않는다, 빛만.
- 빛 가장자리는 검정으로 바로 사그라든다 — 바깥에 어두운 색 판 · 윤곽선 금지. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/c2_chest_rise_a.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
