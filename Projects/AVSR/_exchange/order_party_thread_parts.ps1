$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 이 브리프를 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md .
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 메인과 보조 호스트를 잇는 **상시 혼실**의 부품. PD 가 A 안을 골랐다.
[앵커 — 이 색 · 굵기 · 결 그대로] Projects/AVSR/_exchange/ref/party_thread/anchor_A_strip.png · 게임 속 모습 Projects/AVSR/_exchange/ref/party_thread/anchor_A_scene.png
[중요 — 물결은 그리지 마라] 게임이 실을 마디 12 개짜리 줄로 계산해 휘게 하고, 이 그림을 마디마다 이어 붙인다. 그래서 그림은 **곧게 뻗은 실 한 토막**이어야 한다.
[부품] 전체 512 x 128 한 장에 가로 띠 2 개를 위아래로(칸마다 512 x 64, 비움 없음):
- 위 띠 = 실 토막: 칸 가운데를 왼쪽 끝부터 오른쪽 끝까지 **곧게** 가로지르는 실. 심은 흰 보랏빛 2~3 px, 둘레 은은한 보랏빛 번짐(#B98CFF) 4~6 px. **왼쪽 끝과 오른쪽 끝이 이어 붙었을 때 이음매가 안 보여야 한다**(양 끝 굵기 · 밝기가 같게, 끝이 옅어지지 않게). 아주 미세한 결(밝기 얼룩)은 좋다.
- 아래 띠 = 실 마디 반짝임 점 3 개: 작은 보랏빛 · 흰 심 반짝 점(지름 12~20 px)을 가로로 간격 두고 3 개.
- **바탕은 순수 검정(#000000)** — 빛이라 더하기로 그린다. 외곽선 · 테두리 · 판 금지. 도트 질감.
[저장] Projects/AVSR/_exchange/in/parts_party_thread.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
