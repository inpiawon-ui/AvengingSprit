$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 이 브리프를 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md . 그리고 Projects/AVSR/_exchange/guide_party_possess_fx.md 를 끝까지 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션). 화풍 · 캐릭터 · 바닥은 Projects/AVSR/_exchange/in/mock_partyui_S1b.png 그대로 따른다.
[형식] **4컷 스토리보드 한 장**, 1440 x 1440, 가로 2 x 세로 2 칸(왼위 → 오른위 → 왼아래 → 오른아래 순서). 칸마다 필드 일부를 확대한 장면(캐릭터가 크게 보이게), 칸 왼위에 작은 번호(1~4)와 시각(예: 0.0초). 칸 사이 얇은 검은 선. 설명 글 문단은 넣지 마라.
[공통] 메인 호스트 = 금빛 테두리 기운의 「슬러거(야구 소년, 빨간 모자 · 방망이)」. 유령 = 통통한 흰 몸 · 짧은 꼬리 · 큰 입. 보조 = 몸 전체가 보랏빛(알파 70%) + 발밑 보랏빛 혼고리. 적 호스트 = 원작 도트 캐릭터 「영매(보라 옷 · 모자 여자)」. 주변에 잡몹 2~3. 전투는 멈추지 않는다 · 필드를 어둡게 하지 않는다. 캐릭터 자세는 도트 원작 느낌 그대로.
[장면 — 보조 빙의 자유안] 너는 아트 디렉터다. 「메인으로 싸우는 중에 적 호스트 하나를 내 보조(동료 혼)로 만든다」를 가장 멋지고 빠르게 읽히게 하는 연출을 네가 디자인해 4컷으로. 가이드의 A · B 안과 달라도 된다. 결과는 보조 = 보랏빛 + 발밑 혼고리.
[저장] Projects/AVSR/_exchange/in/mock_possessfx_subFree.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
