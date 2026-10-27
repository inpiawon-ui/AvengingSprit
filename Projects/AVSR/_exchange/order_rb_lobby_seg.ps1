$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 로비 전투 탭 **UI 부품**.
[정답 시안] Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/lobby.png — PD 가 고른 그림. 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/lobby_battle_mock/crop_gauge.png — 시안에서 잘라 낸 것. 이 모양 · 색 · 크기 비율을 따른다.
[부품] rb_lobby_seg — 챕터 칸 방 진행 막대의 칸 하나(4가지)
- 한 장에 **4칸 가로로**(칸마다 128 × 48, 전체 512 × 48). 칸마다 가로로 긴 평평한 둥근 네모 하나(칸의 90% 안 · 모서리 둥글기 작게).
- 1 = 깬 칸: 평평한 하늘색(시안 그대로 `#3BBFCB` 쯤) · 2 = 남은 칸: 어두운 남회색 · 3 = 보스 칸: 어두운 속 + 빨간 테 2 px(시안 맨 끝 칸) · 4 = 중간보스 칸: 어두운 속 + 금색 테 2 px(보스 칸과 같은 모양, 테만 금색).
- 빛 번짐 · 그러데이션 · 그림자 없이 평평하게(시안처럼).
- 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 그림에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_lobby_seg.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
