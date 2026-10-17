$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」) — 홈 화면 부품. 먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md (혼합 규칙 · 5차).
PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_uiv5_home.png — 이 화풍 · 색 · 빛 그대로.
앵커(시안에서 자른 진열 칸): Projects/AVSR/_exchange/ref/ui_v5/home_showcase.png
그릴 것: **진열 칸 배경 그림 한 장** — 달빛 비치는 밤 도시 부두 거리(젖은 바닥 반사 · 가로등 · 철골 · 멀리 빌딩 불빛 · 둥근 달 · 왼쪽 「AVENGING SPIRIT RE:BORN」 낡은 간판은 있어도 됨).
**캐릭터(유령 · 갱스터) · 아이콘 · 등급 배지 · 단추 · 테두리 · 글자는 그리지 마라** — 게임이 그 위에 실제 캐릭터와 단추를 얹는다.
가운데 아래(캐릭터가 설 자리)는 비교적 비우고 바닥 반사만, 오른쪽 위(단추 자리)는 하늘 · 건물로 차분하게.
크기 1600 × 830, 꽉 찬 그림(투명 · 마젠타 없음). 도트 픽셀아트 질감.
[저장] Projects/AVSR/_exchange/in/uiv5_showcase_bg.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
