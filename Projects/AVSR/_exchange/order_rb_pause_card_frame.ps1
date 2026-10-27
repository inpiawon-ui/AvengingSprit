$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 일시정지 창 「지금 빌드」 칸의 **카드 칸 테두리 부품**.
[정답 시안] Projects/AVSR/_exchange/in/mock_pause_panel_free_peak.png — PD 가 고른 그림. 「지금 빌드」 카드 칸(RARE 수호 방패 · LEGENDARY 처형 · EPIC 번개 사슬 · RARE 영혼 탄환)의 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/pause_panel_mock/crop_card.png — 시안에서 잘라 3배로 키운 카드 칸 하나. 이 테 굵기 · 빛 · 위 등급 이름판 · 네 모서리 보석 모양을 따른다.
[부품] rb_pause_card_frame — 세로로 긴 카드 칸 테두리 4가지(등급마다 색만 다르다, 모양은 같다)
- 한 장에 **4칸 가로로**(칸마다 264 × 304, 전체 1056 × 304). 왼쪽부터 일반 = 은회 · 희귀 = 하늘색(시안 RARE 그대로) · 영웅 = 보라(시안 EPIC) · 전설 = 금색(시안 LEGENDARY).
- 칸마다: 밝은 테 + 바깥쪽 은은한 빛 + 네 모서리 작은 보석 + **위쪽 가운데 가로로 긴 등급 이름판**(칸 폭의 70% · 높이 12%, 속은 어두운 남색 · 테는 같은 색). 칸 속 바탕은 어두운 남색(시안처럼 가운데가 아주 조금 밝게).
- 이름판 안 글자(RARE 등) · 카드 그림 · 카드 이름 · Lv 배지는 **그리지 않는다**(게임이 넣는다).
- 칸 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 테 · 빛 · 보석에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_pause_card_frame.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
