$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향 — 0-2절 「호스트 성장 강화 = 금 · 노랑, 용암처럼 약하게 차오름」). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 호스트 성장 「능력치 강화」 연출 부품 (가이드: C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/guide_statup_fx.md 「부품 표」)

■ 화풍 정답(PD 가 고른 시안) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_statup_free_idle.png — 「공격력」 칸 아래의 금빛 용암 줄 · 반짝이와 **같은 결 · 같은 색 · 같은 굵기**.
  확대 앵커 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/statup_mock/anchor_free_idle_lava_x3.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/statup_mock/anchor_free_idle_tile_x2.png
■ 빛 그림이다 — **바탕은 순수 검정 #000000**(게임이 더하기로 겹친다 — 검정은 안 보이고 밝은 곳만 빛난다). 분홍 · 회색 바탕 금지. 칸 판 · 글자 · 아이콘은 그리지 않는다(빛만).
■ 색 : 주황 · 금 · 노랑(#FFB83D · #FFDB52 · 심은 거의 흰 노랑). **초록 금지.**
■ 만들 것 : 용암 줄 **뒤에 퍼지는 부드러운 금빛 번짐**(광원) 한 장 — 아래 변이 가장 밝고 위로 갈수록 부드럽게 사라진다. 가운데가 조금 더 밝다. 좌우 끝도 부드럽게 사라진다.
  위쪽 60% 는 거의 검정(글자 자리를 밝히지 않는다). 딱딱한 가장자리 · 도트 계단 없이 매끄러운 번짐.
■ 크기 : 1360 x 320 한 장.
■ 금지 : 줄무늬 · 별 · 외곽선 · 판.
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rbfx_statup_glow.png
끝나면 경로만 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
