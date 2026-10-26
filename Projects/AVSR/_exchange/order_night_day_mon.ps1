$env:OPENAI_API_mon = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(0-2절 RE:BORN 아웃게임 · 1절 도트 모양 + 광원). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 「망령의 밤」 입장 화면 위 배너 그림 · 요일별 7장 중 견본 1장 : 월 · 근거리의 날
■ 화풍 정답(지금 쓰는 망령의 밤 대표 그림) : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/ReBornKit/Art/rb_art_key_challenge_night.png
  — 비 내린 밤거리 · 젖은 바닥 반사 · 보랏빛 거대 망령 · 해골 무리 · 1991 아케이드 16비트 도트 + 부드러운 광원. **이 결 · 색 · 도트 굵기 그대로.**
■ 이 요일의 테마 : 근거리 — 주먹 · 칼 · 쇠사슬 · 몽둥이 같은 근접 무기가 망령 무리와 맞붙는 순간. 부딪히는 불꽃 · 휘두른 궤적. 따뜻한 주황 빛
  게임 규칙: 이 요일엔 이 종류 호스트만 몸으로 나온다 — 그 느낌이 한눈에 읽혀야 한다(요일마다 그림이 달라 보여야 한다).
■ 구도 : 가로로 긴 띠. **판 전체를 채운다** — 지금 그림처럼 한쪽만 그리고 반을 비우지 않는다. 가운데에 이 요일 테마의 힘주는 장면, 양옆으로 밤거리 · 망령 무리가 이어진다.
  글자 · 숫자 · 아이콘 · 게임 캐릭터(플레이어 호스트)는 그리지 않는다. 판 · 테두리 없음.
■ 크기 : 2064 x 496 (게임에서 1018 x 244 칸에 들어간다 — 위아래 여백 없이 꽉)
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/night_day_mon.png
끝나면 경로만 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
