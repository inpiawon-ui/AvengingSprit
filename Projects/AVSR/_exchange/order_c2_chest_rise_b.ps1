$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(도트 모양 + 셰이더 광원 · 파티클을 섞는 게임 — 이 그림은 그중 도트 모양 · 입자 층).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 상자 연출 부품. **PD 가 고른 정답 시안**: Projects/AVSR/_exchange/in/mock_chest2_free_peak.png · mock_chest2_free_idle.png
[물건 그림(자리 · 크기 기준)] Assets/BaseResource/ChapterScreens/chest_silver_clean.png
[부품] fx_result_chest_rise_b — 위 chest_rise_a 와 **같은 빛살 묶음의 다른 순간** — 빛줄기 길이 · 갈래가 조금 다르다(두 장을 번갈아 깜빡여 일렁이게). 나머지 조건은 chest_rise_a 와 같다
[크기] 480 x 300 한 장. 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 상자 그림은 그리지 않는다, 빛만.
- 빛 가장자리는 검정으로 바로 사그라든다 — 바깥에 어두운 색 판 · 윤곽선 금지. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/c2_chest_rise_b.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
