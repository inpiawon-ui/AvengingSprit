$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 필살기 「강신 의식」.
[정답 시안] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_free1.png (PD 가 고른 보라 화풍 — 이 색 · 도트 질감 그대로).
시전 순간 둘레 적이 2초 둔화될 때 적 발밑을 감는 보라 사슬 고리.
[정답 크롭(3배)] Projects/AVSR/_exchange/fx_medium1030_r1/anchor_r1_chain.png — 적 발밑을 둥글게 감은 보라 빛 사슬(고리 마디가 보이는 사슬), 아래 얇은 보라 원.
[그릴 것] 사슬 고리만(적 빼고). 위에서 비스듬히 본 납작한 타원 고리 모양으로 감긴 사슬.
[프레임] 4프레임 가로로 — 사슬이 조여 감기며 빛이 흐른다(반복 루프).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1152 × 256. 한 프레임 약 256 × 120 칸 안에(게임에서 약 96 × 45).
[저장] Projects/AVSR/_exchange/fx_medium1030_r1/sheet_r1_chain.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
