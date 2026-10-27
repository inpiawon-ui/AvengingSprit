$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 4챕터 방 전체 기믹 「번개 + 물웅덩이」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_lightning_puddle.md 를 읽는다(부품 표 · 2차 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_lightning_puddle_guide_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-LP-02 fx_bolt_hit — 꽂힌 자리 바닥 섬광 · 작은 고리 · 불티
[앵커] Projects/AVSR/_exchange/ref/lightning_puddle_mock/crop_bolt.png — 시안에서 잘라 낸 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **6칸 가로로**(칸마다 160 × 160, 전체 960 × 160). 시간 순서: 1 하얀 섬광 최대 → 2~4 노란 고리가 퍼지고 불티가 튄다 → 5~6 사라진다.
- 바닥에 납작하게 깔린 모양(위에서 내려다본 타원 — 가로 : 세로 ≈ 2 : 1). 크기는 칸의 70% 안(캐릭터 한 명 크기).
- 색: 하얀 · 옅은 노랑. 하늘빛 금지. **완전한 검은 바탕**(#000000).
[저장] Projects/AVSR/_exchange/in/lp_hit.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
