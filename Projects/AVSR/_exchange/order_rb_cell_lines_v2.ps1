$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 0-2절(RE:BORN 아웃게임 — 매끈한 남색 판)을 읽는다. 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 보상 칸 판 두 장(9분할로 늘려 쓴다)
■ 정답(시안 그대로) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/chl_fix/anchor_cell_gold.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/chl_fix/anchor_cell_teal.png
  — 짙은 남색 바탕 · 둥근 모서리 · **얇은 테 한 줄**(왼쪽 = 금갈색, 오른쪽 = 청록). 안의 아이콘 · 숫자 · 글자는 그리지 않는다(빈 판).
■ 판 : 가로로 두 칸 — ① 금갈색 테 ② 청록 테. 칸마다 240 x 240, 칸 사이 투명 40 · 전체 520 x 240. 바탕(칸 밖)은 투명.
  모서리 둥글기 · 테 굵기 · 바탕 밝기 · 은은한 위아래 그러데이션을 앵커와 똑같이. 빛 번짐 · 그림자 · 장식 없음.
■ 색(시안에서 잰 값 — 1차 납품은 테가 너무 밝았다: 금 #F4AF26 · 청록 #01D7FC 였다)
  ① 금갈색 테 #8F6931(밝기 56%) · 칸 가운데 바탕 #2E2C33  ② 청록 테 #25728B(밝기 55%) · 칸 가운데 바탕 #182E49. 테는 **어둡고 차분하게**, 빛나지 않게.
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rb_cell_lines_v2.png
끝나면 경로만 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
