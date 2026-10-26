$env:OPENAI_API_sun = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(0-2절 RE:BORN 아웃게임 · 1절 도트 모양 + 광원). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 「망령의 밤」 입장 화면 위 배너 그림 · 요일별 7장 중 : 일 · 자유의 날
■ 화풍 정답(PD 가 통과시킨 월요일 견본) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/night_day_mon.png
  — 비 내린 밤거리 · 젖은 바닥 반사 · 보랏빛 거대 망령 · 해골 무리 · 1991 아케이드 16비트 도트 + 부드러운 광원 · 판 전체를 채운 구도.
  **결 · 도트 굵기 · 밝기 · 구도(가운데 힘주는 장면, 양옆 밤거리)는 견본 그대로**, 테마와 빛 색만 이 요일 것으로 바꾼다.
■ 이 요일의 테마 : 자유 — 모든 힘이 한꺼번에: 주먹 충격 · 칼날 궤적 · 총구 불꽃 · 마법 번개가 한 화면에 섞여 망령 무리와 맞붙는 축제 같은 밤. 여러 색 빛이 고르게
  게임 규칙: 이 요일엔 이 종류 호스트만 몸으로 나온다 — 그 느낌이 한눈에 읽혀야 한다. 다른 요일 그림과 확실히 달라 보여야 한다.
■ 글자 · 숫자 · 아이콘 · 게임 캐릭터(플레이어 호스트)는 그리지 않는다. 판 · 테두리 없음.
■ 크기 : 가로로 긴 띠 — 견본과 같은 비율(약 2.66 : 1, 예: 2048 x 768).
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/night_day_sun.png
끝나면 경로만 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
