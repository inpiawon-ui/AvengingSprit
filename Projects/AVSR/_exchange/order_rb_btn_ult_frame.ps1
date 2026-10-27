$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 0-2절(RE:BORN 아웃게임 — 매끈한 남색 판)을 읽는다. 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트(ImageDraw · arc · polygon · ellipse)로 도형을 조립해 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 인게임 「필살기」 버튼 테두리 한 장
■ 지금 버튼(둘 다 같은 동그라미 · 색만 다르다 — 그래서 필살기가 특별해 보이지 않는다):
  액티브 C:/won/UnityProject/AvengingSprit/Assets/BaseResource/ReBornKit/Hud/rb_btn_skill.png (파란 고리 — 이건 그대로 둔다)
  필살기 C:/won/UnityProject/AvengingSprit/Assets/BaseResource/ReBornKit/Hud/rb_btn_ult.png (노란 고리 — 이걸 새로 그린다)
■ 목표: 한눈에 「이건 필살기다」 — 액티브의 단순한 고리와 모양부터 다르게. 금빛 · 주황 테에 **위쪽 왕관 · 불꽃 장식(뾰족한 꼭대기 3갈래)** 과 **양옆 짧은 날개 · 가시 장식**,
  테 두께는 지금보다 두껍고 금속 질감 · 안쪽 테 한 줄. 화풍은 rb_btn_skill · rb_btn_ult 와 같은 매끈한 도트 UI(같은 게임 부품으로 보여야 한다).
■ 가운데는 **둥근 구멍**(스킬 그림이 들어간다) — 구멍 지름은 그림 폭의 약 58%, 그림 가운데보다 조금 아래(장식이 위에 있어서). 구멍 안은 분홍으로 비운다.
■ 크기 : 그림 260 x 260 · 가운데 구멍 지름 150 · 구멍 중심 (130, 140). 빛 번짐 · 반짝임 · 글자 없음(빛은 게임에서 따로 얹는다).
■ 바탕 : 그림 밖 · 구멍 안 전부 **순수 분홍 #FF00FF 단색**(나중에 뚫는다). 가장자리에 분홍 · 금색 섞인 번짐 없이 깔끔하게.
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rb_btn_ult_frame.png
끝나면 경로와 구멍 중심 · 지름을 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
