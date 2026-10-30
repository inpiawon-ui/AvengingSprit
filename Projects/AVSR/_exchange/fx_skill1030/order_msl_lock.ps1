$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(색의 뜻 — 내 공격 = 주황 · 금).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주.
코만도(미사일) 필살기 「집중 포격」의 표적 조준 표시.
[정답 그림] 통과 시안 Projects/AVSR/_exchange/mock1030_skill/mock_missile_free1.png 의 표적 위 빨간 조준. 크롭(3배): Projects/AVSR/_exchange/fx_skill1030/anchor_msl_lock.png — 표적을 둘러싼 빨간 원 + 원 바깥에서 가운데를 가리키는 빨간 세모 셋(위 · 왼쪽 아래 · 오른쪽 아래), 원 안쪽 짧은 눈금.
[그릴 것] 조준 표시만(사람 · 폭발 빼고). 정면에서 본 원(살짝 납작해도 됨), 빨강 + 밝은 주홍 테두리 빛, 도트 선 깔끔.
[프레임] 4프레임 가로로 나란히 — 세모 셋이 바깥에서 안으로 살짝 조여 들고 원이 숨쉬듯 밝아졌다 어두워진다(반복). 프레임 사이 검은 빈칸 넉넉히.
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다.
[크기] 캔버스 1152 × 288. 한 프레임 약 256 × 256 칸 안에(게임에서 약 128 × 128 로 줄여 쓴다).
글자 · 숫자 · 사람 · 총 없음.
[저장] Projects/AVSR/_exchange/fx_skill1030/sheet_msl_lock.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
