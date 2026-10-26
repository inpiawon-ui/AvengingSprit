$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 0-2절(RE:BORN 아웃게임 — 매끈한 남색 판)을 읽는다. 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트(ImageDraw · arc · polygon · ellipse)로 도형을 조립해 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 레벨업 카드 · 결과 화면 UI 판 7개(9분할로 늘려 쓴다 · 안의 아이콘 · 글자는 그리지 않는 빈 판)
■ 정답(시안) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/levelup-selected.png
              C:/won/UnityProject/AvengingSprit/Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/result-story.png
■ 화풍 기준(이미 통과한 우리 판 — 이것과 같은 매끈함 · 같은 모서리 처리) : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/ReBornKit/Components/rb_panel_base.png
■ 바탕 : 판 밖은 전부 **순수 분홍 #FF00FF 단색**(나중에 뚫는다). 판끼리 최소 60 px 띄운다. 전체 1600 x 900.
■ 판 7개(크기 · 색은 시안 CSS 에서 잰 값 — 숫자 그대로):
  ① 카드 몸통  400 x 300 · 모서리 둥글기 24 · 위 #172038 → 아래 #0B1222 세로 그러데이션 · 아래로 8 px 단단한 그림자 띠 #050A14(흐림 없음) — 전체 높이 308
  ② 왼쪽 띠    ①과 같은 400 x 300 · 모서리 24 자리에서 **왼쪽 안쪽 8 px 띠만 흰색 #FFFFFF**(모서리 곡선을 따라 휘어짐) · 나머지는 분홍(비움). 색은 게임에서 입힌다
  ③ 고른 테    ①을 둘러싸는 **테 한 줄만** · 굵기 6 px · #ECCD6B · 바깥 크기 408 x 308 · 모서리 28 · 안쪽은 분홍(비움) · 빛 번짐 없음
  ④ 아이콘 칸  240 x 240 · 모서리 18 · #090E1C 단색(그러데이션 · 테 없음)
  ⑤ 작은 꼬리표 160 x 60 · 모서리 10 · #080E1F 단색
  ⑥ 대기 버튼  400 x 160 · 모서리 20 · 바탕 #262C46 단색 · 안쪽 3 px 테 #4A5272(차분하게) · 그림자 없음
  ⑦ 보상 칸    325 x 400 · 모서리 20 · 바탕 #090F20 단색 · 맨 위 안쪽 1 px 가는 선 #3A3631 하나만
■ 빛 · 반짝임 · 질감 · 장식 · 글자 없음. 테두리 가장자리는 깔끔하게(분홍과 맞닿는 곳에 다른 색 번짐 없이).
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rb_levelup_result_parts.png
끝나면 경로와 판 7개의 대략 자리(왼 · 위 · 오른 · 아래 px)를 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
