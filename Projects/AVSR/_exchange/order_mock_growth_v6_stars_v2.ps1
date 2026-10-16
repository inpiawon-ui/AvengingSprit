$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임).
[장면] 「성급 표기판 v2」 — 지난 표기판 Projects/AVSR/_exchange/in/mock_growth_v6_stars.png 와 같은 틀 · 같은 화풍으로 규칙만 바꿔 다시 그린다.
PD 말: 「큰 별 하나를 5성으로 · 5성부터 금별 · 금별은 좀 큰 별이면 좋겠어 · 별 쪼개는 건 별로」.
새 규칙(정확히 이대로):
- 1~4성: **작은 은(흰)별** 1~4개.
- 5성: 작은 별이 사라지고 **큰 금별 1개**.
- 6~9성: **큰 금별 1개 + 작은 은별 1~4개**(큰 금별이 왼쪽).
- 10성: **큰 금별 2개**.
- 큰 금별은 작은 별의 약 1.6배 크기, 금빛에 반짝임 · 테두리를 줘 한눈에 등급이 올라간 느낌. 작은 은별은 담백하게.
- 별 옆에 항상 「N성」 숫자.
보드 구성: 위 = 1성~10성 10줄(2열 × 5) / 가운데 = 작은 호스트 카드 셋(R · SR · SSR 배지, 각각 3성 · 7성 · 10성 예시, 이름 갱스터 · 닌자 · 아마존 정예) + 큰 몸 칸(「성급 7/10」 + 큰 금별 1 + 작은 은별 2) / 아래 = 성급 이정표 띠(4 · 7 · 10성 보조↑, 5성 큰 금별 표식, 6성 각성 옵션, 10성 각성 강화 ×2).
제목 「성급 표기 v2 — 5성마다 큰 금별」.
[저장] Projects/AVSR/_exchange/in/mock_growth_v6_stars_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
