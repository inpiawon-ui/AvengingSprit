$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(도트 모양 + 셰이더 광원 · 파티클을 섞는 게임 — 이 그림은 그중 도트 모양 · 입자 층).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 상자 연출 부품. **PD 가 고른 정답 시안**: Projects/AVSR/_exchange/in/mock_chest2_free_peak.png · mock_chest2_free_idle.png
[물건 그림(자리 · 크기 기준)] Assets/BaseResource/ChapterScreens/chest_silver_clean.png
[부품] fx_result_chest_halo — 상자가 놓인 뒤 **머무는 동안 상자 모양을 따라 감싸는 부드러운 광원(후광)** — 정답 시안 mock_chest2_free_idle.png 의 상자 둘레 파란 빛 그대로. 칸 가운데에 상자(280x192, 물건 그림 chest_silver_clean.png 모양 그대로)를 놓았다고 치고, 그 **실루엣 바깥으로 6~24px 부드럽게 번지는 빛**. 상자 자리 안쪽은 가장 밝게 채워도 된다(상자에 가려진다). 가장자리로 갈수록 부드럽게 사그라든다 — **딱딱한 윤곽선 · 테두리 선 금지**. 위쪽 · 양옆에 짧은 빛 번짐 결 몇 가닥. 흰색 ~ 밝은 회색(게임에서 등급색을 곱한다)
[크기] 480 x 300 한 장. 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 상자 그림은 그리지 않는다, 빛만.
- 빛 가장자리는 검정으로 바로 사그라든다 — 바깥에 어두운 색 판 · 윤곽선 금지. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/c2_chest_halo.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
