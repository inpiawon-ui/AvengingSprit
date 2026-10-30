$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(색의 뜻 — 내 공격 = 주황 · 금).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주.
코만도(미사일) 필살기 「집중 포격」의 미사일이 표적에 꽂혀 터지는 큰 폭발.
[정답 그림] 통과 시안 Projects/AVSR/_exchange/mock1030_skill/mock_missile_free1.png 의 표적 자리 폭발. 크롭(3배): Projects/AVSR/_exchange/fx_skill1030/anchor_msl_blast.png — 하얀 · 노란 심지에서 주황 · 빨강 불덩이가 둥글게 터지고, 바깥으로 불티가 튄다.
[그릴 것] 폭발만(사람 · 미사일 빼고). 둥근 불덩이 폭발, 도트 질감, 가장자리 깔끔.
[프레임] 5프레임 가로로 나란히 — 작은 섬광 → 크게 터짐(가장 밝음) → 불덩이 → 흩어짐 → 연기 끝 사라짐. 프레임 사이 검은 빈칸 넉넉히.
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다.
[크기] 캔버스 1440 × 288. 한 프레임 약 256 × 256 칸 안에(게임에서 약 192 × 192 로 그린다).
글자 · 숫자 · 사람 · 총 없음.
[저장] Projects/AVSR/_exchange/fx_skill1030/sheet_msl_blast.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
