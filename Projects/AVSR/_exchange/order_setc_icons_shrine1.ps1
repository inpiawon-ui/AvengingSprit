$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 아이콘 시트 — PD 가 통과시킨 시안 C 「원혼 회로」(Projects/AVSR/_exchange/in/mock_set_C_levelup.png ·
mock_set_C_altar.png · mock_set_C_shop.png)의 아이콘과 **같은 그림체 · 같은 도트 밀도**로. 상점 아이콘 앵커: Projects/AVSR/_exchange/in/anchor_c/shop_icons.png
[그림체] 시안 altar 선택 칸의 아이콘처럼 — 청록 원혼 빛 선으로 그린 상징 하나(단색 청록 + 흰 하이라이트). 64 px 로 줄여도 읽히게 굵고 단순하게.
시트: 1024 x 1024, 순수 마젠타(#FF00FF) 바탕, 칸 512 x 512 가 2열 x 2줄(왼쪽 위부터 차례로). 그림은 칸 가운데, 칸의 80% 정도, 칸 밖으로 넘치지 않게.
- 칸 1 (shrine_full_heal): 몸을 아문다 — 십자 없는 둥근 붕대 · 하트
- 칸 2 (shrine_soul_heal): 영혼을 채운다 — 물방울 안의 작은 유령
- 칸 3 (shrine_max_hp): 그릇을 넓힌다 — 근육 펴는 몸 실루엣(시안 그대로)
- 칸 4 (shrine_atk): 힘을 받는다 — 위로 솟는 주먹
- 글자 · 숫자 금지. 마젠타/분홍 색을 그림에 쓰지 않는다. 별 · 표창 · 십자 반짝이 금지. 빈 칸은 마젠타.
[저장] Projects/AVSR/_exchange/in/setc_icons_shrine1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
