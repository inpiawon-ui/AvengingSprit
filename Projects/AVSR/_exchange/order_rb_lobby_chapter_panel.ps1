$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 로비 전투 탭 **UI 부품**.
[정답 시안] Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/lobby.png — PD 가 고른 그림. 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/lobby_battle_mock/crop_chapter.png — 시안에서 잘라 낸 챕터 칸. 이 판의 모양 · 색 · 테두리를 따른다.
[부품] rb_lobby_chapter_panel — 챕터 칸 **바탕 판만**
- 한 장 **512 × 128**. 가로로 긴 둥근 사각형 판 하나(모서리 반지름은 판 높이의 약 15%, 앵커와 같은 비율). 나중에 9분할로 늘려 쓴다 — 모서리 · 테두리가 고르게.
- 속은 앵커처럼 짙은 남색(위가 아주 조금 밝음), 둘레에 **아주 가는 어두운 금빛 테두리 1~2 px**(앵커 그대로). 빛 · 무늬 · 장식 없음.
- 위쪽 하늘색 한 줄 · 글자 · 칸 막대 · 버튼은 그리지 않는다(따로 얹는다).
- 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 그림에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_lobby_chapter_panel.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
