$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 필살기 「강신 의식」.
[정답 시안] Projects/AVSR/_exchange/mock1030_skill/mock_ritual_free1.png (PD 가 고른 보라 화풍 — 이 색 · 도트 질감 그대로).
시전 순간 영매 발밑에 펼쳐져 35초 동안 깔리는 큰 보라 의식 진.
[정답 크롭(3배)] Projects/AVSR/_exchange/fx_medium1030_r1/anchor_r1_rune.png — 위에서 비스듬히 본 납작한 큰 타원 진, 보라 · 흰 빛 테두리 여러 겹, 테두리 바깥으로 뻗는 화살촉 눈금, 안쪽 룬 글자 무늬(읽히는 글자 아님).
[그릴 것] 진만(인물 빼고). 납작한 타원(가로:세로 약 5:2).
[프레임] 5프레임 가로로 — 가운데서 펼쳐지며 밝아짐(1~3) → 가득 빛남(4) → 은은히 가라앉음(5). 5는 35초 동안 깔려 있을 상태.
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 2400 × 320. 한 프레임 약 460 × 190 칸 안에(게임에서 약 460 × 190).
[저장] Projects/AVSR/_exchange/fx_medium1030_r1/sheet_r1_rune.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
