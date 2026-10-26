$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향 — 0-2절 「호스트 성장 강화 = 금 · 노랑, 용암처럼 약하게 차오름」). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 호스트 성장 「능력치 강화」 연출 부품 (가이드: C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/guide_statup_fx.md 「부품 표」)

■ 화풍 정답(PD 가 고른 시안) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_statup_free_idle.png — 「공격력」 칸 아래의 금빛 용암 줄 · 반짝이와 **같은 결 · 같은 색 · 같은 굵기**.
  확대 앵커 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/statup_mock/anchor_free_idle_lava_x3.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/statup_mock/anchor_free_idle_tile_x2.png
■ 빛 그림이다 — **바탕은 순수 검정 #000000**(게임이 더하기로 겹친다 — 검정은 안 보이고 밝은 곳만 빛난다). 분홍 · 회색 바탕 금지. 칸 판 · 글자 · 아이콘은 그리지 않는다(빛만).
■ 색 : 주황 · 금 · 노랑(#FFB83D · #FFDB52 · 심은 거의 흰 노랑). **초록 금지.**
■ 만들 것 : 칸 아래 **도트 용암 물결 줄** 반복 8장 — 시안처럼 낮은 산 모양 물결이 칸 바닥을 따라 이어지고, 물결 위쪽이 밝은 금 테두리, 아래로 갈수록 진한 주황 · 부드러운 번짐.
  8장이 차례로 **천천히 일렁이며** 이어진다(8장 다음 1장으로 끊김 없이). 물결 산은 3~5개 · 높이 들쭉날쭉 · 한 장마다 조금씩 오르내리고 옆으로 흐른다.
  좌우 끝은 칸 모서리 둥글기를 따라 자연스럽게 사라진다(뚝 끊긴 네모 금지).
■ 판 : 한 줄 1360 x 120(물결은 줄 아래쪽 절반 안에) 을 **세로로 8줄**, 줄 사이 검정 16 — 전체 1360 x 1072.
■ 금지 : 가는 빛줄기 하나(레이저) · 별 섬광 · 칸 테두리 전체를 두른 빛 · 외곽선 · 판.
■ 저장 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/rbfx_statup_lava.png
끝나면 경로만 알려라.
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
