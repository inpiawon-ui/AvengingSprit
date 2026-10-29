$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 UI) — 인게임 왼쪽 시너지 칸 부품.
[앵커 — 이 모양 그대로] Projects/AVSR/_exchange/ref/party_ui_1029/anchor/a_syn_cell_mock.png (PD 가 확정한 시안 속 칸을 4배로 키운 것) · 전체 화면 Projects/AVSR/_exchange/in/mock_partyui_S1b.png 의 왼쪽 위 「무도가 2/3」 칸.
[부품] **칸 상자만** — 앵커의 바깥 상자(짙은 남색 #0B1530 안쪽 · 얇은 하늘색 #3FD6FF 테두리 2~3 px · 둥근 모서리 · 테두리 바깥 은은한 하늘색 번짐 아주 약하게). **안의 육각 문장 · 글자는 그리지 마라**(비워 둔다 — 코드가 넣는다).
크기 186 x 280(앵커 칸의 2배) 한 장. 9분할로 늘려 쓸 수 있게 네 모서리 모양이 같고 변은 곧게. 바탕은 분홍(#FF00FF) — 상자 밖은 전부 분홍. 외곽 그림자 판 금지.
[저장] Projects/AVSR/_exchange/in/parts_syn_cellbox.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
