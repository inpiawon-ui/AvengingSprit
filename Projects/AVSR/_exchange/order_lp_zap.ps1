$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 4챕터 방 전체 기믹 「번개 + 물웅덩이」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_lightning_puddle.md 를 읽는다(부품 표 · 2차 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_lightning_puddle_guide_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-LP-04 fx_puddle_zap — 웅덩이 위를 기는 노란 · 하얀 전기 줄(반복)
[앵커] Projects/AVSR/_exchange/ref/lightning_puddle_mock/crop_puddle_zap.png — 시안에서 잘라 낸 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **6칸 가로로**(칸마다 256 × 160, 전체 1536 × 160). 6장이 끊김 없이 돌게(지직거리며 줄이 바뀐다).
- 전기 줄은 **웅덩이 모양(FX-LP-03 과 같은 납작한 타원 범위) 안에서만** 가지를 친다 — 밖으로 번지지 않는다. 금 간 유리처럼 얇은 줄 + 몇 군데 밝은 불티.
- 색: 하얀 심 + 노랑. 하늘빛 금지. **완전한 검은 바탕**(#000000) — 가산으로 웅덩이 위에 얹는다. 물 · 바닥은 그리지 않는다(전기만).
[저장] Projects/AVSR/_exchange/in/lp_zap.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
