$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 9챕터 「망령의 거리」(불이 꺼진 거리에 망자가 걷는다) 전투 방 배경 시안 = **3챕터 무대의 변형**.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(특히 「새 챕터 · 새 무대 규칙」 다섯 줄).
[바탕] Projects/AVSR/_exchange/in/mock_simple2_ch3.png — PD 가 통과시킨 3챕터 시안. **방 배치 · 장애물 자리 · 블록 모양 · 화풍 · HUD · 버튼 · 캐릭터 · 적은 이 그림 그대로** 두고 아래 두 가지만 바꾼다.
1. 대표 색: 푸른 회색(3챕터 보라 네온을 끈 밤). 3챕터와 한눈에 다른 챕터로 보여야 하고, 다른 챕터(1~6챕터 남색 · 올리브 · 보라 네온 · 노을 주황 · 청록 · 녹 주황)와도 겹치지 않게. 바닥은 중간보다 어둡게, 채도 낮게, 잔무늬 없이.
2. 물건 하나: 네온 테두리를 꺼진 네온(어두운 유리관)으로 · 자판기 일부를 묘비 같은 표지판 블록으로. 칸 하나 = 블록 하나 규칙은 그대로.
[적 탄] 바탕처럼 6~10발. 가장 밝고 또렷한 테두리, 색은 새 바닥 색의 반대편 — 바닥에 묻히지 않게.
- 테마 유지(유령 · 빙의 · 80~90년대 범죄 도시). 장애물 밑 단색 그림자 판 금지. 지저분한 잔 부품 · 얼룩 금지. 크기 720 x 1280, 메모 넣지 마라.
[저장] Projects/AVSR/_exchange/in/mock_simple_var_ch9.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
