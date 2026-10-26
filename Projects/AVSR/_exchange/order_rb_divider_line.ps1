$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
AVENGING SPIRIT: RE:BORN — 판 안 구분선(가로로 늘려 쓴다)
■ 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_handoff/reborn_ui_1023/03_리소스_및_시안/REBORN_UI_FINAL/Screens/tower.png 아래 판의 「도전」 버튼 줄과 「오늘의 소탕」 사이 가는 가로줄
■ 크기 1040 x 16 · 바탕 투명 · 가운데에 두께 2 의 가로줄 하나 · 색 #1D2331(짙은 남회색, 판 바탕보다 조금 밝다) · 양 끝까지 같은 굵기 · 빛 · 그림자 없음
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rb_divider_line.png
끝나면 경로만 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
