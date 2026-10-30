$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(색의 뜻 — 내 공격 = 주황 · 금).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주.
코만도(기관총) 액티브 「제압 사격」에 맞아 느려진 적의 발밑 표시 — 하얀 소용돌이 고리(둔화).
[정답 그림] 통과 시안 Projects/AVSR/_exchange/mock1030_skill/mock_suppress_free2.png 의 맞은 적 발밑. 크롭(3배): Projects/AVSR/_exchange/fx_skill1030/anchor_sup_swirl.png · anchor_sup_swirl2.png — 적 발밑을 납작한 타원으로 감도는 하얀 · 연회색 소용돌이 선 두세 가닥(바람이 맴도는 느낌).
[그릴 것] 발밑 소용돌이 고리만(적 · 회색 먼지 구름 빼고 — 먼지는 게임이 따로 낸다). 위에서 비스듬히 본 납작한 타원(가로 : 세로 = 약 5 : 2), 끊어진 곡선 두세 가닥이 한 방향으로 돈다. 흰색 + 연한 회청색, 도트 선, 깔끔하게.
[프레임] 4프레임 가로로 나란히 — 가닥이 조금씩 돌아가 이어 붙이면 계속 맴돈다(반복). 프레임 사이 검은 빈칸 넉넉히.
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다.
[크기] 캔버스 1152 × 192. 한 프레임 약 240 × 96 칸 안에(게임에서 약 120 × 48 로 줄여 쓴다).
글자 · 숫자 · 사람 · 총 없음.
[저장] Projects/AVSR/_exchange/fx_skill1030/sheet_sup_swirl.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
