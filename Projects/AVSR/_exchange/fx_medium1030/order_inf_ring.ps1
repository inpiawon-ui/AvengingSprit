$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 스킬.
액티브 「혼 깃들이기」 — 버프가 걸린 몸(메인 · 보조 모두) 발밑에 8초 동안 깔리는 보라 고리.
[정답 그림] Projects/AVSR/_exchange/mock1030_skill/mock_infuse_free1.png 의 각 인물 발밑 보라 고리. 크롭(3배): Projects/AVSR/_exchange/fx_medium1030/anchor_inf_ring.png — 위에서 비스듬히 본 납작한 타원 고리, 보라 · 밝은 자주 빛, 고리 위 작은 반짝 별.
[그릴 것] 고리만(인물 빼고). 납작한 타원(가로:세로 약 5:2).
[프레임] 4프레임 가로로 — 고리가 숨쉬듯 밝아졌다 어두워지고 반짝 별이 고리를 따라 천천히 돈다(반복 루프).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1152 × 256. 한 프레임 약 256 × 110 칸 안에(게임에서 약 130 × 52 로 줄여 쓴다).
[저장] Projects/AVSR/_exchange/fx_medium1030/sheet_inf_ring.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
