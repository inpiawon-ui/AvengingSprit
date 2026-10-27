$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 2챕터 방 전체 기믹 「폭격 표적」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_bomb_target.md 를 읽는다(목표 모습 · 부품 표 · 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_bomb_target_free_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-BT-03 fx_bomb_shell — 위에서 비스듬히 떨어지는 작은 미사일(빨간 머리 · 회색 몸 · 꽁무니 불꽃)
[앵커] Projects/AVSR/_exchange/ref/bomb_target_mock/crop_blast.png — 시안에서 잘라 2배로 키운 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **2칸 가로로**(칸마다 64 × 128, 전체 128 × 128). 머리가 아래로 향한 채 떨어지는 모습, 2칸은 꽁무니 불꽃만 다르게(깜빡임).
- 크기는 캐릭터 머리 하나쯤 — 작게. 외곽선은 도트 게임 물건처럼 짙은 테 1px.
- **분홍 바탕**(#FF00FF) — 나중에 뚫는다. 그림 안에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/fx_bomb_shell.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
