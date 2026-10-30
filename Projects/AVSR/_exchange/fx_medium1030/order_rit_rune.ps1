$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 스킬.
필살기 「강신 의식」 — 시전 순간 영매 발밑에 펼쳐지는 큰 의식 진.
[정답 그림] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_guide.png 의 발밑 붉은 금빛 문양 진(바깥 보라 테). 크롭(3배): Projects/AVSR/_exchange/fx_medium1030/anchor_rit_rune.png
[그릴 것] 진만(인물 빼고). 위에서 비스듬히 본 납작한 타원. 안쪽 붉은 · 금빛 마법 문양(삼각 · 원), 바깥 보라 테.
[프레임] 5프레임 가로로 — 가운데서 펼쳐지며 밝아짐(1~3) → 가득 빛남(4) → 빛이 가라앉음(5). 5는 35초 동안 은은히 깔려 있을 상태.
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 2400 × 320. 한 프레임 약 460 × 200 칸 안에(게임에서 약 460 × 200).
[저장] Projects/AVSR/_exchange/fx_medium1030/sheet_rit_rune.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
