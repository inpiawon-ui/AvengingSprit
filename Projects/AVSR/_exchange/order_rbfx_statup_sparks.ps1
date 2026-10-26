$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향 — 0-2절 「호스트 성장 강화 = 금 · 노랑, 용암처럼 약하게 차오름」). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 호스트 성장 「능력치 강화」 연출 부품 (가이드: C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/guide_statup_fx.md 「부품 표」)

■ 화풍 정답(PD 가 고른 시안) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_statup_free_idle.png — 「공격력」 칸 아래의 금빛 용암 줄 · 반짝이와 **같은 결 · 같은 색 · 같은 굵기**.
  확대 앵커 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/statup_mock/anchor_free_idle_lava_x3.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/statup_mock/anchor_free_idle_tile_x2.png
■ 빛 그림이다 — **바탕은 순수 검정 #000000**(게임이 더하기로 겹친다 — 검정은 안 보이고 밝은 곳만 빛난다). 분홍 · 회색 바탕 금지. 칸 판 · 글자 · 아이콘은 그리지 않는다(빛만).
■ 색 : 주황 · 금 · 노랑(#FFB83D · #FFDB52 · 심은 거의 흰 노랑). **초록 금지.**
■ 만들 것 : 위로 떠오르는 **반짝이 입자 3종** — ① 둥근 점 ② 작은 십자(+) ③ 마름모 — 시안의 반짝이처럼 도트 모양 + 둘레 살짝 번짐.
■ 판 : 칸 128 x 128 을 가로로 3칸(①②③ 차례), 칸 사이 검정 32 — 전체 448 x 128. 각 입자는 제 칸 가운데, 칸의 60% 안 크기.
■ 금지 : 큰 별 섬광(빛살 8갈래 이상) · 흐림만 있는 동그라미 · 외곽선.
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rbfx_statup_sparks.png
끝나면 경로만 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
