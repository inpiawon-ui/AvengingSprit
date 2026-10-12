$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(4절 부품 규칙).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 이펙트 부품 — **바닥 빛 고리** 한 장.
[앵커 — 이 시안의 고리 모양 · 화풍 그대로] Projects/AVSR/_exchange/ref/tiers_mock/anchor/anchor_ring_gold.png , Projects/AVSR/_exchange/ref/tiers_mock/anchor/anchor_ring_platinum.png
(PD 가 통과시킨 시안 Projects/AVSR/_exchange/in/mock_tiers_result_peak.png 의 금 · 백금 상자 밑 고리)
- 상자가 착지할 때 바닥에서 퍼지는 **납작한 타원 고리**(위에서 비스듬히 본 원, 가로:세로 약 4:1). 고리 선은 가늘고 도트 계단이 보인다. 앞쪽 선이 조금 더 밝다.
- 색은 **흰색 · 회백색만**(게임에서 등급색을 곱한다). 고리 선 둘레로 부드럽게 퍼지는 빛 번짐을 아주 약하게.
- **검은 바탕(#000000)**, 게임에서 더하기(가산)로 그린다. 빛 가장자리는 검정으로 바로 사그라든다 — 바깥에 어두운 판 · 테두리 금지. 고리 안쪽도 검정.
- 크기 512 x 160, 고리가 가로 폭의 90% 를 채우고 가운데에.
[저장] Projects/AVSR/_exchange/in/tiers_part_ring.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
