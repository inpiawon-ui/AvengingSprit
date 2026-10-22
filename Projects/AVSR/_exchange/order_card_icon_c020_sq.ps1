$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 레벨업 카드 아이콘 한 장 — 「상태이상 번짐 — 상태이상을 걸 때 옆 적에게도 옮긴다」.
[화풍 앵커] Projects/AVSR/_exchange/in/card_icon_c014.png (관통탄 — 화풍 · 외곽선 · 색 밀도 기준) , Assets/BaseResource/Card/card_c008.png (추가 발사)
[구도 앵커 — 정사각 구도] Assets/BaseResource/Card/card_c001.png (감전) , Assets/BaseResource/Card/card_c004.png (처형) , Assets/BaseResource/Card/card_c010.png (궁지) — 이 셋처럼 그림 덩어리가 정사각으로 칸을 꽉 채운다
- 그림: 해골 셋을 삼각형으로(위 하나 · 아래 둘). 위 해골에서 얼음(파랑) · 불(주황) · 저주(보라) 기운이 굵은 화살처럼 아래 두 해골에게 옮아간다.
- **구도는 정사각이다.** 그림 덩어리의 가로 : 세로가 1 : 1 ~ 1.3 : 1, 위아래도 칸의 85% 이상을 채운다.
  옆으로 늘어놓는 「유령 → 탄 → 적」 구도는 금지 — 지난 판이 칸 높이의 절반만 채워 반려됐다.
- 유령 · 해골은 화풍 앵커와 같은 모양(유령: 흰 몸 · 큰 검은 눈 · 짧은 꼬리). 유령은 넣더라도 그림의 1/4 이하로 작게.
- 글자 · 테두리 · 판 없이 아이콘 그림만.
- 바탕 **분홍(#FF00FF)** 단색 — 게임에서 뚫는다. 투명 바탕으로 나오면 그대로 저장한다. 그림 가장자리에 분홍이 섞인 흐림 금지.
- 크기 256 x 256.
[저장] Projects/AVSR/_exchange/in/card_icon_c020_sq.png
"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
