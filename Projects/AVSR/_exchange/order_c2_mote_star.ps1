$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(도트 모양 + 셰이더 광원 · 파티클을 섞는 게임 — 이 그림은 그중 도트 모양 · 입자 층).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 상자 연출 부품. **PD 가 고른 정답 시안**: Projects/AVSR/_exchange/in/mock_chest2_free_peak.png · mock_chest2_free_idle.png
[물건 그림(자리 · 크기 기준)] Assets/BaseResource/ChapterScreens/chest_silver_clean.png
[부품] fx_result_mote_star — 파티클로 뿌릴 **작은 반짝 별 하나** — 정답 시안의 상자 둘레 흰 십자 반짝임. 칸 가운데, 4갈래 가는 빛 + 가운데 흰 점 + 둘레 부드러운 광원. 흰색(게임에서 색을 곱한다). 갈래 끝이 칸 가장자리 6px 안에서 끝난다
[크기] 64 x 64 한 장. 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 상자 그림은 그리지 않는다, 빛만.
- 빛 가장자리는 검정으로 바로 사그라든다 — 바깥에 어두운 색 판 · 윤곽선 금지. 글자 · 숫자 금지.
[저장] Projects/AVSR/_exchange/in/c2_mote_star.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
