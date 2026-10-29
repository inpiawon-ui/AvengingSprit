$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(도트 모양 + 셰이더 광원 · 파티클을 섞는 게임 — 이 그림은 그중 도트 모양 층).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 레벨업 카드 연출 부품.
[부품] fx_levelup_card_rays — 카드를 고르는 순간 **카드 둘레에서 바깥으로 확 터지는 빛살** — 정답 시안 Projects/AVSR/_exchange/in/mock_lvpopup_free_peak.png 의 왼쪽 카드 둘레 파란 빛살 그대로. 칸 가운데에 세로로 긴 카드(324x640, 2배 크기)가 놓였다고 치고 그 테두리 바깥으로 뾰족한 빛줄기들이 사방으로 뻗는다(위아래 · 모서리 쪽이 길다). 카드 자리 안쪽은 비워도 되고 테두리 바로 바깥이 가장 밝다. 빛줄기마다 흰 심 + 부드럽게 퍼지는 광원, 작은 반짝 별 몇 개. 흰색 ~ 밝은 회색(게임에서 카드 등급색을 곱한다)
[크기] 520 x 760 한 장. 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 카드 그림은 그리지 않는다, 빛만.
- 빛 가장자리는 검정으로 바로 사그라든다 — 바깥에 어두운 색 판 · 윤곽선 금지. 글자 · 숫자 금지. 빛이 칸 밖으로 잘리지 않게.
[저장] Projects/AVSR/_exchange/in/lv_card_rays.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
