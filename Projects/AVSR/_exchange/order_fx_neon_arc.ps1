$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 3챕터 방 전체 기믹 「따라오는 네온 표적」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_neon_tracker.md 를 읽는다(목표 모습 · 부품 표 · 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_neon_tracker_free_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-NT-02 fx_neon_arc — 줄 끝이 바닥을 때린 자리의 분홍 네온 방전(바닥 고리 + 금 간 자국 + 튀는 유리 파편 · 불티)
[앵커] Projects/AVSR/_exchange/ref/neon_tracker_mock/crop_arc.png — 시안에서 잘라 2배로 키운 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **6칸 가로로**(칸마다 192 × 160, 전체 1152 × 160). 시간 순서: 1 하얀 섬광 → 2~4 분홍 고리가 퍼지고 파편 · 불티가 튄다 → 5~6 사라진다.
- 바닥에 납작하게(가로 : 세로 ≈ 2 : 1), 칸의 80% 안(캐릭터 1명 크기). 색: 하얀 심 + 진한 분홍 · 자홍. 노랑 · 하늘빛 금지(4챕터 번개와 달라야 한다).
- **완전한 검은 바탕**(#000000) — 게임에서 가산으로 얹는다. 테두리 · 그림자 금지.
[저장] Projects/AVSR/_exchange/in/fx_neon_arc.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
