$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 2챕터 방 전체 기믹 「폭격 표적」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_bomb_target.md 를 읽는다(목표 모습 · 부품 표 · 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_bomb_target_free_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-BT-01 fx_bomb_reticle — 빨간 조준 표적(납작한 타원 고리 + 상하좌우 눈금 4개 + 가운데 작은 십자)
[앵커] Projects/AVSR/_exchange/ref/bomb_target_mock/crop_reticle.png — 시안에서 잘라 2배로 키운 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장 **256 × 192**. 위에서 내려다본 납작한 타원(가로 : 세로 ≈ 1.4 : 1), 칸의 85% 안.
- 색: 위험 빨강 `#FF4D47` 계열 — 하얀 심이 살짝 든 빨간 선 + 바깥 빨간 빛 번짐. 안쪽은 비운다(차오름은 게임이 따로 그린다).
- **완전한 검은 바탕**(#000000) — 게임에서 가산으로 얹는다. 테두리 · 그림자 금지.
[저장] Projects/AVSR/_exchange/in/fx_bomb_reticle.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
