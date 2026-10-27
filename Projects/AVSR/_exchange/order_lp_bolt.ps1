$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 4챕터 방 전체 기믹 「번개 + 물웅덩이」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_lightning_puddle.md 를 읽는다(부품 표 · 2차 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_lightning_puddle_guide_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-LP-01 fx_bolt — 하늘에서 떨어지는 **가는** 번개 줄기
[앵커] Projects/AVSR/_exchange/ref/lightning_puddle_mock/crop_bolt.png — 시안에서 잘라 낸 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **4칸 가로로**(칸마다 128 × 512, 전체 512 × 512). 칸마다 지그재그 모양이 다르다(번개가 칠 때마다 다르게 보이게).
- 줄기는 위 끝(칸 맨 위)에서 아래 끝(칸 맨 아래 가운데)까지 이어진다. 굵기는 칸 폭의 1/4 이하 — **가늘게**. 곁가지 1~2개.
- 색: 하얀 심 + 옅은 노랑 테 + 바깥 노란 빛 번짐. 하늘빛 · 청록 · 보라 금지.
- **완전한 검은 바탕**(#000000) — 게임에서 가산으로 얹는다. 테두리 · 그림자 금지.
[저장] Projects/AVSR/_exchange/in/lp_bolt.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
