$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 아이콘 시트 — PD 가 통과시킨 시안 C 「원혼 회로」(Projects/AVSR/_exchange/in/mock_set_C_levelup.png ·
mock_set_C_altar.png · mock_set_C_shop.png)의 아이콘과 **같은 그림체 · 같은 도트 밀도**로. 상점 아이콘 앵커: Projects/AVSR/_exchange/in/anchor_c/shop_icons.png
[그림체] 시안 levelup 카드 그림처럼 — 어두운 바탕 없이(마젠타), 가운데 작은 유령(흰 몸 · 노란/청록 눈빛)이 효과의 주인공이 되고 효과(전기 · 방패 · 불꽃 등)가 원혼 빛으로 둘러싼다. 굵은 외곽선 도트, 160 px 로 줄여도 읽히게 큰 덩어리.
시트: 1024 x 1024, 순수 마젠타(#FF00FF) 바탕, 칸 512 x 512 가 2열 x 2줄(왼쪽 위부터 차례로). 그림은 칸 가운데, 칸의 80% 정도, 칸 밖으로 넘치지 않게.
- 칸 1 (card_c001): 감전 — 유령이 내뿜은 번개가 옆으로 튄다(청백 전기)
- 칸 2 (card_c002): 성장 가속 — 위로 솟는 금빛 결정 위의 유령(시안 그대로)
- 칸 3 (card_c003): 수호 방패 — 유령 둘레를 도는 청색 방패 셋(시안 그대로)
- 칸 4 (card_c004): 처형 — 유령이 든 큰 붉은 낫, 아래 금 간 해골
- 글자 · 숫자 금지. 마젠타/분홍 색을 그림에 쓰지 않는다. 별 · 표창 · 십자 반짝이 금지. 빈 칸은 마젠타.
[저장] Projects/AVSR/_exchange/in/setc_icons_cards1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
