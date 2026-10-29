$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 연출 부품. **PD 가 고른 정답 시안**: Projects/AVSR/_exchange/in/mock_result_codex_peak.png · mock_result_codex_idle.png
[앵커 크롭 — 이 화풍 · 색 · 빛살 모양 그대로] Projects/AVSR/_exchange/ref/result_mock/anchor_chest_peak.png
[물건 그림(자리 · 크기 기준)] Assets/BaseResource/ChapterScreens/chest_silver_clean.png
[부품] fx_result_chest_burst_silver — 은 상자가 바닥에 닿는 **순간 터짐**(강) — 정답 시안의 힘주는 순간처럼 상자 뒤에서 빛살이 사방으로 확 펼쳐지고 번짐이 밝게 터진다, 선명한 파랑 · 하늘색 빛, 가장 밝은 심은 흰색. 1 = 작은 섬광 → 3 = 가장 크고 밝음(빛살이 칸 가장자리 8px 안쪽까지) → 8 = 머묾 아우라 크기로 가라앉음. 가운데 280x192 는 상자 자리
[시트] 칸 480 x 300 가 4열로 8장, 왼쪽 위부터 시간 순(시트 전체 1920 x 600). 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 물건(상자 · 금화) 그림은 그리지 않는다, 빛만.
- 빛 덩이 바깥에 어두운 색 판(남색 · 갈색 배경 덩어리)을 깔지 않는다 — 빛 가장자리는 검정으로 바로 사그라든다(샘플 rcfx_chest_aura_silver 에서 어두운 판이 깔려 왔다).
- 글자 · 숫자 금지. 빛이 칸 밖으로 잘리지 않게.
[저장] Projects/AVSR/_exchange/in/rcfx_chest_burst_silver.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
