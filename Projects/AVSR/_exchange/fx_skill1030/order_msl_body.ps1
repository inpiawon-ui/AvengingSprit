$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(색의 뜻 — 내 공격 = 주황 · 금).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주.
코만도(미사일) 필살기 「집중 포격」의 미사일 한 발.
[정답 그림] 통과 시안 Projects/AVSR/_exchange/mock1030_skill/mock_missile_free1.png 의 미사일. 크롭(3배): Projects/AVSR/_exchange/fx_skill1030/anchor_msl_body.png · anchor_msl_body2.png — 하얀 · 은빛 몸통, 빨간 탄두, 꼬리 날개, 꽁무니에서 노랑 · 주황 불꽃이 뿜어진다.
[그릴 것] 미사일 한 발 — **오른쪽을 향한 가로 그림**(탄두가 오른쪽, 불꽃 꼬리가 왼쪽). 몸통 : 불꽃 = 약 3 : 2. 연기는 그리지 않는다(게임이 따로 낸다). 도트 가장자리 깔끔.
[프레임] 3프레임 가로로 나란히 — 몸통은 같고 꽁무니 불꽃 길이 · 밝기만 살짝 깜빡인다. 프레임 사이 빈칸 넉넉히.
[바탕] 순수한 분홍(#FF00FF) 단색 — 단단한 물체라 게임이 분홍을 뚫어 투명으로 쓴다. 분홍과 비슷한 색을 그림에 쓰지 않는다.
[크기] 캔버스 1152 × 160. 한 프레임 약 288 × 96 칸 안에(게임에서 약 96 × 32 로 줄여 쓴다).
글자 · 숫자 · 사람 · 총 없음.
[저장] Projects/AVSR/_exchange/fx_skill1030/sheet_msl_body.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
