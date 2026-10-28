$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 10챕터 「최후의 옥상」 전투 방 **오브젝트 종류 시안**.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다.
[바탕] Projects/AVSR/_exchange/in/mock_simple_var_ch10.png — 지난 변형 시안. **바닥 · 벽 · 색 · HUD · 버튼 · 캐릭터 · 적 · 적 탄은 그대로** 둔다.
[PD 말] 「오브젝트들 종류가 중요하지 배경은 약간만 달라도 돼 — 여러 가지 오브젝트들이 다양하게 표현되면 된다」.
[바꿀 것] 장애물 자리(칸)는 그대로 두고, 그 자리에 서는 물건을 **서로 다른 종류 5가지**로 바꿔 섞는다: 물탱크 · 안테나 철탑 받침 · 헬기 착륙 표시 판(바닥) · 쓰러진 광고판 · 환기 덕트 묶음.
- 한 종류가 방의 절반을 넘지 않게 고루 섞는다. 칸 하나 = 덩어리 블록 하나(윗면 밝게 + 앞면 어둡게), 지저분한 잔 부품 · 얼룩 금지.
- 이 챕터만의 물건으로 보이게 — 1~6챕터 물건(상자 · 철판 · 드럼통 · 자판기 · 캡슐 · 환풍기)을 그대로 다시 쓰지 마라.
- 그림 오른쪽 아래 여백 없이 꽉 채우고 메모는 넣지 마라. 크기 720 x 1280.
[저장] Projects/AVSR/_exchange/in/mock_simple_obj_ch10.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
