$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 스킬.
필살기 「강신 의식」 — 영매에게서 보조 호스트에게 이어지는 빛줄기(버프가 건너감).
[정답 그림] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_guide.png 의 영매에서 좌우 보조에게 휘어 가는 금빛-보라 빛줄기. 크롭(3배): Projects/AVSR/_exchange/fx_medium1030/anchor_rit_link.png
[그릴 것] 왼쪽에서 오른쪽으로 곧게 가로놓인 빛줄기 한 가닥(게임이 돌려서 늘려 쓴다) — 가운데 금빛 심지, 둘레 보라, 살짝 일렁임.
[프레임] 3프레임 세로로 쌓아 — 빛줄기가 흐르듯 일렁인다(반복).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1024 × 384. 한 프레임 약 960 × 96 칸 안에(게임에서 약 240 × 28 로 늘려 쓴다).
[저장] Projects/AVSR/_exchange/fx_medium1030/sheet_rit_link.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
