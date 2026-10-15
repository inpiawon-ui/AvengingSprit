$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: Projects/AVSR/_exchange/in/mock_mode_T1.png — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: Projects/AVSR/_exchange/ref/mode_ui_parts/anchor_tower_ladder.png — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] tower_badge_go — 지금 층 칸 오른쪽의 작은 금 **「도전」 표식**에서 글자만 뺀 빈 알약 — 앵커처럼 **입체감**: 짙은 갈색 바깥 테두리 · 밝은 금 윗면에서 아래로 진해지는 명암 · 위쪽 가는 하이라이트 줄 · 아래쪽 그림자 줄. 평평한 단색 알약 금지(1차 납품이 평평해서 다시 발주). 글자 · 숫자 · 아이콘 · 초상 · 보석은 그리지 않는다 — 그 자리는 판 바탕 그대로 비워 둔다(게임이 위에 얹는다).
[크기] 312 x 160 한 장(게임 크기 78x40 의 4배). 부품이 칸을 꽉 채우되 잘리지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 부품 바깥은 전부 마젠타. 그림에 마젠타 · 분홍을 쓰지 않는다. 부품 가장자리에 마젠타가 섞인 번짐을 만들지 않는다.
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_tower_badge_go.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_tower_badge_go.txt" $prompt
