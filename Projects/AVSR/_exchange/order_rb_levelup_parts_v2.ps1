$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 0-2절(RE:BORN 아웃게임 — 매끈한 남색 판)을 읽는다. 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트(ImageDraw · arc · polygon · ellipse)로 도형을 조립해 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 레벨업 카드 부품 2개 다시 그리기(1차 납품 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rb_levelup_result_parts.png 의 ②왼쪽 띠 · ③고른 테가 너무 가늘었다)
■ 정답(시안) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/levelup-selected.png — 맨 위 카드(금색 테 · 왼쪽 보라 띠)
■ 바탕 : 판 밖 · 판 안 빈 곳 전부 **순수 분홍 #FF00FF 단색**. 전체 1000 x 420. 두 부품 사이 100 px 띄움.
■ ② 왼쪽 띠 : 400 x 300 카드 자리(모서리 둥글기 24)의 **왼쪽 안쪽 띠 하나만** · 색 #FFFFFF 꽉 찬 흰색 · **가로 굵기 정확히 8 px**(1차는 3 px 였다 — 반드시 8 px) · 위 · 아래 끝은 모서리 24 곡선을 따라 휘어 가늘어짐 · 띠 오른쪽(카드 안쪽)은 분홍. 자리 : 왼 40 · 위 60 · 카드 오른쪽 끝 440 · 아래 360
■ ③ 고른 테 : 바깥 408 x 308 · 모서리 둥글기 28 · **테 굵기 정확히 6 px**(1차는 4 px 였다 — 반드시 6 px) · 색 #ECCD6B 단색 · 안쪽은 분홍(안쪽 가장자리에 분홍 · 금색이 섞인 번짐 없이 딱 끊어지게) · 빛 번짐 · 그림자 없음. 자리 : 왼 540 · 위 56 · 오른 948 · 아래 364
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rb_levelup_parts_v2.png
끝나면 경로와 두 부품 자리(왼 · 위 · 오른 · 아래 px)를 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
