$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: Projects/AVSR/_exchange/in/mock_mode_N1.png — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: Projects/AVSR/_exchange/ref/mode_ui_parts/anchor_night_today_card.png — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] night_today_card — 밤 입장 패널 **오늘 카드 틀**(굵은 금테 · 모서리 볼트) — 앵커에서 글자 · 초상 · 안쪽 두 칸 · 오른쪽 위 그림을 모두 뺀 빈 틀. 9분할로 늘려 쓴다 — 장식(볼트 · 모서리 · 끝 장식)은 네 모서리와 양 끝에만, 가운데 줄과 가운데 칸은 무늬 없이 고르게(늘려도 깨지지 않게). 모서리 장식은 가장자리에서 34 px 안에 모은다.
[크기] 1380 x 890 한 장(게임 크기 690x445 의 2배 — 2x2 픽셀을 한 도트로). 부품이 칸을 꽉 채우되 잘리지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 부품 바깥은 전부 마젠타. 그림에 마젠타 · 분홍을 쓰지 않는다. 부품 가장자리에 마젠타가 섞인 번짐을 만들지 않는다.
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_night_today_card.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_night_today_card.txt" $prompt
