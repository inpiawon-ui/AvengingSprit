$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 로비 전투 탭 **UI 부품**.
[정답 시안] Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/lobby.png — PD 가 고른 그림. 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/lobby_battle_mock/crop_tab_selected.png — 시안에서 잘라 낸 것. 이 모양 · 색 · 크기 비율을 따른다.
[부품] rb_tab_selected — 아래 탭 막대에서 **고른 탭 뒤 반원 받침**
- 한 장 **256 × 192**. 아래쪽이 탭 막대에 붙고 위쪽이 둥글게 솟은 반원(위에서 본 돔) 하나 — 폭은 칸의 70%, 높이는 칸의 90%.
- 속은 탭 막대보다 아주 조금 밝은 어두운 남색, 둥근 윗테두리에만 아주 옅은 금빛 선 1 px(시안 「전투」 뒤 모양 그대로). 아이콘 · 글자는 그리지 않는다.
- 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 그림에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_tab_selected.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
