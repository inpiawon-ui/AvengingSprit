$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 설정 창 **켜기/끄기 토글 부품 3개를 한 장에**.
[정답 시안] Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/settings.png — PD 가 고른 그림. 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/settings_mock/crop_toggle_on.png — 시안에서 잘라 낸 켜진 토글. 이 모양 · 색 · 비율을 그대로 따른다.
[부품] 한 장 768 × 256, 세 칸(각 256 × 256)에 하나씩, 칸 가운데:
 1) rb_toggle_on  — 켜진 트랙만: 가로 280 · 세로 152 비율(1.84 : 1)의 둥근 알약 모양, 속은 청록(앵커 그대로), 둘레에 밝은 하늘색 테두리. 칸 폭에 맞게 줄여 그린다(가로 240 · 세로 130 정도)
 2) rb_toggle_off — 꺼진 트랙만: 같은 모양 · 같은 크기, 속은 어두운 남회색, 테두리는 흐린 회청색
 3) rb_toggle_knob — 손잡이만: **정원**(지름 = 트랙 세로의 약 80%), 흰색에 아주 옅은 회색 결(앵커처럼)
- 손잡이는 트랙에 그리지 않는다(따로 얹는다). 글자 · 그림자 판 없음.
- 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 그림에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_toggle_parts.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
