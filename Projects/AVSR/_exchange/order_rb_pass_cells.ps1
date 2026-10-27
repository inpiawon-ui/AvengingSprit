$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 시즌 패스 **보상 칸 바탕 판 2개를 한 장에**.
[정답 시안] Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/pass.png — PD 가 고른 그림. 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/pass_mock/crop_cell_idle.png (기본 칸) · Projects/AVSR/_exchange/ref/pass_mock/crop_cell_claim.png (받을 수 있는 칸). 판의 모양 · 색 · 테두리를 그대로 따른다. 판 안의 아이콘 · 글자는 그리지 않는다.
[부품] 한 장 1024 × 320, 두 칸(각 512 × 320)에 하나씩, 칸 가운데:
 1) rb_pass_cell       — 기본 칸: 가로 440 · 세로 240 비율의 둥근 네모(모서리 반지름 = 세로의 약 12%), 속은 아주 짙은 남색(위가 조금 밝음), 테두리 없음 또는 거의 안 보이는 아주 어두운 선
 2) rb_pass_cell_claim — 받을 수 있는 칸: 같은 모양 · 같은 크기, 속은 조금 더 밝은 남색(앵커 그대로), 둘레에 아주 가는 흐린 금빛 테두리 1 px
- 나중에 9분할로 늘려 쓴다 — 모서리 · 테두리가 고르게. 빛 · 무늬 · 그림자 판 없음.
- 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 그림에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_pass_cells.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
