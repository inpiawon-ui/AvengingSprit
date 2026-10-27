$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 3챕터 방 전체 기믹 「따라오는 네온 표적」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_neon_tracker.md 를 읽는다(목표 모습 · 부품 표 · 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_neon_tracker_free_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-NT-01 obj_neon_cable — 위에서 늘어진 끊어진 네온 전깃줄(검은 피복 · 이음매 · 분홍으로 빛나는 테 · 끝에서 튀는 불티)
[앵커] Projects/AVSR/_exchange/ref/neon_tracker_mock/crop_cable.png — 시안에서 잘라 2배로 키운 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **4칸 가로로**(칸마다 128 × 384, 전체 512 × 384). 줄 위쪽 끝은 칸 맨 위 가운데에 붙고, 아래 끝은 칸 아래쪽 가운데로 내려온다. 4칸은 줄이 조금씩 흔들리는 모습(반복).
- 굵기는 칸 폭의 1/6 이하. 색: 검은 피복 + 하얀 심이 든 진한 분홍 · 자홍 빛 테. 보라(저주 색)로 기울지 않게.
- **초록 바탕**(#00FF00) — 분홍 빛이 있어 분홍 바탕은 못 쓴다. 나중에 뚫는다. 그림 안에 초록이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/obj_neon_cable.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
