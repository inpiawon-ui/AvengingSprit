$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 스킬.
필살기 「강신 의식」 — 시전 순간 하늘에서 영매에게 내려꽂히는 빛기둥(한 번).
[정답 그림] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_guide.png 의 화면 위에서 영매에게 떨어지는 보라 · 흰 빛기둥. 크롭(3배): Projects/AVSR/_exchange/fx_medium1030/anchor_rit_pillar.png
[그릴 것] 세로로 긴 빛기둥 — 가운데 흰 심지, 둘레 보라, 기둥을 감는 혼불 소용돌이.
[프레임] 5프레임 가로로 — 위에서 내려꽂힘(1~2) → 가장 굵고 밝음(3) → 가늘어지며 사라짐(4~5).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1200 × 1024. 한 프레임 약 200 × 960 칸 안에(게임에서 약 110 × 520).
[저장] Projects/AVSR/_exchange/fx_medium1030/sheet_rit_pillar.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
