$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 UI) — 인게임 왼쪽 시너지 칸 부품.
[앵커 — 이 모양 그대로] Projects/AVSR/_exchange/ref/party_ui_1029/anchor/a_syn_cell_mock.png (PD 가 확정한 시안 속 칸을 4배로 키운 것) · 전체 화면 Projects/AVSR/_exchange/in/mock_partyui_S1b.png 의 왼쪽 위 「무도가 2/3」 칸.
[부품] **육각 문장 뒤 금빛 번짐만** — 앵커의 육각 문장 둘레에 퍼진 주황 · 금빛 빛(#FFB347 → #FFD56A). 육각 문장 자체는 그리지 마라(빛만). 가운데가 가장 밝고 바깥으로 부드럽게 사라지는 육각형 결의 번짐.
크기 160 x 160 한 장. **바탕은 순수 검정(#000000)** — 빛이라 더하기로 그린다. 외곽선 · 판 금지.
[저장] Projects/AVSR/_exchange/in/parts_syn_glow.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
