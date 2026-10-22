$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 기능만 써라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 톱날 원숭이가 던지는 **톱날 탄** 그림 (4장 · 돌아가는 모습)

생김새 정답 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/trash_v3/ref_saw_monkey_concept.png 의 등에 멘 녹슨 톱날 원반과 같은 것.
지금 게임의 탄 크기 · 화풍 참조 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/shot_spider_1.png (48 x 48 칸)

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/shot_saw.png — 가로로 긴 판(768 x 192 쯤, 48 x 48 칸 네 개를 4배), 배경 **마젠타 #FF00FF 단색**
  · 네 칸 모두 위에서 내려다본 둥근 톱날 하나 — 칸마다 45°씩 더 돌아간 모습(이어 돌리면 회전으로 보이게).
  · 칸의 80% 를 채우게 가운데. 빛 · 잔상 · 속도선을 그리지 않는다(게임이 돌리고 날린다).
  · 1 px 짙은 외곽선 · 녹슨 쇠색 6~8색 · 2단 명암.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
