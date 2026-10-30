$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 필살기 「강신 의식」.
[정답 시안] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_free1.png (PD 가 고른 보라 화풍 — 이 색 · 도트 질감 그대로).
시전 순간 의식 진에서 피어올라 위로 흩어지는 유령 혼(여러 개를 흩뿌려 쓴다).
[정답 크롭(3배)] Projects/AVSR/_exchange/fx_medium1030_r1/anchor_r1_ghost.png — 보라 연기로 된 해골 얼굴 유령, 꼬리가 연기로 늘어짐, 반투명.
[그릴 것] 유령 혼 하나(위로 떠오르는 방향).
[프레임] 5프레임 가로로 — 아래에서 생겨나 떠오르며 커졌다가(1~3) 연기로 흩어져 사라짐(4~5).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1600 × 320. 한 프레임 약 200 × 280 칸 안에(게임에서 약 70 × 98).
[저장] Projects/AVSR/_exchange/fx_medium1030_r1/sheet_r1_ghost.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
