$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 새 모드 UI 부품.
[정답] 통과한 완성 시안: Projects/AVSR/_exchange/in/mock_mode_N3.png — 이 화면에 들어간 모습이 정답이다.
[앵커] 이 부품 자리를 시안에서 잘라 둔 그림: Projects/AVSR/_exchange/ref/mode_ui_parts/anchor_bighost_plate.png — **이 모양 · 색 · 재질 · 도트 그대로** 옮긴다(새로 디자인하지 마라). 더 정교해지는 것은 좋다.
[부품] bighost_plate — 밤 인게임 **대형 호스트 머리 위 이름판** — 앵커 그대로: 위 「대형 · 설녀」 이름 칸(어두운 남색 판 · 은빛 테두리) + 아래 막대 두 줄의 **빈 홈**(체력 막대 홈 · 「경직」 막대 홈, 홈만 — 채움 색 · 글자 · % 숫자는 빼고). 게임이 이름 글자와 막대 채움을 얹는다. 글자 · 숫자 · 아이콘 · 초상 · 보석은 그리지 않는다 — 그 자리는 판 바탕 그대로 비워 둔다(게임이 위에 얹는다).
[크기] 408 x 184 한 장(게임 크기 204x92 의 2배). 부품이 칸을 꽉 채우되 잘리지 않게.
- 바탕은 순수 마젠타(#FF00FF) — 부품 바깥은 전부 마젠타. 그림에 마젠타 · 분홍을 쓰지 않는다. 부품 가장자리에 마젠타가 섞인 번짐을 만들지 않는다.
- 별 · 십자 반짝이를 새로 뿌리지 않는다. 앵커에 없는 장식을 더하지 않는다.
[저장] Projects/AVSR/_exchange/in/modeui_bighost_plate.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check -o "Projects/AVSR/_exchange/log_modeui_bighost_plate.txt" $prompt
