$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: Projects/AVSR/_exchange/in/mock_mode_T1.png — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: Projects/AVSR/_exchange/ref/mode_ui_parts/anchor_tower_ladder.png — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] tower_ladder_cell — 탑 입장 패널 왼쪽 **층 사다리 칸 4종**(한 칸에 하나, 왼쪽 위부터) — 앵커의 칸 모양 그대로, 글자 · ✓ · 「도전」 · 보스 초상을 뺀 빈 칸. 1 지난 층(어둡고 차분한 남색 · 얇은 테두리, 앵커 12층 · 11층) · 2 앞 층(잠긴 느낌의 회남색, 앵커 14층) · 3 지금 층(금테 · 안쪽 은은한 금빛, 앵커 13층 — 오른쪽 「도전」 표식 자리는 비운다) · 4 보스 층(어두운 판에 붉은 기운 테두리, 왼쪽에 초상 넣을 사각 자리, 앵커 15층). 칸 사이 계단 · 기둥은 그리지 않는다(바탕 그림에 있다). 글자 · 숫자 · 아이콘 · 초상 · 보석은 그리지 않는다 — 그 자리는 판 바탕 그대로 비워 둔다(게임이 위에 얹는다).
[크기] 칸 540 x 192(게임 크기 270x96 의 2배 — 2x2 픽셀을 한 도트로)가 2열 x 2줄, 시트 전체 1080 x 384. 칸마다 하나, 왼쪽 위부터 설명 순서. 칸 경계를 넘지 않게, 칸 사이는 마젠타.
- 바탕은 순수 마젠타(#FF00FF) — 부품 바깥은 전부 마젠타. 그림에 마젠타 · 분홍을 쓰지 않는다. 부품 가장자리에 마젠타가 섞인 번짐을 만들지 않는다.
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_tower_ladder_cell.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_tower_ladder_cell.txt" $prompt
