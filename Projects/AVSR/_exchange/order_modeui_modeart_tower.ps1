$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: Projects/AVSR/_exchange/in/mock_mode_L.png — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: Projects/AVSR/_exchange/ref/mode_ui_parts/anchor_modeart_tower.png — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] modeart_tower — 로비 「망령의 탑」 **모드 칸 그림** — 앵커 그대로: 밤하늘로 솟은 유령 탑(층마다 창에 혼불 · 꼭대기 붉은 눈의 보스 그림자) · 왼쪽 위 달과 십자가 첨탑 · 날아다니는 작은 유령들. 글자 · 칩 · 빨간 점 · 테두리는 그리지 않는다(칸 틀 · 글자는 게임이 얹는다). 아래 3분의 1은 글자가 얹히므로 어둡고 단순하게. 사각 한 장.
[크기] 660 x 300 한 장(게임 크기 330x150 의 2배 — 2x2 픽셀을 한 도트로). 부품이 칸을 꽉 채우되 잘리지 않게.
- 바탕까지 그림이다(투명 처리 안 함). 가장자리까지 꽉 채운다.
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_modeart_tower.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_modeart_tower.txt" $prompt
