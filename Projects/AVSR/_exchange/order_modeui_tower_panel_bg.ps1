$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: Projects/AVSR/_exchange/in/mock_mode_T1.png — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: Projects/AVSR/_exchange/ref/mode_ui_parts/anchor_tower_panel_bg.png — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] tower_panel_bg — 탑 입장 패널 **바탕 그림** — 시안 T1 의 탑 안 풍경 그대로(석조 벽 · 왼쪽 위로 오르는 계단과 기둥 · 푸른 혼불 등 · 유령 깃발 · 위쪽 창과 달빛). **UI(판 · 칸 · 버튼 · 글자 · 사다리 칸)는 모두 빼고** 바탕만. 가운데 · 오른쪽은 판이 덮이므로 너무 밝지 않게. 화면 꽉 찬 한 장.
[크기] 1440 x 2560 한 장(게임 크기 720x1280 의 2배 — 2x2 픽셀을 한 도트로). 부품이 칸을 꽉 채우되 잘리지 않게.
- 바탕까지 그림이다(투명 처리 안 함). 가장자리까지 꽉 채운다.
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_tower_panel_bg.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_tower_panel_bg.txt" $prompt
