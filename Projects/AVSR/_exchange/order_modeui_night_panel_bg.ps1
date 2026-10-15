$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: Projects/AVSR/_exchange/in/mock_mode_N1.png — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: Projects/AVSR/_exchange/ref/mode_ui_parts/anchor_night_panel_bg.png — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] night_panel_bg — 밤 입장 패널 **바탕 그림** — 시안 N1 의 밤 풍경 그대로(보랏빛 큰 달 · 밤 도시 지붕 · 떠도는 푸른 유령들 · 아래 가로등 · 담 위 고양이 · A-2 상자). **UI(판 · 칸 · 버튼 · 글자)는 모두 빼고** 바탕만. 가운데는 판이 덮이므로 너무 밝지 않게. 화면 꽉 찬 한 장(세로 720x1280 비율).
[크기] 1440 x 2560 한 장(게임 크기 720x1280 의 2배 — 2x2 픽셀을 한 도트로). 부품이 칸을 꽉 채우되 잘리지 않게.
- 바탕까지 그림이다(투명 처리 안 함). 가장자리까지 꽉 채운다.
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_night_panel_bg.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_night_panel_bg.txt" $prompt
