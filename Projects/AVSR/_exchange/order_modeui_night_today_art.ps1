$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: Projects/AVSR/_exchange/in/mock_mode_N1.png — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: Projects/AVSR/_exchange/ref/mode_ui_parts/anchor_night_today_card.png — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] night_today_art — 밤 오늘 카드 오른쪽 위에 깔리는 **그림** — 보랏빛 달 · 밤 도시 · 떠도는 푸른 유령 · 몰려오는 적 떼(앵커 카드 오른쪽 위 그림 그대로). 왼쪽 3분의 1은 남색으로 자연스럽게 사라진다(글자가 얹힌다). 글자 금지. 사각 한 장, 바탕 마젠타 아님.
[크기] 680 x 380 한 장(게임 크기 340x190 의 2배 — 2x2 픽셀을 한 도트로). 부품이 칸을 꽉 채우되 잘리지 않게.
- 바탕까지 그림이다(투명 처리 안 함). 가장자리까지 꽉 채운다.
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_night_today_art.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_night_today_art.txt" $prompt
